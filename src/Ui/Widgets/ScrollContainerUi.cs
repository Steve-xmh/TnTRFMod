using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class ScrollContainerUi : BaseUi
{
    private readonly GameObject _container;
    private readonly RectTransform _containerRect;
    private readonly Image _image;
    private readonly ScrollRect _scrollRect;
    private readonly GameObject _viewport;
    private readonly RectTransform _viewportRect;

    public ScrollContainerUi()
    {
        _scrollRect = _go.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.scrollSensitivity = 32;

        _image = _go.AddComponent<Image>();
        _image.sprite = baseUiSprite;
        _image.type = Image.Type.Sliced;
        _image.pixelsPerUnitMultiplier = 100;

        _viewport = new GameObject("Viewport");
        _viewport.transform.SetParent(_go.transform);
        _viewportRect = _viewport.AddComponent<RectTransform>();
        _viewportRect.anchorMin = Vector2.zero;
        _viewportRect.anchorMax = Vector2.one;
        _viewportRect.offsetMin = new Vector2(15, 8);
        _viewportRect.offsetMax = new Vector2(-15, -8);
        var viewportMask = _viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        _scrollRect.viewport = _viewportRect;
        var viewportImage = _viewport.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        viewportImage.maskable = true;

        _container = new GameObject("Container");
        _container.transform.SetParent(_viewport.transform);
        _containerRect = _container.AddComponent<RectTransform>();
        _containerRect.anchorMin = Vector2.zero;
        _containerRect.anchorMax = Vector2.one;
        _scrollRect.content = _containerRect;
        var layoutGroup = _container.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 4;

        var fitter = _container.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _containerRect.offsetMin = Vector2.zero;
        _containerRect.offsetMax = Vector2.zero;
    }

    public new Vector2 Position
    {
        get
        {
            var pos = _transform.anchoredPosition;
            return new Vector2(pos.x + Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - pos.y);
        }
        set => _transform.anchoredPosition =
            new Vector2(value.x - Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - value.y);
    }

    public new Vector2 Size
    {
        get => _transform.sizeDelta;
        set
        {
            _transform.sizeDelta = value;
            _containerRect.sizeDelta =
                new Vector2(_viewportRect.sizeDelta.x - _viewportRect.offsetMin.x + _viewportRect.offsetMax.x, 0);
        }
    }

    public Color Color
    {
        get => _image.color;
        set => _image.color = value;
    }

    public new void AddChild(GameObject child)
    {
        child.transform.SetParent(_container.transform);
    }

    public new void AddChild(BaseUi child)
    {
        child._transform.SetParent(_container.transform);

        var fitter = _container.GetComponent<ContentSizeFitter>();
        fitter.enabled = false;
        fitter.enabled = true;
        _containerRect.offsetMin = Vector2.zero;
        _containerRect.offsetMax = Vector2.zero;
    }
}