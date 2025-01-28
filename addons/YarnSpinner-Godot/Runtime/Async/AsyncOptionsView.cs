using System.Collections.Generic;
#nullable enable
using System.Linq;
using System.Threading;
using Godot;
using Yarn;
using Node = Godot.Node;

namespace YarnSpinnerGodot;

/// <summary>
/// Receives options from a <see cref="DialogueRunner"/>, and displays and
/// manages a collection of <see cref="AsyncOptionItem"/> views for the user
/// to choose from.
/// </summary>
[GlobalClass]
public partial class AsyncOptionsView : Node, AsyncDialogueViewBase
{
    [Export] Control? viewControl;

    [Export] PackedScene? optionViewPrefab;

    // A cached pool of OptionView objects so that we can reuse them
    List<AsyncOptionItem> optionViews = new List<AsyncOptionItem>();

    [Export] bool showsLastLine;

    [Export] RichTextLabel? lastLineText;

    [Export] CanvasItem? lastLineContainer;

    [Export] RichTextLabel? lastLineCharacterNameText;

    [Export] CanvasItem? lastLineCharacterNameContainer;

    /// <summary>
    /// The node that options will be parented to.
    /// You can use a BoxContainer to automatically lay out your options.
    /// </summary>
    [Export] public Godot.Node? optionParent;

    LocalizedLine? lastSeenLine;

    /// <summary>
    /// Controls whether or not to display options whose <see
    /// cref="OptionSet.Option.IsAvailable"/> value is <see
    /// langword="false"/>.
    /// </summary>
    [Export] public bool showUnavailableOptions = false;

