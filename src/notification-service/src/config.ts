export interface Config {
  kafkaBrokers: string[];
  schemaRegistryUrl: string;
  accountCustomerBaseUrl: string;
}

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

export function loadConfig(): Config {
  return {
    kafkaBrokers: requireEnv("ConnectionStrings__kafka").split(","),
    schemaRegistryUrl: requireEnv("services__schema-registry__http__0"),
    accountCustomerBaseUrl: requireEnv("services__account-customer__http__0")
  };
}
