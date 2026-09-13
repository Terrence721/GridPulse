import { loadConfig } from "./config.js";
import { runNotificationConsumer } from "./notificationConsumer.js";

try {
  const config = loadConfig();
  await runNotificationConsumer(config);
} catch (error) {
  console.error("Notification Service failed to start:", error);
  process.exit(1);
}
