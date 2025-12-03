using Cysharp.Threading.Tasks;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Scripts.OutGame.Common;
using Scripts.OutGame.SongSelect;
using TnTRFMod.Ui;
using TnTRFMod.Utils;
using UnityEngine.Events;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class AutoDownloadSubscriptionSongs
{
    private static bool invokedDownload;

    public static async Task StartAutoDownloadSubscriptionSongsAsync()
    {
        if (!TnTrfMod.Instance.enableAutoDownloadSubscriptionSongs.Value) return;
        using var logText = LoggingScreenUi.NewThreadSafe(I18n.Get("autoDownloadSub.stepOne"));
        Logger.Info("Download cache directory: " + PackedSongUtility.LocalStragePath);

        try
        {
            var res = await UTask.RunOnIl2Cpp(SubscriptionUtility.DownloadSubscriptionAvaliable);

            Logger.Info(
                $"Subscription Status: {res.result}, {res.errorText}, {res.responseBody.subscription}, {res.responseBody.expiration_datetime}");

            var curTime = DateTime.Now;
            var expirationTime = DateTimeOffset.FromUnixTimeMilliseconds(res.responseBody.expiration_datetime).DateTime;
            Logger.Info($"Subscription Current Time: {curTime}, Expiration Time: {expirationTime}");

            if (curTime >= expirationTime)
            {
                Logger.Warn("Subscription is not valid now, skip downloading songs");
                logText.Text = I18n.Get("autoDownloadSub.notVaild");
            }
            else
            {
                logText.Text = I18n.Get("autoDownloadSub.stepTwo");

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
                    logText.Text = progressText;

                    Logger.Info($"Start downloading {previewFileSongUids.Length} song previews");
                    await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadPreviewFiles(
                        previewFileSongUids, CancellationToken.None,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading song previews: {prog}%");
                                logText.Text = $"{progressText} ({prog}%)";
                            }
                        )));
                }

                if (subSongFileSongUids.Length > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepFour", subSongFileSongUids.Length);
                    logText.Text = progressText;

                    Logger.Info($"Start downloading {subSongFileSongUids.Length} song files");
                    await UTask.RunOnIl2CppThreadPool(() => SubscriptionUtility.DownloadSongFilesAsync(
                        subSongFileSongUids, CancellationToken.None,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading song files: {prog}%");
                                logText.Text = $"{progressText} ({prog}%)";
                            }
                        )));
                }

                Logger.Info($"Start downloading {dlcSongFileSongUids.Length} dlc song files");
                var i = 1;
                foreach (var uid in dlcSongFileSongUids)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepFive", i, dlcSongFileSongUids.Length);
                    logText.Text = progressText;

                    Logger.Info($"Start downloading dlc song {uid}");
                    await UTask.RunOnIl2CppThreadPool(() =>
                    {
                        var task = SubscriptionUtility.DownloadSongFile(
                            uid, CancellationToken.None,
                            DelegateSupport.ConvertDelegate<UnityAction<float>>((float result) =>
                                {
                                    var prog = (result * 100).ToString("F1");
                                    Logger.Info($"Downloading dlc song files: {prog}%");
                                    logText.Text = $"{progressText} ({prog}%)";
                                }
                            ), true);
                        return task;
                    });

                    i += 1;
                }
            }

            // SubscriptionUtility.DownloadSongFile()

            Logger.Info("Finished download song files!");

            logText.Text = I18n.Get("autoDownloadSub.finished");
        }
        catch (Exception ex)
        {
            logText.Text = I18n.Get("autoDownloadSub.otherError", ex.ToString());
            Logger.Error(ex.ToString());
        }

        logText.Text += I18n.Get("autoDownloadSub.hideTip");
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

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SongSelectSceneUiControllerBase))]
    [HarmonyPatch(nameof(SongSelectSceneUiControllerBase.LoadSubscriptionAsync))]
    [HarmonyPrefix]
    private static bool SongSelectSceneUiControllerBase_LoadSubscriptionAsync_Prefix(ref UniTask __result)
    {
        if (!TnTrfMod.Instance.enableAutoDownloadSubscriptionSongs.Value) return true;
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
}