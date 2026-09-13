using Avro;
using Avro.Specific;

namespace GridPulse.UsageAggregation.Avro;

public sealed class MeterReadingRaw : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "MeterReadingRaw",
          "namespace": "gridpulse.avro",
          "fields": [
            { "name": "meterId", "type": "string" },
            { "name": "accountId", "type": "string", "default": "" },
            { "name": "timestamp", "type": "long" },
            { "name": "kwh", "type": "double" },
            { "name": "readingId", "type": "string" }
          ]
        }
        """);

    public string MeterId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public long TimestampUnixMilliseconds { get; set; }
    public double Kwh { get; set; }
    public string ReadingId { get; set; } = string.Empty;

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => MeterId,
        1 => AccountId,
        2 => TimestampUnixMilliseconds,
        3 => Kwh,
        4 => ReadingId,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: MeterId = (string)fieldValue; break;
            case 1: AccountId = (string)fieldValue; break;
            case 2: TimestampUnixMilliseconds = (long)fieldValue; break;
            case 3: Kwh = (double)fieldValue; break;
            case 4: ReadingId = (string)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
