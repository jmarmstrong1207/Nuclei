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
public class AddFundsCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "addfunds";
    public override string Description { get; } = "Give some money to somebody";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}addfunds <ID in their name> <Amount in millions>. e.g: '{NucleiConfig.CommandPrefixChar}addfunds 1 50' donates 50 million";

    public override bool Validate(Player player, string[] args)
    {
        if (args.Length == 0) return false;
        if (!int.TryParse(args[0], out _) || !int.TryParse(args[1], out _))
        {
            return false;
        }

        return args.Length == 2;
    }

    public override bool Execute(Player player, string[] args)
    {
        var idx = int.Parse(args[0]);
        var amount = int.Parse(args[1]);
        if (!PlayerUtils.TryFindPlayerbyID(idx, out Player? targetPlayer))
        {
            ChatService.SendPrivateChatMessage("Could not find player.", player);
            return false;
        }

        targetPlayer!.SetAllocation(targetPlayer.Allocation + amount);
        
        ChatService.SendChatMessage($"{player.PlayerName} has added ${amount}m to {targetPlayer.PlayerName}");
        return true;
    }

    public override bool Execute(string[] args)
    {
        var idx = int.Parse(args[0]);
        var amount = int.Parse(args[1]);
        if (!PlayerUtils.TryFindPlayerbyID(idx, out Player? targetPlayer))
        {
            Nuclei.Logger?.LogError("Could not find player.");
            return false;
        }

        targetPlayer!.SetAllocation(targetPlayer.Allocation + amount);
        
        ChatService.SendChatMessage($"Server has donated ${amount}m to {targetPlayer.name}");
        return true;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
}