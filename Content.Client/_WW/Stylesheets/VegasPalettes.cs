using Content.Client.Stylesheets.Palette;

namespace Content.Client._WW.Stylesheets;

/// <summary>
/// New Vegas Pip-Boy style UI colours: amber text and outlines on near-black. Used by the Nanotrasen stylesheet
/// (the game's main one) while the <c>ww.ui_vegas_colors</c> option is on; StylesheetManager rebuilds it on change.
/// </summary>
public static class VegasPalettes
{
    /// <summary>
    /// Set by StylesheetManager before it builds the stylesheet.
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// The NV HUD amber.
    /// </summary>
    public static readonly Color Amber = Color.FromHex("#FFB642");

    /// <summary>
    /// Default label text: a slightly softer amber so long text stays readable.
    /// </summary>
    public static readonly Color Text = Color.FromHex("#F5C277");

    // Buttons and other elements: dark burnt amber, brighter on hover.
    public static readonly ColorPalette Primary = ColorPalette.FromHexBase("#7a5418",
        lightnessShift: 0.06f, chromaShift: 0.004f, text: Text);

    // Window and panel backgrounds: warm near-black.
    public static readonly ColorPalette Secondary = ColorPalette.FromHexBase("#3a3226",
        lightnessShift: 0.045f, text: Text);

    // Titles, headings, highlighted labels.
    public static readonly ColorPalette Highlight = ColorPalette.FromHexBase("#FFB642");

    public static readonly ColorPalette Positive = ColorPalette.FromHexBase("#5f9e3c");
    public static readonly ColorPalette Negative = ColorPalette.FromHexBase("#c0392b", chromaShift: 0.02f);
}
