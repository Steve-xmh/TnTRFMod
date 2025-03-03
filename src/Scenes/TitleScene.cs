using System.Collections;
using Il2CppInterop.Runtime;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using CancellationTokenSource = Il2CppSystem.Threading.CancellationTokenSource;

namespace TnTRFMod.Scenes;

public class TitleScene : IScene
{
    private bool isAutoSongDownloaded;
    public string SceneName => "Title";

    public void Start()
    {
        _ = new TextUi
        {
            Text = $"TnTRFMod v{MyPluginInfo.PLUGIN_VERSION} (BepInEx)",
            Position = new Vector2(64f, 64f)
        };

        if (isAutoSongDownloaded) return;
        isAutoSongDownloaded = true;
        TnTrfMod.Instance.StartCoroutine(StartAutoDownloadSubscriptionSongs());
    }

    public void Update()
    {
    }

    private static IEnumerable StartAutoDownloadSubscriptionSongs()
    {
        if (!TnTrfMod.Instance.enableAutoDownloadSubscriptionSongs.Value) yield break;
        SubscriptionGateway.ResponseDataSubscriptionAvaliable res = null;

        yield return SubscriptionUtility.DownloadSubscriptionAvaliable().Await(
            result =>
            {
                res = result;
                TnTrfMod.Log.LogInfo("SubscriptionUtility.DownloadSubscriptionAvaliable result");
            });

        TnTrfMod.Log.LogInfo("Finished StartAutoDownloadSubscriptionSongs");

        TnTrfMod.Log.LogInfo(
            $"Subscription Status: {res.result}, {res.errorText}, {res.responseBody.subscription}, {res.responseBody.expiration_datetime}");

        var curTime = DateTime.Now;
        var expirationTime = DateTimeOffset.FromUnixTimeMilliseconds(res.responseBody.expiration_datetime).DateTime;
        TnTrfMod.Log.LogInfo($"Subscription Current Time: {curTime}, Expiration Time: {expirationTime}");

        if (curTime >= expirationTime)
        {
            TnTrfMod.Log.LogWarning($"Subscription is not valid now, skip downloading songs");
            yield break;
        }

        TnTrfMod.Log.LogInfo("Subscription is still valid, start downloading songs");
        SubscriptionGateway.ResponseDataSonglistDetails songList = null;
        yield return SubscriptionUtility.DownloadSongListDetails().Await(result => { songList = result; });

        TnTrfMod.Log.LogInfo($"Fetched {songList.responseBody.ary_preview_song.Count} preview songs");
        TnTrfMod.Log.LogInfo($"Fetched {songList.responseBody.ary_release_song.Count} released songs");

        var source = new CancellationTokenSource();

        var availableSongPreviewUids = songList.responseBody.ary_release_song.Select(song => song.song_uid).Where(uid =>
                !PackedSongUtility.CheckPreviewFileExists(uid))
            .ToArray();

        if (availableSongPreviewUids.Length > 0)
        {
            TnTrfMod.Log.LogInfo($"Start downloading {availableSongPreviewUids.Length} song previews");
            yield return SubscriptionUtility.DownloadPreviewFiles(
                availableSongPreviewUids, source.Token,
                DelegateSupport.ConvertDelegate<UnityAction<float>>(
                    (float result) => { TnTrfMod.Log.LogInfo($"Downloading song previews: {result * 100}%"); }
                )).Await();
        }

        var availableSongFileUids = songList.responseBody.ary_release_song.Select(song => song.song_uid).Where(uid =>
                !PackedSongUtility.CheckSongFileExists(uid))
            .ToArray();

        if (availableSongFileUids.Length > 0)
        {
            TnTrfMod.Log.LogInfo($"Start downloading {availableSongFileUids.Length} song files");
            yield return SubscriptionUtility.DownloadSongFilesAsync(
                availableSongFileUids, source.Token,
                DelegateSupport.ConvertDelegate<UnityAction<float>>(
                    (float result) => { TnTrfMod.Log.LogInfo($"Downloading song files: {result * 100}%"); }
                )).Await();
        }

        TnTrfMod.Log.LogInfo("Finished download song files!");
    }
}