using System.Text.Json;

namespace GridPulse.MeterSimulator;

public sealed class HouseholdPowerDataset
{
    private readonly double[] _globalActivePowerKw;

    public HouseholdPowerDataset()
    {
        using var stream = typeof(HouseholdPowerDataset).Assembly.GetManifestResourceStream(
            "GridPulse.MeterSimulator.Data.power-data-february-2007.json")
            ?? throw new InvalidOperationException("Embedded power dataset not found.");

        var rows = JsonSerializer.Deserialize<List<JsonElement[]>>(stream)
            ?? throw new InvalidOperationException("Power dataset failed to deserialize.");

        _globalActivePowerKw = rows.Select(row => row[2].GetDouble()).ToArray();
    }

    public int Count => _globalActivePowerKw.Length;

    public double GlobalActivePowerKwAt(int index) => _globalActivePowerKw[index];
}
