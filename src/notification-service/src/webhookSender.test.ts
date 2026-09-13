import { describe, it, expect, vi, afterEach } from "vitest";
import { sendInvoiceNotification } from "./webhookSender.js";

describe("sendInvoiceNotification", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  const notification = {
    invoiceId: "INV-1",
    accountId: "ACC-1",
    amountDue: 42.5,
    dueDate: "2007-04-01T00:00:00.000Z"
  };

  it("posts the notification as JSON to the webhook URL", async () => {
    const fetchMock = vi.fn(async () => ({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await sendInvoiceNotification("http://localhost:9999/webhook", notification);

    expect(fetchMock).toHaveBeenCalledWith("http://localhost:9999/webhook", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(notification)
    });
  });

  it("throws when the webhook responds with a non-ok status", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 500 })));

    await expect(sendInvoiceNotification("http://localhost:9999/webhook", notification)).rejects.toThrow(
      "Webhook POST to 'http://localhost:9999/webhook' returned 500"
    );
  });
});
