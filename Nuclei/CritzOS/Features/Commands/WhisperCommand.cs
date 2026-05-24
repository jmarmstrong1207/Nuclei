using System.Linq;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

/// <summary>
/// Sends a private message to a specific player.
/// </summary>
/// <param name="config"></param>
public class WhisperCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    /// <inheritdoc />
    public override string Name { get; } = "whisper";

    /// <inheritdoc />
    public override string Description { get; } = "Send a private message to a specified user via ID.";
    
    /// <inheritdoc />
    public override string Usage { get; } = "whisper <ID> <message>";

    /// <inheritdoc />
    public override bool Validate(Player player, string[] args)
    {
        if ((args.Length != 0 && !int.TryParse(args[0], out _)) || (args.Length != 0 && int.Parse(args[0]) <= 0))
        {
            ChatService.SendPrivateChatMessage("Number invalid. Please Try again.", player);
            return false;
        }
        if (args.Length < 2)
        {
            ChatService.SendPrivateChatMessage("Please provide a reason.", player);
            return false;
        }

        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        var idx = int.Parse(args[0]);
        
        PlayerUtils.TryFindPlayerbyID(idx, out var found);
        
        if (!found)
        {
            ChatService.SendPrivateChatMessage("Invalid ID", player);
            return false;
        }
        
        var message = string.Join(" ", args.Skip(1));
        ChatService.SendPrivateChatMessage($"{player.PlayerName} whispered: {message}", found!);
        ChatService.SendPrivateChatMessage($"Message sent to {found!.PlayerName}: {message}", player);
        
        _ = CritzOSDB.LogWhisperAsync(player, found, message);
        
        return true;
    }

    public override bool Execute(string[] args)
    {
        throw new System.NotImplementedException();
    }


    /// <inheritdoc />
    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
    
    
    
}