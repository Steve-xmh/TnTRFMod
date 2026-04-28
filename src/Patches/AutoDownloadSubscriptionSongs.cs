using HarmonyLib;
using Il2CppInterop.Runtime;
using TnTRFMod.Config;
using TnTRFMod.Ui;
using TnTRFMod.Utils;
using UnityEngine.Events;
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
    private static bool invokedDownload;

    public static async Task StartAutoDownloadSubscriptionSongsAsync()
    {
        if (!ModConfig.EnableAutoDownloadSubscriptionSongs.Value) return;
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

                await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadSongListDetails(true));
                await UTask.RunOnIl2CppThreadPool(SubscriptionUtility.DownloadSongDataDetailsRequired);
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

                if (previewFileSongUids.Length > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepThree", previewFileSongUids.Length);
                    logText.Text = progressText.Text;

                    Logger.Info($"Start downloading {previewFileSongUids.Length} song previews");
                    CheckResponse(await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadPreviewFiles(
                        previewFileSongUids, CancellationToken.None,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading song previews: {prog}%");
                                logText.Text = $"{progressText.Text} ({prog}%)";
                            }
                        ))));
                }

                if (subSongFileSongUids.Length > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepFour", subSongFileSongUids.Length);
                    logText.Text = progressText.Text;

                    Logger.Info($"Start downloading {subSongFileSongUids.Length} song files");
                    CheckResponse(await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadSongFilesAsync(
                        subSongFileSongUids, CancellationToken.None,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading song files: {prog}%");
                                logText.Text = $"{progressText.Text} ({prog}%)";
                            }
                        ))));
                }

                Logger.Info($"Start downloading {dlcSongFileSongUids.Length} dlc song files");
                var i = 1;
                foreach (var uid in dlcSongFileSongUids)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepFive", i, dlcSongFileSongUids.Length);
                    logText.Text = progressText.Text;

                    Logger.Info($"Start downloading dlc song {uid}");
                    CheckResponse(await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadSongFile(
                        uid, CancellationToken.None,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading dlc song files: {prog}%");
                                logText.Text = $"{progressText.Text} ({prog}%)";
                            }
                        ), true)));

                    i += 1;
                }
            }

            Logger.Info("Finished download song files!");

            logText.Text = I18n.Get("autoDownloadSub.finished").Text;
        }
        catch (NetworkIssueException ex)
        {
            Logger.Error("AutoDownloadSubscriptionSongs failed: " + ex);
            logText.Text = I18n.Get("autoDownloadSub.networkIssue", ex.Message).Text;
        }
        catch (Exception ex)
        {
            logText.Text = I18n.Get("autoDownloadSub.otherError", ex.ToString()).Text;
            Logger.Error(ex.ToString());
        }

        logText.Text += I18n.Get("autoDownloadSub.hideTip").Text;
        await Task.Delay(5000);
        TaikoSingletonMonoBehaviour<Connecting>.Instance.Deactive();
    }

    private static string GetPackedSongStreamFileName(int songUid, int version)
    {
        return $"{songUid:D4}_trail_stream_{version:D3}.zip";
    }

    private static string GetPackedPreviewSongStreamFileName(int songUid, int version)
    {
        return $"{songUid:D4}_trail_{version:D3}.zip";
    }

    private static T CheckResponse<T>(T result)
        where T : SubscriptionGateway.ResponseDataBase
    {
        var errorText = result.errorText;
        if (errorText != null || result.isNetworkError || result.isCanceled || result.isTimeout)
            throw new NetworkIssueException(result.responseCode, errorText ?? "unknown error");

        return result;
    }

    // 配置下载线程数量
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SubscriptionUtility.__c__DisplayClass29_0))]
    [HarmonyPatch(nameof(SubscriptionUtility.__c__DisplayClass29_0._DownloadPreviewsInternal_b__2))]
    [HarmonyPrefix]
    private static bool SongSelectSceneUiControllerBase_LoadSubscriptionAsync_Prefix(
        SubscriptionUtility.__c__DisplayClass29_0 __instance, ref bool __result)
    {
        if (!ModConfig.EnableAutoDownloadSubscriptionSongs.Value) return true;

        __result = __instance.runningCount <= 32;

        return false;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SongSelectSceneUiControllerBase))]
    [HarmonyPatch(nameof(SongSelectSceneUiControllerBase.LoadSubscriptionAsync))]
    [HarmonyPrefix]
    private static bool SongSelectSceneUiControllerBase_LoadSubscriptionAsync_Prefix(ref UniTask __result)
    {
        if (!ModConfig.EnableAutoDownloadSubscriptionSongs.Value) return true;
        // 至少执行一次订阅检查以免有疏漏，后续不在
        if (!invokedDownload)
        {
            invokedDownload = true;
            return true;
        }

        Logger.Warn("Skip downloading songs");
        __result = UniTask.CompletedTask;
        return false;
    }

    private class NetworkIssueException(int result, string errorText) : Exception
    {
        public override string Message => $"{result}: {errorText}";
    }
}