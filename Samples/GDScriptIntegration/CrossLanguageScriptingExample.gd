# Demonstrates interacting with C# YarnSpinner-Godot 
# components from GDScript. 

extends Control
@export var dialogue_runner: DialogueRunner  
@export var ys_logo: Control 
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	dialogue_runner.AddCommandHandlerCallable("show_ys_logo", show_ys_logo) 
	dialogue_runner.onDialogueComplete.connect(on_dialogue_complete)
func show_ys_logo() -> void: 
	print("Showing logo...")
	ys_logo.visible = true 
	
func on_dialogue_complete() -> void: 
	print("Dialogue completed.")
