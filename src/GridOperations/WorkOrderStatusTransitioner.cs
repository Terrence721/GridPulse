namespace GridPulse.GridOperations;

public sealed class WorkOrderStatusTransitioner
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["Reported"] = ["Dispatched", "Ignored"],
        ["Dispatched"] = ["EnRoute"],
        ["EnRoute"] = ["OnSite"],
        ["OnSite"] = ["Repaired"],
        ["Repaired"] = ["Restored"],
        ["Restored"] = ["Closed"],
        ["Closed"] = [],
        ["Ignored"] = []
    };

    public bool CanTransition(string currentStatus, string newStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);

    public bool IsTerminal(string status) =>
        !AllowedTransitions.TryGetValue(status, out var allowed) || allowed.Length == 0;
}
