using Godot;
using System;

namespace HvergiToolkit.UI.Settings;

public partial class Settings : ScrollContainer
{
	public enum AlertType
	{
		Sound = 0,
		TTS = 1
	}
	
	public static UserSettingsData Current {get; private set;} = new();
	
	[Export] private FileDialog _soundPicker;
	
	[ExportGroup("Craft Alerts")]
	[Export] private OptionButton _craftAlertTypeButton;
	[Export] private Control _craftSoundContainer;
	[Export] private Control _craftTTSContainer;
	[Export] private Button _craftSoundChangeButton;
	[Export] private Label _craftSoundFileLabel;
	[Export] private Button _craftSoundReset;
	[Export] private Label _craftTtsMessageLabel;
	[Export] private Button _craftTtsResetButton;
	[Export] private OptionButton _craftTtsSelectorButton;
	[Export] private Button _craftTtsTestButton;

	[ExportGroup("MOI Alerts")]
	[Export] private OptionButton _moiAlertTypeButton;
	[Export] private Control _moiSoundContainer;
	[Export] private Control _moiTTSContainer;
	[Export] private Button _moiSoundChangeButton;
	[Export] private Label _moiSoundFileLabel;
	[Export] private Button _moiSoundReset;
	




	private Action<string> _pendingSoundCallback;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetupAlertToggle(_craftAlertTypeButton,_craftSoundContainer,_craftTTSContainer,
		val => Current.CraftAlertType = val, Current.CraftAlertType);
		SetupAlertToggle(_moiAlertTypeButton,_moiSoundContainer,_moiTTSContainer,
		val => Current.MOIAlertType = val, Current.MOIAlertType);
		
		_soundPicker.FileSelected += OnSoundFileSelected;

		SetupSoundPathChangePressed(_craftSoundChangeButton,_craftSoundFileLabel,
		path => Current.CraftAlertSoundPath = path, Current.CraftAlertSoundPath);
		SetupSoundPathChangePressed(_moiSoundChangeButton,_moiSoundFileLabel,
		path => Current.MOIAlertSoundPath = path, Current.MOIAlertSoundPath);
		
		ResetButtonStringSetup(_craftSoundReset,_craftSoundFileLabel,"res://assets/sounds/craft.wav",path => Current.CraftAlertSoundPath = path);
		ResetButtonStringSetup(_moiSoundReset,_moiSoundFileLabel,"res://assets/sounds/moi.wav",path => Current.MOIAlertSoundPath = path);
		
		var voices = DisplayServer.TtsGetVoices();
		if(voices.Count == 0)
		{
			Terminal.WriteWarning("You have no system TTS setup.");
		}
	}

	private void ResetButtonStringSetup(Button button, Label label, string value, Action<String> onResetPressed)
	{
		button.Pressed += () =>
		{
			label.Text = value;
			onResetPressed(value);
		};
	}

	private void SetupAlertToggle(OptionButton button, Control soundContainer, Control ttsContainer, Action<AlertType> onSettingChanged, AlertType current)
	{
		button.Selected = (int)current;
		soundContainer.Visible = current == AlertType.Sound;
		ttsContainer.Visible = current == AlertType.TTS;

		button.ItemSelected += (index) => OnAlertOptionChanged(index, soundContainer, ttsContainer ,button, onSettingChanged);
	}

	private void SetupSoundPathChangePressed(Button button, Label label, Action<string> onPathSelected, string currentPath)
	{
		label.Text = currentPath;
		button.Pressed += () => 
		{
			_pendingSoundCallback = (path) =>
			{
				label.Text = path;
				onPathSelected(path);
			};
			_soundPicker.PopupCentered();
		};
	}

	private void OnSoundFileSelected(string path)
	{
		_pendingSoundCallback?.Invoke(path);
		_pendingSoundCallback = null;
	}

	private AlertType ValidateAndGetAlertType(long index, OptionButton optionButton)
	{
		if (index is >= 0 and <= (long)AlertType.TTS)
		{
			return (AlertType)index;
		}

   		Terminal.WriteError($"Settings: Invalid alert option index ({index}), defaulting to Sound for {optionButton.Name}.");
		optionButton.Selected = (int)AlertType.Sound;
		return AlertType.Sound;
	}

	private void OnAlertOptionChanged(long index, Control sound, Control tts, OptionButton button, Action<AlertType> onSettingChanged)
	{
		var type = ValidateAndGetAlertType(index,button);
		sound.Visible = type == AlertType.Sound;
		tts.Visible = type == AlertType.TTS;
		onSettingChanged(type);
	}

}
	public class UserSettingsData
	{
		public Settings.AlertType CraftAlertType {get; internal set;} = Settings.AlertType.Sound;
		public Settings.AlertType MOIAlertType {get; internal set;} = Settings.AlertType.Sound;
		public string CraftAlertSoundPath {get; internal set;} = "res://assets/sounds/craft.wav";

		public string MOIAlertSoundPath {get; internal set;} = "res://assets/sounds/moi.wav";
	}
