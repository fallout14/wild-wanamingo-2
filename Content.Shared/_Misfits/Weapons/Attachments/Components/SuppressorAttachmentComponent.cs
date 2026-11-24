// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/Attachments/Components/SuppressorAttachmentComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.Weapons.Attachments.Components;

/// <summary>
/// Replaces the host firearm's report and optionally hides its muzzle flash while installed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SuppressorAttachmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundGunshot = new SoundPathSpecifier("/Audio/WildWanamingo/DesertRose/Weapons/silenced.ogg"); // WW Change - Desert Rose suppressed shot

    [DataField, AutoNetworkedField]
    public bool HideMuzzleFlash = true;
}
