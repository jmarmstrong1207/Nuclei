using System;
using Nuclei.CritzOS.Enums;
using Nuclei.CritzOS.Features.IPC.Packets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nuclei.CritzOS.Features.IPC;

/// <summary>
/// Json converter for handling packets.
/// </summary>
public class PacketTypeConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        
        return typeof(CommunicationPacket).IsAssignableFrom(objectType);
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var type = jo["type"]!.ToObject<PacketType>();
        CommunicationPacket? packet;

        switch (type)
        {
            case PacketType.Spawn:
                packet = new SpawnPacket();
                break;
            case PacketType.Delete:
                packet = new DeletePacket();
                break;
            default:
                Nuclei.Logger.LogError($"Unknown packet type {type}");
                throw new ArgumentOutOfRangeException();
        }
        
        serializer.Populate(jo.CreateReader(), packet);
        return packet;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer,
        object? value,
        JsonSerializer serializer)
    {
        var packet = (CommunicationPacket)value!;
        writer.WriteStartObject();
        
        writer.WritePropertyName("type");
        serializer.Serialize(writer, packet.type.ToString().ToLower());
        
        switch (packet)
        {
            /* example
            case LinkPacket log:
                writer.WritePropertyName("steamID");
                serializer.Serialize(writer, log.SteamID);
                writer.WritePropertyName("oneTimeCode");
                serializer.Serialize(writer, log.OneTimeCode);
                break;
                */
        }

        writer.WriteEndObject();
    }    
}