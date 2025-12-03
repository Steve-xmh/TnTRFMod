using System.Text;
using TnTRFMod.Scenes.Enso;
using TnTRFMod.Ui;
using UnityEngine;
using UnityEngine.UI;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Scenes;

public class BootScene : IScene
{
    public string SceneName => "Boot";

    public void Start()
    {
        var blackGo = new GameObject("BlackGo");
        var transform = blackGo.AddComponent<RectTransform>();
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.black);
        texture.Apply();
        transform.SetParent(Common.GetDrawCanvasForScene());
        transform.pivot = new Vector2(0, 1);
        transform.anchoredPosition =
            new Vector2(Common.ScreenWidth / -2f, Common.ScreenHeight / 2f);
        transform.sizeDelta = new Vector2(1920, 1080);
        var image = blackGo.AddComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 1f);

        if (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
            LiveStreamSongSelectPanel.StartLiveStreamDanmaku();
        if (TnTrfMod.Instance.debugExportMusicNames.Value)
            DumpMusicNames();
        if (TnTrfMod.Instance.debugExportGameData.Value)
            DumpAllAssets();

        // SRDebug.Init();
        // BufferedNoteInputPatch.OnKeyPressEvent += key =>
        // {
        //     if (key != Key.F2) return;
        //     if (SRDebug.Instance.IsDebugPanelVisible)
        //         SRDebug.Instance.HideDebugPanel();
        //     else
        //         SRDebug.Instance.ShowDebugPanel(DefaultTabs.Profiler, false);
        // };
    }

    private void DumpAllAssets()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "model");
        var dest = Path.Combine(TnTrfMod.Dir, "DumpedModels");

        if (!Directory.Exists(dest))
            Directory.CreateDirectory(dest);

        foreach (var filePath in Directory.EnumerateFiles(path, "*.bin", SearchOption.AllDirectories))
        {
            var relPath = filePath[(path.Length + 1)..];
            var destPath = Path.Combine(dest, relPath);
            if (File.Exists(destPath)) continue;
            Logger.Info($"Dumping model: {filePath}");
            var data = Cryptgraphy.ReadAllAesBytes(filePath, Cryptgraphy.AesKeyType.Type0);
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.WriteAllBytes(destPath, data);
        }
    }

    private static void DumpMusicNames()
    {
        var outputPath = Path.Combine(TnTrfMod.Dir, "SongIndices.csv");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        // write bom utf-8
        writer.Write('\uFEFF');

        writer.WriteLine("SongId,NameJA,NameEN,NameFR,NameIT,NameDE,NameES,NameZHT,NameZHS,NameKO");
        foreach (var musicInfo in CommonObjects.Instance.MyDataManager.MusicData.MusicInfoAccesserList)
        {
            writer.Write($"{musicInfo.Id}");
            // await writer.WriteLineAsync(
            //     $"{musicInfo.Id},{musicInfo.SongNames.Join()}");
            foreach (var name in musicInfo.SongNames)
            {
                var modified = name[(name.IndexOf('>') + 1)..];
                modified = modified.Replace("\"", "\"\"");
                writer.Write($",\"{modified}\"");
            }

            writer.WriteLine();
        }

        writer.Flush();
    }
}