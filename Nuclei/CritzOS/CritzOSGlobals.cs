using System;

namespace Nuclei.CritzOS;

/// <summary>
/// Global variables for CritzOS
/// </summary>
public static class CritzOSGlobals
{
    internal static readonly string ServerName = Environment.GetEnvironmentVariable("serverName")!;
    internal static readonly string ConnectionString = Environment.GetEnvironmentVariable("connectionString")!;
    
    internal static readonly string WebhookURL = Environment.GetEnvironmentVariable("webhookURL")!;
    internal static readonly string ChatLogWebhookURL = Environment.GetEnvironmentVariable("chatLogWebhookURL")!;
}