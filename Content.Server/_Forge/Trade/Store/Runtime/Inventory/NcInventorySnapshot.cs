// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_NC/Trade/Store/Runtime/Inventory/NcInventorySnapshot.cs

namespace Content.Server._Forge.Trade;

public sealed class NcInventorySnapshot
{
    public readonly Dictionary<string, int> AncestorCounts = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> ProtoCounts = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> StackTypeCounts = new(StringComparer.Ordinal);

    public void Clear()
    {
        ProtoCounts.Clear();
        AncestorCounts.Clear();
        StackTypeCounts.Clear();
    }
}
