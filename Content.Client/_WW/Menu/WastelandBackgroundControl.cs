using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._WW.Menu;

/// <summary>
/// Menu background in place of Wizden's space parallax: an endless Mojave Sun desert of sand, dry grass, shrubs,
/// cacti and dead trees, scattered at random and drifting slowly, with sandstorms and radstorms sweeping across it.
/// </summary>
/// <remarks>
/// The desert is generated from a hash of each tile's position and a seed picked when the control opens, so it's
/// different every time and needs no memory however far it scrolls.
/// </remarks>
public sealed partial class WastelandBackgroundControl : Control
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IResourceCache _resCache = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const string FloraPath = "/Textures/MojaveSun/Objects/Decoration/Flora/";
    private static readonly ResPath SandPath = new("/Textures/Nuclear14/Tiles/ms13/desert.png");
    private static readonly ResPath WeatherPath = new("/Textures/Effects/weather.rsi");

    private const int TileSize = 32;
    private const int SandVariants = 3;

    // Share of tiles with a prop, cumulative: trees, then shrubs and cacti, then dry grass.
    private const float TreeChance = 0.012f;
    private const float ShrubChance = 0.05f;
    private const float GrassChance = 0.22f;

    /// <summary>
    /// Tiles a tree can reach above its own (trees are 4 tiles tall), so trees rooted below the screen still draw.
    /// </summary>
    private const int TreeOverhang = 4;

    private const float RadstormChance = 0.35f;

    /// <summary>
    /// Dust blowing through the desert between storms.
    /// </summary>
    private const float CalmDust = 0.12f;

    private static readonly Color SandWash = Color.FromHex("#c9a46a");
    private static readonly Color RadWash = Color.FromHex("#a8b84a");
    private static readonly Color RadDust = Color.FromHex("#d4ff9a");

    private const float SandWashAlpha = 0.5f;
    private const float RadWashAlpha = 0.3f;
    private const float NearDustAlpha = 0.45f;
    private const float FarDustAlpha = 0.35f;

    private const int HazeStrip = 4;
    private const int DustSlices = 8;

    /// <summary>
    /// Screen pixels per sprite pixel, before UI scale.
    /// </summary>
    public float Zoom { get; set; } = 3f;

    /// <summary>
    /// Drift of the desert, in sprite pixels per second.
    /// </summary>
    public Vector2 Speed { get; set; } = new(10f, 3f);

    /// <summary>
    /// Speed the storm dust blows, in sprite pixels per second.
    /// </summary>
    public float WindSpeed { get; set; } = 90f;

    private readonly int _seed;
    private readonly Vector2 _origin;
    private readonly Texture _sand;
    private readonly Texture[] _grass;
    private readonly Texture[] _shrubs;
    private readonly Texture[] _trees;
    private readonly RSI.State _dustState;
    private readonly RSI.State _sandstormState;
    private readonly RSI.State _radstormState;

    private float _stormStart;
    private float _stormDuration;
    private bool _isRadstorm;

    public WastelandBackgroundControl()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;

        _seed = _random.Next();
        _origin = new Vector2(_random.Next(0, 100000), _random.Next(0, 100000));

        _sand = _resCache.GetResource<TextureResource>(SandPath).Texture;
        _grass = States("flora.rsi", "drygrass_1", "drygrass_2", "drygrass_3", "drygrass_4", "drygrass_5",
            "drygrass_6", "drygrass_7", "drygrass_8", "drygrass_9", "drygrass_10", "drygrass_11", "drygrass_12",
            "drygrass_13", "drygrass_14", "drygrass_15");
        _shrubs = [
            ..States("flora.rsi", "leafy_plant1", "leafy_plant2", "leafy_plant3", "short_cactus1", "short_cactus2",
                "short_cactus3", "tall_cactus1", "tall_cactus2", "tall_cactus3"),
            ..States("wasteland_flora.rsi", "yucca1", "barrelc1", "barrelc2", "barrelc3"),
        ];
        _trees = States("trees.rsi", "dead_tree1", "dead_tree2", "dead_tree3", "bald");

        var weather = _resCache.GetResource<RSIResource>(WeatherPath).RSI;
        _dustState = weather["sandstorm"];
        _sandstormState = weather["sandstorm_heavy"];
        _radstormState = weather["fallout"];

        // Open partway into a storm, so it shows even on a short connect.
        var now = (float) _timing.RealTime.TotalSeconds;
        NextStorm(now);
        _stormStart = now - _stormDuration * 0.3f;
    }

    private Texture[] States(string rsi, params string[] states)
    {
        var resource = _resCache.GetResource<RSIResource>(new ResPath(FloraPath + rsi)).RSI;
        var textures = new List<Texture>();
        foreach (var state in states)
        {
            if (resource.TryGetState(state, out var found))
                textures.Add(found.Frame0);
        }

        return textures.ToArray();
    }

    private void NextStorm(float start)
    {
        _stormStart = start;
        _stormDuration = _random.NextFloat(20f, 30f);
        _isRadstorm = _random.Prob(RadstormChance);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var time = (float) _timing.RealTime.TotalSeconds;
        var zoom = Math.Max(1, (int) MathF.Round(Zoom * UIScale));
        var cell = TileSize * zoom;
        var size = PixelSize;

        var camera = (_origin + Speed * time) * zoom;
        var firstX = (int) MathF.Floor(camera.X / cell);
        var firstY = (int) MathF.Floor(camera.Y / cell);
        var offset = new Vector2(camera.X - firstX * cell, camera.Y - firstY * cell).Floored();
        var cols = size.X / cell + 2;
        var rows = size.Y / cell + 2;

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                var variant = (int) (Hash(firstX + x, firstY + y, 0) % SandVariants);
                var pos = new Vector2(x * cell, y * cell) - offset;
                handle.DrawTextureRectRegion(_sand, UIBox2.FromDimensions(pos, new Vector2(cell, cell)),
                    UIBox2.FromDimensions(new Vector2(variant * TileSize, 0), new Vector2(TileSize, TileSize)));
            }
        }

        // Row by row from the top, so props lower on the screen overlap the ones behind them.
        for (var y = 0; y < rows + TreeOverhang; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                DrawProp(handle, firstX + x, firstY + y, new Vector2(x * cell, y * cell) - offset, zoom);
            }
        }

        DrawStorm(handle, time, zoom, size);
    }

    private void DrawProp(DrawingHandleScreen handle, int tileX, int tileY, Vector2 pos, int zoom)
    {
        var roll = Unit(Hash(tileX, tileY, 1));
        Texture[] set;
        if (roll < TreeChance)
            set = _trees;
        else if (roll < ShrubChance)
            set = _shrubs;
        else if (roll < GrassChance)
            set = _grass;
        else
            return;

        if (set.Length == 0)
            return;

        var texture = set[Hash(tileX, tileY, 2) % (uint) set.Length];
        var drawSize = texture.Size * zoom;
        var cell = TileSize * zoom;

        // Stand on the tile's bottom edge, nudged off the grid so the scatter doesn't look tiled.
        var jitter = new Vector2(Unit(Hash(tileX, tileY, 3)) - 0.5f, Unit(Hash(tileX, tileY, 4)) - 0.5f) * cell * 0.5f;
        var bottomCenter = pos + new Vector2(cell / 2f, cell) + jitter;
        var rect = UIBox2.FromDimensions((bottomCenter - new Vector2(drawSize.X / 2f, drawSize.Y)).Floored(), drawSize);
        handle.DrawTextureRect(texture, rect);
    }

    /// <summary>
    /// A band of storm blows across the screen from left to right: a tinted haze and thick dust, fading in at the
    /// front and out at the back. Light dust drifts by between storms.
    /// </summary>
    private void DrawStorm(DrawingHandleScreen handle, float time, int zoom, Vector2i size)
    {
        if (time > _stormStart + _stormDuration)
            NextStorm(time + _random.NextFloat(6f, 14f));

        var edge = size.X * 0.35f;
        var body = size.X * 1.2f;
        var progress = (time - _stormStart) / _stormDuration;
        var front = -edge + progress * (size.X + body + 2 * edge);
        var active = progress is >= 0f and <= 1f;

        var storm = new Storm(front, body, edge, active);
        var cell = TileSize * zoom;

        // Haze in thin strips, so its edges fade smoothly instead of stepping a tile at a time.
        if (active)
        {
            var (wash, washAlpha) = _isRadstorm ? (RadWash, RadWashAlpha) : (SandWash, SandWashAlpha);
            for (var x = 0; x < size.X; x += HazeStrip)
            {
                var strength = storm.Strength(x + HazeStrip / 2f);
                if (strength > 0f)
                    handle.DrawRect(new UIBox2(x, 0, x + HazeStrip, size.Y), wash.WithAlpha(washAlpha * strength));
            }
        }

        // Two dust layers at different sizes and speeds, so the sprite's tiling doesn't show.
        var near = Frame(_isRadstorm ? _radstormState : _sandstormState, time);
        var far = Frame(_dustState, time);
        var farColor = _isRadstorm ? RadDust : Color.White;
        DrawDust(handle, far, cell * 0.67f, new Vector2(1.6f, 0.5f) * WindSpeed * zoom * time, size, storm,
            FarDustAlpha, CalmDust, farColor);
        DrawDust(handle, near, cell, new Vector2(1f, 0.3f) * WindSpeed * zoom * time, size, storm,
            NearDustAlpha, 0f, Color.White);
    }

    /// <summary>
    /// Tiles one dust sprite across the screen, blown along by <paramref name="drift"/>. Each tile is drawn in
    /// vertical slices, so the storm's strength fades across it.
    /// </summary>
    private static void DrawDust(DrawingHandleScreen handle, Texture texture, float tile, Vector2 drift,
        Vector2i size, Storm storm, float stormAlpha, float calmAlpha, Color color)
    {
        var slice = tile / DustSlices;
        var source = texture.Size.X / (float) DustSlices;
        var startX = drift.X % tile - tile;
        var startY = drift.Y % tile - tile;
        for (var x = startX; x < size.X; x += tile)
        {
            for (var i = 0; i < DustSlices; i++)
            {
                var sliceX = x + i * slice;
                var strength = storm.Strength(sliceX + slice / 2f);
                var alpha = stormAlpha * strength + calmAlpha * (1f - strength);
                if (alpha <= 0.005f)
                    continue;

                var region = new UIBox2(i * source, 0, (i + 1) * source, texture.Size.Y);
                for (var y = startY; y < size.Y; y += tile)
                {
                    handle.DrawTextureRectRegion(texture, new UIBox2(sliceX, y, sliceX + slice, y + tile), region,
                        color.WithAlpha(alpha));
                }
            }
        }
    }

    /// <summary>
    /// A band of storm crossing the screen: <see cref="Front"/> is its leading edge, the body trails behind it, and
    /// both ends fade over <see cref="Edge"/> pixels.
    /// </summary>
    private readonly record struct Storm(float Front, float Body, float Edge, bool Active)
    {
        public float Strength(float x)
        {
            return Active ? StormStrength(x, Front, Body, Edge) : 0f;
        }
    }

    private static float StormStrength(float x, float front, float body, float edge)
    {
        return SmoothStep((front - x) / edge) * SmoothStep((x - (front - body)) / edge);
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private static Texture Frame(RSI.State state, float time)
    {
        if (!state.IsAnimated)
            return state.Frame0;

        var remaining = time % state.TotalDelay;
        for (var i = 0; i < state.DelayCount; i++)
        {
            remaining -= state.GetDelay(i);
            if (remaining < 0f)
                return state.GetFrame(RsiDirection.South, i);
        }

        return state.Frame0;
    }

    private uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            var h = (uint) _seed;
            h ^= (uint) x * 0x9E3779B1u;
            h = (h << 13 | h >> 19) * 5u + 0xE6546B64u;
            h ^= (uint) y * 0x85EBCA77u;
            h = (h << 13 | h >> 19) * 5u + 0xE6546B64u;
            h ^= (uint) salt * 0xC2B2AE3Du;
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            return h;
        }
    }

    private static float Unit(uint hash)
    {
        return (hash & 0xFFFFFF) / (float) 0x1000000;
    }
}
