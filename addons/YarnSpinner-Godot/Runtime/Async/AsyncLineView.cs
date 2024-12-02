using System.Collections.Generic;
using Yarn.Markup;
#nullable enable
using System.Diagnostics;
using System.Threading;
using Godot;


namespace YarnSpinnerGodot;

/// <summary>
/// A Dialogue View that presents lines of dialogue, using Unity UI
/// elements.
/// </summary>
public partial class AsyncLineView : AsyncDialogueViewBase
{
    [Export] public DialogueRunner? dialogueRunner;

    /// <summary>
    /// The canvas group that contains the UI elements used by this Line
    /// View.
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="CanvasGroup"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public Control? viewControl;

    /// <summary>
    /// The <see cref="RichTextLabel"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    [Export] public RichTextLabel? lineText;

    /// <summary>
    /// The <see cref="Button"/> that represents an on-screen button that
    /// the user can click to continue to the next piece of dialogue.
    /// </summary>
    /// <remarks>
    /// <para>This button's game object will be made inactive when a line
    /// begins appearing, and active when the line has finished
    /// appearing.</para>
    /// <para>When the button is clicked, <see cref="dialogueRunner"/>'s
    /// <see cref="DialogueRunner.RequestNextLine"/> will be called to
    /// signal that the current line should finish up.</para>
    /// </remarks>
    /// <seealso cref="autoAdvance"/>
    public Button? continueButton;

    /// <summary>
    /// Controls whether the <see cref="lineText"/> object will show the
    /// character name present in the line or not.
    /// </summary>
    /// <remarks>
    /// <para style="note">This value is only used if <see
    /// cref="characterNameText"/> is <see langword="null"/>.</para>
    /// <para>If this value is <see langword="true"/>, any character names
    /// present in a line will be shown in the <see cref="lineText"/>
    /// object.</para>
    /// <para>If this value is <see langword="false"/>, character names will
    /// not be shown in the <see cref="lineText"/> object.</para>
    /// </remarks>
    [Export] public bool showCharacterNameInLineView = true;

    /// <summary>
    /// The <see cref="RichTextLabel"/> object that displays the character
    /// names found in dialogue lines.
    /// </summary>
    /// <remarks>
    /// If the <see cref="LineView"/> receives a line that does not contain
    /// a character name, this object will be left blank.
    /// </remarks>
    [Export] public RichTextLabel? characterNameText;

    /// <summary>
    /// The game object that holds the <see cref="characterNameText"/> text
    /// field.
    /// </summary>
    /// <remarks>
    /// This is needed in situations where the character name is contained
    /// within an entirely different game object. Most of the time this will
    /// just be the same game object as <see cref="characterNameText"/>.
    /// </remarks>
    [Export] public CanvasItem? characterNameContainer = null;


    /// <summary>
    /// Controls whether the line view should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="viewControl"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeUpDuration"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeDownDuration"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="viewControl"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="viewControl"/>
    /// <seealso cref="fadeUpDuration"/>
    /// <seealso cref="fadeDownDuration"/>
    [Export] public bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeUpDuration = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeDownDuration = 0.1f;


    /// <summary>
    /// Controls whether this Line View will automatically to the Dialogue
    /// Runner that the line is complete as soon as the line has finished
    /// appearing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this value is true, the Line View will 
    /// </para>
    /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
    /// proceed to the next piece of content (e.g. the next line, or the
    /// next options) until all Dialogue Views have reported that they have
    /// finished presenting their lines. If a <see cref="LineView"/> doesn't
    /// report that it's finished until it receives input, the <see
    /// cref="DialogueRunner"/> will end up pausing.</para>
    /// <para>
    /// This is useful for games in which you want the player to be able to
    /// read lines of dialogue at their own pace, and give them control over
    /// when to advance to the next line.</para></para>
    /// </remarks>
    [Export] public bool autoAdvance = false;

    /// <summary>
    /// The amount of time after the line finishes appearing before
    /// automatically ending the line, in seconds.
    /// </summary>
    /// <remarks>This value is only used when <see cref="autoAdvance"/> is
    /// <see langword="true"/>.</remarks>
    [Export] public float autoAdvanceDelay = 1f;


    // typewriter fields

