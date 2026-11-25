// WW: character editor controls for the Misfits character extras: bark voice (ported from Misfits/Corvax Forge
// Content.Client/_NC/Barks/HumanoidProfileEditor.Barks.cs), vocal style (Misfits HumanoidProfileEditor.SpeechVerb.cs)
// and body size sliders (Misfits/Einstein Engines height and width).

using System.Linq;
using Content.Client._Forge.Barks;
using Content.Shared._Forge.Barks;
using Content.Shared._WW.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Speech;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<BarkPrototype> _barkVoices = new();
    private List<SpeechVerbPrototype> _vocalStyles = new();

    private void InitializeCharacterExtras()
    {
        _barkVoices = _prototypeManager.EnumeratePrototypes<BarkPrototype>()
            .Where(b => b.RoundStart)
            .OrderBy(b => b.Name)
            .ToList();

        _vocalStyles = _prototypeManager.EnumeratePrototypes<SpeechVerbPrototype>()
            .Where(v => v.VocalStyleSelectable)
            .OrderBy(v => v.ID != "Default")
            .ThenBy(v => Loc.GetString(v.Name))
            .ToList();

        BarkVoiceButton.OnItemSelected += args =>
        {
            BarkVoiceButton.SelectId(args.Id);
            Profile = Profile?.WithBarkVoice(_barkVoices[args.Id].ID);
            SetDirty();
        };
        BarkVoicePlayButton.OnPressed += _ =>
        {
            if (Profile != null)
                _entManager.System<BarkSystem>().RequestPreviewBark(Profile.BarkVoice);
        };

        SpeechVerbButton.OnItemSelected += args =>
        {
            SpeechVerbButton.SelectId(args.Id);
            Profile = Profile?.WithSpeechVerb(_vocalStyles[args.Id].ID);
            SetDirty();
        };

        HeightSlider.OnValueChanged += _ => SetBodySize(HeightSlider.Value, null);
        WidthSlider.OnValueChanged += _ => SetBodySize(null, WidthSlider.Value);
        HeightReset.OnPressed += _ => HeightSlider.Value = 1f;
        WidthReset.OnPressed += _ => WidthSlider.Value = 1f;
    }

    private void UpdateCharacterExtrasControls()
    {
        if (Profile == null)
            return;

        BarkContainer.Visible = _cfgManager.GetCVar(WWCVars.BarksEnabled) && _barkVoices.Count > 0;
        BarkVoiceButton.Clear();
        for (var i = 0; i < _barkVoices.Count; i++)
        {
            BarkVoiceButton.AddItem(_barkVoices[i].Name, i);
        }

        var bark = _barkVoices.FindIndex(b => b.ID == Profile.BarkVoice);
        if (bark >= 0)
            BarkVoiceButton.SelectId(bark);

        SpeechVerbButton.Clear();
        for (var i = 0; i < _vocalStyles.Count; i++)
        {
            SpeechVerbButton.AddItem(Loc.GetString(_vocalStyles[i].Name), i);
        }

        var verb = _vocalStyles.FindIndex(v => v.ID == Profile.SpeechVerb);
        if (verb >= 0)
            SpeechVerbButton.SelectId(verb);

        UpdateSizeSliders();
    }

    private void UpdateSizeSliders()
    {
        if (Profile == null || !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species))
            return;

        HeightContainer.Visible = species.SizeAdjustable;
        WidthContainer.Visible = species.SizeAdjustable;

        HeightSlider.MinValue = species.MinHeight;
        HeightSlider.MaxValue = species.MaxHeight;
        HeightSlider.SetValueWithoutEvent(Math.Clamp(Profile.Height, species.MinHeight, species.MaxHeight));
        WidthSlider.MinValue = species.MinWidth;
        WidthSlider.MaxValue = species.MaxWidth;
        WidthSlider.SetValueWithoutEvent(Math.Clamp(Profile.Width, species.MinWidth, species.MaxWidth));
        UpdateSizeLabels();
    }

    private void UpdateSizeLabels()
    {
        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("percent", (int) MathF.Round(HeightSlider.Value * 100)));
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("percent", (int) MathF.Round(WidthSlider.Value * 100)));
    }

    private void SetBodySize(float? height, float? width)
    {
        if (Profile == null)
            return;

        if (height != null)
            Profile = Profile.WithHeight(MathF.Round(height.Value, 2));
        if (width != null)
            Profile = Profile.WithWidth(MathF.Round(width.Value, 2));

        UpdateSizeLabels();
        ReloadProfilePreview();
    }
}
