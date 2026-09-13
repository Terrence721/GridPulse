export interface Account {
  id: string;
  contactWebhookUrl: string;
}

export async function getAccountWebhookUrl(baseUrl: string, accountId: string): Promise<string> {
  const response = await fetch(`${baseUrl}/accounts/${encodeURIComponent(accountId)}`);

  if (!response.ok) {
    throw new Error(`Account/Customer Service returned ${response.status} for account '${accountId}'`);
  }

  const account = (await response.json()) as Account;
  return account.contactWebhookUrl;
}
