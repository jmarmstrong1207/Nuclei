using BepInEx.Configuration;
using NuclearOption.Networking;
using Nuclei.Enums;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Nuclei.Features.Commands.DefaultCommands;

/// <summary>
/// Command to forcefully change to the next mission in queue
/// </summary>
/// <param name="config"></param>
public class NextMissionCommand(ConfigFile config) : PermissionConfigurableCommand(config)
{
    public override string Name => "nextmission";
    public override string Description => "Ends the current mission and starts the next one in the rotation.";
    public override string Usage => "nextmission";
    public override PermissionLevel DefaultPermissionLevel => PermissionLevel.Moderator;


    public override bool Validate(Player player, string[] args) => true;

    public override bool Execute(Player player, string[] args)
    {
        MissionService.StartNextMission(player); 
        return true;
    }

    public override bool Execute(string[] args)
    {
        MissionService.StartNextMission(null); 
        return true;
    }
}
