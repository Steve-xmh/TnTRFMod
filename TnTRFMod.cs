using Il2Cpp;
using MelonLoader;
using TnTRFMod.Ui.Scenes;
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod;

public class TnTRFMod : MelonMod
{
    private ControllerManager? _controllerManager;
    public MelonPreferences_Entry<Vector3> donChanRotation;
    public MelonPreferences_Entry<bool> enableBetterBigHitPatch;
    public MelonPreferences_Entry<bool> enableRotatingDonChanPatch;
    public MelonPreferences_Entry<bool> enableSkipBootScreenPatch;
    public MelonPreferences_Category modSettingsCategory;
    private string sceneName = "";
    public static TnTRFMod Instance { get; private set; }

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        Instance = this;
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
        donChanRotation = modSettingsCategory.CreateEntry(
            "DonChanRotation",
            new Vector3(0, 0, 0),
            "DonChanRotation",
            "The rotation speed and angle of the don-chan model."
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
    }

    private ControllerManager getControllerManager()
    {
        if (_controllerManager == null)
            _controllerManager = GameObject.Find("ControllerManager").GetComponent<ControllerManager>();
        return _controllerManager!;
    }

    public override void OnUpdate()
    {
        if (enableRotatingDonChanPatch.Value)
        {
            var rot = donChanRotation.Value;
            var objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "DonModels");
            foreach (var obj in objects)
            {
                var donModels = obj.transform;
                donModels.Rotate(rot * Time.deltaTime);
            }
        }

        if (sceneName == "DressUp") DressUpModScene.OnUpdate();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        this.sceneName = sceneName;

        if (sceneName == "DressUp") DressUpModScene.Setup();

        if (sceneName == "Title")
            _ = new TextUi
            {
                Text = $"TnTRFMod v{Info.Version}",
                Position = new Vector2(32f, 32f)
            };
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        if (sceneName == "DressUp") DressUpModScene.OnUnload();
    }
}