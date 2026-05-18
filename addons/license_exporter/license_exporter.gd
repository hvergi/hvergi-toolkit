@tool
extends EditorPlugin
const ExportPlugin = preload("res://addons/license_exporter/export_plugin.gd")
var export_plugin = ExportPlugin.new()

func _enable_plugin() -> void:
	# Add autoloads here.
	pass


func _disable_plugin() -> void:
	# Remove autoloads here.
	pass


func _enter_tree() -> void:
	add_export_plugin(export_plugin)
	pass


func _exit_tree() -> void:
	remove_export_plugin(export_plugin)
	pass
