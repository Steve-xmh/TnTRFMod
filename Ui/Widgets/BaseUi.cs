using UnityEngine;

namespace TnTRFMod.Ui.Widgets;

public class BaseUi
{
    internal readonly GameObject _go;
    internal readonly RectTransform _transform;

    protected BaseUi()
    {
        _go = new GameObject("BaseUi");
        _transform = _go.AddComponent<RectTransform>();
        _transform.parent = Common.GetDrawCanvas();
        _transform.pivot = new Vector2(0, 1);
        _go.layer = LayerMask.NameToLayer("UI");
        _transform.transform.position =
            new Vector3(_transform.transform.position.x, _transform.transform.position.y, 90f);
    }

    public Vector2 Position
    {
        get
        {
            var pos = _transform.anchoredPosition;
            return new Vector2(pos.x + Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - pos.y);
        }
        set => _transform.anchoredPosition =
            new Vector2(value.x - Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - value.y);
    }

    public Vector2 Size
    {
        get => _transform.sizeDelta;
        set => _transform.sizeDelta = value;
    }

    public void AddChild(GameObject child)
    {
        child.transform.SetParent(_transform);
    }

    public void AddChild(BaseUi child)
    {
        child._transform.SetParent(_transform);
    }
}