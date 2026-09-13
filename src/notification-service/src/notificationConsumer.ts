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
