using System.Linq;
using Nuclei.CritzOS.Enums;
using UnityEngine;

namespace Nuclei.CritzOS.Features.IPC.Packets;

/// <summary>
/// Return packet for the response to a command.
/// </summary>
public class SpawnPacket: CommunicationPacket
{
    /// <inheritdoc />
    public override PacketType type { get; set; } = PacketType.Spawn;

    public string unitName { get; set; } 
    
    public float globalPosX { get; set; }
    public float globalPosY { get; set; }
    public float globalPosZ { get; set; }
    public float rotationX { get; set; }
    public float rotationY { get; set; } 
    public float rotationZ { get; set; }
    public float rotationW { get; set; }

    public string factionName { get; set; } 

    public string uniqueName { get; set; } = "";

    /// <inheritdoc />
    public override CommunicationPacket? Process()
    {
        
        foreach (var x in UnitDefinitionCombiner.CombinedList)
        {
            if (x.unitName == unitName)
            {
                Nuclei.Logger.LogInfo($"UNIT NAME FOUND");
                foreach (var f in FactionRegistry.factions)
                {
                    if (f.factionName == factionName)
                    {
                        Nuclei.Logger.LogInfo($"FACTION NAME FOUND");
                        var hq = FactionRegistry.HQFromFaction(f);
                        var globalPos = new GlobalPosition(globalPosX, globalPosY, globalPosZ);
                        var q = new Quaternion(rotationX, rotationY, rotationZ, rotationW);
                        Spawner.i.SpawnFromUnitDefinitionInEditor(x, globalPos, q, hq, uniqueName);
                        Nuclei.Logger?.LogInfo($"HorusMod: Spawned {x.unitName} at {globalPos}");
                        
                        if (!ZeusLogBuffer.spawnedBuffer.ContainsKey(unitName))
                            ZeusLogBuffer.spawnedBuffer.Add(unitName, 1);
                        else
                            ZeusLogBuffer.spawnedBuffer[unitName] += 1;
                    }
                }
                
            }
        }
        return null;
    }
}