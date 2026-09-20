using Avro;
using Avro.Specific;

namespace GridPulse.GridOperationsGateway.Avro;

public sealed class UsageAnomalyDetected : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "UsageAnomalyDetected",
          "namespace": "gridpulse.avro",
          "fields": [
            { "name": "meterId", "type": "string", "default": "" },
            { "name": "accountId", "type": "string", "default": "" },
            { "name": "lastSeenAt", "type": "long" }
          ]
        }
        """);

    public string MeterId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public long LastSeenAtUnixMilliseconds { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => MeterId,
        1 => AccountId,
        2 => LastSeenAtUnixMilliseconds,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: MeterId = (string)fieldValue; break;
            case 1: AccountId = (string)fieldValue; break;
            case 2: LastSeenAtUnixMilliseconds = (long)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
