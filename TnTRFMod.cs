using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TnTRFMod;

public class TnTRFMod : MelonMod
{
    private static TnTRFMod? instance;

    private ControllerManager? _controllerManager;
    public MelonPreferences_Entry<Vector3> donChanRotation;
    public MelonPreferences_Entry<bool> enableBetterBigHitPatch;
    public MelonPreferences_Entry<bool> enableRotatingDonChanPatch;
    public MelonPreferences_Entry<bool> enableSkipBootScreenPatch;
    public MelonPreferences_Category modSettingsCategory;
    public static TnTRFMod Instance => instance!;

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        instance = this;
        LoggerInstance.Msg("TnTRFMod has started!");

        modSettingsCategory = MelonPreferences.CreateCategory("TnTRFMod");
        modSettingsCategory.SetFilePath("UserData/TnTRFMod.cfg");
        enableSkipBootScreenPatch = modSettingsCategory.CreateEntry(
            "EnableSkipBootScreenPatch",
            true,
            "EnableSkipBootScreenPatch",
            "Whether to enable Skip Boot Screen Patch"
        );
        enableBetterBigHitPatch = modSettingsCategory.CreateEntry(
            "EnableBetterBigHitPatch",
            true,
            "EnableBetterBigHitPatch",
            "Whether to enable better Big Hit Patch, which will treat one side hit as a big hit."
        );
        enableRotatingDonChanPatch = modSettingsCategory.CreateEntry(
            "EnableRotatingDonChanPatch",
            false,
            "EnableRotatingDonChanPatch",
            "Whether to enable a rotating don-chan model patch."
        );

        modSettingsCategory.LoadFromFile();
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        modSettingsCategory.SaveToFile();
    }

    private void DrawGUI()
    {
        // TODO: 增加可配置信息
    }

    private ControllerManager getControllerManager()
    {
        if (_controllerManager == null)
            _controllerManager = GameObject.Find("ControllerManager").GetComponent<ControllerManager>();
        return _controllerManager!;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        ControllerManager.GetKeyboard(out var keyboard);

        if (enableRotatingDonChanPatch.Value)
        {
            var rot = donChanRotation.Value;
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        base.OnSceneWasLoaded(buildIndex, sceneName);

        if (sceneName == "MyRoom") // TODO: 增加可配置的信息
            MelonEvents.OnGUI.Subscribe(DrawGUI, 100);
        else
            MelonEvents.OnGUI.Unsubscribe(DrawGUI);
    }
}