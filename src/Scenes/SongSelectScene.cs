using Il2CppInterop.Runtime;
using TnTRFMod.Patches;
using TnTRFMod.Scenes.Enso;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = TnTRFMod.Utils.Logger;
using Random = UnityEngine.Random;

#if BEPINEX
using Scripts.OutGame.SongSelect;
using Scripts.OutGame.Common;
using SoundLabelClass = SoundLabel.SoundLabel;

#elif MELONLOADER
using Il2CppScripts.OutGame.SongSelect;
using Il2CppScripts.OutGame.Common;
using SoundLabelClass = Il2CppSoundLabel.SoundLabel;

#endif

namespace TnTRFMod.Scenes;

public class SongSelectScene : IScene
{
    private static readonly ControllerManager mgr = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;
    private static readonly Color32 ToggleOnColor = new(0, 191, 1, 0xFF);
    private static readonly Color32 ToggleOffColor = new(239, 8, 8, 0xFF);
    private SongSelectSceneUiController? _cached;

    private float CurrentPanelButtonPosY = 140f;
    private TextUi liveStreamSongRequestStatus;

    private float repeatCounter;

    private Il2CppSystem.Action<char>? textInputDelegate;
    public string SceneName => "SongSelect";

    public void Start()
    {
        if (TnTrfMod.Instance.enableTatakonKeyboardSongSelect.Value)
        {
            textInputDelegate =
                DelegateSupport.ConvertDelegate<Il2CppSystem.Action<char>>(OnTextInput);
            Keyboard.current.add_onTextInput(textInputDelegate);
        }

        _cached = null;
        if (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
        {
            liveStreamSongRequestStatus = new TextUi
            {
                FontSize = 20,
                Position = new Vector2(1500f, 863f)
            };
            UpdateLiveStreamSongRequestStatus();
        }

        CurrentPanelButtonPosY = 140f;

        SetupSongSearchPanel();
        SetupFumenPostProcessing();
        // SetupDebugPanel();
    }

    public void Destroy()
    {
        if (textInputDelegate != null)
            Keyboard.current.remove_onTextInput(textInputDelegate);
    }

    public void Update()
    {
        if (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
        {
            repeatCounter = Math.Max(0, repeatCounter - Time.deltaTime);

            if (Keyboard.current[Key.O].wasPressedThisFrame)
                UpdatePlaylistToQueuedSongList();
            if (Keyboard.current[Key.L].wasPressedThisFrame)
            {
                var uiController = GetUiSongScroller();
                var selectedSongId = uiController.songScroller.SelectedItem.Value?.Value?.Id;
                if (selectedSongId != null)
                {
                    LiveStreamSongSelectPanel.QueuedSongList.RemoveAt(
                        LiveStreamSongSelectPanel.QueuedSongList.FindIndex(x => x.SongInfo.Id == selectedSongId));
                    UpdatePlaylistToQueuedSongList();
                }
            }

            UpdateLiveStreamSongRequestStatus();
        }

        Common.GetDrawCanvasForSceneCanvasGroup().alpha =
            GetUiSongScroller().UiSongScroller.canvasGroup.alpha;
    }

    private ButtonUi NewPanelButton(I18n.I18nResult text)
    {
        var btn = new ButtonUi
        {
            I18nText = text,
            Position = new Vector2(68f, CurrentPanelButtonPosY),
            Size = new Vector2(300f, 50f),
            ButtonColor = new Color(1, 0.4472f, 0.0968f, 1)
        };
        btn._transform.SetAsFirstSibling();
        CurrentPanelButtonPosY += 58f;
        return btn;
    }

    private void SetupFumenPostProcessing()
    {
        var panelButton = NewPanelButton(I18n.Get("fumenPostProcessing.button"));

        var fumenPostProcessingPanel = new FrameUi
        {
            FrameColor = new Color32(255, 191, 1, 0xFF),
            Position = panelButton.Position + new Vector2(0, panelButton.Size.y + 8),
            Size = new Vector2(500, 392f),
            Visible = false
        };

        var fumenPostProcessingTipLabel = new TextUi
        {
            Parent = fumenPostProcessingPanel,
            Position = new Vector2(15, 15),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 20),
            I18nText = I18n.Get("fumenPostProcessing.tip"),
            WordWrap = true,
            FontSize = 20
        };

        var equalScrollSpeedBtn = new ButtonUi
        {
            Parent = fumenPostProcessingPanel,
            Position =
                fumenPostProcessingTipLabel.Position + new Vector2(0, 80),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 50),
            I18nText = I18n.Get("fumenPostProcessing.equalScrollSpeed"),
            ButtonColor = FumenPostProcessingPatch.EnableEqualScrollSpeed ? ToggleOnColor : ToggleOffColor
        };

        var superSlowScrollSpeedBtn = new ButtonUi
        {
            Parent = fumenPostProcessingPanel,
            Position = equalScrollSpeedBtn.Position + new Vector2(0, equalScrollSpeedBtn.Size.y + 8),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 50),
            I18nText = I18n.Get("fumenPostProcessing.superSlowScrollSpeed"),
            ButtonColor = FumenPostProcessingPatch.EnableSuperSlowSpeed ? ToggleOnColor : ToggleOffColor
        };

