using Content.Client._WW.Stylesheets;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Shared._WW.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The New Vegas UI colours option is on by default and swaps the main stylesheet live.
/// </summary>
[TestFixture]
public sealed class VegasUiColorsTest
{
    [Test]
    public async Task ToggleSwapsStylesheet()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        var cfg = client.ResolveDependency<IConfigurationManager>();
        var ui = client.ResolveDependency<IUserInterfaceManager>();
        var sheets = client.ResolveDependency<IStylesheetManager>();

        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(WWCVars.VegasUiColors), Is.True);
            Assert.That(VegasPalettes.Enabled, Is.True);
            var vegas = ui.Stylesheet;

            foreach (var (name, p) in new[] { ("primary", VegasPalettes.Primary), ("secondary", VegasPalettes.Secondary), ("highlight", VegasPalettes.Highlight) })
            {
                TestContext.Out.WriteLine($"{name}: element {p.Element.ToHex()} hover {p.HoveredElement.ToHex()} pressed {p.PressedElement.ToHex()} disabled {p.DisabledElement.ToHex()} bg {p.Background.ToHex()} bgLight {p.BackgroundLight.ToHex()} bgDark {p.BackgroundDark.ToHex()} text {p.Text.ToHex()}");
            }

            cfg.SetCVar(WWCVars.VegasUiColors, false);
            Assert.That(VegasPalettes.Enabled, Is.False);
            Assert.That(ui.Stylesheet, Is.Not.SameAs(vegas), "turning it off should rebuild the stylesheet");
            Assert.That(sheets.SheetNanotrasen, Is.SameAs(ui.Stylesheet));

            cfg.SetCVar(WWCVars.VegasUiColors, true);
            Assert.That(VegasPalettes.Enabled, Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
