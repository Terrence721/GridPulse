export interface InvoiceGeneratedNotification {
  invoiceId: string;
  accountId: string;
  amountDue: number;
  dueDate: string;
}

export async function sendInvoiceNotification(
  webhookUrl: string,
  notification: InvoiceGeneratedNotification
): Promise<void> {
  const response = await fetch(webhookUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(notification)
  });

  if (!response.ok) {
    throw new Error(`Webhook POST to '${webhookUrl}' returned ${response.status}`);
  }
}
