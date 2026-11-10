using Content.Client._WW.Stylesheets;
using Content.Client.Stylesheets.Palette;

namespace Content.Client.Stylesheets.Stylesheets;

public partial class NanotrasenStylesheet
{
    // WW Change - New Vegas amber palettes while the ww.ui_vegas_colors option is on
    public override ColorPalette PrimaryPalette => VegasPalettes.Enabled ? VegasPalettes.Primary : Palettes.Navy;
    public override ColorPalette SecondaryPalette => VegasPalettes.Enabled ? VegasPalettes.Secondary : Palettes.Slate;
    public override ColorPalette PositivePalette => VegasPalettes.Enabled ? VegasPalettes.Positive : Palettes.Green;
    public override ColorPalette NegativePalette => VegasPalettes.Enabled ? VegasPalettes.Negative : Palettes.Red;
    public override ColorPalette HighlightPalette => VegasPalettes.Enabled ? VegasPalettes.Highlight : Palettes.Gold;
}
