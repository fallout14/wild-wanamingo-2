// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs
// (the S.P.E.C.I.A.L. tab). Changed for Wild Wanamingo: moved into its own partial file; Misfits' Pip-Boy styles are
// local colours here; the effect readouts only list effects ported so far (Charisma loadout points, trade prices,
// lathe/crafting/mining and carry speed come later).

using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Prototypes;
using Robust.Client.Graphics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private static readonly ProtoId<SpecialTuningPrototype> SpecialTuningPrototypeId = "F14SpecialTuning";

        // Pip-Boy terminal colours.
        private static readonly Color PipBoyGreen = Color.FromHex("#33FF66");
        private static readonly Color PipBoyGreenDim = Color.FromHex("#1E9C3D");
        private static readonly Color PipBoyPanelBg = Color.FromHex("#0C1F0E");
        private static readonly Color PipBoyBtnDefault = Color.FromHex("#163E1E");

        private BoxContainer? _specialTab;
        private BoxContainer? _specialRows;
        private Label? _specialPointsLabel;
        // Which SPECIAL stats currently have their effect-details panel expanded.
        // Tracked here so the toggle survives the row rebuild that happens on +/-.
        private readonly HashSet<SpecialStat> _specialExpanded = new();
        private Texture? _specialInfoIcon;

        /// <summary>
        /// Adds the S.P.E.C.I.A.L. tab after the existing ones.
        /// </summary>
        private void InitializeSpecialTab()
        {
            _specialTab = BuildSpecialTab();
            TabContainer.AddChild(_specialTab);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-specials-tab"));
        }

        private BoxContainer BuildSpecialTab()
        {
            var root = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Thickness(10),
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            // Pip-Boy style readout header: acronym title over a phosphor-green divider.
            var title = new Label
            {
                Text = "S.P.E.C.I.A.L.",
                HorizontalAlignment = HAlignment.Center,
            };
            title.StyleClasses.Add(StyleClass.LabelHeading);

            var divider = new PanelContainer
            {
                MinSize = new Vector2(0, 2),
                Margin = new Thickness(0, 4, 0, 6),
                PanelOverride = new StyleBoxFlat { BackgroundColor = PipBoyGreen },
            };

            _specialPointsLabel = new Label
            {
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                FontColorOverride = PipBoyGreen,
            };

            _specialRows = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            var scroll = new ScrollContainer
            {
                HorizontalExpand = true,
                VerticalExpand = true,
            };
            scroll.AddChild(_specialRows);

            root.AddChild(title);
            root.AddChild(divider);
            root.AddChild(_specialPointsLabel);
            root.AddChild(scroll);
            return root;
        }

        private void UpdateSpecialControls()
        {
            if (_specialRows == null || _specialPointsLabel == null)
                return;

            _specialRows.RemoveAllChildren();

            if (Profile == null)
            {
                _specialPointsLabel.Text = string.Empty;
                return;
            }

            var special = SpecialProfile.EnsureValid(Profile.Special);
            if (!special.MemberwiseEquals(Profile.Special))
                Profile = Profile.WithSpecial(special);
            _specialPointsLabel.Text = Loc.GetString(
                "humanoid-profile-editor-special-points-label",
                ("points", special.AvailablePoints),
                ("max", SpecialProfile.BonusPoints));

            foreach (var stat in SpecialStats.All)
            {
                _specialRows.AddChild(BuildSpecialRow(stat, special));
            }
        }

        private Control BuildSpecialRow(SpecialStat stat, SpecialProfile special)
        {
            // Bordered green-on-black terminal box per attribute, evoking a Pip-Boy stat readout.
            var panel = new PanelContainer
            {
                Margin = new Thickness(0, 0, 0, 6),
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = PipBoyPanelBg,
                    BorderColor = PipBoyBtnDefault,
                    BorderThickness = new Thickness(1),
                    ContentMarginLeftOverride = 10,
                    ContentMarginRightOverride = 10,
                    ContentMarginTopOverride = 8,
                    ContentMarginBottomOverride = 8,
                },
            };

            var root = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4,
                HorizontalExpand = true,
            };

            var value = special.Get(stat);

            // Top line: attribute name (left) and the value flanked by -/+ terminal buttons (right).
            var topRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                SeparationOverride = 8,
            };

            var name = new Label
            {
                Text = Loc.GetString(GetSpecialNameLoc(stat)),
                VerticalAlignment = VAlignment.Center,
            };
            name.StyleClasses.Add(StyleClass.LabelHeading);

            // Info toggle: a green circled-"i" sitting next to the attribute name.
            // Clicking it expands/collapses the effect details for this stat.
            _specialInfoIcon ??= IoCManager.Resolve<IResourceCache>()
                .GetTexture("/Textures/Interface/VerbIcons/information.svg.192dpi.png");
            var infoButton = new TextureButton
            {
                TextureNormal = _specialInfoIcon,
                Scale = new Vector2(0.3f, 0.3f),
                Modulate = PipBoyGreen,
                VerticalAlignment = VAlignment.Center,
                ToolTip = Loc.GetString("humanoid-profile-editor-special-info-tooltip"),
            };

            var minusButton = new Button
            {
                Text = "-",
                MinWidth = 32,
                Disabled = value <= SpecialProfile.Minimum,
            };
            minusButton.OnPressed += _ => SetSpecialValue(stat, value - 1);

            var valueLabel = new Label
            {
                Text = value.ToString(),
                MinWidth = 40,
                Align = Label.AlignMode.Center,
                VerticalAlignment = VAlignment.Center,
            };
            valueLabel.StyleClasses.Add(StyleClass.LabelHeading);

            var plusButton = new Button
            {
                Text = "+",
                MinWidth = 32,
                Disabled = value >= SpecialProfile.Maximum || special.AvailablePoints <= 0,
            };
            plusButton.OnPressed += _ => SetSpecialValue(stat, value + 1);

            topRow.AddChild(name);
            topRow.AddChild(infoButton);
            // Spacer pushes the value and -/+ controls to the right edge.
            topRow.AddChild(new Control { HorizontalExpand = true });
            topRow.AddChild(minusButton);
            topRow.AddChild(valueLabel);
            topRow.AddChild(plusButton);

            root.AddChild(topRow);

            // Segmented allocation gauge (Minimum..Maximum), lit up to the current value.
            root.AddChild(BuildSpecialGauge(value));

            // Effect details: hidden by default, revealed by the info button so the
            // list stays glanceable. Built once and toggled via the tracked set.
            var details = BuildSpecialDetails(stat, value);
            details.Visible = _specialExpanded.Contains(stat);
            infoButton.OnPressed += _ =>
            {
                if (!_specialExpanded.Remove(stat))
                    _specialExpanded.Add(stat);
                details.Visible = _specialExpanded.Contains(stat);
            };

            root.AddChild(details);

            panel.AddChild(root);
            return panel;
        }

        /// <summary>
        /// Builds the collapsible effect-details block for a SPECIAL stat: the flavor
        /// description, the current effects (one metric per line), and the lower/raise preview.
        /// </summary>
        private Control BuildSpecialDetails(SpecialStat stat, int value)
        {
            var details = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 2,
                HorizontalExpand = true,
                Margin = new Thickness(0, 4, 0, 0),
            };

            details.AddChild(new Label
            {
                Text = Loc.GetString(GetSpecialDescriptionLoc(stat)),
                FontColorOverride = PipBoyGreenDim,
                HorizontalExpand = true,
            });

            // Current effects, broken onto one line each for at-a-glance reading.
            details.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-special-current-header"),
                FontColorOverride = PipBoyGreen,
                Margin = new Thickness(0, 4, 0, 0),
            });
            foreach (var metric in SplitEffectMetrics(GetSpecialEffectDetails(stat, value)))
            {
                details.AddChild(new Label
                {
                    Text = "  " + metric,
                    FontColorOverride = PipBoyGreen,
                    HorizontalExpand = true,
                });
            }

            return details;
        }

        /// <summary>
        /// Splits a comma-joined effect detail sentence (e.g. "melee damage 0%, carry/pull speed 0%.")
        /// into individual trimmed metrics for line-by-line display. Commas inside parentheses
        /// (e.g. "medical action speed +50% (CPR, healing, surgery, scans)") are not split on.
        /// </summary>
        private static IEnumerable<string> SplitEffectMetrics(string details)
        {
            var trimmed = details.TrimEnd('.', ' ');
            var depth = 0;
            var start = 0;
            for (var i = 0; i < trimmed.Length; i++)
            {
                switch (trimmed[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth = Math.Max(0, depth - 1);
                        break;
                    case ',' when depth == 0:
                        var part = trimmed[start..i].Trim();
                        if (part.Length > 0)
                            yield return part;
                        start = i + 1;
                        break;
                }
            }

            var last = trimmed[start..].Trim();
            if (last.Length > 0)
                yield return last;
        }

        /// <summary>
        /// Builds the segmented S.P.E.C.I.A.L. gauge: one cell per point in the
        /// <see cref="SpecialProfile.Minimum"/>..<see cref="SpecialProfile.Maximum"/> range,
        /// lit phosphor-green up to <paramref name="value"/> and dark beyond it.
        /// </summary>
        private Control BuildSpecialGauge(int value)
        {
            var gauge = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 2,
                HorizontalAlignment = HAlignment.Left,
            };

            for (var i = SpecialProfile.Minimum; i <= SpecialProfile.Maximum; i++)
            {
                var filled = i <= value;
                gauge.AddChild(new PanelContainer
                {
                    MinSize = new Vector2(14, 16),
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = filled ? PipBoyGreen : PipBoyPanelBg,
                        BorderColor = PipBoyGreenDim,
                        BorderThickness = new Thickness(1),
                    },
                });
            }

            return gauge;
        }

        private string GetSpecialEffectDetails(SpecialStat stat, int value)
        {
            var tuning = GetSpecialTuning();

            return stat switch
            {
                SpecialStat.Strength => GetStrengthEffectDetails(value, tuning),
                SpecialStat.Perception => GetPerceptionEffectDetails(value, tuning),
                SpecialStat.Endurance => GetEnduranceEffectDetails(value, tuning),
                SpecialStat.Charisma => GetCharismaEffectDetails(value, tuning),
                SpecialStat.Intelligence => GetIntelligenceEffectDetails(value, tuning),
                SpecialStat.Agility => GetAgilityEffectDetails(value, tuning),
                SpecialStat.Luck => GetLuckEffectDetails(value, tuning),
                _ => "Neutral baseline.",
            };
        }

        private SpecialTuningPrototype GetSpecialTuning()
        {
            return _prototypeManager.Index(SpecialTuningPrototypeId);
        }

        private static string GetStrengthEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            var delta = SharedSpecialSystem.GetCurvedEffectDelta(value);
            var melee = delta * tuning.StrengthMeleeDamageMultiplierPerPoint;
            var unarmed = delta * tuning.StrengthUnarmedDamageMultiplierPerPoint;
            var throwSpeed = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                tuning.StrengthThrowSpeedMultiplierPerPoint);
            return $"melee damage {FormatSignedPercent(melee)}, unarmed damage {FormatSignedPercent(unarmed)}, throw speed {FormatSignedPercent(throwSpeed)}.";
        }

        private static string GetPerceptionEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            var delta = SharedSpecialSystem.GetCurvedEffectDelta(value);
            var spread = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.PerceptionSpreadMultiplierPerPoint);
            var heavyGun = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.PerceptionHeavyGunMultiplierPerPoint);
            var fireDelay = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.PerceptionFireDelayMultiplierPerPoint);
            var scopeDelay = SharedSpecialSystem.GetPerceptionScopeDelayMultiplier(value,
                tuning.PerceptionScopeDelayMultiplierLow, tuning.PerceptionScopeDelayMultiplierHigh) - 1f;

            return $"gun spread/recoil {FormatSignedPercent(spread)}, heavy gun spread/recoil {FormatSignedPercent(heavyGun)}, gun fire delay {FormatSignedPercent(fireDelay)}, scope-in time {FormatSignedPercent(scopeDelay)}.";
        }

        private static string GetEnduranceEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            var delta = SharedSpecialSystem.GetCurvedEffectDelta(value);
            var health = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                tuning.EnduranceHealthModifierPerPoint);
            var needs = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.EnduranceNeedDecayMultiplierPerPoint);
            var stamina = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                tuning.EnduranceStaminaRecoveryMultiplierPerPoint);
            var toxin = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.EnduranceToxinDamageMultiplierPerPoint);

            return $"health thresholds {FormatSignedNumber(health)}, hunger/thirst decay {FormatSignedPercent(needs)}, stamina recovery {FormatSignedPercent(stamina)}, poison/rad damage {FormatSignedPercent(toxin)}.";
        }

        private static string GetCharismaEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            return value switch
            {
                <= 2 => "people find you hard to approach; you fumble your words.",
                < 5 => "people find you a little awkward.",
                _ => "no social penalty.",
            };
        }

        private static string GetIntelligenceEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            return value switch
            {
                <= 1 => "you talk like a caveman.",
                >= 10 => "solution scan verb, medical HUD (toggleable).",
                >= 8 => "solution scan verb.",
                _ => "no effect.",
            };
        }

        private static string GetAgilityEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            var delta = SharedSpecialSystem.GetCurvedEffectDelta(value);
            var move = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                tuning.AgilityMovementSpeedMultiplierPerPoint);
            var actionDelay = SharedSpecialSystem.GetCurvedEffectModifier(
                delta,
                -tuning.AgilityActionDelayMultiplierPerPoint);

            return $"movement speed {FormatSignedPercent(move)}, melee attack delay {FormatSignedPercent(actionDelay)}.";
        }

        private static string GetLuckEffectDetails(int value, SpecialTuningPrototype tuning)
        {
            var delta = SharedSpecialSystem.GetCurvedEffectDelta(value);
            var shotCrit = Math.Clamp(delta * tuning.LuckSingleShotCriticalChancePerPoint, 0f, 1f);
            var revolverCrit = Math.Clamp(shotCrit * 2f, 0f, 1f);
            var clumsy = value == 1 ? 0.10f : 0f;
            // Matches LuckJunkBonusComponent.ChancePerLuckPoint (Misfits showed the unused tuning value here).
            var luckyLoot = Math.Clamp(delta * 0.06666667f, 0f, 1f);
            var unlucky = value switch
            {
                2 => 0.05f,
                3 => 0.03f,
                4 => 0.01f,
                _ => 0f,
            };

            var unluckyDamage = 1f - Math.Clamp(tuning.LuckUnluckyDamageMultiplier, 0f, 1f);

            return $"crit chance per hit {FormatUnsignedPercent(shotCrit)} (revolvers {FormatUnsignedPercent(revolverCrit)}), unlucky hit chance {FormatUnsignedPercent(unlucky)} for {FormatSignedPercent(-unluckyDamage)} damage, lucky junk find chance {FormatUnsignedPercent(luckyLoot)}, clumsy chance {FormatUnsignedPercent(clumsy)}.";
        }

        private static string FormatSignedPercent(float value)
        {
            if (MathF.Abs(value) < 0.0005f)
                return "0%";

            return value > 0f
                ? $"+{value * 100f:0.#}%"
                : $"{value * 100f:0.#}%";
        }

        private static string FormatUnsignedPercent(float value)
        {
            return $"{MathF.Max(0f, value) * 100f:0.#}%";
        }

        private static string FormatSignedNumber(float value)
        {
            if (MathF.Abs(value) < 0.05f)
                return "0";

            return value > 0f
                ? $"+{value:0.#}"
                : $"{value:0.#}";
        }

        private static string FormatSignedInt(int value)
        {
            return value > 0
                ? $"+{value}"
                : value.ToString();
        }

        private void SetSpecialValue(SpecialStat stat, int value)
        {
            if (Profile == null)
                return;

            var special = Profile.Special.With(stat, value);
            if (!special.IsValid)
                return;

            Profile = Profile.WithSpecial(special);
            IsDirty = true;
            UpdateSpecialControls();
        }

        private static string GetSpecialNameLoc(SpecialStat stat)
        {
            return stat switch
            {
                SpecialStat.Strength => "special-character-creation-strength",
                SpecialStat.Perception => "special-character-creation-perception",
                SpecialStat.Endurance => "special-character-creation-endurance",
                SpecialStat.Charisma => "special-character-creation-charisma",
                SpecialStat.Intelligence => "special-character-creation-intelligence",
                SpecialStat.Agility => "special-character-creation-agility",
                SpecialStat.Luck => "special-character-creation-luck",
                _ => "special-character-creation-strength",
            };
        }

        private static string GetSpecialDescriptionLoc(SpecialStat stat)
        {
            return stat switch
            {
                SpecialStat.Strength => "special-character-creation-description-strength",
                SpecialStat.Perception => "special-character-creation-description-perception",
                SpecialStat.Endurance => "special-character-creation-description-endurance",
                SpecialStat.Charisma => "special-character-creation-description-charisma",
                SpecialStat.Intelligence => "special-character-creation-description-intelligence",
                SpecialStat.Agility => "special-character-creation-description-agility",
                SpecialStat.Luck => "special-character-creation-description-luck",
                _ => "special-character-creation-description-strength",
            };
        }
    }
}
