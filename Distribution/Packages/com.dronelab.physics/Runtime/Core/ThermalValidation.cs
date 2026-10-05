using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    internal static class ThermalValidation
    {
        public static IEnumerable<ValidationIssue> Check(DroneProfile p,JObject json)
        {
            if(!p.powerSystem.thermalEnabled) yield break;
            if(p.powerSystem.battery.mode!="Electrical")
                yield return new ValidationIssue("powerSystem.thermalEnabled","Thermal model requires Electrical mode for explicit motor, ESC and battery losses.");
            foreach(var issue in Node(p.powerSystem.battery.thermal,"powerSystem.battery.thermal")) yield return issue;
            for(int i=0;i<p.rotors.Length;i++)
            {
                string path="rotors["+i+"].motor.electrical"; var e=p.rotors[i].motor.electrical;
                if(e==null) continue; // PowerValidation already rejects missing electrical data.
                foreach(var issue in Node(e.thermal,path+".thermal")) yield return issue;
                foreach(var issue in Node(e.escThermal,path+".escThermal")) yield return issue;
                var raw=json["rotors"][i]["motor"]["electrical"];
                if(raw["resistanceReferenceTemperatureK"]==null || raw["resistanceTemperatureCoefficientPerK"]==null)
                    yield return new ValidationIssue(path,"Thermal mode requires explicit resistance reference temperature and coefficient (0 disables the temperature dependence).");
                if(1+e.resistanceTemperatureCoefficientPerK*(100-e.resistanceReferenceTemperatureK)<=0)
                    yield return new ValidationIssue(path+".resistanceTemperatureCoefficientPerK","Winding resistance must remain positive throughout ambient envelope 100..500 K.");
            }
        }
        private static IEnumerable<ValidationIssue> Node(ThermalProfile p,string path)
        {
            if(p==null) { yield return new ValidationIssue(path,"Explicit thermal node required; no silent defaults."); yield break; }
            if(p.cutoffTemperatureK<=p.derateStartTemperatureK)
                yield return new ValidationIssue(path,"cutoffTemperatureK must exceed derateStartTemperatureK.");
        }
    }
}
