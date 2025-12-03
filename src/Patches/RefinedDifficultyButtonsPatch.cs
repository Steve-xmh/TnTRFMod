using HarmonyLib;
using Scripts.Common;
using Scripts.OutGame.SongSelect;
using Scripts.OutGame.SongSelect.DiffSetting;
using Scripts.OutGame.Training;
using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class RefinedDifficultyButtonsPatch
{
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(UiSongButtonDifficulty))]
    [HarmonyPatch(nameof(UiSongButtonDifficulty.Setup))]
    [HarmonyPostfix]
    public static void UiSongButtonDifficulty_Setup_Postfix(UiSongButtonDifficulty __instance,
        ref MusicDataInterface.MusicInfoAccesser item)
    {
        var diff = item.Stars[(int)__instance.difficulty];
        if (diff == 0)
        {
            var gray = new Color(0.5f, 0.5f, 0.5f);
            __instance.number.tmpro.SetText("");
            __instance.transform.FindChild("Background").gameObject.GetComponent<Image>().color = gray;
            var uiText = __instance.transform.FindChild("Text").gameObject.GetComponent<UiText>();
            // var c = new Color(0.5f, 0.5f, 1f, 0.5f);
            var isRef = false;
            uiText.SetUnderlayColor(ref gray, ref isRef);
            uiText.Refresh();
        }
        else
        {
            var white = Color.white;
            __instance.transform.FindChild("Background").gameObject.GetComponent<Image>().color = white;
            var uiText = __instance.transform.FindChild("Text").gameObject.GetComponent<UiText>();
            var isRef = false;
            uiText.SetUnderlayColor(ref white, ref isRef);
            uiText.Refresh();
        }

        if (diff > 10)
        {
            var uiText = __instance.transform.FindChild("Text").gameObject.GetComponent<UiText>();
            uiText.tmpro.SetText("{0}", diff);
        }
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(UiDiffPanel))]
    [HarmonyPatch(nameof(UiDiffPanel.SetData))]
    [HarmonyPostfix]
    public static void UiDiffPanel_SetData_Postfix(UiDiffPanel __instance,
        DiffPanel model)
    {
        if (model.Level == 0)
        {
            var gray = new Color(0.5f, 0.5f, 0.5f);
            __instance.largePanel.diffNum.SetText("");
            __instance.smallPanel.diffNum.SetText("");
            __instance.largePanel.gameObject.transform.FindChild("Back").gameObject.GetComponent<Image>().color = gray;
            __instance.smallPanel.gameObject.transform.FindChild("Back").gameObject.GetComponent<Image>().color = gray;
            var uiText = __instance.largePanel.gameObject.transform.FindChild("DiffName").gameObject
                .GetComponent<UiText>();
            uiText.SetFaceColor(ref gray);
            uiText.Refresh();
            uiText = __instance.smallPanel.gameObject.transform.FindChild("DiffName").gameObject.GetComponent<UiText>();
            uiText.SetFaceColor(ref gray);
            uiText.Refresh();
        }
        else
        {
            var white = Color.white;
            __instance.largePanel.gameObject.transform.FindChild("Back").gameObject.GetComponent<Image>().color = white;
            __instance.smallPanel.gameObject.transform.FindChild("Back").gameObject.GetComponent<Image>().color = white;
            var uiText = __instance.largePanel.gameObject.transform.FindChild("DiffName").gameObject
                .GetComponent<UiText>();
            uiText.SetFaceColor(ref white);
            uiText.Refresh();
            uiText = __instance.smallPanel.gameObject.transform.FindChild("DiffName").gameObject.GetComponent<UiText>();
            uiText.SetFaceColor(ref white);
            uiText.Refresh();
        }

        if (model.Level > 10)
        {
            var uiText = __instance.largePanel.gameObject.transform.FindChild("DiffName").gameObject
                .GetComponent<UiText>();
            uiText.tmpro.SetText("{0}", model.Level);
            uiText = __instance.smallPanel.gameObject.transform.FindChild("DiffName").gameObject.GetComponent<UiText>();
            uiText.tmpro.SetText("{0}", model.Level);
        }
    }

    private static SongSelectSceneUiControllerBase GetUiSongControllerBase()
    {
        var sceneName = TnTrfMod.Instance.GetSceneName();
        if (sceneName == "SongSelectTrainingFree")
        {
            var sceneObj = GameObject.Find("SongSelectTrainingFreeSceneObjects")
                .GetComponent<SongSelectTrainingFreeSceneObjects>();
            return sceneObj.UiController!;
        }
        else
        {
            var sceneObj = GameObject.Find("SongSelectSceneObjects")
                .GetComponent<SongSelectThunderShrineSceneObjects>();
            return sceneObj.UiController!;
        }
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(DiffSelectBase))]
    [HarmonyPatch(nameof(DiffSelectBase.Decision))]
    [HarmonyPrefix]
    public static bool DiffSelectBase_Decision_Prefix(DiffSelectBase __instance)
    {
        var uiSongControllerBase = GetUiSongControllerBase();

        try
        {
            switch (uiSongControllerBase)
            {
                case SongSelectSceneUiController uiController
                    when uiController.selectedSong.Stars[(int)uiController.diffSelect.GetHightlightedType()] != 0:
                    return true;
                case SongSelectSceneUiController uiController:
                {
                    for (var i = 0; i < uiController.UiDiffSelect1P.obis.Count; i++)
                        uiController.UiDiffSelect1P.obis._items[i].obiIcon.enabled = false;
                    for (var i = 0; i < uiController.UiDiffSelect1P.obis.Count; i++)
                        uiController.UiDiffSelect2P.obis._items[i].obiIcon.enabled = false;
                    break;
                }
                case SongSelectTrainingFreeUiController trainUiController
                    when trainUiController.selectedSong.Stars
                        [(int)trainUiController.diffSelect.GetHightlightedType()] != 0:
                    return true;
                case SongSelectTrainingFreeUiController trainUiController:
                {
                    for (var i = 0; i < trainUiController.UiDiffSelect.obis.Count; i++)
                        trainUiController.UiDiffSelect.obis._items[i].obiIcon.enabled = false;
                    break;
                }
            }
        }
        catch
        {
        }

        __instance.SoundManager.PlayCommonSe(SoundLabel.SoundLabel.Common.buzz);

        return false;
    }
}