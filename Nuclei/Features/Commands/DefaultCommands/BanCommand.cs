using System;
using BepInEx.Configuration;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.Features.Commands.DefaultCommands;

/// <summary>
///     Command to ban a player from the server.
/// </summary>
public class BanCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "ban";
    public override string Description { get; } = "Bans a player from the server.";
    public override string Usage { get; } = "ban <player_name> <reason>";

    public override bool Validate(Player player, string[] args)
    {
        return args.Length > 1;
    }

    public override bool Execute(Player player, string[] args)
    {
        var target = args[0];

        int idx = int.Parse(args[0]);
        if (!PlayerUtils.TryFindPlayerbyID(idx, out Player? targetPlayer))
        {
            ChatService.SendPrivateChatMessage("Could not find player to votekick.", player);
            Nuclei.Logger?.LogWarning($"Ban command run. Player [{target}] not found.");
        }

        var reason = $"Player: {targetPlayer.PlayerName}, " + String.Join(" ", args).Substring(1);
        CommandMessage msg = new CommandMessage();
        msg.name = "banlist-add";
        msg.arguments = new string[]
        {
            Convert.ToString(targetPlayer.SteamID), reason
        };

        if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
        {
            ChatService.SendPrivateChatMessage($"Player {targetPlayer.PlayerName} has been banned", player);
            Nuclei.Logger?.LogInfo($"Player {targetPlayer.PlayerName} has been banned");
            return true;
        }
        else
        {
            ChatService.SendPrivateChatMessage($"An error has occured while attempting to ban. Report this to the server owner", player);
            Nuclei.Logger?.LogError($"An error has occured while attempting to ban. Report this to the server owner");
        }

        return false;
    }

    public override bool Execute(string[] args)
    {
        throw new NotImplementedException();
    }


    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
}