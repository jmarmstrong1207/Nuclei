using System.Text.Json.Serialization;
using Nuclei.CritzOS.Enums;

namespace Nuclei.CritzOS.Features.IPC.Packets;

/// <summary>
/// Abstract class to reporesent a Communication Packet
/// </summary>
[JsonConverter(typeof(PacketTypeConverter))]
public abstract class CommunicationPacket
{
    /// <summary>
    ///     Type of the packet.
    /// </summary>
    public virtual PacketType type { get; set; }

    /// <summary>
    /// Applies it's processing to the packet.
    /// </summary>
    /// <returns> The response to send back to the peer process. </returns>
    public abstract CommunicationPacket? Process();
}