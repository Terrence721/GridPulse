using Avro;
using Avro.Specific;

namespace GridPulse.UsageAggregation.Avro;

public sealed class UsageAggregated : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "UsageAggregated",
          "namespace": "gridpulse.avro",
          "fields": [
            { "name": "meterId", "type": "string" },
            { "name": "periodStart", "type": "long" },
            { "name": "periodEnd", "type": "long" },
            { "name": "totalKwh", "type": "double" }
          ]
        }
        """);

    public string MeterId { get; set; } = string.Empty;
    public long PeriodStartUnixMilliseconds { get; set; }
    public long PeriodEndUnixMilliseconds { get; set; }
    public double TotalKwh { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => MeterId,
        1 => PeriodStartUnixMilliseconds,
        2 => PeriodEndUnixMilliseconds,
        3 => TotalKwh,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: MeterId = (string)fieldValue; break;
            case 1: PeriodStartUnixMilliseconds = (long)fieldValue; break;
            case 2: PeriodEndUnixMilliseconds = (long)fieldValue; break;
            case 3: TotalKwh = (double)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
