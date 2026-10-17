// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/Administration/Commands/SetWarpIdCommand.cs

using Content.Server.Administration;
using Content.Server._WW.Teleportation;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Administration.Commands;

/// <summary>
/// Misfits: sets the link id on a ladder (or any warper). Two warpers with the same id lead to each other.
/// WW: ladders are Wizden portals now; this re-pairs them through <see cref="F14PortalLinkComponent"/>.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed partial class SetWarpIdCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;

    public string Command => "setwarpid";
    public string Description => "Sets the link id on a ladder. Give both ends the same id to connect them.";
    public string Help => "setwarpid <entity id> <link id>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEnt) || !_entManager.TryGetEntity(netEnt, out var uid))
        {
            shell.WriteError($"Entity {args[0]} was not found.");
            return;
        }

        if (!_entManager.TryGetComponent<F14PortalLinkComponent>(uid, out var link))
        {
            shell.WriteError($"{_entManager.ToPrettyString(uid.Value)} isn't a ladder (no F14PortalLink component).");
            return;
        }

        _entManager.System<F14PortalLinkSystem>().SetLinkId((uid.Value, link), args[1]);
        shell.WriteLine($"Set link id of {_entManager.ToPrettyString(uid.Value)} to '{args[1]}'.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<entity id>"),
            2 => CompletionResult.FromHint("<link id>"),
            _ => CompletionResult.Empty,
        };
    }
}
