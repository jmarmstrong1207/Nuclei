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

    public float originX { get; set; }
    public float originY { get; set; }
    public float originZ { get; set; }
    public float destinationX { get; set; }
    public float destinationY { get; set; }
    public float destinationZ { get; set; }
    

    /// <inheritdoc />
    public override CommunicationPacket? Process()
    {
        var origin = new Vector3(originX, originY, originZ);
        var destination = new Vector3(destinationX, destinationY, destinationZ);
        var ray = new Ray(origin, destination);
        
        if (!Physics.Raycast(ray, out RaycastHit hit, 100000f))
        {
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

        DeleteUnit(unitRoot);
        return null;
    }
    private static void DeleteUnit(Unit unit)
    {
        if (unit == null) return;
        GameObject go = unit.gameObject;
        string unitName = unit.unitName;

        Mirage.NetworkServer.Destroy(go);
        Nuclei.Logger.LogInfo($"Deleted unit {unitName}");
    }
    
    /// <summary>Finds the nearest gameplay unit root by walking UP the hierarchy only.</summary>
    private static Unit FindUnitRoot(GameObject target)
    {
        return target == null ? null : target.GetComponentInParent<Unit>();
    }
}