/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/


#nullable enable

using Godot;

namespace YarnSpinnerGodot;

public partial class AsyncOptionItem : Control
{
    [Export] RichTextLabel? text;
    [Export] private BaseButton button;
    public YarnTaskCompletionSource<DialogueOption?>? OnOptionSelected;
    public System.Threading.CancellationToken completionToken;

    private bool hasSubmittedOptionSelection = false;

    public void FocusButton()
    {
        button.GrabFocus();
    }


    private DialogueOption? _option;

    public DialogueOption? Option
    {
        get => _option;

        set
        {
            if (value == null)
            {
                _option = null;
                return;
            }

            _option = value;

            hasSubmittedOptionSelection = false;

            // When we're given an Option, use its text and update our
            // interactibility.
            text!.Text = value.Line.TextWithoutCharacterName.Text;
            Visible = value.IsAvailable;
        }
    }

    public override void _Ready()
    {
        if (!IsInstanceValid(text))
        {
            GD.PushError($"No {text} {nameof(RichTextLabel)} is set on this {nameof(AsyncOptionItem)}");
        }

        if (!IsInstanceValid(button))
        {
            GD.PushError($"No {button} is set on this {nameof(AsyncOptionItem)}");
        }
        else
        {
            button.Connect(BaseButton.SignalName.Pressed, Callable.From(InvokeOptionSelected));
        }
    }

    public void InvokeOptionSelected()
    {
        // turns out that Selectable subclasses aren't intrinsically interactive/non-interactive
        // based on their canvasgroup, you still need to check at the moment of interaction
        if (!Visible)
        {
            return;
        }

        // We only want to invoke this once, because it's an error to
        // submit an option when the Dialogue Runner isn't expecting it. To
        // prevent this, we'll only invoke this if the flag hasn't been cleared already.
        if (hasSubmittedOptionSelection == false && !completionToken.IsCancellationRequested)
        {
            hasSubmittedOptionSelection = true;
            OnOptionSelected?.TrySetResult(this.Option);
        }
    }
}