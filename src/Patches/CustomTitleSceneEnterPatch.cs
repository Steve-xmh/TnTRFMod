using HarmonyLib;
#if BEPINEX
using Cysharp.Threading.Tasks;
using Scripts.OutGame.Title;

#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
using Il2CppScripts.OutGame.Title;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class CustomTitleSceneEnterPatch
{
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(TitleSceneUiController))]
    [HarmonyPatch(nameof(TitleSceneUiController.OnDecision))]
    [HarmonyPrefix]
    private static bool TitleSceneUiController_OnDecision_Prefix(ref TitleSceneUiController __instance,
        ref UniTask __result)
    {
        // customTitleSceneEnterSceneName
        // SceneName.SongSelect
        // SceneName.TrainingEntrance
        // SceneName.SongSelectTraining
        // SceneName.SongSelectTrainingFree
        var sceneName = TnTrfMod.Instance.customTitleSceneEnterSceneName.Value;
        if (string.IsNullOrEmpty(sceneName))
            return true;

        CommonObjects.instance.MySceneManager.ChangeScene(sceneName);
        __result = UniTask.CompletedTask;
        return false;
    }
}