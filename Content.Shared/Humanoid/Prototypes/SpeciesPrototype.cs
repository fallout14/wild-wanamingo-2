using Content.Shared.Body;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Dataset;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype]
public sealed partial class SpeciesPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the species.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// User visible name of the species.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    ///     Descriptor. Unused...? This is intended
    ///     for an eventual integration into IdentitySystem
    ///     (i.e., young human person, young lizard person, etc.)
    /// </summary>
    [DataField]
    public string Descriptor { get; private set; } = "humanoid";

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField(required: true)]
    public bool RoundStart { get; private set; } = false;

    // WW Change - species like deathclaws and robots are picked on purpose, never handed out at random
    /// <summary>
    /// Whether random characters (new players, guests, randomize) can roll this species.
    /// </summary>
    [DataField]
    public bool RandomSelectable { get; private set; } = true;

    // WW Change - whitelisted species
    /// <summary>
    ///     Only players on this species' whitelist can pick it (admin command specieswhitelistadd; stored with the
    ///     job whitelists). Ignored while game.role_whitelist is off.
    /// </summary>
    [DataField]
    public bool Whitelisted { get; private set; }

    // WW Change start - body size sliders (from Misfits/Einstein Engines)
    /// <summary>
    ///     Whether the character editor offers height/width sliders for this species (off for robots and deathclaws).
    /// </summary>
    [DataField]
    public bool SizeAdjustable { get; private set; } = true;

    [DataField]
    public float MinHeight { get; private set; } = 0.85f;

    [DataField]
    public float MaxHeight { get; private set; } = 1.15f;

    [DataField]
    public float MinWidth { get; private set; } = 0.85f;

    [DataField]
    public float MaxWidth { get; private set; } = 1.15f;
    // WW Change end

    /// <summary>
    ///     Default skin tone for this species. This applies for non-human skin tones.
    /// </summary>
    [DataField]
    public Color DefaultSkinTone { get; private set; } = Color.White;

    /// <summary>
    ///     Default human skin tone for this species. This applies for human skin tones.
    ///     See <see cref="SkinColor.HumanSkinTone"/> for the valid range of skin tones.
    /// </summary>
    [DataField]
    public int DefaultHumanSkinTone { get; private set; } = 20;

    /// <summary>
    ///     Humanoid species variant used by this entity.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; } = default!;

    /// <summary>
    /// Prototype used by the species for the dress-up doll in various menus.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DollPrototype { get; private set; } = default!;

    /// <summary>
    /// Method of skin coloration used by the species.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SkinColorationPrototype> SkinColoration { get; private set; }

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> MaleFirstNames { get; private set; } = "NamesFirstMale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> FemaleFirstNames { get; private set; } = "NamesFirstFemale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> LastNames { get; private set; } = "NamesLast";

    [DataField]
    public SpeciesNaming Naming { get; private set; } = SpeciesNaming.FirstLast;

    [DataField]
    public List<Sex> Sexes { get; private set; } = new() { Sex.Male, Sex.Female };

    /// <summary>
    ///     Emote sounds prototype conversion id for every sex. This is ALWAYS in the order: Male; Female; Unsexed.
    /// </summary>
    [DataField]
    public ProtoId<EmoteSoundsPrototype>[] DefaultSoundsBySex = ["MaleHuman", "FemaleHuman", "MaleHuman"];

    /// <summary>
    ///     List of user selectable voices in the menu. This should at least have the same sound banks as the defaults.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<EmoteSoundsPrototype>> Voices = ["MaleHuman", "FemaleHuman"];

    /// <summary>
    ///     Characters younger than this are too young to be hired by Nanotrasen.
    /// </summary>
    [DataField]
    public int MinAge = 18;

    /// <summary>
    ///     Characters younger than this appear young.
    /// </summary>
    [DataField]
    public int YoungAge = 30;

    /// <summary>
    ///     Characters older than this appear old. Characters in between young and old age appear middle aged.
    /// </summary>
    [DataField]
    public int OldAge = 60;

    /// <summary>
    ///     Characters cannot be older than this. Only used for restrictions...
    ///     although imagine if ghosts could age people WYCI...
    /// </summary>
    [DataField]
    public int MaxAge = 120;
}

public enum SpeciesNaming : byte
{
    First,
    FirstLast,
    FirstDashFirst,
    TheFirstofLast,
}
