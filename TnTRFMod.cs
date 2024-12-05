using HarmonyLib;
using Il2Cpp;
using Il2CppScripts.OutGame.Boot;
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
        }

        private static Rect addExpButton = new Rect(10, 10, 100, 50);
        public static bool isOnlineMode = false;

        private void DrawGUI()
        {
            // TODO
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            // Il2Cpp.TaikoCorePlayer/SimpleInput
            if (sceneName == "Boot")
            {
                //SceneManager.LoadScene("Title");
            }
            if (sceneName == "MyRoom")
            {
                MelonEvents.OnGUI.Subscribe(DrawGUI, 100);
            }
            else
            {
                MelonEvents.OnGUI.Unsubscribe(DrawGUI);
            }
        }
    }

    namespace SimpleInputPatch
    {

        [HarmonyPatch(typeof(EnsoInput), "GetLastInputForCore", new Type[] { typeof(int) })]
        public class GetLastInputForCorePatch
        {
            static void Postfix(EnsoInput __instance, ref UserInputType __result, int player)
            {
                if (!TnTRFMod.isOnlineMode)
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


        [HarmonyPatch(typeof(EnsoPlayingParameter), "get_IsOnlineMode", new Type[] { })]
        public class IsOnlineModePatch
        {
            static void Postfix(EnsoPlayingParameter __instance, bool __result)
            {
                if (TnTRFMod.isOnlineMode != __result)
                {
                    TnTRFMod.isOnlineMode = __result;
                    if (__result)
                    {
                        MelonLogger.Msg($"识别到在线模式，强制大打已禁用");
                        MelonLogger.Msg($"Detected online mode, force big hit patch has been disabled");
                    }
                    else
                    {
                        MelonLogger.Msg($"识别到非在线模式，强制大打已启用");
                        MelonLogger.Msg($"Detected non-online mode, force big hit patch has been enabled");
                    }
                }
            }
        }


        [HarmonyPatch(typeof(BootImage), "PlayAsync", new Type[] { typeof(float), typeof(bool) })]
        public class ShortenBootImagePatch
        {
            static void Prefix(BootImage __instance, ref float duration, ref bool skippable)
            {
                duration = 0f;
                skippable = true;
                MelonLogger.Msg("已强制缩短启动画面时间并强制允许跳过");
            }
        }
    }
}
