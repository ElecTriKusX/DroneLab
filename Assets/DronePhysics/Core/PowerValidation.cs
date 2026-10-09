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
            bool inertial=p.rotors.Any(r=>r.motor.dynamicsModel=="RotorInertia");
            if(inertial)
            {
                if(b.mode!="Electrical" || !modules.motorResponse)
                    yield return new ValidationIssue(path,"RotorInertia requires Electrical battery mode and motorResponse.");
                for(int i=0;i<p.rotors.Length;i++)
                    if(p.rotors[i].motor.dynamicsModel!="RotorInertia" || p.rotors[i].motor.rotatingInertiaKgM2<=0)
                        yield return new ValidationIssue("rotors["+i+"].motor","All rotors must select RotorInertia with positive rotatingInertiaKgM2; mixed dynamic models are unsupported.");
                var inertia=p.massProperties.inertia;
                var box=PhysicsMath.BoxInertia(p.massProperties.massKg,DVector3.From(p.massProperties.dimensionsM));
                double minimum=inertia.mode=="AutoBox" ? System.Math.Min(box.X,System.Math.Min(box.Y,box.Z))
                    : inertia.principalMomentsKgM2?.Min() ?? double.PositiveInfinity;
                if(p.rotors.Sum(r=>r.motor.rotatingInertiaKgM2)>.05*minimum)
                    yield return new ValidationIssue("rotors.motor.rotatingInertiaKgM2","Spin inertia exceeds the weak-coupling envelope: sum Jr must be <=5% of the smallest locked-body principal moment. Full body/shaft acceleration coupling is outside this model.");
            }
            if(modules.gyroscopicRotorEffects && !inertial)
                yield return new ValidationIssue("physicsConfiguration.modules.gyroscopicRotorEffects","Gyroscopic rotor effects require RotorInertia dynamics with spin inertia.");
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
                {
                    var issue=MapIssue(perf);
                    if(issue!=null) yield return new ValidationIssue("rotors["+i+"].performance",issue);
                }
                if(perf.model=="RpmTable" && perf.rpmTable!=null)
                {
                    var rows=perf.rpmTable.OrderBy(x=>x.rpm).ToArray();
                    for(int k=1;k<rows.Length;k++) if(rows[k].torqueNm<rows[k-1].torqueNm)
                        yield return new ValidationIssue("rotors["+i+"].performance.rpmTable","Battery governor requires nondecreasing torque over sorted RPM.");
                }
            }
        }
        // Q(omega,Vaxial) must be nonnegative and nondecreasing, including clamped grid edges.
        // For bilinear Cq, all relevant derivative minima occur at cell corners.
        internal static string MapIssue(RotorPerformanceProfile p)
        {
            if(p.outOfRangePolicy!="Clamp") return "Battery maps require Clamp for startup and solver trial RPM/J values; Reject is unsupported.";
            if(PerformanceValidation.Check(p).Any()) return "Invalid RPM/J map for battery load solving.";
            if(p.performanceMap.Any(x=>x.cq<0)) return "Battery maps require nonnegative Cq; windmilling/regeneration is unsupported.";
            var rpm=p.performanceMap.Select(x=>x.rpm).Distinct().OrderBy(x=>x).ToArray();
            var j=p.performanceMap.Select(x=>x.advanceRatio).Distinct().OrderBy(x=>x).ToArray();
            var grid=p.performanceMap.ToDictionary(x=>System.Tuple.Create(x.rpm,x.advanceRatio),x=>x.cq);
            for(int r=0;r<System.Math.Max(1,rpm.Length-1);r++) for(int k=0;k<j.Length-1;k++)
            {
                int r1=System.Math.Min(r+1,rpm.Length-1);
                for(int u=0;u<2;u++) for(int v=0;v<2;v++)
                {
                    double rr=rpm[u==0 ? r:r1],jj=j[k+v],q=grid[System.Tuple.Create(rr,jj)];
                    double dr=r1==r ? 0 : (grid[System.Tuple.Create(rpm[r1],jj)]-grid[System.Tuple.Create(rpm[r],jj)])/(rpm[r1]-rpm[r]);
                    double dj=(grid[System.Tuple.Create(rr,j[k+1])]-grid[System.Tuple.Create(rr,j[k])])/(j[k+1]-j[k]);
                    if(2*q+rr*dr<-1e-12 || 2*q-jj*dj<-1e-12 || 2*q+rr*dr-jj*dj<-1e-12)
                        return "Battery map torque must be nondecreasing in RPM at fixed axial flow, including Clamp boundaries; supplied Cq gradients violate this envelope.";
                }
            }
            return null;
        }
    }
}
