using TnTRFMod.Utils;
using UnityEngine;

namespace TnTRFMod.Ui.Widgets;

public class SelectUi<T> : ButtonUi
{
    private readonly ScrollContainerUi _dropDownContainer;
    private readonly List<SelectItem> _items = [];
    private readonly List<BaseUi> _itemUis = [];

    public SelectUi(T defaultValue)
    {
        Value = defaultValue;
        _dropDownContainer = new ScrollContainerUi
        {
            Visible = false
        };
        UpdateDropDownContainerRect();
        AddListener(() =>
        {
            _dropDownContainer.Visible = !_dropDownContainer.Visible;
            _dropDownContainer.Color = ButtonColor;
        });
    }

    public T Value { get; set; }

    public new Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            UpdateDropDownContainerRect();
        }
    }

    public new Vector2 Position
    {
        get => base.Position;
        set
        {
            base.Position = value;
            UpdateDropDownContainerRect();
        }
    }

    public SelectItem[] Items
    {
        get => _items.ToArray();
        set
        {
            _items.Clear();
            _items.AddRange(value);
            RebuildDropDown();
            var selectedItem = _items.FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item.Value, Value));
            if (selectedItem.Text.Text != null)
            {
                I18nText = selectedItem.Text;
                ButtonColor = selectedItem.ButtonColor ?? Color.white;
            }
            else
            {
                Text = string.Empty;
                Value = default;
                ButtonColor = Color.white;
            }
        }
    }

    private void UpdateDropDownContainerRect()
    {
        _dropDownContainer._transform.position = _transform.position;
        _dropDownContainer.Size = new Vector2(Size.x, Size.y * 5f);
    }

    private void RebuildDropDown()
    {
        foreach (var itemUi in _itemUis)
            itemUi.Dispose();
        _itemUis.Clear();
        foreach (var item in _items)
        {
            var itemUi = new ButtonUi
            {
                I18nText = item.Text,
                Size = new Vector2(Size.x, 50),
                ButtonColor = item.ButtonColor ?? Color.white
            };
            _dropDownContainer.AddChild(itemUi);
            itemUi.AddListener(() =>
            {
                Value = item.Value;
                I18nText = item.Text;
                ButtonColor = item.ButtonColor ?? Color.white;
                _dropDownContainer.Visible = false;
            });
        }
    }

    public struct SelectItem
    {
        public T Value;
        public I18n.I18nResult Text;
        public Color? ButtonColor;
    }
}