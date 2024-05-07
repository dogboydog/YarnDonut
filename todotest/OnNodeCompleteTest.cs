using Godot;
using System;
using YarnSpinnerGodot;

public partial class OnNodeCompleteTest : Node
{
    [Export] private DialogueRunner _dialogueRunner;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _dialogueRunner.Connect(DialogueRunner.SignalName.onNodeComplete,
            Callable.From((string nodeName) => OnNodeComplete(nodeName)));
    }

    public void OnNodeComplete(string nodeName)
    {
        GD.Print($"Node complete: {nodeName}");
    }
}