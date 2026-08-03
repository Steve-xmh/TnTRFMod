using System.Globalization;
using TnTRFMod.Config;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TnTRFMod.Ui;

/// <summary>
/// 跨场景持久化的模组配置面板。控件只创建一次，F8 切换时仅同步值和可见性。
/// </summary>
public static class ModSettingsScreenUi
{
    private const float PanelWidth = 760f;
    private const float PanelHeight = 1016f;
    private const float Margin = 32f;
    private const float ContentWidth = PanelWidth - 48f;
    private const float RowWidth = ContentWidth - 30f;
    private const float EditorWidth = 220f;
    private const float RowPadding = 16f;
    private const float BadgeHeight = 24f;
    private const float BadgeEditorSpacing = 8f;
    private const float EditorHeight = 50f;
    private const float ConfigKeyHeight = 22f;
    private const float DescriptionKeySpacing = 4f;
    private const float MinimumRowHeight = RowPadding * 2f + BadgeHeight + BadgeEditorSpacing + EditorHeight;

    private static readonly Color PanelColor = new(0.055f, 0.065f, 0.09f, 0.97f);
    private static readonly Color RowColor = new(0.105f, 0.12f, 0.16f, 0.98f);
    private static readonly Color ActiveColor = new(0.16f, 0.55f, 0.32f, 1f);
    private static readonly Color InactiveColor = new(0.24f, 0.27f, 0.34f, 1f);
    private static readonly Color RestartColor = new(0.72f, 0.42f, 0.12f, 1f);

    private static FrameUi? _panel;
    private static TextUi? _statusText;
    private static ButtonUi? _tabSceneButton;
    private static ButtonUi? _tabAllButton;
    private static ScrollContainerUi? _scrollContainer;
    private static readonly List<SettingRow> Rows = [];
    private static bool _visible;
    private static bool _showSceneItems = true;
    private static ConfigItemMetadata? _waitingForKey;
    private static ButtonUi? _waitingKeyButton;

    public static bool Visible => _visible;

    public static void Init()
    {
        if (_panel != null) return;
        BuildPanel();
        CloseSettings();
    }

    public static void OpenSettings()
    {
        if (_panel == null) return;
        RefreshRows();
        _visible = true;
        _panel!.Visible = true;
    }

    public static void CloseSettings()
    {
        _visible = false;
        CancelKeyCapture();
        CloseAllDropDowns();
        if (_panel != null) _panel.Visible = false;
    }

    public static void ToggleSettings()
    {
        if (_visible) CloseSettings();
        else OpenSettings();
    }

    public static void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (_panel != null && keyboard.f8Key.wasPressedThisFrame)
        {
            ToggleSettings();
            return;
        }

        if (!_visible || _waitingForKey == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelKeyCapture();
            SetStatus(I18n.Get("modSettings.keyBindingCancelled").Text, false);
            return;
        }

