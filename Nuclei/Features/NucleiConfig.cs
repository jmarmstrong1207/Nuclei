using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Nuclei.Enums;
using Nuclei.Helpers;
using Steamworks;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Nuclei.Features;

// TODO: cleanup & review whether we can hook into the base game's config?

/// <summary>
///     Configuration class for Nuclei.
/// </summary>
public static class NucleiConfig
{
    internal const string GeneralSection = "General";
    internal const string VotekickSection = "Votekick";

    internal static ConfigEntry<double>? KickThreshold;
    internal const double DefaultKickThreshold = 0.5;

    internal static ConfigEntry<int>? KickTimeout;
    internal const int DefaultKickTimeout = 20;

    internal static ConfigEntry<string>? MessageOfTheDay;
    internal const string DefaultMessageOfTheDay = "This server is running on Nuclei! Have fun!";
    
    internal static ConfigEntry<uint>? MotDFrequency;
    internal const uint DefaultMotDFrequency = 900;
    
    internal static ConfigEntry<string>? WelcomeMessage;
    internal const string DefaultWelcomeMessage = $"Welcome to the server, {DynamicPlaceholderUtils.PlayerNameCensored}!";
    
    internal static ConfigEntry<string>? Moderators;
    internal const string DefaultModerators = "";
    
    internal static ConfigEntry<string>? Admins;
    internal const string DefaultAdmins = "";
    
    internal static ConfigEntry<string>? Owner;
    internal const string DefaultOwner = "";
    
    internal static ConfigEntry<bool>? RefreshServerNamePeriodically;
    internal const bool DefaultRefreshServerNamePeriodically = true;

    internal static ConfigEntry<bool>? RandomizeWeather;
    internal const bool DefaultRandomizeWeather = true;

    internal static ConfigEntry<string>? CommandPrefix;
    internal const string DefaultCommandPrefix = "/";

    internal static ConfigEntry<bool>? UseStaffPrefix;
    internal const bool DefaultUseStaffPrefix = true;

    internal static ConfigEntry<string>? StaffPrefix;
    internal const string DefaultStaffPrefix = "<color=#FFD700>[Staff]</color>";

    internal static ConfigEntry<string>? ServerBroadcastName;
    internal const string DefaultServerBroadcastName = "<color=#99182e>[Nuclei]</color>";
    internal static ConfigEntry<bool>? RankCatchUp;
    internal const bool DefaultRankCatchUp = true;
    
    internal static List<string> ModeratorsList => Moderators!.Value.Split(';').Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
    
    internal static List<string> AdminsList => Admins!.Value.Split(';').Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

    internal static char CommandPrefixChar => CommandPrefix!.Value[0];
    
