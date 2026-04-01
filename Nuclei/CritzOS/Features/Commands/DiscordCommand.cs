using System;
using System.Diagnostics.Eventing.Reader;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Helpers;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

/// <summary>
///     Send discord invite URL to player
/// </summary>
public class DiscordCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "discord";
    public override string Description { get; } = "Get the discord server invite URL";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}discord";

    public override bool Validate(Player player, string[] args)
    {
        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        ChatService.SendPrivateChatMessage("Discord (type in browser!!!): discord.critzaura.com", player);
        return true;
    }

    public override bool Execute(string[] args)
    {
        return false;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Everyone;
}