        foreach (var keyControl in keyboard.allKeys)
        {
            if (!keyControl.wasPressedThisFrame || keyControl.keyCode is Key.F8 or Key.Escape) continue;
            _waitingForKey.SetValue(keyControl.keyCode);
            _waitingKeyButton!.Text = keyControl.keyCode.ToString();
            SetStatus(GetApplyMessage(_waitingForKey), !_waitingForKey.HotReloadable);
            _waitingForKey = null;
            _waitingKeyButton = null;
            break;
        }
    }

    private static void BuildPanel()
    {
        _panel = new FrameUi
        {
            Name = "TnTRFModSettingsPanel",
            Size = new Vector2(PanelWidth, PanelHeight),
            Position = new Vector2(Common.ScreenWidth - PanelWidth - Margin, Margin),
            FrameColor = PanelColor
        };
        _panel.MoveToNoDestroyCanvas();

        var title = CreateText(I18n.Get("modSettings.title").Text, 32f, Color.white, false);
        AddToPanel(title, new Vector2(24f, 18f), new Vector2(430f, 46f));

        var hint = CreateText(I18n.Get("modSettings.hint").Text, 19f, new Color(0.72f, 0.76f, 0.84f), false);
        AddToPanel(hint, new Vector2(24f, 62f), new Vector2(520f, 30f));

        var close = new ButtonUi
        {
            Text = I18n.Get("modSettings.close").Text,
            Size = new Vector2(126f, 48f),
            ButtonColor = InactiveColor
        };
        _panel.AddChild(close);
        close.Position = new Vector2(PanelWidth - 150f, 22f);
        close.AddListener((Action)CloseSettings);

        _tabSceneButton = new ButtonUi
        {
            Text = I18n.Get("modSettings.currentScene").Text,
            Size = new Vector2(180f, 46f)
        };
        _panel.AddChild(_tabSceneButton);
        _tabSceneButton.Position = new Vector2(24f, 104f);
        _tabSceneButton.AddListener((Action)(() => SwitchTab(true)));

        _tabAllButton = new ButtonUi
        {
            Text = I18n.Get("modSettings.allSettings").Text,
            Size = new Vector2(180f, 46f)
        };
        _panel.AddChild(_tabAllButton);
        _tabAllButton.Position = new Vector2(216f, 104f);
        _tabAllButton.AddListener((Action)(() => SwitchTab(false)));

        _statusText = CreateText(I18n.Get("modSettings.legend").Text, 18f,
            new Color(0.72f, 0.78f, 0.86f), false);
        AddToPanel(_statusText, new Vector2(24f, 158f), new Vector2(ContentWidth, 30f));

        _scrollContainer = new ScrollContainerUi
        {
            Size = new Vector2(ContentWidth, PanelHeight - 214f),
            Color = new Color(0.035f, 0.04f, 0.06f, 0.96f)
        };
        _panel.AddChild(_scrollContainer);
        _scrollContainer._transform.anchorMin = new Vector2(0f, 1f);
        _scrollContainer._transform.anchorMax = new Vector2(0f, 1f);
        _scrollContainer._transform.pivot = new Vector2(0f, 1f);
        _scrollContainer.Position = new Vector2(24f, 198f);

        BuildRows();
        SwitchTab(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel._transform);
    }

    private static void BuildRows()
    {
        foreach (var item in ModConfig.AllItems)
        {
            var description = I18n.Get(item.DescriptionKey).Text;
            var descriptionText = CreateText(description, 20f, Color.white, true);
            descriptionText.Size = new Vector2(RowWidth - EditorWidth - RowPadding * 3f, 100f);
            var measured = descriptionText.GetPreferredSize(descriptionText.Size.x);
            var textColumnHeight = measured.y + DescriptionKeySpacing + ConfigKeyHeight;
            var rowHeight = Math.Max(MinimumRowHeight, textColumnHeight + RowPadding * 2f);

            var row = new FrameUi
            {
                Name = $"Setting_{item.Section}_{item.KeyName}",
                Size = new Vector2(RowWidth, rowHeight),
                FrameColor = RowColor
            };
            _scrollContainer!.AddChild(row);

            row.AddChild(descriptionText);
            descriptionText.Position = new Vector2(RowPadding, RowPadding);
            descriptionText.Size = new Vector2(descriptionText.Size.x, measured.y);

            var configKeyText = CreateText(I18n.Get("modSettings.configKey", item.CategoryKey).Text, 15f,
                new Color(0.58f, 0.64f, 0.73f), false);
            row.AddChild(configKeyText);
            configKeyText.Position = new Vector2(RowPadding, RowPadding + measured.y + DescriptionKeySpacing);
            configKeyText.Size = new Vector2(descriptionText.Size.x, ConfigKeyHeight);

            var badge = CreateText(I18n.Get(item.HotReloadable
                    ? "modSettings.hotReloadable"
                    : "modSettings.restartRequired").Text, 16f,
                item.HotReloadable ? new Color(0.45f, 0.95f, 0.62f) : new Color(1f, 0.72f, 0.3f), false);
            row.AddChild(badge);
            badge.Position = new Vector2(RowWidth - EditorWidth - RowPadding, RowPadding);
            badge.Size = new Vector2(EditorWidth, BadgeHeight);

            var settingRow = new SettingRow(item, row);
            CreateEditor(settingRow, rowHeight);
            Rows.Add(settingRow);
        }
    }

    private static void CreateEditor(SettingRow row, float rowHeight)
    {
        var editorPosition = new Vector2(RowWidth - EditorWidth - RowPadding, rowHeight - EditorHeight - RowPadding);
        if (row.Metadata.Type == ConfigItemType.Bool)
        {
            var button = new ButtonUi { Size = new Vector2(EditorWidth, EditorHeight) };
            row.Container.AddChild(button);
            button.Position = editorPosition;
            button.AddListener(() =>
            {
                var value = !Convert.ToBoolean(row.Metadata.GetValue());
                row.Metadata.SetValue(value);
                UpdateBoolButton(button, value);
                SetStatus(GetApplyMessage(row.Metadata), !row.Metadata.HotReloadable);
            });
            row.BoolButton = button;
            return;
        }

        if (row.Metadata.Type == ConfigItemType.KeyBinding)
        {
            var button = new ButtonUi { Size = new Vector2(EditorWidth, EditorHeight), ButtonColor = InactiveColor };
            row.Container.AddChild(button);
            button.Position = editorPosition;
            button.AddListener(() =>
            {
                CancelKeyCapture();
                _waitingForKey = row.Metadata;
                _waitingKeyButton = button;
                button.Text = I18n.Get("modSettings.pressKey").Text;
                button.ButtonColor = ActiveColor;
            });
            row.KeyButton = button;
            return;
        }

        if (row.Metadata.Options.Length > 0)
        {
            var currentValue = Convert.ToString(row.Metadata.GetValue()) ?? string.Empty;
            var select = new SelectUi<string>(currentValue)
            {
                Size = new Vector2(EditorWidth, EditorHeight),
                ButtonColor = InactiveColor,
                Items = row.Metadata.Options.Select(option => new SelectUi<string>.SelectItem
                {
                    Value = Convert.ToString(option.Value) ?? string.Empty,
                    Text = I18n.Get(option.LabelKey),
                    ButtonColor = InactiveColor
                }).ToArray()
            };
            row.Container.AddChild(select);
            select.Position = editorPosition;
            select.AddOnValueChangedListener(value =>
            {
                row.Metadata.SetValue(value);
                SetStatus(GetApplyMessage(row.Metadata), !row.Metadata.HotReloadable);
            });
            row.Select = select;
            return;
        }

        var field = new TextFieldUi
        {
            Size = new Vector2(EditorWidth, EditorHeight),
            BackgroundColor = new Color(0.88f, 0.9f, 0.94f),
            // TextColor = Color.black,
            Placeholder = I18n.Get(row.Metadata.Type == ConfigItemType.String
                ? "modSettings.textPlaceholder"
                : "modSettings.numberPlaceholder").Text
        };
        row.Container.AddChild(field);
        field.Position = editorPosition;
        field.AddOnEndEditListener(value => ApplyTextValue(row.Metadata, field, value));
        row.TextField = field;
    }

    private static void ApplyTextValue(ConfigItemMetadata metadata, TextFieldUi field, string text)
    {
        try
        {
            object value = metadata.Type switch
            {
                ConfigItemType.Int => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
                ConfigItemType.UInt => uint.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
                ConfigItemType.Float => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                ConfigItemType.Double => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                _ => text
            };
            metadata.SetValue(value);
            field.Value = FormatValue(metadata.GetValue());
            SetStatus(GetApplyMessage(metadata), !metadata.HotReloadable);
        }
        catch (Exception)
        {
            field.Value = FormatValue(metadata.GetValue());
            SetStatus(I18n.Get("modSettings.invalidInput").Text, true);
        }
    }

    private static void SwitchTab(bool sceneItems)
    {
        _showSceneItems = sceneItems;
        _tabSceneButton!.ButtonColor = sceneItems ? ActiveColor : InactiveColor;
        _tabAllButton!.ButtonColor = sceneItems ? InactiveColor : ActiveColor;
        RefreshRows();
    }

    private static void RefreshRows()
    {
        if (_scrollContainer == null) return;
        CloseAllDropDowns();
        var sceneName = TnTrfMod.Instance?.GetSceneName() ?? string.Empty;
        foreach (var row in Rows)
        {
            row.Container.Visible = !_showSceneItems || row.Metadata.RelevantScenes.Length == 0 ||
                                    row.Metadata.RelevantScenes.Contains(sceneName, StringComparer.Ordinal);
            if (!row.Container.Visible) continue;

            if (row.BoolButton != null)
                UpdateBoolButton(row.BoolButton, Convert.ToBoolean(row.Metadata.GetValue()));
            else if (row.KeyButton != null)
                row.KeyButton.Text = row.Metadata.GetValue().ToString();
            else if (row.Select != null)
                row.Select.SetValue(Convert.ToString(row.Metadata.GetValue()) ?? string.Empty);
            else if (row.TextField != null)
                row.TextField.Value = FormatValue(row.Metadata.GetValue());
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContainer._transform);
    }

    private static void UpdateBoolButton(ButtonUi button, bool value)
    {
        button.Text = I18n.Get(value ? "modSettings.enabled" : "modSettings.disabled").Text;
        button.ButtonColor = value ? ActiveColor : InactiveColor;
    }

    private static string FormatValue(object value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }

    private static string GetApplyMessage(ConfigItemMetadata metadata)
    {
        return I18n.Get(metadata.HotReloadable
            ? "modSettings.savedHotReloadable"
            : "modSettings.savedRestartRequired").Text;
    }

    private static void SetStatus(string text, bool warning)
    {
        if (_statusText == null) return;
        _statusText.Text = text;
        _statusText.Color = warning ? new Color(1f, 0.7f, 0.28f) : new Color(0.45f, 0.95f, 0.62f);
    }

    private static void CloseAllDropDowns()
    {
        foreach (var row in Rows)
            row.Select?.CloseDropDown();
    }

    private static void CancelKeyCapture()
    {
        if (_waitingKeyButton != null && _waitingForKey != null)
        {
            _waitingKeyButton.Text = _waitingForKey.GetValue().ToString();
            _waitingKeyButton.ButtonColor = InactiveColor;
        }

        _waitingForKey = null;
        _waitingKeyButton = null;
    }

    private static TextUi CreateText(string text, float fontSize, Color color, bool wordWrap)
    {
        return new TextUi
        {
            Text = text,
            FontSize = fontSize,
            Color = color,
            WordWrap = wordWrap
        };
    }

    private static void AddToPanel(BaseUi child, Vector2 position, Vector2 size)
    {
        _panel!.AddChild(child);
        child.Position = position;
        child.Size = size;
    }

    private sealed class SettingRow(ConfigItemMetadata metadata, FrameUi container)
    {
        public ConfigItemMetadata Metadata { get; } = metadata;
        public FrameUi Container { get; } = container;
        public ButtonUi? BoolButton { get; set; }
        public ButtonUi? KeyButton { get; set; }
        public SelectUi<string>? Select { get; set; }
        public TextFieldUi? TextField { get; set; }
    }
}
