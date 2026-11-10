using Robust.Shared.Configuration;

namespace Content.Shared._WW.CCVar;

/// <summary>
/// Wild Wanamingo CVars.
/// </summary>
[CVarDefs]
public sealed class WWCVars
{
    /// <summary>
    /// New Vegas amber-on-black UI colours. Off = Wizden's navy/slate. Applied live.
    /// </summary>
    public static readonly CVarDef<bool> VegasUiColors =
        CVarDef.Create("ww.ui_vegas_colors", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
