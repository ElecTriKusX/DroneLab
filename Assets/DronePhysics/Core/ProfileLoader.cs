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
                CheckSemantics(result,drone);
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
        private static void CheckSemantics(ProfileLoadResult r,JObject json)
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
                if (field.Name != "motorResponse" && field.Name != "bodyDrag" && field.Name != "windInteraction" && (bool)field.GetValue(modules))
                    unsupported("physicsConfiguration.modules."+field.Name);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<p.rotors.Length;i++)
            {
                var rotor=p.rotors[i]; string path="rotors["+i+"]";
                if (!ids.Add(rotor.rotorId)) error(path+".rotorId","Duplicate rotor ID.");
                if (Math.Abs(DVector3.From(rotor.geometry.thrustAxisLocal).Length-1)>1e-5)
                    error(path+".geometry.thrustAxisLocal","Axis must be normalized and nonzero.");
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
            if (p.powerSystem.battery.mode != "None") unsupported("powerSystem.battery.mode");
            if (e.airDensityMode != "Constant") unsupported("environment.airDensityMode");
            if (e.windMode != "None" && e.windMode != "Constant") unsupported("environment.windMode");
            if (e.gustEnabled) unsupported("environment.gustEnabled");
        }
    }
}
