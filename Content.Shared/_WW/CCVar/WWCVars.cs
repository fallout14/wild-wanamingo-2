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

    /// <summary>
    /// Bark voices (sounds while characters talk, picked in the character editor).
    /// </summary>
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("ww.barks_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Client volume for barks, 0-1 (options menu).
    /// </summary>
    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("ww.barks_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
