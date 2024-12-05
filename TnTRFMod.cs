using HarmonyLib;
using Il2Cpp;
using Il2CppScripts.OutGame.Boot;
using Il2CppScripts.OutGame.Common;
using Il2CppUtageExtensions;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Il2Cpp.TaikoCoreTypes;

namespace TnTRFMod
{
    public class TnTRFMod: MelonMod
    {
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            LoggerInstance.Msg("TnTRFMod has started!");
            MelonEvents.OnGUI.Subscribe(DrawGUI, 100);
        }

        private void DrawGUI()
        {
            // TODO: 增加可配置信息
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            if (sceneName == "MyRoom")
                MelonEvents.OnGUI.Subscribe(DrawGUI, 100);
            else
                MelonEvents.OnGUI.Unsubscribe(DrawGUI);
        }
    }

    namespace SimplePatches
    {
        [HarmonyPatch]
        internal class BetterBigHitPatch
        {

            [HarmonyPatch(typeof(EnsoInput))]
            [HarmonyPatch(nameof(EnsoInput.GetLastInputForCore))]
            [HarmonyPatch(MethodType.Normal)]
            [HarmonyPostfix]
            static void EnsoInput_GetLastInputForCore_Postfix(EnsoInput __instance, ref UserInputType __result, int player)
            {
                // 在线模式下不对输入进行修改
                if (__instance.ensoParam.networkGameMode == Il2CppScripts.EnsoGame.Network.NetworkGameMode.None)
                {
                    switch (__result)
                    {
                        case UserInputType.Don_Weak:
                        case UserInputType.Don_Pad:
                            __result = UserInputType.Don_Strong;
                            break;
                        case UserInputType.Katsu_Weak:
                        case UserInputType.Katsu_Pad:
                            __result = UserInputType.Katsu_Strong;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        [HarmonyPatch]
        internal class SkipBootScreenPatch
        {
            static private bool IsBootScene()
            {
                return SceneManager.GetActiveScene().name == "Boot";
            }

            [HarmonyPatch(typeof(BootImage))]
            [HarmonyPatch(nameof(BootImage.PlayAsync))]
            [HarmonyPatch(MethodType.Normal)]
            [HarmonyPrefix]
            static void BootImage_PlayAsync_Prefix(BootImage __instance, ref float duration, ref bool skippable)
            {
                duration = 0f;
                skippable = true;
            }

            [HarmonyPatch(typeof(FadeCover))]
            [HarmonyPatch(nameof(FadeCover.FadeOutAsync))]
            [HarmonyPatch(MethodType.Normal)]
            [HarmonyPrefix]
            static void FadeCover_FadeOutAsync_Prefix(FadeCover __instance, ref Color color, ref float duration)
            {
                if (IsBootScene())
                {
                    __instance.gameObject.SetActive(false);
                    duration = 0f;
                }
            }

            [HarmonyPatch(typeof(FadeCover))]
            [HarmonyPatch(nameof(FadeCover.FadeInAsync))]
            [HarmonyPatch(MethodType.Normal)]
            [HarmonyPrefix]
            static void FadeCover_FadeInAsync_Prefix(FadeCover __instance, ref Color color, ref float duration)
            {
                if (IsBootScene())
                {
                    __instance.gameObject.SetActive(false);
                    duration = 0f;
                }
            }
        }
    }
}
