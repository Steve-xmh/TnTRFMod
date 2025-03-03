using HarmonyLib;
using TnTRFMod.Utils;
using UnityEngine.InputSystem;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class BufferedNoteInputPatch
{
    private static uint DonLInputCount;
    private static uint KatsuLInputCount;
    private static uint KatsuRInputCount;
    private static uint DonRInputCount;
    private static bool prevInput;

    private static bool Disabled => !TnTrfMod.Instance.enableBufferedInputPatch.Value;

    public static void ResetCounts()
    {
        DonLInputCount = 0;
        KatsuLInputCount = 0;
        KatsuRInputCount = 0;
        DonRInputCount = 0;
    }

    [HarmonyPatch(typeof(Keyboard))]
    [HarmonyPatch(nameof(Keyboard.OnTextInput))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void Keyboard_OnTextInput_Postfix(Keyboard __instance, char character)
    {
        if (Disabled) return;
        var controllerManager = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;
        if (controllerManager == null) return;
        if (!controllerManager.IsKeyOperationAvailable()) return;
        // TnTrfMod.Log.LogInfo($"BufferedNoteInputPatch called {character} {(short)character}");

        var maxBufferedInputCount = TnTrfMod.Instance.maxBufferedInputCount.Value;
        var donLKey = controllerManager.keyConfig[(int)ControllerManager.Taiko.DonL];
        var donRKey = controllerManager.keyConfig[(int)ControllerManager.Taiko.DonR];
        var katsuLKey = controllerManager.keyConfig[(int)ControllerManager.Taiko.KatsuL];
        var katsuRKey = controllerManager.keyConfig[(int)ControllerManager.Taiko.KatsuR];
        var charCode = (short)KeyConversion.CharToKey(character);
        if (charCode == donLKey) DonLInputCount = Math.Clamp(DonLInputCount + 1, 0, maxBufferedInputCount);
        else if (charCode == donRKey) DonRInputCount = Math.Clamp(DonRInputCount + 1, 0, maxBufferedInputCount);
        else if (charCode == katsuLKey) KatsuLInputCount = Math.Clamp(KatsuLInputCount + 1, 0, maxBufferedInputCount);
        else if (charCode == katsuRKey) KatsuRInputCount = Math.Clamp(KatsuRInputCount + 1, 0, maxBufferedInputCount);
    }

    [HarmonyPatch(typeof(EnsoInput))]
    [HarmonyPatch(nameof(EnsoInput.UpdateController))]
    [HarmonyPostfix]
    private static void EnsoInput_UpdateController_Postfix(EnsoInput __instance, int player,
        ref EnsoInput.EnsoInputFlag __result)
    {
        if (Disabled) return;
        var controllerManager = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;
        if (controllerManager == null) return;
        if (!controllerManager.IsKeyOperationAvailable()) return;
        if (prevInput)
        {
            prevInput = false;
            __result = EnsoInput.EnsoInputFlag.None;
            return;
        }

        // DonL DonR KatsuL KatsuR
        if (DonLInputCount > 0)
        {
            DonLInputCount--;
            prevInput = true;
            __result = EnsoInput.EnsoInputFlag.DonL;
        }
        else if (DonRInputCount > 0)
        {
            DonRInputCount--;
            prevInput = true;
            __result = EnsoInput.EnsoInputFlag.DonR;
        }
        else if (KatsuLInputCount > 0)
        {
            KatsuLInputCount--;
            prevInput = true;
            __result = EnsoInput.EnsoInputFlag.KatsuL;
        }
        else if (KatsuRInputCount > 0)
        {
            KatsuRInputCount--;
            prevInput = true;
            __result = EnsoInput.EnsoInputFlag.KatsuR;
        }
        else
        {
            __result = EnsoInput.EnsoInputFlag.None;
        }
    }
}