    /// <summary>
    /// Controls whether the text of <see cref="lineText"/> should be
    /// gradually revealed over time.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="lineText"/> object's <see
    /// cref="RichTextLabel.maxVisibleCharacters"/> property will animate from 0
    /// to the length of the text, at a rate of <see
    /// cref="typewriterEffectSpeed"/> letters per second when the line
    /// appears. <see cref="onCharacterTyped"/> is called for every new
    /// character that is revealed.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="lineText"/> will all be revealed at the same time.</para>
    /// <para style="note">If <see cref="useFadeEffect"/> is <see
    /// langword="true"/>, the typewriter effect will run after the fade-in
    /// is complete.</para>
    /// </remarks>
    /// <seealso cref="lineText"/>
    /// <seealso cref="onCharacterTyped"/>
    /// <seealso cref="typewriterEffectSpeed"/>
    [Export] public bool useTypewriterEffect = true;

    /// <summary>
    /// The number of characters per second that should appear during a
    /// typewriter effect.
    /// </summary>
    [Export] public int typewriterEffectSpeed = 60;


    /// <summary>
    /// A signal that is emitted each time a character is revealed
    /// during a typewriter effect.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onCharacterTypedEventHandler();

    /// <summary>
    /// A Unity Event that is called when a pause inside of the typewriter effect occurs.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onPauseStartedEventHandler();

    /// <summary>
    /// A Unity Event that is called when a pause inside of the typewriter effect finishes and the typewriter has started once again.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onPauseEndedEventHandler();

    private TypewriterHandler? typewriter;

    /// <summary>
    /// A list of <see cref="TemporalMarkupHandler"/> objects that will be
    /// used to handle markers in the line.
    /// </summary>
    public List<TemporalMarkupHandler> temporalProcessors = new List<TemporalMarkupHandler>();

    /// <inheritdoc/>
    public override YarnTask OnDialogueCompleteAsync()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = true;
        }

        return YarnTask.CompletedTask;
    }

    /// <inheritdoc/>
    public override YarnTask OnDialogueStartedAsync()
    {
        if (IsInstanceValid(viewControl))
        {
            viewControl.Visible = true;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by Unity on first frame.
    /// </summary>
    protected void Awake()
    {
        if (useTypewriterEffect)
        {
            typewriter = new TypewriterHandler();
            AddChild(typewriter);
            typewriter.lettersPerSecond = typewriterEffectSpeed;
            typewriter.onCharacterTyped += () => EmitSignal(SignalName.onCharacterTyped);
            temporalProcessors.Add(typewriter);
        }

        if (characterNameContainer == null && characterNameText != null)
        {
            characterNameContainer = characterNameText;
        }

        if (dialogueRunner == null)
        {
            // If we weren't provided with a dialogue runner at design time, try to find one now
            dialogueRunner = DialogueRunner.FindChild(nameof(DialogueRunner)) as DialogueRunner;
            if (dialogueRunner == null)
            {
                GD.PushWarning(
                    $"{nameof(AsyncLineView)} failed to find a dialogue runner! Please ensure that a {nameof(DialogueRunner)} is present, or set the {nameof(dialogueRunner)} property in the Inspector.",
                    this);
            }
        }
    }

    /// <summary>Presents a line using the configured text view.</summary>
    /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/param"/>
    /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/returns"/>
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (lineText == null)
        {
            GD.PushError($"Line view does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
            return;
        }

        MarkupParseResult text;

        // configuring the text fields
        if (showCharacterNameInLineView)
        {
            if (characterNameText == null)
            {
                GD.PushWarning(
                    $"Line view is configured to show character names, but no character name text view was provided.",
                    this);
            }
            else
            {
                characterNameText.Text = line.CharacterName;
            }

            text = line.TextWithoutCharacterName;
        }
        else
        {
            // we don't want to show character names but do have a valid container for showing them
            // so we should just disable that and continue as if it didn't exist
            if (IsInstanceValid(characterNameContainer))
            {
                characterNameContainer!.Visible = false;
            }

            text = line.Text;
        }

        lineText.Text = text.Text;

        var continueHandler = Callable.From(OnContinuePressed);
        // setting the continue button up to let us advance dialogue
        if (continueButton != null)
        {
            continueButton.Connect(BaseButton.SignalName.Pressed, continueHandler);
        }

        // letting every temporal processor know that fade up (if set) is about to begin
        if (temporalProcessors.Count > 0)
        {
            foreach (var processor in temporalProcessors)
            {
                processor.PrepareForLine(text, lineText);
            }
        }

        if (IsInstanceValid(viewControl))
        {
            // fading up the UI
            if (useFadeEffect)
            {
                await Effects.FadeAlphaAsync(viewControl, 0, 1, fadeDownDuration, token.HurryUpToken);
            }
            else
            {
                // We're not fading up, so set the canvas group's alpha to 1 immediately.
                viewControl.Visible = true;
                var oldModulate = viewControl.Modulate;
                viewControl.Modulate = new Color(oldModulate.R, oldModulate.G, oldModulate.B, 1.0f);
            }
        }

        if (temporalProcessors.Count > 0)
        {
            // letting every temporal processor know that fading is done and display is about to begin
            foreach (var processor in temporalProcessors)
            {
                processor.OnLineTextWillAppear(text, lineText);
            }

            // going through each character of the line and letting the processors know about it
            for (int i = 0; i < text.Text.Length; i++)
            {
                // telling every processor that it is time to process the current character
                foreach (var processor in temporalProcessors)
                {
                    // if the typewriter exists we need to turn it on and off depending on if a line is blocking or not
                    if (useTypewriterEffect)
                    {
                        if (typewriter == null)
                        {
                            throw new System.InvalidOperationException(
                                $"Error when displaying line: {nameof(useTypewriterEffect)} was enabled but {nameof(typewriter)} is null?");
                        }

                        var task = processor.PresentCharacter(i, lineText, token.HurryUpToken);
                        if (!task.IsCompleted() && processor != typewriter)
                        {
                            typewriter.StopwatchRunning = false;
                        }

                        await task;
                        typewriter.StopwatchRunning = true;
                    }
                    else
                    {
                        await processor.PresentCharacter(i, lineText, token.HurryUpToken);
                    }
                }
            }

            // letting each temporal processor know the line has finished displaying
            foreach (var processor in temporalProcessors)
            {
                processor.OnLineDisplayComplete();
            }
        }

        // if we are set to autoadvance how long do we hold for before continuing?
        if (autoAdvance)
        {
            await YarnTask.Delay((int) (autoAdvanceDelay * 1000), token.NextLineToken);
        }
        else
        {
            await YarnTask.WaitUntilCanceled(token.NextLineToken);
        }

        if (IsInstanceValid(viewControl))
        {
            // we fade down the UI
            if (useFadeEffect)
            {
                await Effects.FadeAlphaAsync(viewControl, 1, 0, fadeDownDuration, token.HurryUpToken);
            }
            else
            {
                viewControl.Visible = false;
            }
        }

        // the final bit of clean up is to remove the cancel listener from the button
        if (continueButton != null)
        {
            continueButton.Disconnect(BaseButton.SignalName.Pressed, Callable.From(OnContinuePressed));
        }
    }

    /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/summary"/> 
    /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/param"/> 
    /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/returns"/> 
    /// <remarks>
    /// This dialogue view does not handle any options.
    /// </remarks>
    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
        return YarnTask<DialogueOption?>.FromResult(null);
    }

    private void OnContinuePressed()
    {
        if (!IsInstanceValid(dialogueRunner))
        {
            GD.PushWarning($"Continue button was clicked, but {nameof(dialogueRunner)} is invalid!", this);
            return;
        }

        dialogueRunner!.RequestNextLine();
    }
}

