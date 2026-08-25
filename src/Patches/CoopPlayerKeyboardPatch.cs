using HarmonyLib;
using TnTRFMod.Config;
using UnityEngine.InputSystem;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class CoopPlayerKeyboardPatch
{
    // [HarmonyPatch(typeof(MainMenuSceneUiController))]
    // [HarmonyPatch(nameof(MainMenuSceneUiController.Setup))]
    // [HarmonyPostfix]
    // private static void UiControllerPairingView_Setup_Postfix(MainMenuSceneUiController __instance)
    // {
    //     __instance.OnPlayNumberDecide(PlayerMode._2P);
    //     // CurrentTarget.OnPlayNumberDecide(Scripts.OutGame.MainMenu.PlayerMode._2P);
    // }

    private const int P2PseudoDeviceId = 1000; // 用于 P2 键盘的占位设备 ID（非 -1，避免被当作“未分配”）

    /// <summary><see cref="ModConfig.EnableCoopP2Keyboard"/></summary>
    private static bool P2KeyboardEnabled => ModConfig.EnableCoopP2Keyboard.Value;

    /// <summary>
    /// 把 P2 的 ControllerInfo 标记为“已连接（键盘）”。
    /// 游戏侧 ControllerCheck 每帧会检测 enabled && !connected 的玩家位并要求用实体手柄按 A/X，
    /// 只有把真实字段 connected 置 true 且 deviceId 非 -1，才不会触发“请选择控制器并按下 A/X”。
    /// </summary>
    private static void MarkP2AsKeyboardConnected()
    {
        if (!P2KeyboardEnabled) return;

        var mgr = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;
        if (mgr == null) return;

        var controllers = mgr.Controllers;
        if (controllers == null || controllers.Length <= (int)ControllerManager.ControllerPlayerNo.Player2) return;

        var p2 = controllers[(int)ControllerManager.ControllerPlayerNo.Player2];
        p2.enabled = true;
        p2.connected = true;
        p2.type = ControllerManager.ControllerType.Keyboard;
        p2.deviceId = P2PseudoDeviceId;
        controllers[(int)ControllerManager.ControllerPlayerNo.Player2] = p2;
    }

    /// <summary>
    /// 让步游戏认为 P2 已连接，同时在每次查询时确保 P2 保持键盘已连接状态。
    /// </summary>
    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch(nameof(ControllerManager.IsPlayerControllerConnected))]
    [HarmonyPrefix]
    private static bool ControllerManager_IsPlayerControllerConnected_Prefix(ControllerManager __instance,
        ref bool __result,
        ControllerManager.ControllerPlayerNo controllerPlayerNo,
        bool ignoreKeyboard = false)
    {
        if (P2KeyboardEnabled && controllerPlayerNo == ControllerManager.ControllerPlayerNo.Player2)
        {
            MarkP2AsKeyboardConnected();
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 阻止游戏把 P2 从“已连接”状态断开。
    /// 无实体手柄时 ControllerManager.UpdatePlayerControllerInfo 会因找不到对应 Gamepad 而调用
    /// DisconnectController(Player2) 把 connected 清掉，这里把 P2 直接跳过。
    /// </summary>
    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch("DisconnectController")]
    [HarmonyPatch(MethodType.Normal)]
    private static bool ControllerManager_DisconnectController_Prefix(ControllerManager __instance,
        int playerNo)
    {
        if (P2KeyboardEnabled && playerNo == (int)ControllerManager.ControllerPlayerNo.Player2)
            return false; // 跳过断开，保持 P2 键盘连接

        return true;
    }

    /// <summary>
    /// 在游戏启用 P1~P4 玩家位（打开配对界面）时，立刻把 P2 标记为键盘已连接，
    /// 避免后续每帧的“等待手柄按 A/X”绑定流程卡住。
    /// </summary>
    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch("EnablePlayerController")]
    [HarmonyPatch(new[] { typeof(ControllerManager.ControllerPlayerNo), typeof(ControllerManager.ControllerPlayerNo) })]
    [HarmonyPostfix]
    private static void ControllerManager_EnablePlayerController_Postfix(ControllerManager __instance)
    {
        MarkP2AsKeyboardConnected();
    }

    /// <summary>
    /// 让 P2 也能用键盘“确定”（配对确认界面、选曲确认等）。
    /// 游戏侧 GetOkDown 的键盘分支只在 Player1 生效，这里为 Player2 补上独立确认键。
    /// </summary>
    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch(nameof(ControllerManager.GetOkDown))]
    [HarmonyPostfix]
    private static void ControllerManager_GetOkDown_Postfix(ControllerManager __instance,
        ControllerManager.ControllerPlayerNo controllerPlayerNo,
        ref bool __result)
    {
        if (!P2KeyboardEnabled || controllerPlayerNo != ControllerManager.ControllerPlayerNo.Player2) return;
        if (__result) return;

        var p2OkKey = ModConfig.P2OkKey.Value;
        var keyboard = Keyboard.current;
        if (p2OkKey == Key.None || keyboard == null) return;

        // 上升沿：仅在“这一帧刚按下”时算一次确认，避免按住时反复触发
        __result = keyboard[p2OkKey].wasPressedThisFrame;
    }

    /// <summary>
    /// 让 P2 也能用键盘“取消”（配对界面回退、暂停菜单等）。
    /// </summary>
    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch(nameof(ControllerManager.GetCancelDown))]
    [HarmonyPostfix]
    private static void ControllerManager_GetCancelDown_Postfix(ControllerManager __instance,
        ControllerManager.ControllerPlayerNo controllerPlayerNo,
        ref bool __result)
    {
        if (!P2KeyboardEnabled || controllerPlayerNo != ControllerManager.ControllerPlayerNo.Player2) return;
        if (__result) return;

        var p2CancelKey = ModConfig.P2CancelKey.Value;
        var keyboard = Keyboard.current;
        if (p2CancelKey == Key.None || keyboard == null) return;

        __result = keyboard[p2CancelKey].wasPressedThisFrame;
    }
}
