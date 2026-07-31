using UnityEngine;
using Object = UnityEngine.Object;

namespace TnTRFMod.Ui.Widgets;

public class BaseUi : IDisposable
{
    protected static Sprite baseUiSprite;

    public readonly GameObject _go;
    public readonly RectTransform _transform;

    protected BaseUi()
    {
        if (!baseUiSprite)
        {
            var baseUiTexture = TextureManager.LoadTexture(TextureManager.Textures.UiBase);
            baseUiSprite = Sprite.Create(baseUiTexture, new Rect(0, 0, baseUiTexture.width, baseUiTexture.height),
                new Vector2(0.5f, 0.5f), 1f, 0,
                SpriteMeshType.Tight, new Vector4(15f, 15f, 15f, 15f));
            baseUiSprite.name = "BaseUiSprite";
        }

        _go = new GameObject(GetType().Name);
        _transform = _go.AddComponent<RectTransform>();
        _transform.SetParent(Common.GetDrawCanvasForScene(), false);
        _transform.pivot = new Vector2(0, 1);
        _transform.anchorMin = new Vector2(0, 1);
        _transform.anchorMax = new Vector2(0, 1);
        _go.layer = LayerMask.NameToLayer("UI");
        _transform.localScale = Vector3.one;
        _transform.localRotation = Quaternion.identity;
        _transform.anchoredPosition3D = Vector3.zero;
    }

    private bool IsChildOfCanvas => _transform.parent == Common.GetDrawCanvasForScene() ||
                                    _transform.parent == Common.GetDrawCanvasNoDestroyForScene();

    public Vector2 Position
    {
        get
        {
            var pos = _transform.anchoredPosition;
            return new Vector2(pos.x, -pos.y);
        }
        set => _transform.anchoredPosition = new Vector2(value.x, -value.y);
    }

    public Vector2 Size
    {
        get => _transform.sizeDelta;
        set => _transform.sizeDelta = value;
    }

    public string Name
    {
        get => _go.name;
        set => _go.name = value;
    }

    public bool Visible
    {
        get => _go.activeSelf;
        set => _go.SetActive(value);
    }

    public BaseUi Parent
    {
        set => value.AddChild(this);
    }

    public virtual Vector2 PreferredSize => new(100f, 100f);

    public void Dispose()
    {
        Object.Destroy(_go);
    }

    public void SetActive(bool active)
    {
        _go.SetActive(active);
    }

    /// <summary>
    /// 使用游戏自带的 MouseOverable，在悬停期间切换为游戏的手型光标。
    /// </summary>
    protected void EnableGamePointerCursor()
    {
        if (_go.GetComponent<MouseOverable>() == null)
            _go.AddComponent<MouseOverable>();
    }

    public void MoveToNoDestroyCanvas()
    {
        var originalPos = Position;
        _transform.SetParent(Common.GetDrawCanvasNoDestroyForScene(), false);
        _transform.localScale = Vector3.one;
        Position = originalPos;
    }

    public void SetParent(GameObject parent)
    {
        _transform.SetParent(parent.transform, false);
    }

    public void AddChild(GameObject child)
    {
        child.transform.SetParent(_transform, false);
    }

    public void AddChild(BaseUi child)
    {
        child._transform.SetParent(_transform, false);
    }

    private class TempDisableInputComponent : MonoBehaviour
    {
        private static int _inputDisableCount;

        private void OnMouseEnter()
        {
            DisableGameInput();
        }

        private void OnMouseExit()
        {
            EnableGameInput();
        }

        private static void DisableGameInput()
        {
            if (_inputDisableCount == 0) Common.GetControllerManager().SetActiveSafe(false);

            _inputDisableCount++;
        }

        private static void EnableGameInput()
        {
            _inputDisableCount = Math.Max(0, _inputDisableCount - 1);
            if (_inputDisableCount != 0) return;
            Common.GetControllerManager().SetActiveSafe(true);
        }
    }
}