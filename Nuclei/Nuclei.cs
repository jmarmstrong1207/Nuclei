using System;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOption.Networking;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;
using Nuclei.CritzOS.Features.Commands;
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
    internal static Nuclei? Instance { get; private set; }
    internal new static ManualLogSource? Logger { get; private set; }
    private static Harmony? Harmony { get; set; }
    private static bool IsPatched { get; set; }
    
    private void Awake()
    {
        Instance = this;
        
        Logger = base.Logger;
        
        Logger?.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}...");
        

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
        CommandService.RegisterCommand(new NewMissionCommand(Config));
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
        CommandService.RegisterCommand(new updateMotdCommand(Config));
        CommandService.RegisterCommand(new DiscordCommand(Config));
        CommandService.RegisterCommand(new AddFundsCommand(Config));
        CommandService.RegisterCommand(new SetMissionCommand(Config));
    }

    private void SubscribeToEvents()
    {
        PlayerEvents.PlayerJoined += OnPlayerJoin;
    }

    // TODO: Move these somewhere else?
    private static void HandleConsoleCommand(string rawLine, string[] args)
    {
        Logger?.LogInfo($"> {rawLine}");
        if (args.Length == 0) return;

        var cmd = args[0].ToLowerInvariant();
        CommandService.TryExecuteCommand(cmd, args.Skip(1).ToArray());
    }

    private static void OnPlayerJoin(Player player)
    {
        Nuclei.Logger?.LogInfo($"{player.PlayerName} joined the game! SteamID: {player.SteamID}");
        PlayerUtils.ApplyOrRemoveStaffTag(player);
        PlayerUtils.ApplyID(player);
        
        // CRITZOS-SPECIFIC STUFF
        if (NucleiConfig.RankCatchUp!.Value) RankCatchUpService.CatchUpPlayer(player);
        ReportCommandService.LogChatMessage($"CritzOS {CritzOSGlobals.ServerName}",
            $"`{player.PlayerName} ({player.SteamID}) joined the game`");
        
        
        // Add user to database or update their username
        new Thread(() => 
        {
            Thread.CurrentThread.IsBackground = true; 
            CritzOSDB.AddPlayer(player.SteamID, player.PlayerName);
        }).Start();

    }
}