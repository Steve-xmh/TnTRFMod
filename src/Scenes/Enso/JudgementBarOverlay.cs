using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if BEPINEX
using TMPro;
#elif MELONLOADER
using Il2CppTMPro;
#endif

namespace TnTRFMod.Scenes.Enso;

/// <summary>
/// A judgement timing error bar overlay inspired by YunYunJudge.
/// Displays colored timing window segments, tick marks for recent hits,
/// and triangles indicating short-term and long-term average offset.
/// </summary>
public class JudgementBarOverlay
{
    private const int MaxRecentHits = 50;
    private const float BarWidth = 576f;
    private const float BarHeight = 32f;
    private const float BottomPadding = 40f;
    private const float TickWidth = 2f;
    private const float TriangleSize = 12f;
    private const float BackgroundPadding = 6f;

    // Static queue for receiving hit offsets directly from EnsoGameBasePatch
    private static readonly Queue<float> _pendingOffsets = new();
    private static readonly object _pendingLock = new();

    /// <summary>
    /// Called from EnsoGameBasePatch.OnSimpleHit to feed exact per-hit offsets.
    /// </summary>
    public static void PushHitOffsetFromPatch(float offsetMs)
    {
        lock (_pendingLock)
        {
            _pendingOffsets.Enqueue(offsetMs);
        }
    }

    // Recent hit offsets (circular buffer)
    private readonly float[] _recentHits = new float[MaxRecentHits];
    private int _recentHitIndex;
    private int _recentHitCount;
    private float _recentHitSum;

    // All-session hit tracking
    private float _allHitSum;
    private int _allHitCount;

    // UI elements
    private GameObject _barRoot;
    private RectTransform _barRootTransform;

    // Segment images (left to right): Fuka, Ka, Ryo, Center, Ryo, Ka, Fuka
    private Image _segFukaLeft;
    private Image _segKaLeft;
    private Image _segRyoLeft;
    private Image _segCenter;
    private Image _segRyoRight;
    private Image _segKaRight;
    private Image _segFukaRight;

    // Background
    private Image _background;

    // Tick marks for recent hits
    private readonly Image[] _tickMarks = new Image[MaxRecentHits];

    // Average indicator triangles
    private RectTransform _avgRecentTriangle;
    private RectTransform _avgAllTriangle;

    // Average text
    private TextUi _avgText;

    // Timing range in ms (max displayable offset)
    private float TimeRangeMs => ModConfig.JudgementBarTimeRange.Value;

    private static Color ColorFuka => new(1f, 140f / 255f, 0f, 0.8f); // dark orange
    private static Color ColorKa => new(0f, 128f / 255f, 0f, 0.8f); // dark green
    private static Color ColorRyo => new(0f, 102f / 255f, 204f / 255f, 0.8f); // dark blue
    private static Color ColorCenter => new(1f, 1f, 1f, 0.9f); // white
    private static Color ColorBackground => new(0f, 0f, 0f, 0.75f);
    private static Color ColorTick => new(1f, 1f, 1f, 0.9f);
    private static Color ColorAvgRecent => new(1f, 0.55f, 0f, 1f); // orange
    private static Color ColorAvgAll => new(1f, 1f, 0f, 1f); // yellow

    public void Start()
    {
        ResetData();
        CreateBarUI();
    }

    public void Update()
    {
        // Reset with backtick key (same as YunYunJudge)
        if (Keyboard.current != null && Keyboard.current[Key.Backquote].wasPressedThisFrame)
        {
            ResetData();
        }

        // Drain all pending offsets from the patch callback
        lock (_pendingLock)
        {
            while (_pendingOffsets.Count > 0)
            {
                PushHitOffset(_pendingOffsets.Dequeue());
            }
        }

        UpdateTickMarks();
        UpdateAverageIndicators();
        UpdateSegmentWidths();
    }

    public void Destroy()
    {
        _avgText?.Dispose();
        if (_barRoot != null)
            UnityEngine.Object.Destroy(_barRoot);
    }

    private void ResetData()
    {
        _recentHitIndex = 0;
        _recentHitCount = 0;
        _recentHitSum = 0;
        _allHitSum = 0;
        _allHitCount = 0;
        Array.Clear(_recentHits, 0, _recentHits.Length);

        lock (_pendingLock)
        {
            _pendingOffsets.Clear();
        }
    }

    private void PushHitOffset(float offsetMs)
    {
        // Update all-session average
        _allHitSum += offsetMs;
        _allHitCount++;

        // Update circular buffer for recent hits
        if (_recentHitCount >= MaxRecentHits)
        {
            // Remove oldest value from sum
            _recentHitSum -= _recentHits[_recentHitIndex];
        }
        else
        {
            _recentHitCount++;
        }

        _recentHits[_recentHitIndex] = offsetMs;
        _recentHitSum += offsetMs;
        _recentHitIndex = (_recentHitIndex + 1) % MaxRecentHits;
    }

