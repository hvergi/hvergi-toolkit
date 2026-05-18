@tool
extends EditorExportPlugin

const LICENSE_SOURCE = "res://GODOTLICENSE.txt" # Change this to your license file's path
const LICENSE_DESTINATION = "GODOTLICENSE.txt" # Name of the file in the exported folder

func _get_name() -> String:
	return "LicenseExporter"

func _export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int) -> void:
	# Get the folder directory where the binary is being exported
	var export_dir = path.get_base_dir()
	var dest_path = export_dir.path_join(LICENSE_DESTINATION)
	
	# Copy the license file directly to the export folder
	var dir = DirAccess.open(export_dir)
	if dir:
		var source_file = LICENSE_SOURCE
		# Fallback to local system if checking 'res://' path fails outside of editor
		if not FileAccess.file_exists(source_file):
			printerr("License file not found at: ", source_file)
			return
			
		var err = dir.copy(source_file, dest_path)
		if err != OK:
			printerr("Failed to export license file. Error code: ", err)
		else:
			print("Successfully exported license file to: ", dest_path)
