using HarmonyLib;

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

    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch(nameof(ControllerManager.IsPlayerControllerConnected))]
    [HarmonyPrefix]
    private static bool ControllerManager_IsPlayerControllerConnected_Postfix(ControllerManager __instance,
        ref bool __result,
        ControllerManager.ControllerPlayerNo controllerPlayerNo,
        bool ignoreKeyboard = false)
    {
        if (controllerPlayerNo == ControllerManager.ControllerPlayerNo.Player2)
        {
            __result = true;
            return false;
        }

        return true;
    }
}