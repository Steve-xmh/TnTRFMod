namespace TnTRFMod.Scenes;

public interface IScene
{
    public string SceneName { get; }
    public bool LowLatencyMode => false;

    public void Init()
    {
    }

    public void Start()
    {
    }

    public void Update()
    {
    }

    public void Destroy()
    {
    }
}