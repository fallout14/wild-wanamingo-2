using System.Linq;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Players.JobWhitelist;
using Content.Shared.Administration;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._WW.Administration.Commands;

/// <summary>
/// WW: add or remove a player's whitelist for a whitelisted species (super mutants, deathclaws, most robots...).
/// Stored in the job whitelist table under the species ID; <c>jobwhitelistget</c> lists them too.
/// </summary>
public abstract partial class SpeciesWhitelistCommandBase : LocalizedCommands
{
    [Dependency] protected IServerDbManager Db = default!;
    [Dependency] protected JobWhitelistManager JobWhitelist = default!;
    [Dependency] protected IPlayerLocator PlayerLocator = default!;
    [Dependency] protected IPlayerManager Players = default!;
    [Dependency] protected IPrototypeManager Prototypes = default!;

    protected abstract bool Add { get; }

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var player = args[0].Trim();
        var species = new ProtoId<SpeciesPrototype>(args[1].Trim());
        if (!Prototypes.TryIndex(species, out var speciesPrototype))
        {
            shell.WriteError(Loc.GetString("cmd-specieswhitelist-species-does-not-exist", ("species", species.Id)));
            return;
        }

        var data = await PlayerLocator.LookupIdByNameAsync(player);
        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-jobwhitelist-player-not-found", ("player", player)));
            return;
        }

        var name = Loc.GetString(speciesPrototype.Name);
        var isWhitelisted = await Db.IsJobWhitelisted(data.UserId, new ProtoId<JobPrototype>(species.Id));
        if (Add == isWhitelisted)
        {
            shell.WriteLine(Loc.GetString(Add ? "cmd-specieswhitelistadd-already" : "cmd-specieswhitelistremove-not",
                ("player", player), ("species", name)));
            return;
        }

        if (Add)
            JobWhitelist.AddSpeciesWhitelist(data.UserId, species);
        else
            JobWhitelist.RemoveSpeciesWhitelist(data.UserId, species);

        shell.WriteLine(Loc.GetString(Add ? "cmd-specieswhitelistadd-added" : "cmd-specieswhitelistremove-removed",
            ("player", player), ("species", name)));

        if (!speciesPrototype.Whitelisted)
            shell.WriteLine(Loc.GetString("cmd-specieswhitelist-not-whitelisted-species", ("species", name)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(Players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-jobwhitelist-hint-player")),
            2 => CompletionResult.FromHintOptions(
                Prototypes.EnumeratePrototypes<SpeciesPrototype>().Where(p => p.Whitelisted).Select(p => p.ID),
                Loc.GetString("cmd-specieswhitelist-hint-species")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class SpeciesWhitelistAddCommand : SpeciesWhitelistCommandBase
{
    public override string Command => "specieswhitelistadd";
    protected override bool Add => true;
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class SpeciesWhitelistRemoveCommand : SpeciesWhitelistCommandBase
{
    public override string Command => "specieswhitelistremove";
    protected override bool Add => false;
}
