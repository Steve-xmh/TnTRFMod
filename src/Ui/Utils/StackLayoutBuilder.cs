using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Utils;

public class StackLayoutBuilder
{
    private readonly List<BaseUi> _children = [];
    private readonly BaseUi _container;

    private readonly LayoutMode _layoutMode;

    public StackLayoutBuilder(LayoutMode layoutMode, BaseUi container)
    {
        if (layoutMode == LayoutMode.None)
            throw new ArgumentException("LayoutMode must be None to use LayoutMode.None");

        _layoutMode = layoutMode;
        _container = container;
    }

    /// <summary>使用 x/y/width/height 分别表示左/上/右/下内边距。</summary>
    public Rect Padding { get; set; } = new();
    public float Spacing { get; set; }
    public CrossAxisAlign CrossAxisAlign { get; set; } = CrossAxisAlign.Start;

    public void AddChild(BaseUi child)
    {
        _children.Add(child);
    }

    public void Build()
    {
        if (_children.Count == 0)
        {
            _container.Size = new Vector2(Padding.x + Padding.width, Padding.y + Padding.height);
            return;
        }

        var preferredSizes = _children.Select(child => child.PreferredSize).ToArray();
        var crossSize = _layoutMode == LayoutMode.Horizontal
            ? preferredSizes.Max(size => size.y)
            : preferredSizes.Max(size => size.x);
        var mainSize = preferredSizes.Sum(size => _layoutMode == LayoutMode.Horizontal ? size.x : size.y) +
                       Spacing * (_children.Count - 1);

        _container.Size = _layoutMode == LayoutMode.Horizontal
            ? new Vector2(Padding.x + mainSize + Padding.width, Padding.y + crossSize + Padding.height)
            : new Vector2(Padding.x + crossSize + Padding.width, Padding.y + mainSize + Padding.height);

        var cursor = _layoutMode == LayoutMode.Horizontal ? Padding.x : Padding.y;
        for (var index = 0; index < _children.Count; index++)
        {
            var child = _children[index];
            var size = preferredSizes[index];
            var childCrossSize = _layoutMode == LayoutMode.Horizontal ? size.y : size.x;
            var crossOffset = CrossAxisAlign switch
            {
                CrossAxisAlign.Center => (crossSize - childCrossSize) * 0.5f,
                CrossAxisAlign.End => crossSize - childCrossSize,
                _ => 0f
            };

            if (CrossAxisAlign == CrossAxisAlign.Stretch)
            {
                if (_layoutMode == LayoutMode.Horizontal) size.y = crossSize;
                else size.x = crossSize;
            }

            child.Size = size;
            child.Position = _layoutMode == LayoutMode.Horizontal
                ? new Vector2(cursor, Padding.y + crossOffset)
                : new Vector2(Padding.x + crossOffset, cursor);
            cursor += (_layoutMode == LayoutMode.Horizontal ? size.x : size.y) + Spacing;
        }
    }
}