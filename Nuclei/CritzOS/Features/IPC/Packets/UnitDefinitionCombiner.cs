using System.Collections.Generic;
using System.Linq;

namespace Nuclei.CritzOS.Features.IPC.Packets;

public static class UnitDefinitionCombiner
{
    public static List<UnitDefinition> CombinedList = [];

    static UnitDefinitionCombiner()
    {
        CombinedList.AddRange(Encyclopedia.i.aircraft.Cast<UnitDefinition>().ToList());
        CombinedList.AddRange(Encyclopedia.i.vehicles.Cast<UnitDefinition>().ToList());
        CombinedList.AddRange(Encyclopedia.i.ships.Cast<UnitDefinition>().ToList()); 
        CombinedList.AddRange(Encyclopedia.i.buildings.Cast<UnitDefinition>().ToList()); 
        CombinedList.AddRange(Encyclopedia.i.scenery.Cast<UnitDefinition>().ToList());
    }
    
}