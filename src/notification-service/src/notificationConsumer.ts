import { Kafka, type EachMessagePayload } from "kafkajs";
import { SchemaRegistry } from "@kafkajs/confluent-schema-registry";
import type { Config } from "./config.js";
import { getAccountWebhookUrl } from "./accountClient.js";
import { sendInvoiceNotification } from "./webhookSender.js";

interface BillingInvoiceGenerated {
  invoiceId: string;
  accountId: string;
  amountDue: number;
  dueDate: number;
}

export async function runNotificationConsumer(config: Config): Promise<void> {
  const kafka = new Kafka({ brokers: config.kafkaBrokers, clientId: "notification-service" });
  const consumer = kafka.consumer({ groupId: "notification-service" });
  const schemaRegistry = new SchemaRegistry({ host: config.schemaRegistryUrl });

  await consumer.connect();
  await consumer.subscribe({ topic: "billing.invoice.generated", fromBeginning: true });

  await consumer.run({
    eachMessage: async ({ message }: EachMessagePayload) => {
      if (!message.value) {
        return;
      }

      const invoice = (await schemaRegistry.decode(message.value)) as BillingInvoiceGenerated;

      if (!invoice.accountId) {
        // @kafkajs/confluent-schema-registry decodes strictly against the writer's
        // registered schema, not a reader schema with defaults applied — an old
        // message written before the AccountId re-keying has no accountId property
        // at all, decoding to undefined here rather than the schema's "" default.
        // Skip it rather than let the error propagate, which would otherwise retry
        // the same offset forever and crash the consumer out of its group.
        console.warn(`Skipping billing.invoice.generated message at offset ${message.offset}: missing accountId`);
        return;
      }

      const webhookUrl = await getAccountWebhookUrl(config.accountCustomerBaseUrl, invoice.accountId);

      await sendInvoiceNotification(webhookUrl, {
        invoiceId: invoice.invoiceId,
        accountId: invoice.accountId,
        amountDue: invoice.amountDue,
        dueDate: new Date(invoice.dueDate).toISOString()
      });
    }
  });
}
