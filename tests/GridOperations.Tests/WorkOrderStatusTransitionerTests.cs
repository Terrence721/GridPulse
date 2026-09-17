using GridPulse.GridOperations;

namespace GridPulse.GridOperations.Tests;

public class WorkOrderStatusTransitionerTests
{
    private readonly WorkOrderStatusTransitioner _transitioner = new();

    [Theory]
    [InlineData("Reported", "Dispatched")]
    [InlineData("Reported", "Ignored")]
    [InlineData("Dispatched", "EnRoute")]
    [InlineData("EnRoute", "OnSite")]
    [InlineData("OnSite", "Repaired")]
    [InlineData("Repaired", "Restored")]
    [InlineData("Restored", "Closed")]
    public void CanTransition_AllowedTransition_ReturnsTrue(string currentStatus, string newStatus)
    {
        Assert.True(_transitioner.CanTransition(currentStatus, newStatus));
    }

    [Theory]
    [InlineData("Reported", "Closed")]
    [InlineData("Reported", "OnSite")]
    [InlineData("Dispatched", "Ignored")]
    [InlineData("Closed", "Reported")]
    [InlineData("Ignored", "Dispatched")]
    public void CanTransition_DisallowedTransition_ReturnsFalse(string currentStatus, string newStatus)
    {
        Assert.False(_transitioner.CanTransition(currentStatus, newStatus));
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Ignored")]
    public void IsTerminal_TerminalStatus_ReturnsTrue(string status)
    {
        Assert.True(_transitioner.IsTerminal(status));
    }

    [Theory]
    [InlineData("Reported")]
    [InlineData("Dispatched")]
    [InlineData("EnRoute")]
    [InlineData("OnSite")]
    [InlineData("Repaired")]
    [InlineData("Restored")]
    public void IsTerminal_NonTerminalStatus_ReturnsFalse(string status)
    {
        Assert.False(_transitioner.IsTerminal(status));
    }
}
