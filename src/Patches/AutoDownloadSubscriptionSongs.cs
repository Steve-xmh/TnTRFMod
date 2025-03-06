using System.Collections;
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
    private Exception exception;

    private IEnumerator ShowException()
    {
        if (exception == null) yield break;
        downloadText.Text = $"自动歌曲下载发生错误：\n{exception.Message}";
        yield return WaitToHide();
    }

    private IEnumerator WaitToHide()
    {
        downloadText.Text += "\n文字信息将在 5 秒后消失";
        yield return new WaitForSeconds(5);
        downloadText.Dispose();
        downloadText = null;
    }

    public IEnumerator StartAutoDownloadSubscriptionSongs()
    {
        if (!TnTrfMod.Instance.enableAutoDownloadSubscriptionSongs.Value) yield break;
        if (downloadText != null) yield break;

        downloadText = new TextUi
        {
            Text = "自动下载歌曲已开启\n(1/4) 正在检索 Music Pass 订阅可用性",
            Position = new Vector2(64f, 128f),
            FontSize = 24
        };
        downloadText.MoveToNoDestroyCanvas();

        SubscriptionGateway.ResponseDataSubscriptionAvaliable res = null;

        yield return SubscriptionUtility.DownloadSubscriptionAvaliable().Await(
            result => { res = result; }, onEx);

        if (exception != null)
        {
            yield return ShowException();
            yield break;
        }

        if (res == null)
        {
            exception = new Exception("无法确认 Music Pass 订阅可用性，可能是网络错误");
            yield return ShowException();
            yield break;
        }

        Logger.Info(
            $"Subscription Status: {res.result}, {res.errorText}, {res.responseBody.subscription}, {res.responseBody.expiration_datetime}");

        var curTime = DateTime.Now;
        var expirationTime = DateTimeOffset.FromUnixTimeMilliseconds(res.responseBody.expiration_datetime).DateTime;
        Logger.Info($"Subscription Current Time: {curTime}, Expiration Time: {expirationTime}");

        if (curTime >= expirationTime)
        {
            Logger.Warn("Subscription is not valid now, skip downloading songs");
            downloadText.Text = "Music Pass 订阅不可用或已过期";
            yield return WaitToHide();
            yield break;
        }

        downloadText.Text = "(2/4) Music Pass 订阅可用，正在检索歌曲列表";

        Logger.Info("Subscription is still valid, start downloading songs");
        SubscriptionGateway.ResponseDataSonglistDetails songList = null;
        yield return SubscriptionUtility.DownloadSongListDetails().Await(result => { songList = result; }, onEx);

        var songUids = songList.responseBody.ary_release_song.Select(song => song.song_uid)
            .ToArray();

        if (exception != null)
        {
            yield return ShowException();
            yield break;
        }

        Logger.Info($"Fetched {songList.responseBody.ary_preview_song.Count} preview songs");
        Logger.Info($"Fetched {songList.responseBody.ary_release_song.Count} released songs");

        var source = new CancellationTokenSource();

        var availableSongPreviewUids = new List<int>();

        foreach (var uid in songUids)
            if (!PackedSongUtility.CheckPreviewFileExists(uid))
                availableSongPreviewUids.Add(uid);
        // yield return null; // 因为 PackedSongUtility.CheckPreviewFileExists 会阻塞，所以需要 yield return null 确保流畅
        if (availableSongPreviewUids.Count > 0)
        {
            var progressText = $"(3/4) 正在下载 {availableSongPreviewUids.Count} 首歌曲预览";
            downloadText.Text = progressText;

            Logger.Info($"Start downloading {availableSongPreviewUids.Count} song previews");
            yield return SubscriptionUtility.DownloadPreviewFiles(
                availableSongPreviewUids.ToArray(), source.Token,
                DelegateSupport.ConvertDelegate<UnityAction<float>>(
                    (float result) =>
                    {
                        Logger.Info($"Downloading song previews: {result * 100}%");
                        downloadText.Text = $"{progressText} ({result * 100}%)";
                    }
                )).Await(null, onEx);

            if (exception != null)
            {
                yield return ShowException();
                yield break;
            }
        }

        var availableSongFileUids = new List<int>();

        foreach (var uid in songUids)
            if (!PackedSongUtility.CheckSongFileExists(uid))
                availableSongFileUids.Add(uid);
        // yield return null; // 因为 PackedSongUtility.CheckPreviewFileExists 会阻塞，所以需要 yield return null 确保流畅
        if (availableSongFileUids.Count > 0)
        {
            var progressText = $"(4/4) 正在下载 {availableSongFileUids.Count} 首歌曲文件";
            downloadText.Text = progressText;

            Logger.Info($"Start downloading {availableSongFileUids.Count} song files");
            yield return SubscriptionUtility.DownloadSongFilesAsync(
                availableSongFileUids.ToArray(), source.Token,
                DelegateSupport.ConvertDelegate<UnityAction<float>>(
                    (float result) =>
                    {
                        Logger.Info($"Downloading song files: {result * 100}%");
                        downloadText.Text = $"{progressText} ({result * 100}%)";
                    }
                )).Await(null, onEx);

            if (exception != null)
            {
                yield return ShowException();
                yield break;
            }
        }

        // SubscriptionUtility.DownloadSongFile()

        Logger.Info("Finished download song files!");

        downloadText.Text = "歌曲已下载完成！";
        yield return WaitToHide();
    }

    private void onEx(Exception ex)
    {
        exception = ex;
    }
}