#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Yarn.Markup;

namespace YarnSpinnerGodot;

[GlobalClass]
public partial class TextLineProvider : LineProviderBehaviour
{
    /// <summary>Specifies the language code to use for text content
    /// for this <see cref="TextLineProvider"/>.
    /// </summary>
    [Language] [Export] public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

    public override async YarnTask<LocalizedLine> GetLocalizedLineAsync(Yarn.Line line, CancellationToken cancellationToken)
    {
        string text;
        // By default, this provider will treat "en" as matching "en-UK", "en-US" etc. You can 
        // remap language codes how you like if you don't want this behavior 
        if (textLanguageCode.ToLower().StartsWith(YarnProject.baseLocalization.LocaleCode.ToLower()))
        {
            text = YarnProject.baseLocalization.GetLocalizedString(line.ID);
        }
        else
        {
            text = Tr($"{line.ID}");
            // fall back to base locale
            if (text.Equals(line.ID))
            {
                text = YarnProject.baseLocalization.GetLocalizedString(line.ID);
            }
        }

        return new LocalizedLine()
        {
            TextID = line.ID,
            RawText = text,
            Substitutions = line.Substitutions,
            Metadata = YarnProject.LineMetadata.GetMetadata(line.ID),
        };
    }

    public override void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor markerProcessor)
    {
        throw new NotImplementedException();
    }

    public override void DeregisterMarkerProcessor(string attributeName)
    {
        throw new NotImplementedException();
    }


    public override bool LinesAvailable => YarnProject?.baseLocalization?.stringTable != null;

    public override string LocaleCode
    {
        get => textLanguageCode;
        set => textLanguageCode = value;
    }
}