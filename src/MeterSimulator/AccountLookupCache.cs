namespace GridPulse.MeterSimulator;

public sealed class AccountLookupCache
{
    private IReadOnlyDictionary<string, Guid>? _accountIdsByMeterId;

    public void Populate(IReadOnlyDictionary<string, Guid> accountIdsByMeterId)
    {
        _accountIdsByMeterId = accountIdsByMeterId;
    }

    public Guid GetAccountId(string meterId) => _accountIdsByMeterId![meterId];
}
