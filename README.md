# SteveXMH's TnTRF Mod

A simple mod for PC version of Taiko no Tatsujin: Rhythm Festival.

一个简易的 PC 版本的 太鼓之达人：咚咚雷音祭 的模组。

## Features / 功能

- Allow to treat one side hit as a big hit, which will behave as the arcade version. (Will be disabled when in
  online mode to avoid being detected as cheating)
- Buffered keyboard input, allow to avoid miss key in frame.
- Faster boot scene to quickly get into game.
- Support to skip reward dialog when already max level and max coins.
- Allow to control animation and preview angle of Don-chan model when in costume page.
- Automatically download all songs when starting the game (and can automatically skip download if already have songs)
- Allow to change texture filter of Onpu/Note to nearest neighbor.
- Built-in REAL tool feature (force to reduce audio output buffer to reduce latency)
- Remove shadows of Onpu/Note to reduce motion blur effect. **(Unstable)**

---

- 允许将单侧击打强制识别成大打，和街机行为一致（在线上模式下会被自动禁用以免被视为作弊）
- 缓冲键盘输入，可以捕获任何时间下的输入而不吃音。
- 快速跳过启动页面，可以更快地进入游戏。
- 允许控制在装扮页面时的小咚的模型动画和预览角度。
- 支持在已经满级满金币的情况下跳过奖励对话框。
- 支持自动在启动时全量下载歌曲（且可以自动识别已有歌曲并跳过下载）
- 允许将音符纹理过滤设置成最近邻居。
- 内置 REAL 工具功能（强制降低音频输出缓冲以降低延迟）
- 移除音符的阴影以缓解拖影效果。**（不稳定）**

## Installation / 安装方式

### Use Taiko Mod Manager / 使用 Taiko Mod Manager 直接安装

 <a href="taikomodmanager:https://github.com/Steve-xmh/TnTRFMod"> <img src="https://github.com/Deathbloodjr/RF.ModTemplate/blob/main/Resources/InstallButton.png?raw=true" alt="点击此处直接安装 / Click here to directly install" width="256"/> </a>

### Manual Installation / 手动安装

Install [BepInEx](https://github.com/BepInEx/BepInEx) into your game, then download this mod from
the [release page](https://github.com/Steve-xmh/TnTRFMod/releases/latest) and put it in the `BepInX/plugins` folder
under the game
directory and start the game.

You can access `BepInEx/config/net.stevexmh.TnTRFMod.cfg` to configure the mod features.

为你的游戏安装好 [BepInEx](https://github.com/BepInEx/BepInEx)
，然后从[发行页面](https://github.com/Steve-xmh/TnTRFMod/releases/latest)中下载本模组并放入游戏目录下的 `BepInX/plugins`
文件夹后启动游戏即可。

你可以通过访问 `BepInEx/config/net.stevexmh.TnTRFMod.cfg` 来配置模组的功能。

## Disclaimer / 免责声明

This mod is not fully tested in multiplayer games. Even if it can disable some functions that are unfair to online game,
it **CANNOT** ensure that they will not be considered as cheating tools. If you are worried about this, it is
recommended to disable or delete this mod before playing online mode.

本模组存在部分可能会被认为是滥用的功能（例如歌曲下载），且也有功能未在多人游戏中进行完整测试，即便可以自主禁用部分有失多人公平性的功能但
**无法确保**
被视作使用作弊工具，如果担心这种情况则建议在游玩多人模式前禁用或删除本模组。

## Credits / 鸣谢

- [cainan-c/TaikoModManager](https://github.com/cainan-c/TaikoModManager)
- [Deathbloodjr/RF.ModTemplate](https://github.com/Deathbloodjr/RF.ModTemplate)
- [miniant-git/REAL](https://github.com/miniant-git/REAL)
