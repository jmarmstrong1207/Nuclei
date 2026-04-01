using System;
using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Nuclei.Features.Commands.DefaultCommands;

// TODO: This needs to be reworked so that it uses the NetworkManagerInstance.Server()?

/// <summary>
///     Command to end the current mission and start a new one.
/// </summary>
public class NewMissionCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name { get; } = "newmission";
    public override string Description { get; } = "Choose the next mission (same as votemission)";
    public override string Usage { get; } = "newmission <mission ID>";
    public override bool Validate(Player player, string[] args)
    {
        return true;
    }

    public override bool Execute(Player player, string[] args)
    {
        throw new NotImplementedException();
    }
    
    public override bool Execute(string[] args)
    {
        throw new NotImplementedException();
    }

    public override PermissionLevel DefaultPermissionLevel { get; } = PermissionLevel.Admin;
}