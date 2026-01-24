using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.Features.Commands.DefaultCommands;

/// <summary>
///     Command to ban a player from the server.
/// </summary>
public class DonateCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "donate";
    public override string Description { get; } = "Donate some of your money to somebody";
    public override string Usage { get; } = "donate <ID in their name> <Amount in millions>. e.g: '/donate 1 50' donates 50 million";

    public override bool Validate(Player player, string[] args)
    {
        if (args.Length == 0) return false;
        if (!int.TryParse(args[0], out _) || !int.TryParse(args[1], out _))
        {
            return false;
        }

        if (int.Parse(args[1]) <= 0)
        {
            ChatService.SendPrivateChatMessage("You must donate more than $0m!", player);
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
            ChatService.SendPrivateChatMessage("Could not find player to votekick.", player);
            return false;
        }

        var playerBal = player.Allocation;
        if (amount > playerBal)
        {
            ChatService.SendPrivateChatMessage("You don't have that much money. Please try again", player);
            return false;
        }
        
        player.SetAllocation(player.Allocation - amount);
        targetPlayer!.SetAllocation(targetPlayer.Allocation + amount);
        
        ChatService.SendChatMessage($"{player.PlayerName} has donated ${amount}m to {targetPlayer.PlayerName}");
        return true;
    }

    public override bool Execute(string[] args)
    {
        var idx = int.Parse(args[0]);
        var amount = int.Parse(args[1]);
        if (!PlayerUtils.TryFindPlayerbyID(idx, out Player? targetPlayer))
        {
            Nuclei.Logger?.LogError("Could not find player to votekick.");
            return false;
        }

        targetPlayer!.SetAllocation(targetPlayer.Allocation + amount);
        
        ChatService.SendChatMessage($"Server has donated ${amount}m to {targetPlayer.name}");
        return true;
    }


    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;
}