    internal static void InitSettings(ConfigFile config)
    {
        Nuclei.Logger?.LogDebug("Loading settings...");

        KickThreshold = config.Bind(VotekickSection, "KickThreshold", DefaultKickThreshold, "The percentage of the lobby that needs to agree to kick the player.");
        Nuclei.Logger?.LogDebug($"KickThreshold: {KickThreshold.Value}");

        KickTimeout = config.Bind(VotekickSection, "KickTimeout", DefaultKickTimeout, "The time it takes before the votekick expires.");
        Nuclei.Logger?.LogDebug($"KickTimeout: {KickTimeout.Value}");
        
        MessageOfTheDay = config.Bind(GeneralSection, "MessageOfTheDay", DefaultMessageOfTheDay, "The message of the day for the server. This message is displayed periodically to all players.");
        Nuclei.Logger?.LogDebug($"MessageOfTheDay: {MessageOfTheDay.Value}");
        
        MotDFrequency = config.Bind(GeneralSection, "MotDFrequency", DefaultMotDFrequency, "The frequency in seconds at which the message of the day is displayed. Set to 0 to disable the message of the day. Checks are done every minute.");
        Nuclei.Logger?.LogDebug($"MotDFrequency: {MotDFrequency.Value}");
        
        WelcomeMessage = config.Bind(GeneralSection, "WelcomeMessage", DefaultWelcomeMessage, "The message displayed to players when they join the server. See the readme for placeholders.");
        Nuclei.Logger?.LogDebug($"WelcomeMessage: {WelcomeMessage.Value}");
        
        Moderators = config.Bind(GeneralSection, "Moderators", DefaultModerators, "A list of moderators who have access to moderator commands. Separate steam IDs with a semicolon.");
        Nuclei.Logger?.LogDebug($"Moderators: {Moderators.Value}");
        
        Admins = config.Bind(GeneralSection, "Admins", DefaultAdmins, "A list of admins who have access to admin commands. Separate steam IDs with a semicolon.");
        Nuclei.Logger?.LogDebug($"Admins: {Admins.Value}");
        
        Owner = config.Bind(GeneralSection, "Owner", DefaultOwner, "The Steam ID of the server owner. This player has access to all commands, and cannot be removed from the admin list.");
        Nuclei.Logger?.LogDebug($"Owner: {Owner.Value}");

        CommandPrefix = config.Bind(GeneralSection, "CommandPrefix", DefaultCommandPrefix, "What to use as the command prefix (the character at the start of a command).");
        Nuclei.Logger?.LogDebug($"CommandPrefix: {CommandPrefix.Value}");

        UseStaffPrefix = config.Bind(GeneralSection, "UseStaffPrefix", DefaultUseStaffPrefix,
            "Whether to use staff prefix or not.");
        Nuclei.Logger?.LogDebug($"UseStaffPrefix: {UseStaffPrefix.Value}");

        StaffPrefix = config.Bind(GeneralSection, "StaffPrefix", DefaultStaffPrefix,
            "The prefix added in-front of the usernames of Moderators, Admins and the Owner.");
        Nuclei.Logger?.LogDebug($"StaffTag: {StaffPrefix.Value}");

        ServerBroadcastName = config.Bind(GeneralSection, "ServerBroadcastName", DefaultServerBroadcastName,
            "The name that appears in the chat when the server broadcasts a message.");
        Nuclei.Logger?.LogDebug($"ServerBroadcastName: {ServerBroadcastName}");
        RankCatchUp = config.Bind(GeneralSection, "RankCatchUp", DefaultRankCatchUp, "Whether to enable the rank catch-up system, which gives players a rank and allocation boost based on how far into the mission they join.");
        Nuclei.Logger?.LogDebug($"RankCatchUp: {RankCatchUp.Value}");

        RandomizeWeather = config.Bind(GeneralSection, "RandomizeWeather", DefaultRandomizeWeather,
            "Randomize weather by modifying the .json mission file directly. This requires the missions to be in " +
            "the mission folder you assigned in DedicatedServerConfig.json, meaning all missions' MissionGroup must be User, not BuiltIn");

        Nuclei.Logger?.LogDebug($"CommandPrefix: {CommandPrefix.Value}");
        
        Nuclei.Logger?.LogDebug("Loaded settings!");
    }

    internal static void ValidateSettings()
    {
        Nuclei.Logger?.LogDebug("Validating settings...");

        if (CommandPrefix!.Value.Length != 1)
        {
            Nuclei.Logger?.LogWarning("CommandPrefix must be a single character! Resetting to default value.");
            CommandPrefix.Value = DefaultCommandPrefix;
        }

        if (UseStaffPrefix!.Value && StaffPrefix!.Value.Length == 0)
        {
            Nuclei.Logger?.LogWarning("UseStaffPrefix is enabled, however no StaffPrefix has been set. Resetting to default value.");
            StaffPrefix.Value = DefaultStaffPrefix;
        }

        if (ServerBroadcastName!.Value.Length == 0)
        {
            Nuclei.Logger?.LogWarning("No ServerBroadcastName has been set. Resetting to default value.");
            ServerBroadcastName.Value = DefaultServerBroadcastName;
        }
        
        ValidateForUserErrors();
        
        Nuclei.Logger?.LogDebug("Settings validated!");
    }

