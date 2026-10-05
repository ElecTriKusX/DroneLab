using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    public sealed class ProfileLoadResult
    {
        public readonly List<ValidationIssue> Issues = new List<ValidationIssue>();
        public DroneProfile Drone { get; internal set; }
        public EnvironmentProfile Environment { get; internal set; }
        public RuntimeDroneParameters Parameters { get; internal set; }
        public bool Success => Parameters != null && !Issues.Any(x => x.Severity == "Error");
    }

    public static class ProfileLoader
    {
        public static ProfileLoadResult Load(string droneJson,string environmentJson,string droneSchema,string environmentSchema)
        {
            var result=new ProfileLoadResult();
            try
            {
                var drone=Parse(droneJson); var env=Parse(environmentJson);
                result.Issues.AddRange(ContractValidator.Validate(drone,Parse(droneSchema)));
                result.Issues.AddRange(ContractValidator.Validate(env,Parse(environmentSchema))
                    .Select(x => new ValidationIssue("environment"+x.Path.Substring(1),x.Message,x.Severity)));
                if (result.Issues.Count != 0) return result;
                result.Drone=drone.ToObject<DroneProfile>(); result.Environment=env.ToObject<EnvironmentProfile>();
                CheckSemantics(result,drone,env);
                if (result.Issues.Any(x=>x.Severity == "Error")) return result;
                result.Parameters=new RuntimeDroneParameters(result.Drone,result.Environment);
                if (result.Parameters.ThrustToWeight <= 1)
                    result.Issues.Add(new ValidationIssue("rotors","Sum of maximum thrust is not above weight; hover may be impossible.","Warning"));
                // This is a necessary capacity bound, NOT a generic hover feasibility solver.
                if (result.Drone.massProperties.inertia.mode == "AutoBox")
                    result.Issues.Add(new ValidationIssue("massProperties.inertia","Uniform box inertia about the supplied COM is an approximation.","Warning"));
                foreach(var p in result.Drone.parameterProvenance)
                    if (p.sourceType == "Estimated" || p.sourceType == "Preset")
                        result.Issues.Add(new ValidationIssue(p.path,"Parameter source: "+p.sourceType+"; "+p.source,"Warning"));
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException || ex is OverflowException)
            { result.Issues.Add(new ValidationIssue("$","Cannot load profile: "+ex.Message)); }
            return result;
        }
        private static JObject Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new JsonException("Empty JSON.");
            using (var reader=new JsonTextReader(new StringReader(text)) { DateParseHandling=DateParseHandling.None, MaxDepth=64 })
            {
                var token=JObject.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new JsonException("Unexpected content after the JSON object.");
                return token;
            }
        }
        private static void CheckSemantics(ProfileLoadResult r,JObject json,JObject environmentJson)
        {
            var p=r.Drone; var e=r.Environment;
            Action<string,string> error=(path,message)=>r.Issues.Add(new ValidationIssue(path,message));
            Action<string> unsupported=path=>error(path,"This mode is reserved in contract 1.0.0 but not implemented by this runtime.");
            void Required(JToken parent,string key,string path) { if (parent[key] == null) error(path+"."+key,"Required for the selected model."); }
            void PositiveVector(double[] a,string path)
            { if (a == null || a.Any(x=>x<=0)) error(path,"Three positive components are required."); }
            foreach(var n in json.Descendants().OfType<JValue>().Where(x=>x.Type==JTokenType.Float || x.Type==JTokenType.Integer))
                if (Math.Abs((double)n)>1e12) error(n.Path,"Value exceeds runtime numerical safety limit (1e12 SI).");
            PositiveVector(p.massProperties.dimensionsM,"massProperties.dimensionsM");
            var inertia=p.massProperties.inertia;
            if (inertia.mode == "ManualPrincipal")
            {
                PositiveVector(inertia.principalMomentsKgM2,"massProperties.inertia.principalMomentsKgM2");
                if (inertia.principalMomentsKgM2 != null)
                {
                    var a=inertia.principalMomentsKgM2;
                    if (2*a.Max()>a.Sum()+1e-10) error("massProperties.inertia","Principal moments must satisfy triangle inequalities.");
                }
                if (inertia.principalAxesRotationXyzw == null || Math.Abs(inertia.principalAxesRotationXyzw.Sum(x=>x*x)-1)>1e-5)
                    error("massProperties.inertia.principalAxesRotationXyzw","A unit quaternion is required.");
            }
            var modules=p.physicsConfiguration.modules;
            foreach(var field in typeof(PhysicsModulesProfile).GetFields())
                if (field.Name != "motorResponse" && field.Name != "bodyDrag" && field.Name != "windInteraction" && field.Name != "groundEffect" && field.Name != "rotorAerodynamics" && field.Name != "bladeFlapping" && field.Name != "inducedDrag" && field.Name != "batteryDischarge" && field.Name != "batteryVoltageSag" && field.Name != "motorElectrical" && field.Name != "gyroscopicRotorEffects" && (bool)field.GetValue(modules))
                    unsupported("physicsConfiguration.modules."+field.Name);
            if(modules.groundEffect)
            {
                if(p.groundEffect==null) error("groundEffect","Ground effect settings are required when the module is enabled.");
                else r.Issues.Add(new ValidationIssue("groundEffect","Empirical thrust-only gain: base performance must be measured out of ground effect; torque/current are unchanged.","Warning"));
            }
            var ids=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<p.rotors.Length;i++)
            {
                var rotor=p.rotors[i]; string path="rotors["+i+"]";
                if (!ids.Add(rotor.rotorId)) error(path+".rotorId","Duplicate rotor ID.");
                if (Math.Abs(DVector3.From(rotor.geometry.thrustAxisLocal).Length-1)>1e-5)
                    error(path+".geometry.thrustAxisLocal","Axis must be normalized and nonzero.");
                var settings=rotor.advancedAerodynamics;
                bool lift=modules.rotorAerodynamics && settings!=null && settings.translationalLiftCoefficientKgPerM>0;
                if((modules.rotorAerodynamics || modules.bladeFlapping || modules.inducedDrag) && settings==null)
                    error(path+".advancedAerodynamics","Aerodynamic settings are required for every rotor when its module is enabled.");
                if(settings!=null && (modules.bladeFlapping || modules.inducedDrag || lift))
                {
                    var sj=json["rotors"][i]["advancedAerodynamics"]; var sp=path+".advancedAerodynamics";
                    Required(sj,"referenceAirDensityKgM3",sp); Required(sj,"maxAirSpeedMps",sp);
                    if(modules.bladeFlapping) { Required(sj,"bladeFlappingCoefficient",sp); Required(sj,"maxFlappingMomentRatio",sp); }
                    if(modules.inducedDrag || lift) Required(sj,"maxThrustCorrectionFraction",sp);
                    if(modules.inducedDrag) Required(sj,"inducedDragCoefficient",sp);
                    if(rotor.performance.model=="PerformanceMap" && modules.inducedDrag)
                        error(sp,"Axial inflow is already represented by the RPM/J map; inducedDrag must be disabled.");
                    if(rotor.performance.model=="PerformanceMap" && lift)
                        error(sp,"This empirical lift bundle requires static base performance; do not stack it on an RPM/J map.");
                    r.Issues.Add(new ValidationIssue(sp,"Bounded empirical airflow corrections: coefficients require their own source and speed envelope; Q/current stay on the base performance model (not a coupled energy solver).","Warning"));
                }
                var m=rotor.motor;
                if (m.minRpm>m.idleRpm || m.idleRpm>=m.maxRpm) error(path+".motor","Require 0 <= minRpm <= idleRpm < maxRpm.");
                var perf=rotor.performance; var perfJson=json["rotors"][i]["performance"];
                if (perf.model == "OmegaSquared")
                {
                    Required(perfJson,"kThrustNPerRadPerSecSquared",path+".performance");
                    Required(perfJson,"kTorqueNmPerRadPerSecSquared",path+".performance");
                    Required(perfJson,"referenceAirDensityKgM3",path+".performance");
                    if (Math.Abs(perf.referenceAirDensityKgM3-e.airDensityKgM3)>1e-6)
                        error(path+".performance.referenceAirDensityKgM3","Measured kT/kQ require their reference density. Use CtCq for density scaling.");
                }
                else if (perf.model == "CtCq") { Required(perfJson,"ct",path+".performance"); Required(perfJson,"cq",path+".performance"); }
                else if(perf.model=="RpmTable" || perf.model=="PerformanceMap")
                {
                    Required(perfJson,perf.model=="RpmTable" ? "rpmTable" : "performanceMap",path+".performance");
                    foreach(var issue in PerformanceValidation.Check(perf)) error(path+".performance",issue);
                    if(perf.model=="RpmTable")
                    {
                        Required(perfJson,"referenceAirDensityKgM3",path+".performance");
                        if(Math.Abs(perf.referenceAirDensityKgM3-e.airDensityKgM3)>1e-6)
                            error(path+".performance.referenceAirDensityKgM3","Measured thrust/torque/current tables require their reference density; no implicit scaling.");
                        if(perf.rpmTable!=null && perf.outOfRangePolicy=="Reject" && perf.rpmTable.Max(x=>x.rpm)<m.maxRpm-1e-6)
                            error(path+".performance.rpmTable","Reject table must cover motor maxRpm.");
                    }
                    else if(perf.performanceMap!=null && perf.outOfRangePolicy=="Reject")
                    {
                        if(m.maxRpm<perf.performanceMap.Min(x=>x.rpm)-1e-6 || m.maxRpm>perf.performanceMap.Max(x=>x.rpm)+1e-6)
                            error(path+".performance.performanceMap","Reject map must cover motor maxRpm.");
                        r.Issues.Add(new ValidationIssue(path+".performance","Reject map cannot cover RPM=0 startup: any positive RPM below the first row rejects the simulation step. Clamp is recommended for motor response.","Warning"));
                    }
                }
                else unsupported(path+".performance.model");
            }
            if (modules.bodyDrag)
            {
                var b=p.bodyAerodynamics;
                var bj=json["bodyAerodynamics"];
                if (b.model == "AxisApproximation")
                {
                    PositiveVector(b.referenceAreaM2,"bodyAerodynamics.referenceAreaM2");
                    if (b.dragCd == null || b.dragCd.Any(x=>x<0)) error("bodyAerodynamics.dragCd","Nonnegative drag coefficients are required.");
                }
                else if(b.model=="ProjectedArea")
                {
                    Required(bj,"dragCoefficient","bodyAerodynamics"); Required(bj,"projectedArea","bodyAerodynamics");
                    var area=b.projectedArea;
                    if(area!=null && area.mode=="AxisApproximation")
                    {
                        if(area.referenceAreaM2==null || area.referenceAreaM2.Any(x=>x<0) || area.referenceAreaM2.Sum()<=0)
                            error("bodyAerodynamics.projectedArea.referenceAreaM2","Nonnegative axis areas, with at least one positive area, are required.");
                    }
                    else if(area!=null)
                    {
                        if(area.samples==null || area.samples.Length==0 || area.samples.Length>256)
                            error("bodyAerodynamics.projectedArea.samples","Require 1..256 directional samples.");
                        else for(int i=0;i<area.samples.Length;i++)
                        {
                            var direction=DVector3.From(area.samples[i].directionLocal);
                            if(Math.Abs(direction.Length-1)>1e-5) error("bodyAerodynamics.projectedArea.samples["+i+"].directionLocal","A unit direction is required.");
                            for(int j=0;j<i;j++) if(Math.Abs(DVector3.Dot(direction,DVector3.From(area.samples[j].directionLocal)))>1-1e-10)
                                error("bodyAerodynamics.projectedArea.samples["+i+"]","Duplicate projection axis, including opposite directions.");
                        }
                    }
                }
                else if(b.model=="Surfaces")
                {
                    if(b.surfaces==null || b.surfaces.Length==0 || b.surfaces.Length>256)
                        error("bodyAerodynamics.surfaces","Require 1..256 surfaces.");
                    else
                    {
                        var surfaceIds=new HashSet<string>(StringComparer.Ordinal);
                        for(int i=0;i<b.surfaces.Length;i++)
                        {
                            var surface=b.surfaces[i]; string path="bodyAerodynamics.surfaces["+i+"]";
                            if(!surfaceIds.Add(surface.surfaceId)) error(path+".surfaceId","Duplicate surface ID.");
                            if(Math.Abs(DVector3.From(surface.normalLocal).Length-1)>1e-5) error(path+".normalLocal","A unit normal is required.");
                        }
                    }
                }
            }
            foreach(var issue in PowerValidation.Check(p,json)) error(issue.Path,issue.Message);
            foreach(var issue in ThermalValidation.Check(p,json)) error(issue.Path,issue.Message);
            if(p.powerSystem.thermalEnabled)
                r.Issues.Add(new ValidationIssue("powerSystem.thermalEnabled","Effective lumped motor/ESC/battery temperatures with estimated cooling and continuous current derating. No cell chemistry/cold-capacity model, thermal runaway, internal gradients or active braking. Coefficients require sources.","Warning"));
            if(p.powerSystem.battery.mode!="None")
                r.Issues.Add(new ValidationIssue("powerSystem","Estimated DC/BLDC equivalent currents; measured CSV current is separate. No regeneration/inductance. RotorInertia accounts spin energy with midpoint integration; legacy FirstOrder does not. Electrical derives losses from Kv/R/I0; constant efficiency is Simple only.","Warning"));
            foreach(var issue in EnvironmentValidation.Check(e,environmentJson,p)) error(issue.Path,issue.Message);
            if(e.windMode=="DrydenFrozen") r.Issues.Add(new ValidationIssue("environment.dryden","Finite-band frozen-line Dryden spectrum synthesis, not full 3D turbulence or MIL angular gust gradients. Check retained variance and timestep/spatial sampling; parameters require their own source.","Warning"));
            if(e.windMode=="CustomField") r.Issues.Add(new ValidationIssue("environment.windMode","CustomField needs an explicit IWindProvider in the Unity adapter; disabled windInteraction ignores it.","Warning"));
        }
    }
}
