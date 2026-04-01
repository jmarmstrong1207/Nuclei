using System;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
using Nuclei.Features;
using Nuclei.Features.Commands;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Nuclei.CritzOS.Features.Commands;

/// <summary>
///     Command to ban a player from the server.
/// </summary>
public class updateMotdCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "updatemotd";
    public override string Description { get; } = "update motd";
    public override string Usage { get; } = "updatemotd";

    public override bool Validate(Player player, string[] args)
    {
        return args.Length == 0;
    }

    public override bool Execute(Player player, string[] args)
    {
        try
        {
            ChatService.UpdateMotD();
            return true;
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
            return false;
        }
    }

    public override bool Execute(string[] args)
    {
        try
        {
            ChatService.UpdateMotD();
            return true;
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
            return false;
        }
    }


    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
}