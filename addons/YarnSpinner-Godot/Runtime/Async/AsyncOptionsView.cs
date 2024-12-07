using System.Collections.Generic;
#nullable enable
using System.Linq;
using System.Threading;
using Godot;
using Yarn;

namespace YarnSpinnerGodot;

/// <summary>
/// Receives options from a <see cref="DialogueRunner"/>, and displays and
/// manages a collection of <see cref="AsyncOptionItem"/> views for the user
/// to choose from.
/// </summary>
public partial class AsyncOptionsView : AsyncDialogueViewBase
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
    [Export] public Godot.Node optionParent;

    LocalizedLine? lastSeenLine;

    /// <summary>
    /// Controls whether or not to display options whose <see
    /// cref="OptionSet.Option.IsAvailable"/> value is <see
    /// langword="false"/>.
    /// </summary>
    [Export] public bool showUnavailableOptions = false;

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to dismiss the options view
    /// when dialogue is complete.
    /// </summary>
    /// <returns>A completed task.</returns>
    public override YarnTask OnDialogueCompleteAsync()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = false;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by Godot to set up the object.
    /// </summary>
    public override void _Ready()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = false;
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
    public override YarnTask OnDialogueStartedAsync()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = false;
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
    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
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
    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
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

        optionViews.First(view => view.Visible).GrabFocus();

        // Update the last line, if one is configured
        if (lastLineContainer != null)
        {
            if (lastSeenLine != null && showsLastLine)
            {
                // if we have a last line character name container
                // and the last line has a character then we show the nameplate
                // otherwise we turn off the nameplate
                var line = lastSeenLine.Text;
                if (lastLineCharacterNameContainer != null)
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

        // fade up the UI now
        await Effects.FadeAlphaAsync(viewControl, 0, 1, 1, cancellationToken);

        // allow interactivity and wait for an option to be selected
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = true;
        }

        // Wait for a selection to be made, or for the task to be completed.
        var completedTask = await selectedOptionCompletionSource.Task;
        completionCancellationSource.Cancel();


        // fade down
        await Effects.FadeAlphaAsync(viewControl, 1, 0, 1, cancellationToken);

        // disabling ALL the options views now
        foreach (var optionView in optionViews)
        {
            optionView.Visible = false;
        }

        await YarnTask.NextFrame();

        // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
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

        optionParent.AddChild(optionView);
        optionView.Visible = false;

        return optionView;
    }
}