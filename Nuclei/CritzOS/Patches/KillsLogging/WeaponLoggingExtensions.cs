using System.Collections.Generic;
using System.Linq;
using NuclearOption.Networking;
using Nuclei.CritzOS.Features;
using UnityEngine;

namespace Nuclei.CritzOS.Patches.KillsLogging;

/// <summary>
/// Unit extensions for weapon logging.
/// </summary>
public static class WeaponLoggingExtensions
{
    /// <summary>
    /// Records damage on a unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="lastDamagedBy"></param>
    /// <param name="damageAmount"></param>
    /// <param name="weaponName"></param>
    // ReSharper disable once ConvertToExtensionBlock
    public static void RecordDamage(
        this Unit unit,
        PersistentID lastDamagedBy,
        float damageAmount,
        string weaponName)
    {
        if (unit == null)
        {
            global::Nuclei.Nuclei.Logger?.LogError("Unit is null in recordDamage!");
            return;
        }
        unit.damageCredit ??= new Dictionary<PersistentID, float>();

        unit.damageCredit.TryGetValue(lastDamagedBy, out var originalDamageAmount);
        unit.damageCredit[lastDamagedBy] = originalDamageAmount + damageAmount;

        var state = global::Nuclei.Nuclei.WeaponStorage.Get(unit);
        var weaponCredit = state.WeaponCredit;

        if (!weaponCredit.TryGetValue(lastDamagedBy, out var existingDamageCredit))
        {
            existingDamageCredit = new Dictionary<string, float>
            {
                [weaponName] = damageAmount
            };
        }
        else
        {
            existingDamageCredit[weaponName] =
                existingDamageCredit.TryGetValue(weaponName, out var current)
                    ? current + damageAmount
                    : damageAmount;
        }

        weaponCredit[lastDamagedBy] = existingDamageCredit;
    }

