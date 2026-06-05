using System;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Helpers;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

/// <summary>
///     Command to ban a player from the server.
/// </summary>
public class ReportCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "report";
    public override string Description { get; } = "Report anything to the server owner";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}report <message>";

    public override bool Validate(Player player, string[] args)
    {
        return args.Length >= 1;
    }

    public override bool Execute(Player player, string[] args)
    {
        string report = String.Join(" ", args);
        Nuclei.Logger?.LogInfo($"Player {player.PlayerName} reported: {report}.");
        try
        {
            ReportCommandService.SendManualReport(PlayerUtils.StripStaffPrefix(player.PlayerName), report);
            ChatService.SendPrivateChatMessage("Report has been sent!", player);
        }
        catch
        {
            ChatService.SendPrivateChatMessage("Message failed to send (THIS SHOULDN'T HAPPEN!!!)", player);
        }
        return true;
    }

    public override bool Execute(string[] args)
    {
        return false;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;
}