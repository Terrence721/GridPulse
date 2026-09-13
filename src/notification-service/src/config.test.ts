import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { loadConfig } from "./config.js";

const ENV_KEYS = ["ConnectionStrings__kafka", "services__schema-registry__http__0", "services__account-customer__http__0"];

describe("loadConfig", () => {
  const originalValues: Record<string, string | undefined> = {};

  beforeEach(() => {
    for (const key of ENV_KEYS) {
      originalValues[key] = process.env[key];
      delete process.env[key];
    }
  });

  afterEach(() => {
    for (const key of ENV_KEYS) {
      if (originalValues[key] === undefined) {
        delete process.env[key];
      } else {
        process.env[key] = originalValues[key];
      }
    }
  });

  it("reads and splits kafka brokers, schema registry URL, and account-customer base URL", () => {
    process.env["ConnectionStrings__kafka"] = "broker1:9092,broker2:9092";
    process.env["services__schema-registry__http__0"] = "http://schema-registry";
    process.env["services__account-customer__http__0"] = "http://account-customer";

    const config = loadConfig();

    expect(config).toEqual({
      kafkaBrokers: ["broker1:9092", "broker2:9092"],
      schemaRegistryUrl: "http://schema-registry",
      accountCustomerBaseUrl: "http://account-customer"
    });
  });

  it("throws naming the missing environment variable", () => {
    process.env["services__schema-registry__http__0"] = "http://schema-registry";
    process.env["services__account-customer__http__0"] = "http://account-customer";

    expect(() => loadConfig()).toThrow("Missing required environment variable: ConnectionStrings__kafka");
  });
});
