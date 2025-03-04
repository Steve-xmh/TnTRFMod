using UnityEngine;
using Object = UnityEngine.Object;

namespace TnTRFMod.Ui.Widgets;

public class BaseUi
{
    protected static Texture2D baseUiTexture;
    protected static Sprite baseUiSprite;

    internal readonly GameObject _go;
    internal readonly RectTransform _transform;

    protected BaseUi()
    {
        if (baseUiTexture == null)
        {
            baseUiTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            baseUiTexture.LoadImage(Resources.UiBase);
            baseUiTexture.filterMode = FilterMode.Point;
            baseUiSprite = Sprite.Create(baseUiTexture, new Rect(0, 0, baseUiTexture.width, baseUiTexture.height),
                new Vector2(0.5f, 0.5f), 1f, 0,
                SpriteMeshType.Tight, new Vector4(4f, 4f, 4f, 4f));
            baseUiSprite.name = "BaseUiSprite";
        }

        _go = new GameObject("BaseUi");
        _transform = _go.AddComponent<RectTransform>();
        _transform.SetParent(Common.GetDrawCanvasForScene());
        _transform.pivot = new Vector2(0, 1);
        _go.layer = LayerMask.NameToLayer("UI");
        _transform.transform.position =
            new Vector3(_transform.transform.position.x, _transform.transform.position.y, 90f);

        // _go.OnMouseEnterAsObservable().Subscribe(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Unit>>((Unit _) =>
        // {
        //     DisableGameInput();
        // }));
        //
        // _go.OnMouseExitAsObservable().Subscribe(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Unit>>((Unit _) =>
        // {
        //     EnableGameInput();
        // }));
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

    public bool Visible
    {
        get => _go.activeSelf;
        set => _go.SetActive(value);
    }

    public void Dispose()
    {
        _go.transform.SetParent(null);
        Object.Destroy(_go);
    }

    public void MoveToNoDestroyCanvas()
    {
        _transform.SetParent(Common.GetDrawCanvasNoDestroyForScene());
    }

    public void AddChild(GameObject child)
    {
        child.transform.SetParent(_transform);
    }

    public void AddChild(BaseUi child)
    {
        child._transform.SetParent(_transform);
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