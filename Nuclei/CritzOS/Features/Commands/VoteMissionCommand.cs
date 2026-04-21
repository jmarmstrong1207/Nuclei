using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

public class SetMissionCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    private static List<MissionOptions>? _fetchedMissions;
    public override string Name { get; } = "setmission";
    public override string Description { get; } = "Queue next mission";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}setmission to get list of missions. {NucleiConfig.CommandPrefixChar}setmission <number> to queue that mission";
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
        // TODO: MODULARIZE THIS PART GETTING A LIST OF MISSIONS. It uses the same code as votemission
        if (args.Length == 0)
        {
            _fetchedMissions = MissionService.GetAllMissions();

            ChatService.SendPrivateChatMessage($"Choose from the following missions ({NucleiConfig.CommandPrefixChar}setmission <number>):", player);
            // Get missions
            for (var i = 0; i < _fetchedMissions.Count; i++) ChatService.SendPrivateChatMessage($"{i + 1}: {_fetchedMissions[i].Key.Name}", player);
            return true;
        }

        if (_fetchedMissions == null)
        {
            ChatService.SendPrivateChatMessage($"Please type '{NucleiConfig.CommandPrefixChar}setmission' without arguments to fetch the mission list first.", player);
            return false;
        }

        var idx = int.Parse(args[0]);

        if (idx > _fetchedMissions.Count || idx < 1)
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }

        var m = $"Mission '{_fetchedMissions![idx - 1].Key.Name}' has been queued";
        ReportCommandService.LogChatMessage($"{player.PlayerName}", m);

        MissionService.SetNextMission(_fetchedMissions![idx - 1]);
        _fetchedMissions = null;
        return true;
    }

    public override bool Execute(string[] args)
    {
        throw new Exception("Requires Player object");
    }
}