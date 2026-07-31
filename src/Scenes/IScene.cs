namespace TnTRFMod.Scenes;

public interface IScene
{
    string SceneName { get; }
    bool LowLatencyMode => false;

    void Init()
    {
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void Destroy()
    {
    }
}