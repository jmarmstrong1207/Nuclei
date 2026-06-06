using System;
using System.Linq;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;
using Nuclei.Enums;
using Nuclei.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.Features.Commands.DefaultCommands;

public class VoteKickCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "votekick";
    public override string Description { get; } = "lets you vote to kick a user";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}votekick <ID from the Scoreboard> <Reason>";
    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;

    public override bool Validate(Player player, string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }
        if ((args.Length == 1 && !int.TryParse(args[0], out _)) || (args.Length == 1 && int.Parse(args[0]) <= 0))
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }
        if (args.Length < 2)
        {
            ChatService.SendPrivateChatMessage("Please provide a reason.", player);
            return false;
        }
        return args.Length >= 2;
    }

    public override bool Execute(Player player, string[] args)
    {
        int idx = int.Parse(args[0]);
        string reason = string.Join(" ", args.Skip(1).ToArray());
        PlayerIdentificationService.GetPlayerById(idx, out var targetPlayer);
        if (targetPlayer == null)
        {
            ChatService.SendPrivateChatMessage("Could not find player to votekick.", player);
            return false;
        }
        
        PlayerUtils.TryFindPlayerBySteamId((ulong)targetPlayer, out var p);

        void OnPass()
        {
            _ = PlayerUtils.KickPlayerAsync(p, reason);
            ReportCommandService.SendReport($"{player.PlayerName} ({CritzOSGlobals.ServerName}", $"Votekick for {p.PlayerName} has passed. Reason: {reason}");
            _ = CritzOSDB.LogVoteKickAsync((ulong)targetPlayer, player.SteamID, reason);
        }

        if (VoteService.CanStartVote())
        {
            var startingMessage = $"A vote to kick {p!.PlayerName} has started. Reason: {reason}";
            ReportCommandService.SendReport($"{player.PlayerName} ({CritzOSGlobals.ServerName}",
                startingMessage);
            ChatService.SendChatMessage(startingMessage);
            
            VoteService.StartVote(player, OnPass, false, false, reason, player.PlayerName);
            return true;
        }
        else
        {
            ChatService.SendPrivateChatMessage("Cannot start a new votekick, please wait for current vote to expire.", player);
            return false;
        }
    }

    public override bool Execute(string[] args)
    {
        throw new Exception("Requires Player object");
    }
}