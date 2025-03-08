using HarmonyLib;
using Il2CppInterop.Runtime;
using TnTRFMod.Utils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class BufferedNoteInputPatch
{
    private static bool Injected;

    private static readonly ControllerManager mgr = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;

    private static readonly List<InputState> playerInputStates =
    [
        new(ControllerManager.ControllerPlayerNo.Player1),
        new(ControllerManager.ControllerPlayerNo.Player2),
        new(ControllerManager.ControllerPlayerNo.Player3),
        new(ControllerManager.ControllerPlayerNo.Player4)
    ];

    private static bool Disabled => !TnTrfMod.Instance.enableBufferedInputPatch.Value;

    public static void ResetCounts()
    {
        foreach (var inputState in playerInputStates) inputState.Reset();

        if (Injected) return;
        InputSystem.onEvent +=
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<InputEventPtr, InputDevice>>(OnInputSystemEvent);
        Keyboard.current.add_onTextInput(
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<char>>(OnTextInput));

        Injected = true;
    }

    // TODO: Gamepad support
    private static void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.handled) return;
        foreach (var inputState in playerInputStates)
        {
            var ctler = mgr.Controllers[(int)inputState.PlayerNo];
            if (ctler.deviceId == device.deviceId)
            {
                var gamepad = device as Gamepad;
                // Logger.Info($"OnInputSystemEvent {inputState.PlayerNo} {ctler.deviceId}");
                inputState.Scan(gamepad, eventPtr);
                return;
            }
        }
    }

    private static void OnTextInput(char character)
    {
        if (Disabled) return;
        if (!mgr.IsKeyOperationAvailable()) return;

        // (InputSystem.FindControl("") as ButtonControl).isPressed;
        mgr.GetNormalAxis(ControllerManager.ControllerPlayerNo.All, ControllerManager.Buttons.A);

        var donLKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonL];
        var donRKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonR];
        var katsuLKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuL];
        var katsuRKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuR];
        var charCode = (short)KeyConversion.CharToKey(character);
        if (charCode == donLKey) playerInputStates[0].InvokeDonL();
        else if (charCode == donRKey) playerInputStates[0].InvokeDonR();
        else if (charCode == katsuLKey) playerInputStates[0].InvokeKatsuL();
        else if (charCode == katsuRKey) playerInputStates[0].InvokeKatsuR();
    }

    [HarmonyPatch(typeof(EnsoInput))]
    [HarmonyPatch(nameof(EnsoInput.UpdateController))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoInput_UpdateController_Postfix(EnsoInput __instance, int player,
        ref EnsoInput.EnsoInputFlag __result)
    {
        if (Disabled) return;
        var inputState = playerInputStates[player];
        inputState.Resolve(ref __result);
    }

    private class InputState(ControllerManager.ControllerPlayerNo playerNo)
    {
        public readonly ControllerManager.ControllerPlayerNo PlayerNo = playerNo;
        private uint DonL;
        private uint DonR;
        private uint KatsuL;
        private uint KatsuR;
        private bool prevInput;

        private uint MaxBufferedInputCount => TnTrfMod.Instance.maxBufferedInputCount.Value;

        public void Scan(Gamepad gamepad, InputEventPtr eventPtr)
        {
            // mgr.controlType
            // var typeTable = mgr.TypeTable.Cast<Il2CppArrayBase<ControllerManager.Buttons>>();
            // mgr.GetGamepadButtonControl(ref gamepad, ControllerManager.Buttons.A)
            // mgr.analogThreshold
            if (mgr.GetDonKatsuDown(PlayerNo, ControllerManager.Taiko.DonL))
                InvokeDonL();

            if (mgr.GetDonKatsuDown(PlayerNo, ControllerManager.Taiko.DonR))
                InvokeDonR();

            if (mgr.GetDonKatsuDown(PlayerNo, ControllerManager.Taiko.KatsuL))
                InvokeKatsuL();

            if (mgr.GetDonKatsuDown(PlayerNo, ControllerManager.Taiko.KatsuR))
                InvokeKatsuR();
        }

        public void InvokeDonL()
        {
            Logger.Info($"Player {PlayerNo} DonL");
            DonL = Math.Clamp(DonL + 1, 0, MaxBufferedInputCount);
        }

        public void InvokeDonR()
        {
            Logger.Info($"Player {PlayerNo} DonR");
            DonR = Math.Clamp(DonR + 1, 0, MaxBufferedInputCount);
        }

        public void InvokeKatsuL()
        {
            Logger.Info($"Player {PlayerNo} KatsuL");
            KatsuL = Math.Clamp(KatsuL + 1, 0, MaxBufferedInputCount);
        }

        public void InvokeKatsuR()
        {
            Logger.Info($"Player {PlayerNo} KatsuR");
            KatsuR = Math.Clamp(KatsuR + 1, 0, MaxBufferedInputCount);
        }

        public void Reset()
        {
            DonL = 0;
            DonR = 0;
            KatsuL = 0;
            KatsuR = 0;
            prevInput = false;
        }

        public bool Resolve(ref EnsoInput.EnsoInputFlag result)
        {
            if (prevInput)
            {
                prevInput = false;
                result = EnsoInput.EnsoInputFlag.None;
                return true;
            }

            if (DonL > 0 && DonR > 0)
            {
                DonL--;
                DonR--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.DaiDon;
                return true;
            }

            if (KatsuL > 0 && KatsuR > 0)
            {
                KatsuL--;
                KatsuR--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.DaiKatsu;
                return true;
            }

            if (DonL > 0)
            {
                DonL--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.DonL;
                return true;
            }

            if (DonR > 0)
            {
                DonR--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.DonR;
                return true;
            }

            if (KatsuL > 0)
            {
                KatsuL--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.KatsuL;
                return true;
            }

            if (KatsuR > 0)
            {
                KatsuR--;
                prevInput = true;
                result = EnsoInput.EnsoInputFlag.KatsuR;
                return true;
            }

            return false;
        }
    }
}