using System;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

/// <summary>
///     Send discord invite URL to player
/// </summary>
public class GambleCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "gamble";
    public override string Description { get; } = "50% chance you receive 2x your bet. IF you lose, 20% chance you eject out of rage";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}gamble <$ in million>. eg: '{NucleiConfig.CommandPrefixChar}gamble 100' gambles 100m";

    public override bool Validate(Player player, string[] args)
    {
        if (!int.TryParse(args[0], out _))
        {
            return false;
        }

        if (player.Aircraft == null)
        {
            ChatService.SendPrivateChatMessage("You must be spawned in before you can gamble!", player);
            return false;
        }

        if (!player.Aircraft.airborne)
        {
            ChatService.SendPrivateChatMessage("You must be airborne before you can gamble!", player);
            return false;
        }

        if (int.Parse(args[0]) <= 0)
        {
            ChatService.SendPrivateChatMessage("Your bet cannot be negative!", player);
            return false;
        }

        if (int.Parse(args[0]) > player.Allocation)
        {
            ChatService.SendPrivateChatMessage("You don't have that much money. Please try again", player);
            return false;
        }
        
        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        var bet = int.Parse(args[0]);
        if (GambleService.CoinFlip())
        {
            
            ChatService.SendChatMessage($"{player.PlayerName} has won +${bet}m!");
            player.SetAllocation(player.Allocation + bet);
            ChatService.SendPrivateChatMessage( $"Current balance: ${player.Allocation}m", player);
            
        }
        else
        {
            ChatService.SendChatMessage($"{player.PlayerName} has lost -${bet}m!", player);
            player.SetAllocation(player.Allocation - bet);
            var punishment = GambleService.Rnd.NextDouble() * 100;
            if (punishment <= 20)
            {
                ChatService.SendChatMessage($"{player.PlayerName} has ejected out of rage!", player);
                player.Aircraft.StartEjectionSequence();
            }
            ChatService.SendPrivateChatMessage( $"Current balance: ${player.Allocation}m", player);
        }
        return true;
    }

    public override bool Execute(string[] args)
    {
        return false;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;
}

public static class GambleService
{
    public static readonly Random Rnd = new Random();

    public static bool CoinFlip()
    {
        return Rnd.Next(0, 2) == 0;
    }

}