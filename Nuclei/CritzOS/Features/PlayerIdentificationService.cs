using System.Collections.Generic;
using NuclearOption.Networking;
using Nuclei.Helpers;

namespace Nuclei.CritzOS.Features;

internal static class PlayerIdentificationService
{
    private static readonly Dictionary<ulong, int> _players  = new();
    private static readonly Stack<int> _ids =  new Stack<int>();
    private static bool _idsOk;

    private static void InitIDs()
    {
        Nuclei.Logger?.LogDebug("Initializing IDs");
        for (var i = Globals.DedicatedServerManagerInstance.Config.MaxPlayers; i > 0; i--)
        {
            _ids.Push(i);
        }
        _idsOk = true;
    }

    static PlayerIdentificationService()
    {
        InitIDs();
    }

    public static void AssignNewPlayer(Player player)
    {
        var steamID = player.SteamID;
        if (!_players.ContainsKey(player.SteamID))
        {
            _players.Add(steamID, _ids.Pop());
        }
        
        var newName = $"[{GetPlayerId(player)}] {player.PlayerName}";
        player.PlayerName = newName; 
    }

    public static void RemovePlayer(Player player)
    {
        var found = _players.TryGetValue(player.SteamID, out var id);
        if (!found) return;
        _ids.Push(id);
        _players.Remove(player.SteamID);
    }

    private static int GetPlayerId(Player player)
    {
        return _players[player.SteamID];
    }

    public static void GetPlayerById(int id, out ulong? player)
    {
        foreach (var keyValuePair in _players)
        {
            if (keyValuePair.Value == id)
            {
                player = keyValuePair.Key;
                return;
            }
        }

        player = null;
    }
}