        var randomScrollSpeedBtn = new ButtonUi
        {
            Parent = fumenPostProcessingPanel,
            Position = superSlowScrollSpeedBtn.Position + new Vector2(0, superSlowScrollSpeedBtn.Size.y + 8),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 50),
            I18nText = I18n.Get("fumenPostProcessing.randomScrollSpeed"),
            ButtonColor = FumenPostProcessingPatch.EnableRandomSlowSpeed ? ToggleOnColor : ToggleOffColor
        };

        var reverseScrollSpeedBtn = new ButtonUi
        {
            Parent = fumenPostProcessingPanel,
            Position = randomScrollSpeedBtn.Position + new Vector2(0, randomScrollSpeedBtn.Size.y + 8),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 50),
            I18nText = I18n.Get("fumenPostProcessing.reverseScrollSpeed"),
            ButtonColor = FumenPostProcessingPatch.EnableReverseSlowSpeed ? ToggleOnColor : ToggleOffColor
        };

        var strictJudgeTimingBtn = new ButtonUi
        {
            Parent = fumenPostProcessingPanel,
            Position = reverseScrollSpeedBtn.Position + new Vector2(0, reverseScrollSpeedBtn.Size.y + 8),
            Size = new Vector2(fumenPostProcessingPanel.Size.x - 30, 50),
            I18nText = I18n.Get("fumenPostProcessing.strictJudgeTiming"),
            ButtonColor = FumenPostProcessingPatch.EnableStrictJudgeTiming ? ToggleOnColor : ToggleOffColor
        };

        panelButton.AddListener(() => { fumenPostProcessingPanel.Visible = !fumenPostProcessingPanel.Visible; });
        equalScrollSpeedBtn.AddListener(() =>
        {
            FumenPostProcessingPatch.EnableEqualScrollSpeed = !FumenPostProcessingPatch.EnableEqualScrollSpeed;
            equalScrollSpeedBtn.ButtonColor =
                FumenPostProcessingPatch.EnableEqualScrollSpeed ? ToggleOnColor : ToggleOffColor;
        });
        superSlowScrollSpeedBtn.AddListener(() =>
        {
            FumenPostProcessingPatch.EnableSuperSlowSpeed = !FumenPostProcessingPatch.EnableSuperSlowSpeed;
            superSlowScrollSpeedBtn.ButtonColor =
                FumenPostProcessingPatch.EnableSuperSlowSpeed ? ToggleOnColor : ToggleOffColor;
        });
        randomScrollSpeedBtn.AddListener(() =>
        {
            FumenPostProcessingPatch.EnableRandomSlowSpeed = !FumenPostProcessingPatch.EnableRandomSlowSpeed;
            randomScrollSpeedBtn.ButtonColor =
                FumenPostProcessingPatch.EnableRandomSlowSpeed ? ToggleOnColor : ToggleOffColor;
        });
        reverseScrollSpeedBtn.AddListener(() =>
        {
            FumenPostProcessingPatch.EnableReverseSlowSpeed = !FumenPostProcessingPatch.EnableReverseSlowSpeed;
            reverseScrollSpeedBtn.ButtonColor =
                FumenPostProcessingPatch.EnableReverseSlowSpeed ? ToggleOnColor : ToggleOffColor;
        });
        strictJudgeTimingBtn.AddListener(() =>
        {
            FumenPostProcessingPatch.EnableStrictJudgeTiming = !FumenPostProcessingPatch.EnableStrictJudgeTiming;
            strictJudgeTimingBtn.ButtonColor =
                FumenPostProcessingPatch.EnableStrictJudgeTiming ? ToggleOnColor : ToggleOffColor;
        });
    }

    private void SetupDebugPanel()
    {
        var panelButton = NewPanelButton(I18n.Get("debugPlaySong.button"));
        panelButton.AddListener(() =>
        {
            UTask.RunOnIl2CppBlocking(() =>
            {
                var song = CommonObjects.instance.MyDataManager.MusicData.GetInfoById("natsu");
                if (song == null)
                {
                    Logger.Error("无法找到测试歌曲 natsu");
                    return;
                }

                EnsoGameBasePatch.StartEnsoGame(ref song, EnsoData.EnsoLevelType.Mania);
            });
        });
    }

    private void SetupSongSearchPanel()
    {
        var panelButton = NewPanelButton(I18n.Get("advanceSongSearch.button"));

        var advanceSearchPanel = new FrameUi
        {
            FrameColor = new Color32(255, 191, 1, 0xFF),
            Position = panelButton.Position + new Vector2(0, panelButton.Size.y + 8),
            Size = new Vector2(500, 338f),
            Visible = false
        };

        var songKeywordLabel = new TextUi
        {
            Parent = advanceSearchPanel,
            Position = new Vector2(15, 15),
            I18nText = I18n.Get("advanceSongSearch.keyword.label"),
            FontSize = 20
        };

        var songKeywordField = new TextFieldUi
        {
            Parent = advanceSearchPanel,
            Position = songKeywordLabel.Position + new Vector2(0, songKeywordLabel.Size.y + 8),
            Size = new Vector2(advanceSearchPanel.Size.x - 30, 50),
            I18nPlaceholder = I18n.Get("advanceSongSearch.keyword.placeholder")
        };

        var diffFilterLabel = new TextUi
        {
            Parent = advanceSearchPanel,
            Position = songKeywordField.Position + new Vector2(0, songKeywordField.Size.y + 8),
            I18nText = I18n.Get("advanceSongSearch.diffFilter.label"),
            FontSize = 20
        };

        var diffTypeBtn = new SelectUi<string>("all")
        {
            Parent = advanceSearchPanel,
            Position = diffFilterLabel.Position + new Vector2(0, diffFilterLabel.Size.y + 8),
            Size = new Vector2(200, 50),
            Items =
            [
                new SelectUi<string>.SelectItem
                {
                    Value = "all",
                    Text = I18n.Get("advanceSongSearch.diffFilter.all")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "easy",
                    Text = I18n.Get("advanceSongSearch.diffFilter.easy"),
                    ButtonColor = new Color32(220, 40, 0, 0xFF)
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "normal",
                    Text = I18n.Get("advanceSongSearch.diffFilter.normal"),
                    ButtonColor = new Color32(120, 160, 20, 0xFF)
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "hard",
                    Text = I18n.Get("advanceSongSearch.diffFilter.hard"),
                    ButtonColor = new Color32(40, 120, 160, 0xFF)
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "oni",
                    Text = I18n.Get("advanceSongSearch.diffFilter.oni"),
                    ButtonColor = new Color32(183, 32, 129, 0xFF)
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "ura",
                    Text = I18n.Get("advanceSongSearch.diffFilter.ura"),
                    ButtonColor = new Color32(90, 60, 220, 0xFF)
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "oni-ura",
                    Text = I18n.Get("advanceSongSearch.diffFilter.oni-ura"),
                    ButtonColor = new Color32(90, 60, 220, 0xFF)
                }
            ]
        };

        var diffMinLevelField = new TextFieldUi
        {
            Parent = advanceSearchPanel,
            Position = diffTypeBtn.Position + new Vector2(diffTypeBtn.Size.x + 8, 0),
            Size = new Vector2(100, 50),
            I18nPlaceholder = I18n.Get("advanceSongSearch.diffFilter.min-level-placeholder"),
            Value = "0"
        };

        var diffMaxLevelField = new TextFieldUi
        {
            Parent = advanceSearchPanel,
            Position = diffMinLevelField.Position + new Vector2(diffMinLevelField.Size.x + 8, 0),
            Size = new Vector2(100, 50),
            I18nPlaceholder = I18n.Get("advanceSongSearch.diffFilter.max-level-placeholder"),
            Value = "10"
        };

        var sortMethodLabel = new TextUi
        {
            Parent = advanceSearchPanel,
            Position = diffTypeBtn.Position + new Vector2(0, diffTypeBtn.Size.y + 8),
            I18nText = I18n.Get("advanceSongSearch.sortMethod.label"),
            FontSize = 20
        };

        var sortMethodSelect = new SelectUi<string>("normal")
        {
            Parent = advanceSearchPanel,
            Position = sortMethodLabel.Position + new Vector2(0, sortMethodLabel.Size.y + 8),
            Size = new Vector2(advanceSearchPanel.Size.x - 30, 50),
            Items =
            [
                new SelectUi<string>.SelectItem
                {
                    Value = "normal",
                    Text = I18n.Get("advanceSongSearch.sortMethod.normal")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "name-asc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.name-asc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "name-desc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.name-desc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "level-asc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.level-asc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "level-desc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.level-desc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "score-asc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.score-asc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "score-desc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.score-desc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "shinuti-score-asc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.shinuti-score-asc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "shinuti-score-desc",
                    Text = I18n.Get("advanceSongSearch.sortMethod.shinuti-score-desc")
                },
                new SelectUi<string>.SelectItem
                {
                    Value = "random",
                    Text = I18n.Get("advanceSongSearch.sortMethod.random")
                }
            ]
        };

        var applySearchBtn = new ButtonUi
        {
            Parent = advanceSearchPanel,
            Position = sortMethodSelect.Position + new Vector2(0, sortMethodSelect.Size.y + 8),
            Size = new Vector2(advanceSearchPanel.Size.x - 30, 50),
            I18nText = I18n.Get("advanceSongSearch.confirm"),
            ButtonColor = new Color32(0, 191, 1, 0xFF)
        };

        // searchBtn.AddListener(() => { SearchSong(searchTextInput.Value); });
        panelButton.AddListener(() => { advanceSearchPanel.Visible = !advanceSearchPanel.Visible; });
        applySearchBtn.AddListener(() =>
        {
            advanceSearchPanel.Visible = false;
            SearchSong(songKeywordField.Value, diffTypeBtn.Value,
                int.TryParse(diffMinLevelField.Value, out var minLevel) ? minLevel : 0,
                int.TryParse(diffMaxLevelField.Value, out var maxLevel) ? maxLevel : 10,
                sortMethodSelect.Value);
        });
    }

    private void SearchSong(string keyword, string diffFilter, int minLevel, int maxLevel, string sortMethod)
    {
        var uiController = GetUiSongScroller();
        var allSongs =
            SongSelectUtility.GetFilteredSongs(FilterTypes.MyLibrary, EnsoData.EnsoType.Normal, SongSelectType.Normal);
        var capacity = allSongs.Count;
        List<MusicDataInterface.MusicInfoAccesser> list = new(capacity);
        List<int> addedIndex = new(capacity);

        if (SongAliasTable.TryGetAlias(keyword, out var musicId))
            foreach (var music in allSongs)
            {
                if (music.Id.Contains('_'))
                {
                    var customSongIdSplit = music.Id.Split('_');
                    if (customSongIdSplit.Length < 2) continue;
                    var customSongId = customSongIdSplit[1];
                    if (customSongId.ToLower() != musicId) continue;
                    AddSongToList(music);
                }
                else if (music.Id.ToLower() != musicId)
                {
                    continue;
                }

                AddSongToList(music);
            }

        var keywordLower = keyword.ToLower();

        if (keywordLower.Length > 0)
            foreach (var music in allSongs)
            {
                if (music.Id.ToLower() == keyword)
                {
                    AddSongToList(music);
                    continue;
                }

                if (music.SongNames.Any(musicSongName => musicSongName.ToLower().Contains(keywordLower)))
                {
                    AddSongToList(music);
                    continue;
                }

                if (music.SongSubs.Any(musicSongName => musicSongName.ToLower().Contains(keywordLower)))
                    AddSongToList(music);
            }
        else
            foreach (var music in allSongs)
                AddSongToList(music);

        var level = DiffFilterToLevel(diffFilter);

        switch (sortMethod)
        {
            case "normal":
                break;
            case "name-asc":
                list.Sort(CompareSongName);
                break;
            case "name-desc":
                list.Sort(CompareSongName);
                list.Reverse();
                break;
            case "level-asc":
                list.Sort(new SongLevelComparer(level));
                break;
            case "level-desc":
                list.Sort(new SongLevelComparer(level));
                list.Reverse();
                break;
            case "score-asc":
                list.Sort(new SongScoreComparer(level));
                break;
            case "score-desc":
                list.Sort(new SongScoreComparer(level));
                list.Reverse();
                break;
            case "shinuti-score-asc":
                list.Sort(new SongShinutiScoreComparer(level));
                break;
            case "shinuti-score-desc":
                list.Sort(new SongShinutiScoreComparer(level));
                list.Reverse();
                break;
            case "random":
                list = list.OrderBy(_ => Random.value).ToList();
                break;
        }

        var result = new Il2CppSystem.Collections.Generic.List<MusicDataInterface.MusicInfoAccesser>(list.Count);
        for (var i = 0; i < list.Count; i++)
            result.Add(list[i]);

        var buttons = uiController.songScroller.CreateItemList(result);
        uiController.songScroller.SelectItem(buttons, true);
        CommonObjects.Instance.MySoundManager.PlayCommonSe(SoundLabelClass.Common.don);
        return;

        int DiffFilterToLevel(string diff)
        {
            return diff switch
            {
                "easy" => 0,
                "normal" => 1,
                "hard" => 2,
                "oni" => 3,
                "ura" => 4,
                "oni-ura" => -1,
                _ => -1
            };
        }

        void AddSongToList(MusicDataInterface.MusicInfoAccesser songInfo)
        {
            switch (diffFilter)
            {
                case "all":
                    if (songInfo.Stars.Max() < minLevel) return;
                    if (songInfo.Stars.Min() > maxLevel) return;
                    break;
                case "easy":
                    if (songInfo.Stars[0] < minLevel) return;
                    if (songInfo.Stars[0] > maxLevel) return;
                    break;
                case "normal":
                    if (songInfo.Stars[1] < minLevel) return;
                    if (songInfo.Stars[1] > maxLevel) return;
                    break;
                case "hard":
                    if (songInfo.Stars[2] < minLevel) return;
                    if (songInfo.Stars[2] > maxLevel) return;
                    break;
                case "oni":
                    if (songInfo.Stars[3] < minLevel) return;
                    if (songInfo.Stars[3] > maxLevel) return;
                    break;
                case "ura":
                    if (songInfo.Stars[4] < minLevel) return;
                    if (songInfo.Stars[4] > maxLevel) return;
                    break;
                case "oni-ura":
                    if (songInfo.Stars[3] < minLevel && songInfo.Stars[4] < minLevel) return;
                    if (songInfo.Stars[3] > maxLevel && songInfo.Stars[4] > maxLevel) return;
                    break;
                default:
                    return;
            }

            if (addedIndex.Contains(songInfo.UniqueId)) return;
            addedIndex.Add(songInfo.UniqueId);
            list.Add(songInfo);
        }
    }

    private static int CompareSongName(MusicDataInterface.MusicInfoAccesser a, MusicDataInterface.MusicInfoAccesser b)
    {
        return string.Compare(a.SongNames[(int)I18n.CurrentLanguage], b.SongNames[(int)I18n.CurrentLanguage],
            StringComparison.CurrentCultureIgnoreCase);
    }

    private void OnTextInput(char character)
    {
        var donLKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonL];
        var donRKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonR];
        var katsuLKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuL];
        var katsuRKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuR];
        var charCode = (short)KeyConversion.CharToKey(character);

        if (charCode == katsuLKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.OnDirectionInput(ControllerManager.Dir.Up);
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.OnDirectionInput(ControllerManager.Dir.Left);
                    break;
                case Focuses.Songs:
                    if (repeatCounter > 0)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Left);
                    else if (!uiController.UiSongScroller.IsScrolling.Value)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Up);
                    repeatCounter = 0.1f;
                    break;
            }
        }

        if (charCode == katsuRKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.OnDirectionInput(ControllerManager.Dir.Down);
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.OnDirectionInput(ControllerManager.Dir.Right);
                    break;
                case Focuses.Songs:
                    if (repeatCounter > 0)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Right);
                    else if (!uiController.UiSongScroller.IsScrolling.Value)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Down);
                    break;
            }

            repeatCounter = 0.1f;
        }

        if (charCode == donLKey || charCode == donRKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.Decision();
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.Decision();
                    break;
                case Focuses.Songs:
                    uiController.songScroller.Decision();
                    break;
            }
        }
    }

    private class SongLevelComparer(int level) : IComparer<MusicDataInterface.MusicInfoAccesser>
    {
        public int Compare(MusicDataInterface.MusicInfoAccesser a, MusicDataInterface.MusicInfoAccesser b)
        {
            if (level == -1)
            {
                var starA = a.Stars.Max();
                var starB = b.Stars.Max();
                return starA.CompareTo(starB);
            }
            else
            {
                var starA = a.Stars[level];
                var starB = b.Stars[level];
                return starA.CompareTo(starB);
            }
        }
    }

    private class SongScoreComparer(int level) : IComparer<MusicDataInterface.MusicInfoAccesser>
    {
        public int Compare(MusicDataInterface.MusicInfoAccesser a, MusicDataInterface.MusicInfoAccesser b)
        {
            if (level == -1)
            {
                var starA = a.DonScores.Max();
                var starB = b.DonScores.Max();
                return starA.CompareTo(starB);
            }
            else
            {
                var starA = a.DonScores[level];
                var starB = b.DonScores[level];
                return starA.CompareTo(starB);
            }
        }
    }

    private class SongShinutiScoreComparer(int level) : IComparer<MusicDataInterface.MusicInfoAccesser>
    {
        public int Compare(MusicDataInterface.MusicInfoAccesser a, MusicDataInterface.MusicInfoAccesser b)
        {
            if (level == -1)
            {
                var starA = a.ShinutiScores.Max();
                var starB = b.ShinutiScores.Max();
                return starA.CompareTo(starB);
            }
            else
            {
                var starA = a.ShinutiScores[level];
                var starB = b.ShinutiScores[level];
                return starA.CompareTo(starB);
            }
        }
    } // ReSharper disable Unity.PerformanceAnalysis

    private SongSelectSceneUiController GetUiSongScroller()
    {
        if (_cached != null) return _cached;
        var sceneObj = GameObject.Find("SongSelectSceneObjects")
            .GetComponent<SongSelectThunderShrineSceneObjects>();
        _cached = sceneObj.UiController;
        return _cached!;
    }

    private void UpdatePlaylistToQueuedSongList()
    {
        var uiController = GetUiSongScroller();
        Il2CppSystem.Collections.Generic.List<MusicDataInterface.MusicInfoAccesser> list = new();
        foreach (var info in LiveStreamSongSelectPanel.QueuedSongList)
            list.Add(info.SongInfo);
        var btns = uiController.songScroller.CreateItemList(list);
        uiController.songScroller.SelectItem(btns, true);
    }

    private void UpdateLiveStreamSongRequestStatus()
    {
        liveStreamSongRequestStatus.Text =
            $"直播功能已启用\n按下 O 键显示当前点歌列表\n按下 L 键从点歌歌单中删除所选歌曲\n当前已有 {LiveStreamSongSelectPanel.QueuedSongList.Count} 首点歌";
    }
}