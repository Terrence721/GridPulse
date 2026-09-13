using Avro;
using Avro.Specific;

namespace GridPulse.AppHost.Tests.Avro;

public sealed class BillingInvoiceGenerated : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "BillingInvoiceGenerated",
          "namespace": "gridpulse.avro",
          "fields": [
            { "name": "invoiceId", "type": "string" },
            { "name": "meterId", "type": "string" },
            { "name": "amountDue", "type": "double" },
            { "name": "dueDate", "type": "long" }
          ]
        }
        """);

    public string InvoiceId { get; set; } = string.Empty;
    public string MeterId { get; set; } = string.Empty;
    public double AmountDue { get; set; }
    public long DueDateUnixMilliseconds { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => InvoiceId,
        1 => MeterId,
        2 => AmountDue,
        3 => DueDateUnixMilliseconds,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: InvoiceId = (string)fieldValue; break;
            case 1: MeterId = (string)fieldValue; break;
            case 2: AmountDue = (double)fieldValue; break;
            case 3: DueDateUnixMilliseconds = (long)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
