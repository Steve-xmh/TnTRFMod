<div align="center">
<img alt="Mod Logo" src="./src/Resources/ModLogo.png" >

<br>

# SteveXMH's TnTRF Mod

A multi-functional mod aiming to improve experiences of PC version of Taiko no Tatsujin: Rhythm Festival.

(Made for myself because I don't like install multiple mods to get features I want...)

一个针对 PC 版本的 太鼓之达人：咚咚雷音祭 的多合一功能体验优化模组。

（自己做的，因为我不喜欢安装一堆模组来获得我想要的功能……）

</div>

> [!CAUTION]
> 
> Due to the [statement](https://x.com/taiko_kateiyou/status/1935532991863820367) in Bandai Namco's handling of modding,
> and due to the threat of [DMCA takedowns](https://github.com/TaikoModding/TekaTeka),
> this repository may have unpredictable development status,
> and the possibility of archiving/deleting/migrating repository without warning.
>
> If you are the copyright owner (e.g: Bandai Namco) and having issues with this mod project,
> please send an email to [`stevexmh@qq.com`](mailto:stevexmh@qq.com) to contact me for any action for this repository smoothly, thank you.
>
> **Note:** This mod contains features that may be considered as a abuse/cheating feature (like downloading songs or single hit patch),
> but it does **NOT** contain any copyrighted assets or reverse engineering code.
> (All of them are generated from BepInEx/MelonLoader APIs or written from scratch)

> [!CAUTION]
> 
> 由于万代南梦宫对于模组开发的[严肃声明](https://x.com/taiko_kateiyou/status/1935532991863820367)，
> 以及对于现有游戏模组项目的[ DMCA 封锁](https://github.com/TaikoModding/TekaTeka),
> 本仓库可能会有不可预知的开发状态，以及有可能在毫无预警的情况下包括但不限于归档/删除/转移仓库，敬请留意。
>
> 如果你是版权所有方（例如：万代南梦宫），且对本模组项目有任何异议，请电邮至 [`stevexmh@qq.com`](mailto:stevexmh@qq.com) 以更快且温和地处置本仓库，非常感谢。
>
> **Note:** 本模组包含部分可能会被认为是滥用或作弊的功能（例如歌曲下载及单打补丁），但**不包含**任何受版权保护的资源或逆向工程代码。
> （所有接入代码均来自 BepInEx/MelonLoader 生成的接口或自行编写）

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
- ~~Support WASAPI Exclusive Audio mode, which can get lower audio latency.~~ (This feature has been separated
  into [TnTRFMod.ExclusiveAudio](https://github.com/Steve-xmh/TnTRFMod.ExclusiveAudio))
- Support high precision timer, which can improve hit timing judgement and frame rate stuttering.
- Configurable auto play renda speed.
- Faster boot scene to quickly get into game.
- Support to skip reward dialog when already max level and max coins.
- Allow to control animation and preview angle of Don-chan model when in costume page.
- Automatically download all songs when starting the game (and can automatically skip download if already have songs)
- Allow to change texture filter of Onpu/Note to nearest neighbor.
- Built-in REAL tool feature (force to reduce audio output buffer to reduce latency)
- Reopen Steam invite friend overlay when in online room lobby.
- Nijiiro-like score rank icon & note text rail.
- Remove shadows of Onpu/Note to reduce motion blur effect. **(Unstable)**
- Instant relay scene (Quickly open/close the relay scene without waiting for the animation)
- Rapidly jump to the song select scene from the title scene.
- Darkens and prevents presses for difficulties not present in charts.
- ...And More(?)

---

- 允许将单侧击打强制识别成大打，和街机行为一致（在线上模式下会被自动禁用以免被视为作弊）
- 缓冲键盘输入，可以捕获任何时间下的输入而不吃音。（目前对手柄不工作）
- 在游戏中显示击打数据面板，和过去的特训模式类似的面板，同时可以显示音符击打的时间差距。
- ~~支持独占音频播放模式，可以获得更加低的音频延迟。~~
  （本功能已分离到 [TnTRFMod.ExclusiveAudio](https://github.com/Steve-xmh/TnTRFMod.ExclusiveAudio) 模组）
- 支持高精度计时器，可以改善敲击时间判定和帧数抖动。
- 可配置的自动演奏连打速度。
- 快速跳过启动页面，可以更快地进入游戏。
- 允许控制在装扮页面时的小咚的模型动画和预览角度。
- 支持在已经满级满金币的情况下跳过奖励对话框。
- 支持自动在启动时全量下载歌曲（且可以自动识别已有歌曲并跳过下载）
- 允许将音符纹理过滤设置成最近邻居。
- 内置 REAL 工具功能（强制降低音频输出缓冲以降低延迟）
- 支持在联机房间页面里重新打开 Steam 好友邀请页面
- 类似虹版街机的分数评级图标和音符文本轨道。
- 移除音符的阴影以缓解拖影效果。**（不稳定）**
- 快速中继场景（无需等待动画即可快速打开/关闭中继场景）
- 快速从标题页面跳转到歌曲选择页面。
- 将不存在难度的谱面将其难度按钮变暗并禁止按下。
- ……可能还有更多（？）

## Installation / 安装方式

### Manual Installation / 手动安装

#### Use BepInEx / 使用 BepInEx

Install [BepInEx](https://github.com/BepInEx/BepInEx) into your game, then download this mod from
the [release page](https://github.com/Steve-xmh/TnTRFMod/releases/latest) and put it in the `BepInX/plugins` folder
under the game directory and start the game.

> [!INFO]
> Please make sure you have installed the latest version of BepInEx 6 (Or the version must higher than `6.0.0-be.697`).
>
> 请确保你已经安装了最新版本的 BepInEx 6 (或者版本必须高于 `6.0.0-be.697`).

For configuration, please read [below](#configuration--配置).

为你的游戏安装好 [BepInEx](https://github.com/BepInEx/BepInEx)
，然后从[发行页面](https://github.com/Steve-xmh/TnTRFMod/releases/latest)中下载本模组并放入游戏目录下的 `BepInX/plugins`
文件夹后启动游戏即可。

如需配置，请阅读[下方](#configuration--配置)。

#### Use MelonLoader / 使用 MelonLoader

Install [MelonLoader](https://github.com/LavaGang/MelonLoader) into your game, then download this mod from
the [release page](https://github.com/Steve-xmh/TnTRFMod/releases/latest) and put it in the `Mods` folder
under the game
directory and start the game.

For configuration, please read [below](#configuration--配置).

为你的游戏安装好 [MelonLoader](https://github.com/LavaGang/MelonLoader)
，然后从[发行页面](https://github.com/Steve-xmh/TnTRFMod/releases/latest)中下载本模组并放入游戏目录下的 `Mods`
文件夹后启动游戏即可。

如需配置，请阅读[下方](#configuration--配置)。

## Configuration / 配置

For my own convenience, the configuration file is located in the `TnTRFMod` folder under the game directory with two of
them:

- `config.toml`: This is the actual configuration file that the mod will read from it, and will be generated when the
  file is not exists.
- `config.example.toml`: This is an example configuration file that contains default configs comments for each option,
  and will be generated each time the mod is loaded.

If you are new to this configuration language, you can learn it from here: [TOML](https://toml.io/en/).

After editing and saving the configuration file,
please restart the game to apply the changes. (Thought some options may support hot-reloading)

为了方便起见，配置文件位于游戏目录下的 `TnTRFMod` 文件夹中，包含两个文件：

- `config.toml`：这是模组实际读取的配置文件，如果文件不存在则会生成该文件。
- `config.example.toml`：这是一个示例配置文件，包含每个选项的默认值和注释，每次加载模组时都会生成该文件。

如果你不熟悉这种配置语言，可以从这里了解：[TOML](https://toml.io/cn/)。

编辑并保存配置文件后，请重启游戏以应用更改。（尽管某些选项可能支持热更新）

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
- [Deathbloodjr/RF.SkipCoinAndRewardScreen](https://github.com/Deathbloodjr/RF.SkipCoinAndRewardScreen)
