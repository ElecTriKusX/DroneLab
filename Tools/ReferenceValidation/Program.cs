using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DroneLab.Physics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class ReferenceValidation
{
    public static int Main(string[] args)
    {
        try
        {
            string folder=args.Length>0 ? args[0] : Path.Combine(AppContext.BaseDirectory,"Profiles");
            string Read(string name)=>File.ReadAllText(Path.Combine(folder,name+".json"));
            var benchmarks=JObject.Parse(Read("Benchmarks/reference-benchmarks")); var reports=new JArray();
            foreach(JObject data in benchmarks["datasets"])
            {
                string name=(string)data["profile"]; var loaded=ProfileLoader.Load(Read(name),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
                if(!loaded.Success) throw new ArgumentException(string.Join("\n",loaded.Issues));
                var p=loaded.Parameters; var r=p.Rotors[0]; new QuadAllocator(p);
                var report=new JObject { ["dataset"]=data["id"],["kind"]=data["kind"],["profile"]=name,["sourceUrl"]=data["sourceUrl"],
                    ["massKg"]=p.Mass,["staticThrustToWeight"]=p.ThrustToWeight,["sourceWarnings"]=new JArray(loaded.Issues.Select(i=>i.ToString())) };
                var errorsT=new List<double>(); var errorsQ=new List<double>(); var relative=new List<double>(); var comparisons=new JArray();
                if(data["rows"]!=null)
                {
                    foreach(JObject row in data["rows"].Where(x=>(string)x["role"]=="holdout"))
                    {
                        double rpm=(double?)(row["rpm"] ?? data["rpm"]) ?? 0,j=(double?)row["advanceRatio"] ?? 0;
                        double actualT,actualQ=0; bool torqueKnown=row["cp"]!=null;
                        if(row["totalThrustGram"]!=null) actualT=(double)row["totalThrustGram"]*.00980665/4;
                        else { actualT=(double)row["ct"]*1.225*Math.Pow(rpm/60,2)*Math.Pow(r.Diameter,4); actualQ=(double)row["cp"]/(2*Math.PI)*1.225*Math.Pow(rpm/60,2)*Math.Pow(r.Diameter,5); }
                        var predicted=r.Performance.Evaluate(PhysicsMath.RpmToOmega(rpm),j*rpm/60*r.Diameter);
                        errorsT.Add(predicted.Thrust-actualT); if(torqueKnown) errorsQ.Add(predicted.Torque-actualQ);
                        if(actualT>0) relative.Add(Math.Abs(predicted.Thrust-actualT)/actualT);
                        comparisons.Add(new JObject { ["rpm"]=rpm,["advanceRatio"]=j,["referenceThrustN"]=actualT,["predictedThrustN"]=predicted.Thrust,
                            ["thrustErrorN"]=predicted.Thrust-actualT,["referenceTorqueNm"]=torqueKnown ? (JToken)actualQ : JValue.CreateNull(),
                            ["predictedTorqueNm"]=predicted.Torque,["torqueReferenceKind"]=torqueKnown ? "DerivedFromMeasuredCpWithAssumedDensity" : "UnvalidatedProxy" });
                    }
                    report["comparisonScope"]=data["kind"].ToString()=="MeasuredStatic" ? "Independent withheld measured total thrust / 4; torque proxy and electrical current NOT validated." : "Independent withheld CT/CP coefficients converted to N/Nm using assumed rho=1.225; not direct full-aircraft force/flight validation.";
                    report["holdoutCount"]=errorsT.Count; report["thrustRmseN"]=Math.Sqrt(errorsT.Average(x=>x*x));
                    report["maxAbsoluteThrustErrorN"]=errorsT.Max(x=>Math.Abs(x));
                    report["meanPositiveThrustRelativeError"]=relative.Average(); report["maxPositiveThrustRelativeError"]=relative.Max();
                    if(errorsQ.Count>0) report["torqueRmseNm"]=Math.Sqrt(errorsQ.Average(x=>x*x));
                    report["comparisons"]=comparisons;
                }
                else if((string)data["kind"]=="PublishedFit")
                {
                    double sigma=(double)data["normalizationOmegaRadS"];
                    double Eval(JToken a,double x)=>((double)a[0]*x+(double)a[1])*x*x+(double)a[2]*x+(double)a[3];
                    for(double omega=275;omega<2900;omega+=50)
                    {
                        var predicted=r.Performance.Evaluate(omega); double t=Eval(data["thrustCoefficients"],omega/sigma),q=Eval(data["torqueCoefficients"],omega/sigma);
                        errorsT.Add(predicted.Thrust-t); errorsQ.Add(predicted.Torque-q);
                    }
                    report["comparisonScope"]="Representation of published identified fit at withheld midpoints; NOT independent experimental accuracy.";
                    report["sampleCount"]=errorsT.Count; report["maxAbsoluteThrustRepresentationErrorN"]=errorsT.Max(x=>Math.Abs(x));
                    report["maxAbsoluteTorqueRepresentationErrorNm"]=errorsQ.Max(x=>Math.Abs(x));
                }
                else
                {
                    double roll=(double)data["rollMomentNm"]/p.Inertia.X,yaw=(double)data["yawMomentNm"]/p.Inertia.Y;
                    report["comparisonScope"]="Published identified parameters / rounded analytical saturation reference, NOT raw experimental trajectories.";
                    report["rollAccelerationRadS2"]=roll; report["paperRollAccelerationRadS2"]=data["rollAccelerationRadS2"];
                    report["yawAccelerationRadS2"]=yaw; report["paperYawAccelerationRadS2"]=data["yawAccelerationRadS2"];
                }
                reports.Add(report);
            }
            var result=new JObject { ["benchmarkFormatVersion"]=benchmarks["formatVersion"],["csvFormatVersion"]=FlightCsvWriter.FormatVersion,["reports"]=reports };
            string text=result.ToString(Formatting.Indented)+"\n";
            if(args.Length>1) File.WriteAllText(args[1],text); else Console.Write(text);
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
