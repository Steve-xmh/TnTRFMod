using Il2CppInterop.Runtime;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using CancellationTokenSource = Il2CppSystem.Threading.CancellationTokenSource;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

public class AutoDownloadSubscriptionSongs
{
    private TextUi downloadText;

    public async Task StartAutoDownloadSubscriptionSongsAsync()
    {
        if (!TnTrfMod.Instance.enableAutoDownloadSubscriptionSongs.Value) return;
        if (downloadText != null) return;

        downloadText = new TextUi
        {
            Text = I18n.Get("autoDownloadSub.stepOne"),
            Position = new Vector2(64f, 128f),
            FontSize = 24
        };
        downloadText.MoveToNoDestroyCanvas();

        try
        {
            var res = await SubscriptionUtility.DownloadSubscriptionAvaliable().ToTask();

            Logger.Info(
                $"Subscription Status: {res.result}, {res.errorText}, {res.responseBody.subscription}, {res.responseBody.expiration_datetime}");

            var curTime = DateTime.Now;
            var expirationTime = DateTimeOffset.FromUnixTimeMilliseconds(res.responseBody.expiration_datetime).DateTime;
            Logger.Info($"Subscription Current Time: {curTime}, Expiration Time: {expirationTime}");

            if (curTime >= expirationTime)
            {
                Logger.Warn("Subscription is not valid now, skip downloading songs");
                downloadText.Text = I18n.Get("autoDownloadSub.notVaild");
            }
            else
            {
                downloadText.Text = I18n.Get("autoDownloadSub.stepTwo");

                Logger.Info("Subscription is still valid, start downloading songs");

                var songList = await SubscriptionUtility.DownloadSongListDetails().ToTask();

                var songUids = songList.responseBody.ary_release_song.Select(song => song.song_uid)
                    .ToArray();

                Logger.Info($"Fetched {songList.responseBody.ary_preview_song.Count} preview songs");
                Logger.Info($"Fetched {songList.responseBody.ary_release_song.Count} released songs");

                var source = new CancellationTokenSource();

                var availableSongPreviewUids =
                    songUids.Where(uid => !PackedSongUtility.CheckPreviewFileExists(uid)).ToList();

                if (availableSongPreviewUids.Count > 0)
                {
                    var progressText = I18n.Get("autoDownloadSub.stepThree", availableSongPreviewUids.Count);
                    downloadText.Text = progressText;

                    Logger.Info($"Start downloading {availableSongPreviewUids.Count} song previews");
                    await SubscriptionUtility.DownloadPreviewFiles(
                        availableSongPreviewUids.ToArray(), source.Token,
                        DelegateSupport.ConvertDelegate<UnityAction<float>>(
                            (float result) =>
                            {
                                var prog = (result * 100).ToString("F1");
                                Logger.Info($"Downloading song previews: {prog}%");
                                downloadText.Text = $"{progressText} ({prog}%)";
                            }
                        )).ToTask();
                }

                var availableSongFileUids = songUids.Where(uid => !PackedSongUtility.CheckSongFileExists(uid)).ToList();

                if (availableSongFileUids.Count > 0)
                {
                    var songDataDetails = await SubscriptionUtility
                        .DownloadSongDataDetails(availableSongFileUids.ToArray()).ToTask();
                    foreach (var songDetail in songDataDetails.responseBody.ary_song_datail)
                    {
                        var info = songDetail.ToMusicInfo();
                        // Logger.Info($"Song Id: {info.Id}");
                        // Logger.Info($"  Song File Name: {info.SongFileName}");
                        // Logger.Info($"  Song name (JP): {info.SongNameJP}");
                        // Logger.Info($"  Song name (CN): {info.SongNameCN}");
                        // Logger.Info($"  Song name (EN): {info.SongNameEN}");
                        // Logger.Info($"  InPackage: {info.InPackage}");
                        // Logger.Info($"  DlcRegionList: {info.DlcRegionList}");
                        // Logger.Info($"  PlayableRegionList: {info.PlayableRegionList}");
                        // Logger.Info($"  SubscriptionRegionList: {info.SubscriptionRegionList}");
                        if (info.SubscriptionRegionList == "") availableSongFileUids.Remove(info.UniqueId);
                    }

                    if (availableSongFileUids.Count > 0)
                    {
                        var progressText = I18n.Get("autoDownloadSub.stepFour", availableSongFileUids.Count);
                        downloadText.Text = progressText;

                        Logger.Info($"Start downloading {availableSongFileUids.Count} song files");
                        await SubscriptionUtility.DownloadSongFilesAsync(
                            availableSongFileUids.ToArray(), source.Token,
                            DelegateSupport.ConvertDelegate<UnityAction<float>>(
                                (float result) =>
                                {
                                    var prog = (result * 100).ToString("F1");
                                    Logger.Info($"Downloading song files: {prog}%");
                                    downloadText.Text = $"{progressText} ({prog}%)";
                                }
                            )).ToTask();
                    }
                }

                // SubscriptionUtility.DownloadSongFile()

                Logger.Info("Finished download song files!");

                downloadText.Text = I18n.Get("autoDownloadSub.finished");
            }
        }
        catch (Exception ex)
        {
            downloadText.Text = I18n.Get("autoDownloadSub.otherError", ex.ToString());
            Logger.Error(ex.ToString());
        }

        downloadText.Text += I18n.Get("autoDownloadSub.hideTip");
        await Task.Delay(5000);
        downloadText.Dispose();
        downloadText = null;
    }
}