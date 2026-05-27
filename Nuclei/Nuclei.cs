using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mirage.SteamworksSocket;
using NuclearOption.Networking;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;
using Nuclei.CritzOS.Features.Commands;
using Nuclei.CritzOS.Patches.KillsLogging;
using Nuclei.Events;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Features.Commands.DefaultCommands;
using Nuclei.Helpers;
using Nuclei.Plugins;

namespace Nuclei;

/// <summary>
///     Main plugin class for Nuclei.
/// </summary>
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Nuclei : BaseUnityPlugin
{
    internal static DateTime ServerStartTime; // Used to restart server over 24 hours
    internal static Nuclei? Instance { get; private set; }
    internal new static ManualLogSource? Logger { get; private set; }
    private static Harmony? Harmony { get; set; }
    private static bool IsPatched { get; set; }

    /// <summary>
    /// Weapon type storage for weapon kill detection.
    /// </summary>
    public static readonly UnitWeaponLogStorage WeaponStorage = new();
    
    /// <summary>
    /// Weapon name storage for shockwaves.
    /// </summary>
    public static readonly ShockwaveWeaponTypeStorage ShockwaveWeaponStorage = new();

    private void Awake()
    {
        ServerStartTime = DateTime.Now;
        Instance = this;
        
        Logger = base.Logger;
        
        Logger?.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}...");
        
        ServerEvents.OnServerStarted();

        try
        {
            NucleiConfig.InitSettings(Config);
            NucleiConfig.ValidateSettings();
        }
        catch (ArgumentException e)
        {
            Logger?.LogError(
                $"Aborting server launch: Failed to load or validate settings. One of the settings might be the wrong type of value. For more information, see this error trace:\n{e}");
            return;
        }
        catch (Exception e)
        {
            Logger?.LogError($"Aborting server launch: Failed to load or validate settings. For more information, see this error trace:\n{e}");
            return;
        }

        PatchAll();
        RegisterCommands();
        SubscribeToEvents();
        
        ChatService.UpdateMotD();
        
        if (IsPatched)
            Logger?.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        else
            Logger?.LogError($"Plugin {PluginInfo.PLUGIN_GUID} failed to load correctly!");
    }

    private static void PatchAll()
    {
        if (IsPatched)
        {
            Logger?.LogWarning("Already patched!");
            return;
        }

        Logger?.LogDebug("Patching...");

        Harmony ??= new Harmony(PluginInfo.PLUGIN_GUID);

        try
        {
            Harmony.PatchAll();
            IsPatched = true;
            Logger?.LogDebug("Patched!");
        }
        catch (Exception e)
        {
            Logger?.LogError($"Aborting server launch: Failed to Harmony patch the game. For more information, see this error trace:\n{e}");
        }
    }

    private void UnpatchSelf()
    {
        if (Harmony == null)
        {
            Logger?.LogError("Harmony instance is null!");
            return;
        }
        
        if (!IsPatched)
        {
            Logger?.LogWarning("Already unpatched!");
            return;
        }

        Logger?.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();
        IsPatched = false;

        Logger?.LogDebug("Unpatched!");
    }

    private void RegisterCommands()
    {
        CommandService.RegisterCommand(new SayCommand(Config));
        CommandService.RegisterCommand(new KickCommand(Config));
        CommandService.RegisterCommand(new BanCommand(Config));
        CommandService.RegisterCommand(new SetPermissionLevelCommand(Config));
        CommandService.RegisterCommand(new HelpCommand(Config));
        CommandService.RegisterCommand(new NextMissionCommand(Config));
        CommandService.RegisterCommand(new VoteKickCommand(Config));
        CommandService.RegisterCommand(new VoteYesCommand(Config));
        CommandService.RegisterCommand(new VoteNoCommand(Config));
        CommandService.RegisterCommand(new VoteSkipCommand(Config));
        CommandService.RegisterCommand(new VoteMissionCommand(Config));
        
        CommandService.RegisterCommand(new ReportCommand(Config));
        CommandService.RegisterCommand(new DonateCommand(Config));
        CommandService.RegisterCommand(new UpdateMotdCommand(Config));
        CommandService.RegisterCommand(new DiscordCommand(Config));
        CommandService.RegisterCommand(new AddFundsCommand(Config));
        CommandService.RegisterCommand(new WhisperCommand(Config));
        CommandService.RegisterCommand(new SetMissionCommand(Config));
        CommandService.RegisterCommand(new RestartCommand(Config));
        CommandService.RegisterCommand(new RestartAfterMissionCommand(Config));
        
        CommandService.RegisterCommand(new GambleCommand(Config));
    }

    private void SubscribeToEvents()
    {
        PlayerEvents.PlayerJoined += OnPlayerJoin;
        PlayerEvents.PlayerLeft += OnPlayerLeave;
    }

    private static void OnPlayerJoin(Player player)
    {
        Logger?.LogInfo($"{player.PlayerName} joined the game! SteamID: {player.SteamID}");
        PlayerUtils.ApplyOrRemoveStaffTag(player);
        
        PlayerIdentificationService.AssignNewPlayer(player);
        
        // CRITZOS-SPECIFIC STUFF
        if (NucleiConfig.RankCatchUp!.Value) RankCatchUpService.CatchUpPlayer(player);
        ReportCommandService.LogChatMessage($"CritzOS {CritzOSGlobals.ServerName}",
            $"`{player.PlayerName} ({player.SteamID}) joined the game`");
        
        
        // Add user to database or update their username
        _ = CritzOSDB.AddPlayerAsync(player.SteamID, player.PlayerName);
        
        RestartService.CancelRestart();
    }

    private static void OnPlayerLeave(Player player)
    {
       ReportCommandService.LogChatMessage($"CritzOS {CritzOSGlobals.ServerName}",
            $"`{player.PlayerName} ({player.SteamID}) left the game`");
        Logger?.LogInfo($"{player.PlayerName} : {player.SteamID} - left the game");
        PlayerIdentificationService.RemovePlayer(player);

        Logger?.LogInfo($"Player left. Remaining players: {PlayerUtils.GetPlayerCount()}");
        RestartService.CheckIfNoPlayers();
    }
    internal static void RestartServer()
    {
        var port = Globals.DedicatedServerManagerInstance.Config.QueryPort.Value + 1; // Always 1 increment above this
        Logger?.LogInfo($"RESTARTING SERVER AFTER MISSION ENDS...");
        Process.Start("python3",
            $"/home/steam/Nuclear-Option-Server-Tools/restart-server.py {port}");
    }
}