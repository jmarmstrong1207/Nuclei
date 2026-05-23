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
public class RestartCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "restart";
    public override string Description { get; } = "IMMEDIATELY restart server";
    public override string Usage { get; } = $"{NucleiConfig.CommandPrefixChar}restart";

    public override bool Validate(Player player, string[] args)
    {
        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        Nuclei.RestartServer();
        return true;
    }

    public override bool Execute(string[] args)
    {
        return false;
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Moderator;
}