    /// <summary>
    /// Controls whether the options list view should fade in when options appear, and
    /// fade out when options disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="viewControl"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeUpDuration"/> seconds when options
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeDownDuration"/> seconds when options disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="viewControl"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="viewControl"/>
    /// <seealso cref="fadeUpDuration"/>
    /// <seealso cref="fadeDownDuration"/>
    [Export] public bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade options in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeUpDuration = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade options out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeDownDuration = 0.1f;

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to dismiss the options view
    /// when dialogue is complete.
    /// </summary>
    /// <returns>A completed task.</returns>
    public YarnTask OnDialogueCompleteAsync()
    {
        lastSeenLine = null;
        if (IsInstanceValid(lastLineText))
        {
            lastLineText.Visible = false;
        }
        if (IsInstanceValid(lastLineCharacterNameContainer))
        {
            lastLineCharacterNameContainer!.Visible = false;
        }
        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by Godot to set up the object.
    /// </summary>
    public override void _Ready()
    {
        if (!IsInstanceValid(viewControl) || !IsInstanceValid(optionParent))
        {
            GD.PushError(
                $"Make sure to set both {nameof(viewControl)} and {optionParent} on this {nameof(AsyncOptionsView)}");
        }
        else
        {
            viewControl!.Visible = false;
        }

        if (optionParent is Node2D parent2D && IsInstanceValid(parent2D))
        {
            parent2D.Visible = false;
        }

        if (!IsInstanceValid(lastLineContainer) && lastLineText != null)
        {
            lastLineContainer = lastLineText;
        }

        if (!IsInstanceValid(lastLineCharacterNameContainer) && lastLineCharacterNameText != null)
        {
            lastLineCharacterNameContainer = lastLineCharacterNameText;
        }
    }

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to set up the options view
    /// when dialogue begins.
    /// </summary>
    /// <returns>A completed task.</returns>
    public YarnTask OnDialogueStartedAsync()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl!.Visible = false;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> when a line needs to be
    /// presented, and stores the line as the 'last seen line' so that it
    /// can be shown when options appear.
    /// </summary>
    /// <remarks>This view does not display lines directly, but instead
    /// stores lines so that when options are run, the last line that ran
    /// before the options appeared can be shown.</remarks>
    /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync"
    /// path="/param"/>
    /// <returns>A completed task.</returns>
    public YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (showsLastLine)
        {
            lastSeenLine = line;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to display a collection of
    /// options to the user. 
    /// </summary>
    /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
    /// path="/param"/>
    /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
    /// path="/returns"/>
    public async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
        if (!IsInstanceValid(optionParent))
        {
            throw new System.InvalidOperationException(
                $"Can't display options from {nameof(AsyncOptionsView)}. No {nameof(optionParent)} is set " +
                $"to parent the options to.");
        }

        // If we don't already have enough option views, create more
        while (dialogueOptions.Length > optionViews.Count)
        {
            var optionView = CreateNewOptionView();
            optionViews.Add(optionView);
        }

        // A completion source that represents the selected option.
        YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource =
            new YarnTaskCompletionSource<DialogueOption?>();

        // A cancellation token source that becomes cancelled when any
        // option item is selected, or when this entire option view is
        // cancelled
        var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async YarnTask CancelSourceWhenDialogueCancelled()
        {
            await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

            if (cancellationToken.IsCancellationRequested == true)
            {
                // The overall cancellation token was fired, not just our
                // internal 'something was selected' cancellation token.
                // This means that the dialogue view has been informed that
                // any value it returns will not be used. Set a 'null'
                // result on our completion source so that that we can get
                // out of here as quickly as possible.
                selectedOptionCompletionSource.TrySetResult(null);
            }
        }

        // Start waiting 
        CancelSourceWhenDialogueCancelled().Forget();

        // tracks the options views created so we can use it to configure the interaction correctly
        int optionViewsCreated = 0;
        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            var optionView = optionViews[i];
            var option = dialogueOptions[i];

            if (option.IsAvailable == false && showUnavailableOptions == false)
            {
                // option is unavailable, skip it
                continue;
            }

            optionView.Visible = true;
            optionView.Option = option;

            optionView.OnOptionSelected = selectedOptionCompletionSource;
            optionView.completionToken = completionCancellationSource.Token;


            optionViewsCreated += 1;
        }
        // The first available option is selected by default

        optionViews.First(view => view.Visible).FocusButton();

        // Update the last line, if one is configured
        if (IsInstanceValid(lastLineContainer))
        {
            if (lastSeenLine != null && showsLastLine)
            {
                // if we have a last line character name container
                // and the last line has a character then we show the nameplate
                // otherwise we turn off the nameplate
                var line = lastSeenLine.Text;
                if (IsInstanceValid(lastLineCharacterNameContainer))
                {
                    if (string.IsNullOrWhiteSpace(lastSeenLine.CharacterName))
                    {
                        lastLineCharacterNameContainer.Visible = false;
                    }
                    else
                    {
                        line = lastSeenLine.TextWithoutCharacterName;
                        lastLineCharacterNameContainer.Visible = true;
                        if (lastLineCharacterNameText != null)
                        {
                            lastLineCharacterNameText.Text = lastSeenLine.CharacterName;
                        }
                    }
                }
                else
                {
                    line = lastSeenLine.TextWithoutCharacterName;
                }

                if (lastLineText != null)
                {
                    lastLineText.Text = line.Text;
                }

                lastLineContainer.Visible = true;
            }
            else
            {
                lastLineContainer.Visible = false;
            }
        }
        // allow interactivity and wait for an option to be selected

        if (IsInstanceValid(viewControl))
        {
            viewControl!.Visible = true;
        }

        var parent2D = optionParent as Node2D;
        if (IsInstanceValid(parent2D))
        {
            parent2D!.Visible = true;
        }


        if (useFadeEffect)
        {
            // fade up the UI now
            await Effects.FadeAlphaAsync(viewControl, 0, 1, fadeUpDuration,
                cancellationToken);
        }


        // Wait for a selection to be made, or for the task to be completed.
        var completedTask = await selectedOptionCompletionSource.Task;
        completionCancellationSource.Cancel();

        if (useFadeEffect)
        {
            // fade down
            await Effects.FadeAlphaAsync(viewControl, 1, 0, fadeDownDuration,
                cancellationToken);
        }

        // disabling ALL the options views now
        foreach (var optionView in optionViews)
        {
            optionView.Visible = false;
        }

        if (IsInstanceValid(viewControl))
        {
            viewControl!.Visible = false;
        }

        if (IsInstanceValid(parent2D))
        {
            parent2D!.Visible = false;
        }

        await YarnTask.NextFrame();

        // if we are cancelled we still need to return, but we don't want to have a selection, so we return no selected option
        if (cancellationToken.IsCancellationRequested)
        {
            return await DialogueRunner.NoOptionSelected;
        }

        // finally we return the selected option
        return completedTask;
    }

    private AsyncOptionItem CreateNewOptionView()
    {
        if (optionViewPrefab == null)
        {
            throw new System.InvalidOperationException(
                $"Can't create new option view: {nameof(optionViewPrefab)} is null");
        }

        var optionView = optionViewPrefab.Instantiate<AsyncOptionItem>();


        if (optionView == null)
        {
            throw new System.InvalidOperationException($"Can't create new option view: {nameof(optionView)} is null");
        }


        optionParent!.AddChild(optionView);
        optionView.Visible = false;

        return optionView;
    }
}