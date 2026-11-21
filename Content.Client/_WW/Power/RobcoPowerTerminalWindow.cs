using System.Linq;
using Content.Shared._WW.Power;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._WW.Power;

/// <summary>
/// Green-on-black RobCo screen for the power terminal, in the style of the Misfits terminals
/// (holotape reader, vault door console).
/// </summary>
public sealed class RobcoPowerTerminalWindow : DefaultWindow
{
    private static readonly Color Background = Color.FromHex("#080c08");
    private static readonly Color Bright = Color.FromHex("#39ff5c");
    private static readonly Color Dim = Color.FromHex("#1f8b34");
    private static readonly Color Warning = Color.FromHex("#ff4136");

    private readonly BoxContainer _summary;
    private readonly BoxContainer _rows;

    public RobcoPowerTerminalWindow()
    {
        Title = Loc.GetString("robco-power-title");
        MinSize = new(560, 460);
        SetSize = new(640, 560);

        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = Background },
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new(12, 8, 12, 8),
            SeparationOverride = 4,
        };
        box.AddChild(Line(Loc.GetString("robco-power-header"), Bright));
        box.AddChild(Line(Loc.GetString("robco-power-subheader"), Dim));
        box.AddChild(Line(" ", Dim));

        _summary = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        box.AddChild(_summary);
        box.AddChild(Line(" ", Dim));

        _rows = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        box.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _rows },
        });

        panel.AddChild(box);
        Contents.AddChild(panel);
    }

    public void UpdateState(RobcoPowerTerminalState state)
    {
        var gens = state.Devices.Where(d => d.Kind == RobcoPowerDeviceKind.Generator).ToList();
        var stores = state.Devices.Where(d => d.Kind == RobcoPowerDeviceKind.Storage).ToList();
        var breakers = state.Devices.Where(d => d.Kind == RobcoPowerDeviceKind.Breaker).ToList();

        _summary.RemoveAllChildren();
        _summary.AddChild(Line(Loc.GetString("robco-power-range", ("range", (int) state.Range)), Dim));
        _summary.AddChild(Line(Loc.GetString("robco-power-generation",
            ("output", Watts(gens.Sum(d => d.Output))), ("capacity", Watts(gens.Sum(d => d.Capacity)))), Bright));
        _summary.AddChild(Line(Loc.GetString("robco-power-demand", ("load", Watts(breakers.Sum(d => d.Output)))), Bright));
        if (stores.Count > 0)
        {
            var level = stores.Average(d => d.Charge ?? 0f);
            var net = stores.Sum(d => d.Input - d.Output);
            _summary.AddChild(Line(Loc.GetString("robco-power-storage",
                ("percent", Percent(level)), ("net", (net >= 0 ? "+" : "-") + Watts(Math.Abs(net)))), Bright));
        }

        _rows.RemoveAllChildren();
        if (state.Devices.Count == 0)
        {
            _rows.AddChild(Line(Loc.GetString("robco-power-none"), Dim));
            return;
        }

        foreach (var kind in Enum.GetValues<RobcoPowerDeviceKind>())
        {
            var group = state.Devices.Where(d => d.Kind == kind).ToList();
            if (group.Count == 0)
                continue;

            _rows.AddChild(Line(Loc.GetString($"robco-power-section-{kind.ToString().ToLowerInvariant()}"), Bright));

            // Fields of identical devices (solar panels) collapse into one row.
            foreach (var same in group.GroupBy(d => d.Name))
            {
                var list = same.ToList();
                var first = list[0];
                var name = list.Count > 1 ? $"{first.Name} x{list.Count}" : first.Name;
                var where = first.Direction == Direction.Invalid
                    ? Loc.GetString("robco-power-here")
                    : Loc.GetString("robco-power-where", ("distance", first.Distance),
                        ("dir", Loc.GetString($"robco-power-dir-{first.Direction.ToString().ToLowerInvariant()}")));

                string detail;
                var bad = false;
                if (kind == RobcoPowerDeviceKind.Generator)
                {
                    var capacity = list.Sum(d => d.Capacity);
                    bad = capacity <= 0f;
                    detail = bad
                        ? Loc.GetString("robco-power-offline")
                        : Loc.GetString("robco-power-gen-row", ("output", Watts(list.Sum(d => d.Output))), ("capacity", Watts(capacity)));
                    if (first.Fuel is { } fuel && list.Count == 1)
                    {
                        detail += "  " + Loc.GetString("robco-power-core", ("percent", Percent(fuel)));
                        bad |= fuel < 0.1f;
                    }
                }
                else
                {
                    var charge = list.Average(d => d.Charge ?? 0f);
                    bad = charge < 0.1f;
                    detail = Loc.GetString("robco-power-battery-row", ("percent", Percent(charge)),
                        ("output", Watts(list.Sum(d => d.Output))), ("input", Watts(list.Sum(d => d.Input))));
                }

                _rows.AddChild(Line($"  > {name.ToUpperInvariant()}  [{where}]", bad ? Warning : Bright));
                _rows.AddChild(Line($"      {detail}", bad ? Warning : Dim));
            }

            _rows.AddChild(Line(" ", Dim));
        }
    }

    private static Label Line(string text, Color color)
    {
        return new Label
        {
            Text = text,
            FontColorOverride = color,
            StyleClasses = { "monospace" },
            ClipText = true,
        };
    }

    private static string Percent(float level)
    {
        return $"{(int) MathF.Round(level * 100f)}%";
    }

    private static string Watts(float watts)
    {
        return watts >= 1000f ? $"{watts / 1000f:0.0} kW" : $"{watts:0} W";
    }
}
