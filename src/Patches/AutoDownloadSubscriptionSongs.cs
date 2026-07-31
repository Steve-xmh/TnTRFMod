using System.Net;
using HarmonyLib;
using TnTRFMod.Config;
using TnTRFMod.Ui;
using TnTRFMod.Utils;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;
using Logger = TnTRFMod.Utils.Logger;

#if BEPINEX
using Cysharp.Threading.Tasks;
using Scripts.OutGame.Common;
using Scripts.OutGame.SongSelect;

#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
using Il2CppScripts.OutGame.Common;
using Il2CppScripts.OutGame.SongSelect;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class AutoDownloadSubscriptionSongs
{
    private const int DownloadUrlBatchSize = 50;
    private const int MaxConcurrentDownloads = 16;
    private static readonly TimeSpan ConsoleProgressInterval = TimeSpan.FromSeconds(1);
    private static readonly HttpClient DownloadClient = CreateDownloadClient();
    private static AutoDownloadState autoDownloadState;
    private static bool invokedGameDownload;

    public static async Task StartAutoDownloadSubscriptionSongsAsync()
    {
        if (!ModConfig.EnableAutoDownloadSubscriptionSongs.Value) return;
        if (autoDownloadState is AutoDownloadState.Downloading or AutoDownloadState.Completed) return;

        autoDownloadState = AutoDownloadState.Downloading;
        using var logText = LoggingScreenUi.NewThreadSafe(I18n.Get("autoDownloadSub.stepOne").Text);
        Logger.Info("Download cache directory: " + PackedSongUtility.LocalStragePath);

        try
        {
            var res = CheckResponse(await UTask.RunOnIl2Cpp(SubscriptionUtility.DownloadSubscriptionAvaliable));

            Logger.Info(
                $"Subscription Status:       {res.result}, {res.responseCode}, {res.errorText}");

            if (res.responseBody == null)
                throw new NetworkIssueException(res.result,
                    $"response body is empty when checking subscription");

            Logger.Info(
                $"Subscription responseBody: {res.responseBody.subscription}, {res.responseBody.expiration_datetime}");

            var curTime = DateTime.Now;
            var expirationTime = DateTimeOffset.FromUnixTimeMilliseconds(res.responseBody.expiration_datetime).DateTime;
            Logger.Info($"Subscription Current Time: {curTime}, Expiration Time: {expirationTime}");

            if (curTime >= expirationTime)
            {
                Logger.Warn("Subscription is not valid now, skip downloading songs");
                logText.Text = I18n.Get("autoDownloadSub.notValid").Text;
            }
            else
            {
                logText.Text = I18n.Get("autoDownloadSub.stepTwo").Text;

                Logger.Info("Subscription is still valid, start downloading songs");

                var time = DateTime.Now;

                _ = CheckResponse(await UTask.RunOnIl2Cpp(() => SubscriptionUtility.DownloadSongListDetails(true)));
                await DownloadRequiredSongDataDetailsAsync();
                await UTask.RunOnIl2CppThreadPool(PackedSongUtility.DeleteOldPreviewFiles);
                await UTask.RunOnIl2CppThreadPool(PackedSongUtility.DeleteOldSongFiles);

                var allSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                {
                    CommonObjects.instance.ServerDataCache.RemoveDisabledSongs();
                    var uids = (int[])CommonObjects.instance.ServerDataCache.GetAllSongUniqueIdsFromSongList();
                    if (uids == null) return [];
                    uids = uids.Where(uid =>
                        CommonObjects.instance.ServerDataCache.IsAvailableSong(
                            CommonObjects.instance.MyDataManager.MusicData.GetInfoByUniqueId(uid))).ToArray();
                    return uids;
                });
                var previewFileSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                {
                    return allSongUids.Where(uid =>
                        !PackedSongUtility.CheckPreviewFileExists(uid)).ToArray();
                });
                var dlcSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                    allSongUids.Where(uid =>
                        SongSelectUtility.IsSongCachedActiveDlc(CommonObjects.instance.MyDataManager.MusicData
                            .GetInfoByUniqueId(uid))).ToArray()
                );
                var subSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                    allSongUids.Where(uid =>
                        SongSelectUtility.IsSongSubscription(CommonObjects.instance.MyDataManager.MusicData
                            .GetInfoByUniqueId(uid))).ToArray()
                );
                var dlcSongFileSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                {
                    return dlcSongUids.Where(uid => !PackedSongUtility.CheckSongFileExists(uid)).ToArray();
                });
                var subSongFileSongUids = await UTask.RunOnIl2CppThreadPool(() =>
                {
                    return subSongUids.Where(uid => !PackedSongUtility.CheckSongFileExists(uid)).ToArray();
                });

                Logger.Info($"Fetched {previewFileSongUids.Length} preview songs to update");
                Logger.Info($"Fetched {subSongFileSongUids.Length} subscription songs to update");
                Logger.Info($"Fetched {dlcSongFileSongUids.Length} dlc songs to update");
                Logger.Info($"Summerize songs took: {(DateTime.Now - time).TotalMilliseconds} ms");

                await PrepareDownloadGatewayAsync();

                if (previewFileSongUids.Length > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepThree", previewFileSongUids.Length);
                    logText.Text = progressText.Text;
                    Logger.Info($"Start downloading {previewFileSongUids.Length} song previews");

                    var requests = previewFileSongUids.Select(uid => new DownloadRequest(uid, 2, "")).ToArray();
                    await DownloadFilesAsync(requests, false, progress =>
                    {
                        var prog = (progress * 100).ToString("F1");
                        logText.Text = $"{progressText.Text} ({prog}%)";
                    });

                    var missingPreviews = await UTask.RunOnIl2CppThreadPool(() =>
                        previewFileSongUids.Where(uid => !PackedSongUtility.CheckPreviewFileExists(uid)).ToArray());
                    if (missingPreviews.Length > 0)
                    {
                        Logger.Error(
                            $"Preview verification failed: {missingPreviews.Length}/{previewFileSongUids.Length} " +
                            $"files are still missing; sample UIDs={string.Join(",", missingPreviews.Take(20))}");
                        throw new InvalidDataException(
                            $"{missingPreviews.Length} preview files are still missing after download");
                    }

                    Logger.Info($"Preview verification completed: all {previewFileSongUids.Length} files are visible " +
                                "to PackedSongUtility.CheckPreviewFileExists");
                }

                var songRequests = subSongFileSongUids.Select(uid => new DownloadRequest(uid, 1, "")).ToList();
                if (dlcSongFileSongUids.Length > 0)
                {
                    Logger.Info($"Creating release keys for {dlcSongFileSongUids.Length} dlc songs");
                    var releaseKeys = await Task.WhenAll(dlcSongFileSongUids.Select(async uid =>
                        new DownloadRequest(uid, 1, await UTask.RunOnIl2Cpp(() =>
                            CommonObjects.instance.Platform.DlcDataCache.CreateSubscriptionKeyAsync(uid)))));
                    songRequests.AddRange(releaseKeys);
                }

                if (songRequests.Count > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepFour", songRequests.Count);
                    logText.Text = progressText.Text;
                    Logger.Info($"Start downloading {songRequests.Count} song files");

                    await DownloadFilesAsync(songRequests, true, progress =>
                    {
                        var prog = (progress * 100).ToString("F1");
                        logText.Text = $"{progressText.Text} ({prog}%)";
                    });
                }
            }

            autoDownloadState = AutoDownloadState.Completed;
            Logger.Info("Finished download song files; automatic subscription download state is now completed");

            logText.Text = I18n.Get("autoDownloadSub.finished").Text;
        }
        catch (NetworkIssueException ex)
        {
            autoDownloadState = AutoDownloadState.Failed;
            Logger.Error("AutoDownloadSubscriptionSongs failed; game download fallback has been enabled: " + ex);
            logText.Text = I18n.Get("autoDownloadSub.networkIssue", ex.Message).Text;
        }
        catch (Exception ex)
        {
            autoDownloadState = AutoDownloadState.Failed;
            logText.Text = I18n.Get("autoDownloadSub.otherError", ex.ToString()).Text;
            Logger.Error("AutoDownloadSubscriptionSongs failed; game download fallback has been enabled: " + ex);
        }

        logText.Text += I18n.Get("autoDownloadSub.hideTip").Text;
        await Task.Delay(5000);
        TaikoSingletonMonoBehaviour<Connecting>.Instance.Deactive();
    }

    private static async Task DownloadRequiredSongDataDetailsAsync()
    {
        var uniqueIds = await UTask.RunOnIl2Cpp(() =>
            (int[])CommonObjects.instance.ServerDataCache.GetUpdateRequredSongdataUniqueIds());
        if (uniqueIds == null || uniqueIds.Length == 0)
        {
            Logger.Info("All song data details are already up to date");
            return;
        }

        Logger.Info($"Updating song data details for {uniqueIds.Length} songs");
        _ = CheckResponse(await UTask.RunOnIl2Cpp(() =>
            SubscriptionUtility.DownloadSongDataDetails(uniqueIds)));
    }

    private static async Task PrepareDownloadGatewayAsync()
    {
        var gateway = CommonObjects.instance.SubscriptionGateway;
        Logger.Info($"Preparing download gateway: common key update required={gateway.IsCommonKeyUpdateRequired}");
        if (!await UTask.RunOnIl2Cpp(() => gateway.UpdateIdTokenIfRequired()))
            throw new NetworkIssueException(-1, "failed to update the id token");
        Logger.Info("Download gateway ID token is ready");

        if (gateway.IsCommonKeyUpdateRequired)
        {
            Logger.Info("Registering a new subscription common key");
            _ = CheckResponse(await UTask.RunOnIl2Cpp(SubscriptionUtility.RegisterCommonkeyToServer));
            Logger.Info(
                $"Subscription common key registration completed: still required={gateway.IsCommonKeyUpdateRequired}");
        }

        var storeId = await UTask.RunOnIl2Cpp(SubscriptionUtility.UpdateUserStoreId);
        Logger.Info($"Download gateway store ID update completed: available={!string.IsNullOrEmpty(storeId)}");
    }

    private static async Task DownloadFilesAsync(
        IReadOnlyList<DownloadRequest> requests,
        bool addToDownloadedSongs,
        Action<float> onProgress)
    {
        if (requests.Count == 0) return;

        var startedAt = DateTime.UtcNow;
        var fileType = requests[0].Type == 1 ? "song" : "preview";
        var batchCount = (requests.Count + DownloadUrlBatchSize - 1) / DownloadUrlBatchSize;
        Logger.Info($"Preparing {requests.Count} {fileType} downloads in {batchCount} URL batches");

        var songNames = await UTask.RunOnIl2Cpp(() => requests
            .Select(request => request.SongUid)
            .Distinct()
            .ToDictionary(uid => uid, GetSongDisplayName));
        _ = Directory.CreateDirectory(PackedSongUtility.LocalStragePath);

        var urls = new List<DownloadUrl>(requests.Count);
        var downloadTasks = new List<Task>(requests.Count);
        var completed = 0;
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        Logger.Info($"Starting pipelined {fileType} downloads with concurrency={MaxConcurrentDownloads}");

        try
        {
            for (var offset = 0; offset < requests.Count; offset += DownloadUrlBatchSize)
            {
                var count = Math.Min(DownloadUrlBatchSize, requests.Count - offset);
                var batchNumber = offset / DownloadUrlBatchSize + 1;
                Logger.Info($"Requesting {fileType} download URLs: batch {batchNumber}/{batchCount}, " +
                            $"items {offset + 1}-{offset + count}");
                var batchOffset = offset;
                var response = CheckResponse(await UTask.RunOnIl2Cpp(() =>
                {
                    var requestData = new SubscriptionGateway.RequestDataSongdataDownloadUrlObject[count];
                    for (var i = 0; i < count; i++)
                    {
                        var request = requests[batchOffset + i];
                        requestData[i] = new SubscriptionGateway.RequestDataSongdataDownloadUrlObject
                        {
                            song_uid = request.SongUid,
                            type = request.Type,
                            individual_release_key = request.ReleaseKey
                        };
                    }

                    return CommonObjects.instance.SubscriptionGateway.PostSongdataDownloadUrlAPI(
                        requestData,
                        CancellationToken.None,
                        null,
                        SubscriptionGateway.UrlType.Default,
                        SubscriptionGateway.PlatformType.Default);
                }));
                if (response.result != 1 || response.responseCode != (int)HttpStatusCode.OK ||
                    response.responseBody == null)
                    throw new NetworkIssueException(response.result,
                        response.errorText ?? "failed to request song download URLs");

                if (response.responseBody.ary_dl_url == null)
                    throw new NetworkIssueException(response.result, "song download URL list is empty");

                Logger.Info($"Received {response.responseBody.ary_dl_url.Length} {fileType} download URLs " +
                            $"for batch {batchNumber}/{batchCount}");
                foreach (var item in response.responseBody.ary_dl_url)
                {
                    if (item == null)
                        throw new NetworkIssueException(-1, "server returned an empty song download URL entry");
                    if (item.sub_result != 1 || string.IsNullOrEmpty(item.dl_url))
                        throw new NetworkIssueException(item.sub_result,
                            $"server did not return a download URL for song {item.song_uid}");

                    var download = new DownloadUrl(item.song_uid, item.type, item.dl_url);
                    urls.Add(download);
                    downloadTasks.Add(DownloadAndReportProgressAsync(download));
                }

                Logger.Info($"Queued batch {batchNumber}/{batchCount} for immediate download; " +
                            $"queued={downloadTasks.Count}/{requests.Count}, completed={Volatile.Read(ref completed)}");
            }
        }
        catch
        {
            Logger.Warn(
                $"URL pipeline failed after queuing {downloadTasks.Count}/{requests.Count} {fileType} downloads; " +
                "waiting for already-started downloads");
            await AwaitStartedDownloadsIgnoringErrorsAsync(downloadTasks);
            throw;
        }

        if (urls.Count != requests.Count)
        {
            await AwaitStartedDownloadsIgnoringErrorsAsync(downloadTasks);
            throw new NetworkIssueException(-1,
                $"requested {requests.Count} files but the server returned {urls.Count} URLs");
        }

        Logger.Info($"All URL batches submitted: requested={requests.Count}, queued={downloadTasks.Count}, " +
                    $"alreadyCompleted={Volatile.Read(ref completed)}");
        await Task.WhenAll(downloadTasks);
        Logger.Info($"Downloaded {urls.Count} files in {(DateTime.UtcNow - startedAt).TotalSeconds:F1} seconds");

        async Task DownloadAndReportProgressAsync(DownloadUrl download)
        {
            await semaphore.WaitAsync();
            try
            {
                await DownloadFileAsync(download, songNames[download.SongUid]);
                var current = Interlocked.Increment(ref completed);
                var progress = (float)current / requests.Count;
                onProgress(progress);
                Logger.Info($"Completed {fileType} downloads: {current}/{requests.Count} ({progress * 100:F1}%)");
            }
            finally
            {
                _ = semaphore.Release();
            }
        }

        if (addToDownloadedSongs)
            await UTask.RunOnIl2Cpp(() =>
            {
                foreach (var uid in urls.Select(url => url.SongUid).Distinct())
                    CommonObjects.instance.MusicData.AddDownloadedSong(uid);
                CommonObjects.instance.SaveData.Save();
            });
    }

    private static async Task AwaitStartedDownloadsIgnoringErrorsAsync(IReadOnlyCollection<Task> downloadTasks)
    {
        if (downloadTasks.Count == 0) return;

        try
        {
            await Task.WhenAll(downloadTasks);
        }
        catch (Exception ex)
        {
            Logger.Warn($"One or more already-started downloads also failed while draining the pipeline: {ex.Message}");
        }
    }

    private static async Task DownloadFileAsync(DownloadUrl download, string songName)
    {
        var startedAt = DateTime.UtcNow;
        var fileType = download.Type == 1 ? "song" : "preview";
        var initialProgress = I18n.Get("autoDownloadSub.downloading", songName, FormatMiB(0),
            I18n.Get("autoDownloadSub.unknownSize").Text).Text;
        await using var logHandle = LoggingScreenUi.NewAsync();
        await logHandle.SetTextAsync(initialProgress);
        Logger.Info($"{initialProgress}; uid={download.SongUid}, type={fileType}");

        try
        {
            using var response = await DownloadClient.GetAsync(download.Url, HttpCompletionOption.ResponseHeadersRead);
            Logger.Info($"Received HTTP headers for song {download.SongUid} ({songName}): " +
                        $"status={(int)response.StatusCode} {response.ReasonPhrase}, " +
                        $"contentLength={response.Content.Headers.ContentLength?.ToString() ?? "unknown"}");
            _ = response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(Uri.UnescapeDataString(new Uri(download.Url).AbsolutePath));
            var expectedPrefix = download.Type == 1
                ? $"{download.SongUid:D4}_trail_"
                : $"{download.SongUid:D4}_trail_stream_";
            var versionText = fileName.Length == expectedPrefix.Length + 7
                ? fileName.Substring(expectedPrefix.Length, 3)
                : "";
            if (!fileName.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
                !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                versionText.Length != 3 || !versionText.All(c => c is >= '0' and <= '9') ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException($"Unexpected song file name '{fileName}' for song {download.SongUid}");

            var destinationPath = Path.Combine(PackedSongUtility.LocalStragePath, fileName);
            var temporaryPath = destinationPath + ".part";
            Logger.Info($"Writing song download: uid={download.SongUid}, name={songName}, file={fileName}");
            try
            {
                var totalBytes = response.Content.Headers.ContentLength;
                long downloadedBytes = 0;
                await using (var source = await response.Content.ReadAsStreamAsync())
                await using (var destination = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write,
                                 FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    var buffer = new byte[128 * 1024];
                    var nextUiProgressUpdate = DateTime.UtcNow;
                    var nextConsoleProgressUpdate = DateTime.UtcNow;
                    while (true)
                    {
                        var bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        await destination.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;
                        var now = DateTime.UtcNow;
                        var progressText = FormatDownloadProgress(songName, downloadedBytes, totalBytes);
                        if (now >= nextUiProgressUpdate)
                        {
                            await logHandle.SetTextAsync(progressText);
                            nextUiProgressUpdate = now.AddMilliseconds(100);
                        }

                        if (now >= nextConsoleProgressUpdate)
                        {
                            Logger.Info($"{progressText}; uid={download.SongUid}, type={fileType}");
                            nextConsoleProgressUpdate = now.Add(ConsoleProgressInterval);
                        }
                    }

                    await destination.FlushAsync();
                }

                var finalProgress = FormatDownloadProgress(songName, downloadedBytes, totalBytes);
                await logHandle.SetTextAsync(finalProgress);
                File.Move(temporaryPath, destinationPath, true);
                var elapsed = DateTime.UtcNow - startedAt;
                var speed = elapsed.TotalSeconds > 0 ? downloadedBytes / elapsed.TotalSeconds : 0;
                Logger.Info($"Finished downloading song {download.SongUid} ({songName}): file={fileName}, " +
                            $"size={FormatMiB(downloadedBytes)}, elapsed={elapsed.TotalSeconds:F1}s, " +
                            $"averageSpeed={FormatMiB((long)speed)}/s");
            }
            catch
            {
                File.Delete(temporaryPath);
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed downloading song {download.SongUid} ({songName}), type={fileType}: {ex}");
            throw;
        }
    }

    private static string GetSongDisplayName(int songUid)
    {
        var song = CommonObjects.instance.MyDataManager.MusicData.GetInfoByUniqueId(songUid);
        if (song == null) return songUid.ToString();

        var names = song.SongNames;
        var languageIndex = (int)I18n.CurrentLanguage;
        var name = languageIndex >= 0 && languageIndex < names.Length
            ? names[languageIndex]
            : names[0];
        var tagEnd = name.IndexOf('>');
        return tagEnd >= 0 ? name[(tagEnd + 1)..] : name;
    }

    private static string FormatDownloadProgress(string songName, long downloadedBytes, long? totalBytes)
    {
        var downloaded = FormatMiB(downloadedBytes);
        var total = totalBytes.HasValue
            ? FormatMiB(totalBytes.Value)
            : I18n.Get("autoDownloadSub.unknownSize").Text;
        return I18n.Get("autoDownloadSub.downloading", songName, downloaded, total).Text;
    }

    private static string FormatMiB(long bytes)
    {
        return $"{bytes / 1048576d:0.#}MiB";
    }

    private static HttpClient CreateDownloadClient()
    {
        return new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            MaxConnectionsPerServer = MaxConcurrentDownloads,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        })
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    private static T CheckResponse<T>(T result)
        where T : SubscriptionGateway.ResponseDataBase
    {
        var errorText = result.errorText;
        if (errorText != null || result.isNetworkError || result.isCanceled || result.isTimeout)
            throw new NetworkIssueException(result.responseCode, errorText ?? "unknown error");

        return result;
    }


    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SongSelectSceneUiControllerBase))]
    [HarmonyPatch(nameof(SongSelectSceneUiControllerBase.LoadSubscriptionAsync))]
    [HarmonyPrefix]
    private static bool SongSelectSceneUiControllerBase_LoadSubscriptionAsync_Prefix(ref UniTask __result)
    {
        if (!ModConfig.EnableAutoDownloadSubscriptionSongs.Value) return true;

        if (autoDownloadState == AutoDownloadState.Failed)
        {
            Logger.Warn("Automatic subscription download failed; use the game's LoadSubscriptionAsync fallback");
            return true;
        }

        if (autoDownloadState == AutoDownloadState.NotStarted && !invokedGameDownload)
        {
            invokedGameDownload = true;
            Logger.Info("Automatic subscription download has not started; allow one game download pass");
            return true;
        }

        Logger.Info($"Skip the game's duplicate LoadSubscriptionAsync; automatic state={autoDownloadState}");
        __result = UniTask.CompletedTask;
        return false;
    }

    private enum AutoDownloadState
    {
        NotStarted,
        Downloading,
        Completed,
        Failed
    }

    private readonly record struct DownloadRequest(int SongUid, int Type, string ReleaseKey);

    private readonly record struct DownloadUrl(int SongUid, int Type, string Url);

    private class NetworkIssueException(int result, string errorText) : Exception
    {
        public override string Message => $"{result}: {errorText}";
    }
}