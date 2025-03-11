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
            Text = "自动下载歌曲已开启\n(1/4) 正在检索 Music Pass 订阅可用性",
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
                downloadText.Text = "Music Pass 订阅不可用或已过期";
            }
            else
            {
                downloadText.Text = "(2/4) Music Pass 订阅可用，正在检索歌曲列表";

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
                    var progressText = $"(3/4) 正在下载 {availableSongPreviewUids.Count} 首歌曲预览";
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
                    var progressText = $"(4/4) 正在下载 {availableSongFileUids.Count} 首歌曲文件";
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

                // SubscriptionUtility.DownloadSongFile()

                Logger.Info("Finished download song files!");

                downloadText.Text = "歌曲已下载完成！";
            }
        }
        catch (Exception ex)
        {
            downloadText.Text = $"自动歌曲下载发生错误：\n{ex.Message}";
        }

        downloadText.Text += "\n文字信息将在 5 秒后消失";
        await Task.Delay(5000);
        downloadText.Dispose();
        downloadText = null;
    }
}