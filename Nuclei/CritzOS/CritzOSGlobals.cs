using System;

namespace Nuclei.CritzOS;

public class CritzOSGlobals
{
    internal static string ServerName = Environment.GetEnvironmentVariable("serverName")!;
    internal static string connectionString = Environment.GetEnvironmentVariable("connectionString")!;
    
    internal static readonly string WebhookURL = Environment.GetEnvironmentVariable("webhookURL")!;
    internal static readonly string ChatLogWebhookURL = Environment.GetEnvironmentVariable("chatLogWebhookURL")!;
}