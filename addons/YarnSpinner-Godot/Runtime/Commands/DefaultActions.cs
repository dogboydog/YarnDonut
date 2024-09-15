#nullable disable
using System;
using System.Threading.Tasks;
using Godot;


namespace YarnSpinnerGodot;

public partial class DefaultActions : Godot.Node
{
    #region Commands

    /// <summary>
    /// Yarn Spinner defines two built-in commands: "wait", and "stop".
    /// Stop is defined inside the Virtual Machine (the compiler traps it
    /// and makes it a special case.) Wait is defined here in Godot.
    /// </summary>
    /// <param name="duration">How long to wait.</param>
    [YarnCommand("wait")]
    public static async Task Wait(double duration)
    {
        var sceneTree = (SceneTree)Engine.GetMainLoop();
        var timer = sceneTree.CreateTimer((float)duration);
        await sceneTree.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    #endregion
}