    /// <summary>
    /// Unit.ReportKilled implementation that takes the weapon logging into account.
    /// </summary>
    public static void ReportKilled(this Unit unit)
    {
        var killerID = PersistentID.None;
        if (!UnitRegistry.TryGetPersistentUnit(unit.persistentID, out var killedUnit))
            return;
        // ReSharper disable once InconsistentNaming
        var killedHQ = killedUnit.GetHQ();
        var killerDamage = 0.0f;
        var totalReceivedDamage = 0.0f;
        var state = global::Nuclei.Nuclei.WeaponStorage.Get(unit);
        var weaponCredit = state.WeaponCredit;
        if (unit.damageCredit != null)

        {
            totalReceivedDamage += unit.damageCredit.Sum(keyValuePair => keyValuePair.Value);

            var damageDealerPlayers = new Dictionary<Player, float>();
            foreach (var receivedDamage in unit.damageCredit)
            {
                if (!UnitRegistry.TryGetPersistentUnit(receivedDamage.Key, out var damageDealerUnit)) continue;
                var dealtDamageProportion = receivedDamage.Value / totalReceivedDamage;
                if (dealtDamageProportion < 0.009999999776482582)
                    continue; // process only if dealt damage is significant (over 1%)
                if (receivedDamage.Value >= (double)killerDamage)
                {
                    // Update max damage dealer (killer)
                    killerDamage = receivedDamage.Value;
                    killerID = receivedDamage.Key;
                }

                // ReSharper disable once InconsistentNaming
                var damageDealerHQ = damageDealerUnit.GetHQ();
                if (killedHQ == damageDealerHQ || killedHQ == null) continue;
                var score = Mathf.Sqrt(killedUnit.definition.value) * dealtDamageProportion;
                var reward = score * damageDealerHQ.killReward;
                damageDealerHQ.AddScore(score);
                damageDealerHQ.AddFunds(reward * damageDealerHQ.playerTaxRate);
                if (damageDealerUnit.player == null) continue;
                // add player to dict of damage dealer players
                if (!damageDealerPlayers.ContainsKey(damageDealerUnit.player))
                    damageDealerPlayers.Add(damageDealerUnit.player, 0.0f);
                damageDealerPlayers[damageDealerUnit.player] += dealtDamageProportion;
            }

            foreach (var damageDealer in damageDealerPlayers)
                damageDealer.Key.HQ.ReportKillAction(damageDealer.Key, unit, damageDealer.Value);
        }

        ulong? killedSteamID;
        Aircraft? killedAircraft;
        if (killedUnit.unit is Aircraft killedAircraftUnit)
        {
            killedAircraft = killedAircraftUnit;
            killedSteamID = killedAircraft.Player?.SteamID;
        }
        else
        {
            killedAircraft = null;
            killedSteamID = killedUnit.player?.SteamID;
        }
        ulong? killerSteamID;
        Aircraft? killerAircraft;

        var killerIsUnit = UnitRegistry.TryGetUnit(killerID, out var killerUnit);
        UnitRegistry.TryGetPersistentUnit(killerID, out var killerPUnit);
        if (killerUnit is Aircraft killerAircraftUnit)
        {
            killerAircraft = killerAircraftUnit;
            killerSteamID = killerAircraft.Player?.SteamID;
        }
        else
        {
            killerAircraft = null;
            killerSteamID = killerPUnit?.player?.SteamID;
        }

        KeyValuePair<string, float>? killerWeapon;
        string killerWeaponName;
        if (!weaponCredit.TryGetValue(killerID, out var killerAircraftWeapons))
        {
            killerWeaponName = "";
        }
        else if (killerAircraftWeapons is null)
        {
            global::Nuclei.Nuclei.Logger?.LogError(
                "This should not happen. Something killed something else without recording any dealt damage");
            killerWeaponName = "";
        }
        else
        {
            killerWeapon = killerAircraftWeapons.FirstOrDefault();
            foreach (var kvp in killerAircraftWeapons.Where(kvp => kvp.Value > killerWeapon.Value.Value))
            {
                killerWeapon = kvp;
            }
            killerWeaponName = killerWeapon?.Key ?? "";
        }

        string killedName;
        if (killedSteamID != null && killedUnit.unitName.IndexOf('[') != -1)
        {
            var s = killedUnit.unitName;
            killedName = s.Substring(s.IndexOf('[') + 1, s.IndexOf(']') - (s.IndexOf('[') + 1));
        }
        else killedName = killedUnit.unitName;
        
        string? killerName;
        if (killerSteamID != null && killerPUnit?.unitName.IndexOf('[') != -1)
        {
            var s = killerPUnit?.unitName;
            killerName = s?.Substring(s.IndexOf('[') + 1, s.IndexOf(']') - (s.IndexOf('[') + 1));
        }
        else killerName = killerPUnit?.unitName;
        
        Nuclei.Logger?.LogDebug($"An {killedName} was killed by {killerName} with weapon {killerWeaponName}");

        if (killerAircraft != null &&
            killerAircraft.Player != null &&
            killerAircraft.NetworkHQ == killedUnit.unit.NetworkHQ &&
            totalReceivedDamage > 1.0)
        {
            // if player-anything teamkill
            Nuclei.Logger?.LogInfo($"{killerAircraft.unitName} (||{killerAircraft.Player.SteamID}||) killed friendly {killedUnit.unitName}! with {killerWeaponName}");

            // Prevent duplicate reports if player TK
            if (killedAircraft == null)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"{killerAircraft.unitName} (||{killerAircraft.Player.SteamID}||) killed friendly {killedUnit.unitName} with weapon {killerWeaponName}!");
                _ = CritzOSDB.LogAITeamkillAsync(killerAircraft.Player, killedUnit);
            }
        }
        
        if (killedAircraft is not null &&
            killedAircraft.Player != null &&
            totalReceivedDamage > 1.0 && killerIsUnit &&
            killerUnit!.NetworkHQ == killedAircraft.NetworkHQ &&
            killerAircraft is not null &&
            killerAircraft.Player != null)
        {
            // if player-player TEAMKILL
            var amount = killedAircraft.definition.value + killedAircraft.weaponManager.GetCurrentValue(true);
            killerAircraft.Player.AddScore(-Mathf.Sqrt(killedAircraft.definition.value));
            killerAircraft.Player.AddAllocation(-amount);
            killedAircraft.Player.AddAllocation(amount);
            
            ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}",
                $"{killerAircraft.unitName} (||{killerAircraft.Player.SteamID}||) teamkilled player {killedAircraft.unitName} with weapon {killerWeaponName}!");
            // TODO - LOG WEAPON NAME IN DB
            _ = CritzOSDB.LogPlayerTeamkillAsync(killerAircraft.Player, killedAircraft.Player);
            global::Nuclei.Nuclei.Logger?.LogInfo($"{killerAircraft.unitName} (||{killerAircraft.Player.SteamID}||) teamkilled player {killedAircraft.unitName} with weapon {killerWeaponName}!");
        }
        
        // ReSharper disable once RedundantCast
        var killedType = unit switch
        {
            Missile _ => KillType.Missile,
            Building _ => KillType.Building,
            Aircraft _ => KillType.Aircraft,
            Ship _ => KillType.Ship,
            _ => KillType.Vehicle
        };

        if (NetworkSceneSingleton<MessageManager>.i == null)
            return;
        NetworkSceneSingleton<MessageManager>.i.RpcKillMessage(killerID, unit.persistentID, killedType);
    }
}