    private void CreateBarUI()
    {
        // Root container for the bar, positioned at bottom-center of screen
        _barRoot = new GameObject("JudgementBarOverlay");
        _barRootTransform = _barRoot.AddComponent<RectTransform>();
        _barRootTransform.SetParent(Common.GetDrawCanvasForScene(), false);
        _barRootTransform.pivot = new Vector2(0.5f, 0f);
        _barRootTransform.anchorMin = new Vector2(0.5f, 0f);
        _barRootTransform.anchorMax = new Vector2(0.5f, 0f);
        _barRootTransform.anchoredPosition = new Vector2(0f, BottomPadding);
        _barRootTransform.sizeDelta = new Vector2(BarWidth, BarHeight);
        _barRoot.layer = LayerMask.NameToLayer("UI");

        // Background - covers the full visual area including diamond indicators and avg text
        var totalHeight = BarHeight + (TriangleSize + 4f) * 2f + 24f; // bar + diamonds above/below + text line
        _background = CreateColoredImage("BG", _barRootTransform, ColorBackground,
            new Vector2(0f, -10f), new Vector2(BarWidth + BackgroundPadding * 2f, totalHeight + BackgroundPadding * 2f),
            new Vector2(0.5f, 0.5f));

        // Colored segments - will be sized dynamically based on judge ranges
        var bandHeight = BarHeight * 0.5f;
        var bandY = 0f; // centered vertically

        // Create segments (initial sizes, will be updated in UpdateSegmentWidths)
        _segFukaLeft = CreateColoredImage("SegFukaL", _barRootTransform, ColorFuka,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));
        _segKaLeft = CreateColoredImage("SegKaL", _barRootTransform, ColorKa,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));
        _segRyoLeft = CreateColoredImage("SegRyoL", _barRootTransform, ColorRyo,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));
        _segCenter = CreateColoredImage("SegCenter", _barRootTransform, ColorCenter,
            Vector2.zero, new Vector2(2f, bandHeight), new Vector2(0.5f, 0.5f));
        _segRyoRight = CreateColoredImage("SegRyoR", _barRootTransform, ColorRyo,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));
        _segKaRight = CreateColoredImage("SegKaR", _barRootTransform, ColorKa,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));
        _segFukaRight = CreateColoredImage("SegFukaR", _barRootTransform, ColorFuka,
            Vector2.zero, new Vector2(0, bandHeight), new Vector2(0.5f, 0.5f));

        // Tick marks (hidden initially)
        for (var i = 0; i < MaxRecentHits; i++)
        {
            _tickMarks[i] = CreateColoredImage($"Tick{i}", _barRootTransform, ColorTick,
                Vector2.zero, new Vector2(TickWidth, BarHeight), new Vector2(0.5f, 0.5f));
            _tickMarks[i].gameObject.SetActive(false);
        }

        // Average indicators (triangles approximated as narrow tall images)
        _avgRecentTriangle = CreateTriangleIndicator("AvgRecent", _barRootTransform, ColorAvgRecent, true);
        _avgAllTriangle = CreateTriangleIndicator("AvgAll", _barRootTransform, ColorAvgAll, false);

        // Average text below the bar
        _avgText = new TextUi(true)
        {
            Name = "JudgementBarAvgText",
            Text = "",
            FontSize = 24,
            Alignment = TextAlignmentOptions.TopLeft,
            Position = new Vector2(
                (Common.ScreenWidth - BarWidth) / 2f,
                Common.ScreenHeight - BottomPadding + 4f)
        };
    }

    private void UpdateSegmentWidths()
    {
        var playerState = EnsoGameBasePatch.PlayerStates[0];
        var ryoRange = playerState.RyoJudgeRange;
        var kaRange = playerState.KaJudgeRange;
        var fukaRange = playerState.FukaJudgeRange;
        var timeRange = TimeRangeMs;

        // If ranges aren't initialized yet, use defaults
        if (ryoRange <= 0) ryoRange = 25f;
        if (kaRange <= 0) kaRange = 75f;
        if (fukaRange <= 0) fukaRange = 108f;

        // Clamp ranges to our display range
        ryoRange = Mathf.Min(ryoRange, timeRange);
        kaRange = Mathf.Min(kaRange, timeRange);
        fukaRange = Mathf.Min(fukaRange, timeRange);

        var bandHeight = BarHeight * 0.5f;
        var halfBar = BarWidth / 2f;

        // Convert ms ranges to pixel widths
        var ryoPixels = (ryoRange / timeRange) * halfBar;
        var kaPixels = (kaRange / timeRange) * halfBar;
        var fukaPixels = (fukaRange / timeRange) * halfBar;

        // Center line
        var centerRect = _segCenter.GetComponent<RectTransform>();
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = new Vector2(2f, bandHeight);

        // Ryo segments (innermost colored band)
        var segRyoLRect = _segRyoLeft.GetComponent<RectTransform>();
        segRyoLRect.sizeDelta = new Vector2(ryoPixels, bandHeight);
        segRyoLRect.anchoredPosition = new Vector2(-ryoPixels / 2f, 0f);

        var segRyoRRect = _segRyoRight.GetComponent<RectTransform>();
        segRyoRRect.sizeDelta = new Vector2(ryoPixels, bandHeight);
        segRyoRRect.anchoredPosition = new Vector2(ryoPixels / 2f, 0f);

        // Ka segments
        var kaWidth = kaPixels - ryoPixels;
        var segKaLRect = _segKaLeft.GetComponent<RectTransform>();
        segKaLRect.sizeDelta = new Vector2(kaWidth, bandHeight);
        segKaLRect.anchoredPosition = new Vector2(-(ryoPixels + kaWidth / 2f), 0f);

        var segKaRRect = _segKaRight.GetComponent<RectTransform>();
        segKaRRect.sizeDelta = new Vector2(kaWidth, bandHeight);
        segKaRRect.anchoredPosition = new Vector2(ryoPixels + kaWidth / 2f, 0f);

        // Fuka segments (outermost)
        var fukaWidth = fukaPixels - kaPixels;
        var segFukaLRect = _segFukaLeft.GetComponent<RectTransform>();
        segFukaLRect.sizeDelta = new Vector2(fukaWidth, bandHeight);
        segFukaLRect.anchoredPosition = new Vector2(-(kaPixels + fukaWidth / 2f), 0f);

        var segFukaRRect = _segFukaRight.GetComponent<RectTransform>();
        segFukaRRect.sizeDelta = new Vector2(fukaWidth, bandHeight);
        segFukaRRect.anchoredPosition = new Vector2(kaPixels + fukaWidth / 2f, 0f);
    }

    private void UpdateTickMarks()
    {
        var timeRange = TimeRangeMs;
        var halfBar = BarWidth / 2f;

        for (var i = 0; i < MaxRecentHits; i++)
        {
            if (i >= _recentHitCount)
            {
                _tickMarks[i].gameObject.SetActive(false);
                continue;
            }

            _tickMarks[i].gameObject.SetActive(true);
            var offset = Mathf.Clamp(_recentHits[i], -timeRange, timeRange);
            var xPos = (offset / timeRange) * halfBar;
            var tickRect = _tickMarks[i].GetComponent<RectTransform>();
            tickRect.anchoredPosition = new Vector2(xPos, 0f);
        }
    }

    private void UpdateAverageIndicators()
    {
        var timeRange = TimeRangeMs;
        var halfBar = BarWidth / 2f;

        // Recent average (last 50)
        if (_recentHitCount > 0)
        {
            var avgRecent = _recentHitSum / _recentHitCount;
            var xRecent = Mathf.Clamp(avgRecent, -timeRange, timeRange) / timeRange * halfBar;
            _avgRecentTriangle.anchoredPosition = new Vector2(xRecent, BarHeight / 2f + TriangleSize / 2f + 2f);
            _avgRecentTriangle.gameObject.SetActive(true);
        }
        else
        {
            _avgRecentTriangle.gameObject.SetActive(false);
        }

        // All-session average
        if (_allHitCount > 0)
        {
            var avgAll = _allHitSum / _allHitCount;
            var xAll = Mathf.Clamp(avgAll, -timeRange, timeRange) / timeRange * halfBar;
            _avgAllTriangle.anchoredPosition = new Vector2(xAll, -(BarHeight / 2f + TriangleSize / 2f + 2f));
            _avgAllTriangle.gameObject.SetActive(true);

            _avgText.Text = $"Avg: {avgAll:F1}ms";
        }
        else
        {
            _avgAllTriangle.gameObject.SetActive(false);
            _avgText.Text = "";
        }
    }

    private static Image CreateColoredImage(string name, RectTransform parent, Color color,
        Vector2 position, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.pivot = pivot;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.layer = LayerMask.NameToLayer("UI");

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        var image = go.AddComponent<Image>();
        image.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    /// <summary>
    /// Creates a diamond/triangle indicator using a rotated square image.
    /// </summary>
    private static RectTransform CreateTriangleIndicator(string name, RectTransform parent, Color color, bool pointUp)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(TriangleSize, TriangleSize);
        rect.localRotation = Quaternion.Euler(0, 0, 45f); // rotate square to diamond
        go.layer = LayerMask.NameToLayer("UI");

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        var image = go.AddComponent<Image>();
        image.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        image.color = color;
        image.raycastTarget = false;

        go.SetActive(false);
        return rect;
    }
}
