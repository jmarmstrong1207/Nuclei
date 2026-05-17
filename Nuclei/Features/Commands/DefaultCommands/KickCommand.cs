using System;
using BepInEx.Configuration;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using Nuclei.CritzOS.Features;
using Nuclei.Enums;
using Nuclei.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.Features.Commands.DefaultCommands;

/// <summary>
///     Command to kick a player from the server.
/// </summary>
public class KickCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "kick";
    public override string Description { get; } = "Kicks a player from the server.";
    public override string Usage { get; } = "kick <ID>";

    public override bool Validate(Player player, string[] args)
    {
        if ((args.Length != 0 && !int.TryParse(args[0], out _)) || (args.Length != 0 && int.Parse(args[0]) <= 0))
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }
        
        return args.Length == 1;
    }

    public override bool Execute(Player player, string[] args)
    {
        var target = args[0];

        int idx = int.Parse(args[0]);
        PlayerIdentificationService.GetPlayerById(idx, out var targetPlayer);
        if (targetPlayer == null)
        {
            ChatService.SendPrivateChatMessage("Could not find player to votekick.", player);
            Nuclei.Logger?.LogWarning($"Ban command run. Player [{target}] not found.");
            return false;
        }
        
        PlayerUtils.TryFindPlayerBySteamId((ulong)targetPlayer, out var p);

        var msg = new CommandMessage
        {
            name = "kick-player",
            arguments =
            [
                Convert.ToString(p.SteamID)
            ]
        };

        if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
        {
            ChatService.SendPrivateChatMessage($"Player {p.PlayerName} has been kicked", player);
            CritzOSDB.LogKick((ulong)targetPlayer);
            Nuclei.Logger?.LogInfo($"Player {p.PlayerName} has been kicked");
            return true;
        }
        else
        {
            ChatService.SendPrivateChatMessage($"An error has occured while attempting to kick. Report this to the server owner", player);
            Nuclei.Logger?.LogError($"An error has occured while attempting to kick. Report this to the server owner");
        }

        return false;
    }
    
    public override bool Execute(string[] args)
    {
        var target = args[0];

        if (PlayerUtils.TryFindPlayer(target, out var targetPlayer))
        {
            PlayerUtils.KickPlayer(targetPlayer!);
            Nuclei.Logger?.LogInfo($"Player {target} was kicked from the server.");
            ChatService.SendChatMessage($"Player {target} was kicked from the server.");
            return true;
        }

        Nuclei.Logger?.LogWarning($"Player {target} not found.");
        return false;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
}