using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class ScrollContainerUi : BaseUi
{
    private readonly GameObject _container;
    private readonly ScrollRect _scrollRect;
    private readonly GameObject _viewport;

    public ScrollContainerUi()
    {
        _scrollRect = _go.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.scrollSensitivity = 32;

        _viewport = new GameObject("Viewport");
        _viewport.transform.SetParent(_go.transform);
        var viewportRect = _viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        var viewportMask = _viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        _scrollRect.viewport = viewportRect;
        var viewportImage = _viewport.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        viewportImage.maskable = true;

        _container = new GameObject("Container");
        _container.transform.SetParent(_viewport.transform);
        var containerRect = _container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        _scrollRect.content = containerRect;
        var layoutGroup = _container.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        var fitter = _container.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        set => _transform.sizeDelta = value;
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
    }
}