<div align="center">
<img alt="Mod Logo" src="./src/Resources/ModLogo.png" >

<br>

# SteveXMH's TnTRF Mod

A mod aiming to improve experiences of PC version of Taiko no Tatsujin: Rhythm Festival.

一个针对 PC 版本的 太鼓之达人：咚咚雷音祭 的体验优化模组。

</div>

> [!WARNING]
>
> This mod is still unstable and under construction, use it at your own risk!
>
> 此模组仍在开发当中，仍不稳定，敬请留意！

## Features / 功能

- Allow to treat one side hit as a big hit, which will behave as the arcade version. (Will be disabled when in
  online mode to avoid being detected as cheating)
- Buffered keyboard input, allow to avoid miss key in frame. (Currently not work with joystick controller)
- Hit stats panel in enso game, similar to the past training mode panel, and can show the time difference of note hits.
- Faster boot scene to quickly get into game.
- Support to skip reward dialog when already max level and max coins.
- Allow to control animation and preview angle of Don-chan model when in costume page.
- Automatically download all songs when starting the game (and can automatically skip download if already have songs)
- Allow to change texture filter of Onpu/Note to nearest neighbor.
- Built-in REAL tool feature (force to reduce audio output buffer to reduce latency)
- Reopen Steam invite friend overlay when in online room lobby.
- Nijiiro-like score rank icon & note text rail.
- Remove shadows of Onpu/Note to reduce motion blur effect. **(Unstable)**
- ...And More(?)

---

- 允许将单侧击打强制识别成大打，和街机行为一致（在线上模式下会被自动禁用以免被视为作弊）
- 缓冲键盘输入，可以捕获任何时间下的输入而不吃音。（目前对手柄不工作）
- 在游戏中显示击打数据面板，和过去的特训模式类似的面板，同时可以显示音符击打的时间差距。
- 快速跳过启动页面，可以更快地进入游戏。
- 允许控制在装扮页面时的小咚的模型动画和预览角度。
- 支持在已经满级满金币的情况下跳过奖励对话框。
- 支持自动在启动时全量下载歌曲（且可以自动识别已有歌曲并跳过下载）
- 允许将音符纹理过滤设置成最近邻居。
- 内置 REAL 工具功能（强制降低音频输出缓冲以降低延迟）
- 支持在联机房间页面里重新打开 Steam 好友邀请页面
- 类似虹版街机的分数评级图标和音符文本轨道。
- 移除音符的阴影以缓解拖影效果。**（不稳定）**
- ……可能还有更多（？）

## Installation / 安装方式

### Use Taiko Mod Manager / 使用 Taiko Mod Manager 直接安装

 <a href="https://shorturl.at/hHKUL"> <img src="https://github.com/Deathbloodjr/RF.ModTemplate/blob/main/Resources/InstallButton.png?raw=true" alt="点击此处直接安装 / Click here to directly install" width="256"/> </a>

### Manual Installation / 手动安装

#### Use BepInEx / 使用 BepInEx

Install [BepInEx](https://github.com/BepInEx/BepInEx) into your game, then download this mod from
the [release page](https://github.com/Steve-xmh/TnTRFMod/releases/latest) and put it in the `BepInX/plugins` folder
under the game
directory and start the game.

You can access `BepInEx/config/net.stevexmh.TnTRFMod.cfg` to configure the mod features.

为你的游戏安装好 [BepInEx](https://github.com/BepInEx/BepInEx)
，然后从[发行页面](https://github.com/Steve-xmh/TnTRFMod/releases/latest)中下载本模组并放入游戏目录下的 `BepInX/plugins`
文件夹后启动游戏即可。

你可以通过访问 `BepInEx/config/net.stevexmh.TnTRFMod.cfg` 来配置模组的功能。

#### Use MelonLoader / 使用 MelonLoader

Install [MelonLoader](https://github.com/LavaGang/MelonLoader) into your game, then download this mod from
the [release page](https://github.com/Steve-xmh/TnTRFMod/releases/latest) and put it in the `Mods` folder
under the game
directory and start the game.

You can access `UserData/net.stevexmh.TnTRFMod.cfg` to configure the mod features.

为你的游戏安装好 [MelonLoader](https://github.com/LavaGang/MelonLoader)
，然后从[发行页面](https://github.com/Steve-xmh/TnTRFMod/releases/latest)中下载本模组并放入游戏目录下的 `Mods`
文件夹后启动游戏即可。

你可以通过访问 `UserData/net.stevexmh.TnTRFMod.cfg` 来配置模组的功能。

## Disclaimer / 免责声明

This mod contains features that may be considered as a abuse feature (like downloading songs),
and some of the features are not fully tested in multiplayer games.
Even if it can disable some functions that are unfair to online game,
it **CANNOT** ensure that they will not be considered as cheating tools.
If you are worried about this, it is recommended to disable or delete this mod before playing online mode.

本模组存在部分可能会被认为是滥用的功能（例如歌曲下载），且也有功能未在多人游戏中进行完整测试，即便可以自主禁用部分有失多人公平性的功能但
**无法确保**
被视作使用作弊工具，如果担心这种情况则建议在游玩多人模式前禁用或删除本模组。

## Credits / 鸣谢

- [cainan-c/TaikoModManager](https://github.com/cainan-c/TaikoModManager)
- [Deathbloodjr/RF.ModTemplate](https://github.com/Deathbloodjr/RF.ModTemplate)
- [miniant-git/REAL](https://github.com/miniant-git/REAL)
