namespace TnTRFMod.Ui.Utils;

public interface ILayoutable
{
    void ChangeLayoutMode(LayoutConfig? layoutConfig = null, AutoSizeConfig? autoSizeFitter = null);
}