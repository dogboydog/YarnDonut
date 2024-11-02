extends Node
# Example of writing an options list view in GDScript
@export var option_view_prefab : PackedScene
@export var options_container: Container
@export var view_control: Control 

var option_selected_handler: Callable 

func _ready() -> void: 
	view_control.visible = false 
	
func run_options(options: Array, on_option_selected: Callable) -> void:
	print("Options: %s"  % JSON.stringify(options))
	option_selected_handler = on_option_selected
	for option in options:
		var option_view: SimpleGDScriptOptionView = option_view_prefab.instantiate() 
		option_view.set_option(option, select_option)
		options_container.add_child(option_view)
	
	view_control.visible = true 
	
func select_option(option_id: int) -> void:
	option_selected_handler.call(option_id)
	view_control.visible = false 
	while options_container.get_child_count() > 0:
		options_container.remove_child(options_container.get_child(0))
