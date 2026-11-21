namespace Content.Server._WW.Power;

/// <summary>
/// A fixed solar panel that follows the map's day/night cycle (<see cref="Content.Shared.Light.Components.LightCycleComponent"/>)
/// instead of Wizden's tracked space sun: full output at midday, nothing at night or under a roof.
/// No solar control console needed.
/// </summary>
[RegisterComponent, Access(typeof(WastelandSolarSystem))]
public sealed partial class WastelandSolarPanelComponent : Component
{
    /// <summary>
    /// Watts in full sun.
    /// </summary>
    [DataField]
    public float MaxSupply = 1000f;

    /// <summary>
    /// The last daylight factor applied (0 to 1).
    /// </summary>
    [ViewVariables]
    public float Sunlight;
}
