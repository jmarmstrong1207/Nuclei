using System.Linq;
using Nuclei.CritzOS.Enums;
using UnityEngine;

namespace Nuclei.CritzOS.Features.IPC.Packets;

/// <summary>
/// Return packet for the response to a command.
/// </summary>
public class DeletePacket: CommunicationPacket
{
    /// <inheritdoc />
    public override PacketType type { get; set; } = PacketType.Delete;

    public float globalPosX { get; set; }
    public float globalPosY { get; set; }
    public float globalPosZ { get; set; }
    

    /// <inheritdoc />
    public override CommunicationPacket? Process()
    {
        var destination = new GlobalPosition(globalPosX, globalPosY, globalPosZ);
        
        DeleteNearestUnit(destination);
        return null;
        
        /*
        if (!Physics.Raycast(ray, out RaycastHit hit, 100000f))
        {
            Nuclei.Logger.LogWarning("WARNING: Could not find selected target to delete");
            return null;
        }

        GameObject hitObject = hit.collider != null ? hit.collider.gameObject : null;
        if (hitObject == null)
        {
            Nuclei.Logger.LogInfo("Horus: target is not deletable (no object).");
            return null;
        }

        // Walk UP only to the nearest gameplay unit root. Terrain/roads/static map geometry
        // have no Unit component, so this returns null and the object is left untouched.
        Unit unitRoot = FindUnitRoot(hitObject);
        if (unitRoot == null)
        {
            Nuclei.Logger.LogInfo($"Horus: target is not deletable (map/environment object '{hitObject.name}').");
            return null;
        }
        */

    }
    private static void DeleteNearestUnit(GlobalPosition pos)
    {
        UnitRegistry.TryGetNearestUnit(pos, out var go, 100f);
        string unitName = go.unitName;

        Mirage.NetworkServer.Destroy(go);
        Nuclei.Logger.LogInfo($"Deleted unit {unitName}");
        
        if (!ZeusLogBuffer.deletedBuffer.ContainsKey(unitName))
            ZeusLogBuffer.deletedBuffer.Add(unitName, 1);
        else
            ZeusLogBuffer.deletedBuffer[unitName] += 1;
    }
    
    /// <summary>Finds the nearest gameplay unit root by walking UP the hierarchy only.</summary>
    private static Unit FindUnitRoot(GameObject target)
    {
        return target == null ? null : target.GetComponentInParent<Unit>();
    }
}