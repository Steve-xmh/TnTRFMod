using System.Text;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;
#if BEPINEX
using Cysharp.Threading.Tasks;
using TMPro;
#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
using Il2CppTMPro;
#endif


namespace TnTRFMod.Scenes.Enso;

public class LiveStreamSongSelectPanel
{
    private static readonly HashSet<string> SongRequestSet = new();
    public static List<QueuedSongInfo> QueuedSongList = [];
    private static readonly List<QueuedSongInfo> NotifySongList = [];
    private static bool NeedUpdateNotify;

    private static Task? _liveStreamDanmakuTask;

    // 
    // CommonObjects.Instance.MySceneManager.ChangeRelayScene("Enso");
    // SongSelectUtility.
    // var ensoManager = CommonObjects.Instance.MyDataManager.EnsoData;
    // ensoManager.ensoSettings.mu

    public static void StartLiveStreamDanmaku()
    {
        if (_liveStreamDanmakuTask != null) return;
        _liveStreamDanmakuTask = Task.Run(InitLiveStreamDanmaku);
        _ = DisplayNotifyTextLoop();
    }

    private static async Task DisplayNotifyTextLoop()
    {
        var notifyText = new TextUi
        {
            FontSize = 20,
            Position = new Vector2(1200f, 950f),
            Alignment = TextAlignmentOptions.BottomRight
        };
        notifyText.MoveToNoDestroyCanvas();
        var text = new StringBuilder(1024);
        while (true)
        {
            while (!NeedUpdateNotify)
                await UniTask.WaitForEndOfFrame().ToTask();
            await text.Clear();
            foreach (var info in NotifySongList)
            {
                await text.AppendLine($"用户 {info.DammakuMessage.SenderName} ({info.DammakuMessage.SenderUid}) 点歌");
                var curLangName = "";

                if (I18n.CurrentLanguage != DataConst.LanguageType.Japanese)
                {
                    await text.Append("  ");
                    curLangName = info.SongInfo.SongNames[(int)I18n.CurrentLanguage];
                    curLangName = curLangName[(curLangName.IndexOf('>') + 1)..];
                    await text.AppendLine(curLangName);
                }

                var jpName = info.SongInfo.SongNames[(int)DataConst.LanguageType.Japanese];
                jpName = jpName[(jpName.IndexOf('>') + 1)..];
                if (curLangName == jpName) continue;
                await text.Append("  ");
                await text.AppendLine(jpName);
            }

            notifyText.Text = text.ToString();
            NeedUpdateNotify = false;
        }
    }

    private static async Task InitLiveStreamDanmaku()
    {
        if (!TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value) return;
        if (TnTrfMod.Instance.bilibiliLiveStreamSongRoomId.Value == 0)
        {
            Logger.Error("Bilibili Live Stream Song Room Id is unset or invalid.");
            return;
        }

        Logger.Warn("Starting Bilibili Live Stream Song Request...");
        var crawer = new BilibiliLiveCommentCrawer(TnTrfMod.Instance.bilibiliLiveStreamSongRoomId.Value,
            TnTrfMod.Instance.bilibiliLiveStreamSongToken.Value);
        crawer.OnDanmakuMessage += (_, msg) =>
        {
            var message = msg.Message;
            if (!message.StartsWith("/点歌 ")) return;
            var songQuery = message[4..].ToLower();

            if (SongAliasTable.TryGetAlias(songQuery, out var musicId))
                foreach (var music in CommonObjects.Instance.MyDataManager.MusicData.MusicInfoAccesserList)
                {
                    if (music.Id.ToLower() != musicId) continue;
                    AddSongToQueue(music, msg);
                    return;
                }

            foreach (var music in CommonObjects.Instance.MyDataManager.MusicData.MusicInfoAccesserList)
            {
                if (music.Id.ToLower() == songQuery)
                {
                    AddSongToQueue(music, msg);
                    return;
                }

                if (music.SongNames.Any(musicSongName => musicSongName.ToLower().Contains(songQuery)))
                {
                    AddSongToQueue(music, msg);
                    return;
                }

                if (music.SongSubs.Any(musicSongName => musicSongName.ToLower().Contains(songQuery)))
                {
                    AddSongToQueue(music, msg);
                    return;
                }
            }
        };
        while (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
        {
            try
            {
                await crawer.StartAsync();
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to start Bilibili Live Stream Song Request:");
                Logger.Warn(e);
            }

            try
            {
                await crawer.Stop();
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to stop Bilibili Live Stream Song Request:");
                Logger.Warn(e);
            }

            await Task.Delay(5000);
            Logger.Info("Restarting Bilibili Live Stream Song Request...");
        }
    }

    private static void AddSongToQueue(MusicDataInterface.MusicInfoAccesser songInfo,
        BilibiliLiveCommentCrawer.DammakuMessage dammakuMessage)
    {
        // if (QueuedSongList.Any(queuedSongInfo => queuedSongInfo.SongInfo.UniqueId == songInfo.UniqueId))
        //     return;
        if (SongRequestSet.Contains(songInfo.Id))
            return;
        SongRequestSet.Add(songInfo.Id);

        var info = new QueuedSongInfo
        {
            SongInfo = songInfo,
            DammakuMessage = dammakuMessage
        };

        Logger.Info("Live Song Requested: ");
        info.Print();

        _ = NotifySongRequest(info);

        QueuedSongList.Add(info);
    }

    private static async Task NotifySongRequest(QueuedSongInfo info)
    {
        NotifySongList.Add(info);
        NeedUpdateNotify = true;
        await Task.Delay(TimeSpan.FromSeconds(5));
        await NotifySongList.Remove(info);
        NeedUpdateNotify = true;
    }

    public record struct QueuedSongInfo
    {
        public BilibiliLiveCommentCrawer.DammakuMessage DammakuMessage;
        public MusicDataInterface.MusicInfoAccesser SongInfo;

        public void Print()
        {
            Logger.Info($"\tSong Id: {SongInfo.Id} ({SongInfo.UniqueId})");
            Logger.Info("\tSong Names: ");
            foreach (var songName in SongInfo.SongNames)
                Logger.Info($"\t\t- {songName}");
            Logger.Info("\tSong Sub Names: ");
            foreach (var subName in SongInfo.SongSubs)
                Logger.Info($"\t\t- {subName}");
            Logger.Info($"\tRequested by: {DammakuMessage.SenderName} ({DammakuMessage.SenderUid})");
        }
    }
}