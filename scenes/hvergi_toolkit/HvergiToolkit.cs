using Godot;
using System;

namespace HvergiToolkit
{
    public partial class HvergiToolkit : Control
    {
        [Export]
        public RichTextLabel terminalOutput;
        [ExportGroup("Tab Controls")]
        [Export]
        public TabContainer contentTabContainer;
        [Export]
        public Button newsButton;
        [Export]
        public Button appsButton;
        [Export]
        public Button settingsButton;
        [ExportGroup("App Buttons")]
        [Export]
        public Button playerEditorButton;
        [Export]
        public Button moiTrackerButton;
        [Export]
        public Button skillTrackerButton;
        [Export]
        public Button tradeWatcherButton;
        [Export]
        public Button stpCalcButton;
        [Export]
        public Button skillCompareButton;
        [Export]
        public Button sermonButton;
        [Export]
        public Button logAlertButton;
        [Export]
        public Button logSearchButton;
        [Export]
        public Button affinityFoodPlannerButton;
        [Export]
        public Button dyeEstimatorButton;
        [Export]
        public Button settlementPlannerButton;

        private string _currentPickerTarget = "";

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            Terminal.Output = terminalOutput;
            AppSettings.Load();
            
            if (!Players.Load())
            {
                Terminal.WriteWarning("No player data found. Please open the Player Editor to configure your Wurm paths.");
            }
            else
            {
                Terminal.Write($"Loaded {Players.WurmPaths.Count} paths and {Players.PlayerDict.Count} players.");
            }

            newsButton.Pressed += OnNewsButtonPressed;
            appsButton.Pressed += OnAppsButtonPressed;
            settingsButton.Pressed += OnSettingsButtonPressed;

            playerEditorButton.Pressed += () => onAppButtonPressed("res://scenes/apps/player_editor/player_editor.tscn");
            moiTrackerButton.Pressed += () => onAppButtonPressed("res://scenes/apps/moi_tracker/moi_tracker.tscn");
            skillTrackerButton.Pressed += () => onAppButtonPressed("res://scenes/apps/skill_tracker/skill_tracker.tscn");
            tradeWatcherButton.Pressed += () => onAppButtonPressed("res://scenes/apps/trade_watcher/trade_watcher.tscn");
            stpCalcButton.Pressed += () => onAppButtonPressed("res://scenes/apps/stp_calculator/stp_calculator.tscn");
            skillCompareButton.Pressed += () => onAppButtonPressed("res://scenes/apps/skill_compare/skill_compare.tscn");
            sermonButton.Pressed += () => onAppButtonPressed("res://scenes/apps/sermon_warden/sermon_warden.tscn");
            logAlertButton.Pressed += () => onAppButtonPressed("res://scenes/apps/log_alert/log_alert.tscn");
            logSearchButton.Pressed += () => onAppButtonPressed("res://scenes/apps/log_search/log_search.tscn");
            affinityFoodPlannerButton.Pressed += () => onAppButtonPressed("res://scenes/apps/affinity_food_planner/affinity_food_planner.tscn");
            dyeEstimatorButton.Pressed += () => onAppButtonPressed("res://scenes/apps/dye_estimator/dye_estimator.tscn");
            settlementPlannerButton.Pressed += () => onAppButtonPressed("res://scenes/apps/settlement_planner/settlement_planner.tscn");

            InitializeMoiSettings();
        }

