using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    internal static class PowerValidation
    {
        public static IEnumerable<ValidationIssue> Check(DroneProfile p,JObject json)
        {
            var b=p.powerSystem.battery; var modules=p.physicsConfiguration.modules;
            const string path="powerSystem.battery";
            if(b.mode=="None")
            {
                if(modules.batteryDischarge || modules.batteryVoltageSag || modules.motorElectrical)
                    yield return new ValidationIssue(path,"Battery modules require Simple or Electrical battery mode.");
                yield break;
            }
            if(modules.motorElectrical!=(b.mode=="Electrical"))
                yield return new ValidationIssue("physicsConfiguration.modules.motorElectrical","Enable exactly for Electrical battery mode; disable for Simple.");
            var batteryJson=json["powerSystem"]["battery"];
            foreach(string key in new[]{"cellCount","nominalVoltageV","capacityAh","initialSoc","internalResistanceOhm","maxDischargeCurrentA","ocvCurve"})
                if(batteryJson[key]==null) yield return new ValidationIssue(path+"."+key,"Required for an enabled battery.");
            if(b.ocvCurve!=null)
            {
                if(b.ocvCurve.Length<2 || b.ocvCurve.Length>256 || b.ocvCurve[0].soc!=0 || b.ocvCurve[b.ocvCurve.Length-1].soc!=1)
                    yield return new ValidationIssue(path+".ocvCurve","Require 2..256 sorted points covering SOC 0 and 1.");
                for(int i=1;i<b.ocvCurve.Length;i++)
                    if(b.ocvCurve[i].soc<=b.ocvCurve[i-1].soc || b.ocvCurve[i].voltageV<b.ocvCurve[i-1].voltageV)
                        yield return new ValidationIssue(path+".ocvCurve","SOC must strictly increase and pack voltage must not decrease.");
            }
            for(int i=0;i<p.rotors.Length;i++)
            {
                var rotor=p.rotors[i]; var e=rotor.motor.electrical; string motorPath="rotors["+i+"].motor.electrical";
                if(e==null) { yield return new ValidationIssue(motorPath,"Motor power settings required for every rotor with battery enabled."); continue; }
                if(e.noLoadCurrentA>=e.maxCurrentA || e.noLoadCurrentA>=e.escMaxCurrentA)
                    yield return new ValidationIssue(motorPath,"No-load current must be below both current limits.");
                if(e.efficiencyCurve!=null && e.efficiencyCurve.Length>0)
                    yield return new ValidationIssue(motorPath+".efficiencyCurve","Load-dependent efficiencies await a validated governor; use constant motorEfficiency for now.");
                var perf=rotor.performance;
                if(perf.model=="PerformanceMap")
                    yield return new ValidationIssue("rotors["+i+"].performance","Battery governor currently supports static OmegaSquared, CtCq and RpmTable; coupled RPM/J power-map solving is pending.");
                if(perf.model=="RpmTable" && perf.rpmTable!=null)
                {
                    var rows=perf.rpmTable.OrderBy(x=>x.rpm).ToArray();
                    for(int k=1;k<rows.Length;k++) if(rows[k].torqueNm<rows[k-1].torqueNm)
                        yield return new ValidationIssue("rotors["+i+"].performance.rpmTable","Battery governor requires nondecreasing torque over sorted RPM.");
                }
            }
        }
    }
}
