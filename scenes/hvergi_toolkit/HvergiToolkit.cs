using Godot;
using System;

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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Terminal.Output = terminalOutput;
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
