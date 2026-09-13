import { describe, it, expect, vi, afterEach } from "vitest";
import { getAccountWebhookUrl } from "./accountClient.js";

describe("getAccountWebhookUrl", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns the account's contactWebhookUrl when the request succeeds", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({
      ok: true,
      json: async () => ({ id: "ACC-1", contactWebhookUrl: "http://localhost:9999/webhook" })
    })));

    const url = await getAccountWebhookUrl("http://account-customer", "ACC-1");

    expect(url).toBe("http://localhost:9999/webhook");
  });

  it("requests the account by id, URL-encoded", async () => {
    const fetchMock = vi.fn(async () => ({
      ok: true,
      json: async () => ({ id: "ACC 1", contactWebhookUrl: "http://localhost:9999/webhook" })
    }));
    vi.stubGlobal("fetch", fetchMock);

    await getAccountWebhookUrl("http://account-customer", "ACC 1");

    expect(fetchMock).toHaveBeenCalledWith("http://account-customer/accounts/ACC%201");
  });

  it("throws when the request fails", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 404 })));

    await expect(getAccountWebhookUrl("http://account-customer", "ACC-404")).rejects.toThrow(
      "Account/Customer Service returned 404 for account 'ACC-404'"
    );
  });
});
