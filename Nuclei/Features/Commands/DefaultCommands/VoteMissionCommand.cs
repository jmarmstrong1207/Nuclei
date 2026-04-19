using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking;
using Nuclei.CritzOS.Features;
using Nuclei.Enums;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.Features.Commands.DefaultCommands;

public class VoteMissionCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    private static List<MissionOptions>? _fetchedMissions;
    public override string Name { get; } = "votemission";
    public override string Description { get; } = "lets you vote the next mission";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}votemission to get list of missions. {NucleiConfig.CommandPrefixChar}votemission <number> to start a vote for that mission";
    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;

    public override bool Validate(Player player, string[] args)
    {
        if (args.Length > 1)
            return false;
        if ((args.Length == 1 && !int.TryParse(args[0], out _)) || (args.Length == 1 && int.Parse(args[0]) <= 0))
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }
        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        if (args.Length == 0)
        {
            _fetchedMissions = MissionService.GetAllMissions();

            ChatService.SendPrivateChatMessage($"Choose from the following missions ({NucleiConfig.CommandPrefixChar}votemission <number>):", player);
            // Get missions
            for (var i = 0; i < _fetchedMissions.Count; i++) ChatService.SendPrivateChatMessage($"{i + 1}: {_fetchedMissions[i].Key.Name}", player);
            return true;
        }

        if (_fetchedMissions == null)
        {
            ChatService.SendPrivateChatMessage($"Please type '{NucleiConfig.CommandPrefixChar}votemission' without arguments to fetch the mission list first.", player);
            return false;
        }

        var idx = int.Parse(args[0]);

        if (idx > _fetchedMissions.Count || idx < 1)
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }

        void OnPass()
        {
            var m = $"Mission vote for {_fetchedMissions![idx - 1].Key.Name} has passed";
            ReportCommandService.LogChatMessage($"{player.PlayerName}", m);

            MissionService.SetNextMission(_fetchedMissions![idx - 1]);
            _fetchedMissions = null;
        }

        if (VoteService.CanStartVote())
        {
            var startingMessage = $"vote to queue {_fetchedMissions[idx - 1].Key.Name} as the next mission has started";
            ReportCommandService.LogChatMessage($"{player.PlayerName}",
                startingMessage);
            ChatService.SendChatMessage(startingMessage);
            VoteService.StartVote(
                player,
                OnPass,
                false,
                false,
                reason: "Queue as next mission",
                targetName: _fetchedMissions[idx - 1].Key.Name
            );
        }
        else
        {
            ChatService.SendPrivateChatMessage("Cannot start a new mission vote, please wait for current vote to expire.", player);
            _fetchedMissions = null;
            return false;
        }
        return true;
    }

    public override bool Execute(string[] args)
    {
        throw new Exception("Requires Player object");
    }
}