/// <summary>
/// A <see cref="TemporalMarkupHandler"/> is an object that reacts to the
/// delivery of a line of dialogue, and can optionally control the timing of
/// that delivery.
/// </summary>
/// <remarks>
/// <para>
/// There are a number of cases where a line's delivery needs to have its
/// timing controlled. For example, <see cref="TypewriterHandler"/> adds a
/// small delay between each character, creating a 'typewriter' effect as
/// each letter appears over time.
/// </para>
/// <para>
/// Another example of a <see cref="TemporalMarkupHandler"/> is an in-line
/// event or animation, such as causing a character to play an animation
/// (and waiting for that animation to complete before displaying the rest
/// of the line).
/// </para>
/// </remarks>
public abstract partial class TemporalMarkupHandler : Node
{
    /// <summary>
    /// Called when the line view receives the line, to prepare for showing
    /// the line.
    /// </summary>
    /// <remarks>
    /// This method is called before any part of the line is visible, and is
    /// an opportunity to set up any part of the <see
    /// cref="TemporalMarkupHandler"/>'s display before the user can see it.
    /// </remarks>
    /// <param name="line">The line being presented.</param>
    /// <param name="text">A <see cref="RichTextLabel"/> object that the line is
    /// being displayed in.</param>
    public abstract void PrepareForLine(MarkupParseResult line, RichTextLabel text);

    /// <summary>
    /// Called immediately before the first character in the line is
    /// presented. 
    /// </summary>
    /// <param name="line">The line being presented.</param>
    /// <param name="text">A <see cref="RichTextLabel"/> object that the line is
    /// being displayed in.</param>
    public abstract void OnLineTextWillAppear(MarkupParseResult line, RichTextLabel text);