    internal static void ValidateForUserErrors()
    {
        if (Owner!.Value == "")
        {
            Nuclei.Logger?.LogWarning("Owner is not set! It is recommended to set the owner in the config file.");
        }
        else
        {
            if (!ulong.TryParse(Owner.Value, out _))
                Nuclei.Logger?.LogWarning("Owner is not a valid Steam ID! Remove or correct it in the config file.");
            if (Owner.Value.Contains(";"))
                Nuclei.Logger?.LogWarning("Owner cannot be a list of Steam IDs! Remove or correct it in the config file. (Found a semicolon ';')");
        }
        
        if (ModeratorsList.Any(m => !ulong.TryParse(m, out _))) 
            Nuclei.Logger?.LogWarning("One or more moderators are not valid Steam IDs! Remove or correct them in the config file.");

        if (Moderators!.Value.EndsWith(";"))
        {
            Nuclei.Logger?.LogWarning("Moderators list ends with a semicolon ';'. Removing it.");
            Moderators.Value = Moderators.Value.TrimEnd(';');
        }
        
        if (Moderators!.Value.Contains(";;"))
        {
            Nuclei.Logger?.LogWarning("Moderators list contains multiple following semicolons ';;'. Fixing it.");
            Moderators.Value = Moderators.Value.Replace(";;", ";");
        }

        if (AdminsList.Any(a => !ulong.TryParse(a, out _)))
            Nuclei.Logger?.LogWarning("One or more admins are not valid Steam IDs! Remove or correct them in the config file.");

        if (Admins!.Value.EndsWith(";"))
        {
            Nuclei.Logger?.LogWarning("Admins list ends with a semicolon ';'. Removing it.");
            Admins.Value = Admins.Value.TrimEnd(';');
        }
        
        if (Admins!.Value.Contains(";;"))
        {
            Nuclei.Logger?.LogWarning("Admins list contains multiple following semicolons ';;'. Fixing it.");
            Admins.Value = Admins.Value.Replace(";;", ";");
        }
    }
    
    internal static void RemoveModerator(string steamId)
    {
        var moderatorsList = ModeratorsList;
        moderatorsList.Remove(steamId);
        Moderators!.Value = string.Join(";", moderatorsList);
    }
    
    internal static void AddModerator(string steamId)
    {
        var moderatorsList = ModeratorsList;
        if (moderatorsList.Contains(steamId))
            return;
        moderatorsList.Add(steamId);
        Moderators!.Value = string.Join(";", moderatorsList);
    }
    
    internal static void RemoveAdmin(string steamId)
    {
        var adminsList = AdminsList;
        adminsList.Remove(steamId);
        Admins!.Value = string.Join(";", adminsList);
    }
    
    internal static void AddAdmin(string steamId)
    {
        var adminsList = AdminsList;
        if (adminsList.Contains(steamId))
            return;
        adminsList.Add(steamId);
        Admins!.Value = string.Join(";", adminsList);
    }
    
    /// <summary>
    ///     Check if the given Steam ID is a moderator.
    /// </summary>
    /// <param name="steamId"> The Steam ID to check. </param>
    /// <returns> Whether the Steam ID is a moderator. </returns>
    public static bool IsModerator(ulong steamId)
    {
        return ModeratorsList.Contains(steamId.ToString());
    }

    /// <summary>
    ///     Check if the given Steam ID is an admin.
    /// </summary>
    /// <param name="steamId"> The Steam ID to check. </param>
    /// <returns> Whether the Steam ID is an admin. </returns>
    public static bool IsAdmin(ulong steamId)
    {
        return AdminsList.Contains(steamId.ToString());
    }

    /// <summary>
    ///     Check if the given Steam ID is the owner.
    /// </summary>
    /// <param name="steamId"> The Steam ID to check. </param>
    /// <returns> Whether the Steam ID is the owner. </returns>
    public static bool IsOwner(ulong steamId)
    {
        return Owner!.Value == steamId.ToString();
    }
}