        private void InitializeMoiSettings()
        {
            var voiceSelect = GetNode<OptionButton>("%MoiVoiceSelect");
            var voices = DisplayServer.TtsGetVoices();
            int selectedIndex = -1;
            for (int i = 0; i < voices.Count; i++)
            {
                voiceSelect.AddItem($"{voices[i]["name"]} ({voices[i]["language"]})");
                if ((string)voices[i]["id"] == AppSettings.MoiTracker.TtsVoiceId)
                    selectedIndex = i;
            }
            if (selectedIndex != -1) voiceSelect.Selected = selectedIndex;
            voiceSelect.ItemSelected += (id) => {
                AppSettings.MoiTracker.TtsVoiceId = (string)voices[(int)id]["id"];
            };

            SetupAlertGroup("Craft", AppSettings.MoiTracker.CraftAlert);
            SetupAlertGroup("Moi", AppSettings.MoiTracker.MoiAlert);

            GetNode<FileDialog>("%SoundPicker").FileSelected += OnSoundFileSelected;
        }

        private void SetupAlertGroup(string prefix, AppSettings.AlertSettings settings)
        {
            var modeBtn = GetNode<OptionButton>($"%{prefix}AlertMode");
            var soundSettings = GetNode<Control>($"%{prefix}SoundSettings");
            var ttsSettings = GetNode<Control>($"%{prefix}TTSSettings");
            var pathLabel = GetNode<Label>($"%{prefix}SoundPath");
            var volSlider = GetNode<HSlider>($"%{prefix}SoundVolume");
            var msgInput = GetNode<LineEdit>($"%{prefix}TTSMessage");

            modeBtn.Selected = settings.Mode;
            soundSettings.Visible = settings.Mode == 0;
            ttsSettings.Visible = settings.Mode == 1;

            pathLabel.Text = settings.SoundPath;
            volSlider.Value = settings.Volume;
            msgInput.Text = settings.TTSMessage;

            modeBtn.ItemSelected += (id) => {
                settings.Mode = (int)id;
                soundSettings.Visible = id == 0;
                ttsSettings.Visible = id == 1;
            };

            GetNode<Button>($"%{prefix}SoundChange").Pressed += () => {
                _currentPickerTarget = prefix;
                GetNode<FileDialog>("%SoundPicker").PopupCentered();
            };

            volSlider.ValueChanged += (val) => settings.Volume = (float)val;
            msgInput.TextChanged += (text) => settings.TTSMessage = text;

            GetNode<Button>($"%{prefix}SoundTest").Pressed += () => {
                var player = GetNode<AudioStreamPlayer>("%AudioPlayer");
                if (Godot.FileAccess.FileExists(settings.SoundPath))
                {
                    var stream = GD.Load<AudioStream>(settings.SoundPath);
                    player.Stream = stream;
                    player.VolumeDb = Mathf.LinearToDb(settings.Volume);
                    player.Play();
                }
                else
                {
                    Terminal.WriteError($"Sound file not found: {settings.SoundPath}");
                }
            };

            GetNode<Button>($"%{prefix}TTSTest").Pressed += () => {
                string msg = settings.TTSMessage.Replace("{player}", "TestPlayer");
                DisplayServer.TtsSpeak(msg, AppSettings.MoiTracker.TtsVoiceId);
            };
        }

        private void OnSoundFileSelected(string path)
        {
            if (_currentPickerTarget == "Craft")
            {
                AppSettings.MoiTracker.CraftAlert.SoundPath = path;
                GetNode<Label>("%CraftSoundPath").Text = path;
            }
            else if (_currentPickerTarget == "Moi")
            {
                AppSettings.MoiTracker.MoiAlert.SoundPath = path;
                GetNode<Label>("%MoiSoundPath").Text = path;
            }
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                AppSettings.Save();
                Players.Save();
            }
        }

        private void OnNewsButtonPressed()
        {
            contentTabContainer.CurrentTab = 0;
        }
        private void OnAppsButtonPressed()
        {
            contentTabContainer.CurrentTab = 1;
        }
        private void OnSettingsButtonPressed()
        {
            contentTabContainer.CurrentTab = 2;
        }

        private void onAppButtonPressed(string scenePath)
        {
            var scene = GD.Load<PackedScene>(scenePath);
            var instance = scene.Instantiate<Window>();
            AddChild(instance);
            instance.Show();
        }
    }
}
