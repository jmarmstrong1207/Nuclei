using System.Collections.Specialized;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace Nuclei.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ReportCommandService
{


    private static string webhookURL =
        new ConfigurationBuilder().AddUserSecrets<ReportCommandService>().Build()["webhookURL"];
    public static bool SendDiscordMessage(string username, string message)
    {
        NameValueCollection discordValues = new NameValueCollection();
        discordValues.Add("username", username);
        //discordValues.Add("avatar_url", profilepic);
        discordValues.Add("content", message);

        try
        {
            new WebClient().UploadValues(webhookURL, discordValues);
            return true;
        }
        catch (WebException e)
        {
            Nuclei.Logger?.LogError(e.Message);
            return false;
        }
    } 
}