    /// <summary>
    /// Called repeatedly for each visible character in the line.
    /// </summary>
    /// <remarks> This method is a <see cref="TemporalMarkupHandler"/>
    /// object's main opportunity to take action during line
    /// display.</remarks>
    /// <param name="currentCharacterIndex">The zero-based index of the
    /// character being displayed.</param>
    /// <param name="text">A <see cref="RichTextLabel"/> object that the line is
    /// being displayed in.</param>
    /// <param name="cancellationToken">A cancellation token representing
    /// whether the </param>
    /// <returns>A task that completes when the <see
    /// cref="TemporalMarkupHandler"/> has completed presenting this
    /// character. Dialogue views will wait until this task is complete
    /// before displaying the remainder of the line.</returns>
    public abstract YarnTask PresentCharacter(int currentCharacterIndex, RichTextLabel text,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called after the last call to <see cref="PresentCharacter(int,
    /// RichTextLabel, CancellationToken)"/>.
    /// </summary>
    /// <remarks>This method is an opportunity for a <see
    /// cref="TemporalMarkupHandler"/> to finalise its presentation after
    /// all of the characters in the line have been presented.</remarks>
    public abstract void OnLineDisplayComplete();
}

/// <summary>
/// A <see cref="TemporalMarkupHandler"/> that progressively reveals the
/// text of a line at a fixed rate of time.
/// </summary>
public sealed partial class TypewriterHandler : TemporalMarkupHandler
{
    /// <summary>
    /// The number of letters that will be shown per second.
    /// </summary>
    public int lettersPerSecond = 60;

    /// <summary>
    /// A signal that is emitted each time a character is revealed
    /// during a typewriter effect.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onCharacterTypedEventHandler();

    private double accumulatedTime = 0;
    private Stack<(int position, float duration)> pauses = new Stack<(int position, float duration)>();
    private float accumulatedPauses = 0;

    private float SecondsPerLetter
    {
        get { return 1f / lettersPerSecond; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see
    /// cref="TypewriterHandler"/> is currently tracking the amount of time
    /// elapsed since the last letter was revealed.
    /// </summary>
    internal bool StopwatchRunning { get; set; } = false;

    public override void _Process(double deltaTime)
    {
        if (StopwatchRunning)
        {
            accumulatedTime += deltaTime;
        }
    }

    /// <summary>Resets the typewriter back to the initial state.</summary>
    /// <inheritdoc cref="TemporalMarkupHandler.PrepareForLine" path="/param"/>
    public override void PrepareForLine(MarkupParseResult line, RichTextLabel text)
    {
        text.VisibleRatio = 0;
        accumulatedPauses = 0;
    }

    /// <summary>Calculates the points in the typewriter should pause at,
    /// and starts the typewriter's internal time counter.</summary>
    /// <inheritdoc cref="TemporalMarkupHandler.OnLineTextWillAppear"
    /// path="/param"/>
    public override void OnLineTextWillAppear(MarkupParseResult line, RichTextLabel text)
    {
        pauses = DialogueRunner.GetPauseDurationsInsideLine(line);

        accumulatedTime = 0;
        StopwatchRunning = true;
    }

    /// <summary>
    /// Stops the internal time counter, and finishes displaying the line.
    /// </summary>
    public override void OnLineDisplayComplete()
    {
        StopwatchRunning = false;
        accumulatedTime = 0;
        pauses.Clear();
        accumulatedPauses = 0;
    }

    /// <summary>
    /// Presents a single character of the line, at the appropriate point in
    /// time.
    /// </summary>
    /// <inheritdoc cref="TemporalMarkupHandler.PresentCharacter(int,
    /// RichTextLabel, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="TemporalMarkupHandler.PresentCharacter(int,
    /// RichTextLabel, CancellationToken)" path="/returns"/>
    public override async YarnTask PresentCharacter(int currentCharacterIndex, RichTextLabel text,
        CancellationToken cancellationToken)
    {
        float pauseDuration = 0;
        if (pauses.Count > 0)
        {
            var pause = pauses.Peek();
            if (pause.position == currentCharacterIndex)
            {
                pauses.Pop();
                pauseDuration = pause.duration;
            }
        }

        accumulatedPauses += pauseDuration;

        float timePoint = accumulatedPauses;
        if (lettersPerSecond > 0)
        {
            timePoint += (float) currentCharacterIndex * SecondsPerLetter;
        }

        await YarnTask.WaitUntil(() => accumulatedTime >= timePoint, cancellationToken);
        text.VisibleCharacters += 1;

        EmitSignal(SignalName.onCharacterTyped);
    }
}