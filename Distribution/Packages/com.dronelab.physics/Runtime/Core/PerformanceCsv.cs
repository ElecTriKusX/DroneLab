using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DroneLab.Physics
{
    // Strict numeric CSV in SI; deliberately no guessed unit conversion or vendor column aliases.
    public static class PerformanceCsv
    {
        public static RotorPerformanceProfile Parse(string text,string model,string policy,double referenceDensity=1.225)
        {
            if(model!="RpmTable" && model!="PerformanceMap") throw new ArgumentException("CSV model must be RpmTable or PerformanceMap.");
            var result=new RotorPerformanceProfile { model=model,outOfRangePolicy=policy,referenceAirDensityKgM3=referenceDensity };
            var table=new List<RpmPerformancePoint>(); var map=new List<PerformanceMapPoint>();
            using(var reader=new StringReader(text ?? ""))
            {
                string header=reader.ReadLine()?.TrimStart('\uFEFF');
                if(header==null) throw new ArgumentException("Empty CSV.");
                var columns=header.Split(',').Select(x=>x.Trim()).ToArray();
                string[] required=model=="RpmTable" ? new[]{"rpm","thrustN","torqueNm"} : new[]{"rpm","advanceRatio","ct","cq"};
                string optional=model=="RpmTable" ? "currentA" : "reynolds";
                if(columns.Distinct().Count()!=columns.Length || required.Any(x=>!columns.Contains(x)) || columns.Any(x=>!required.Contains(x) && x!=optional))
                    throw new ArgumentException("Expected CSV columns: "+string.Join(",",required)+" (optional "+optional+"). Cq is not Cp.");
                int line=1; string row;
                while((row=reader.ReadLine())!=null)
                {
                    line++; if(string.IsNullOrWhiteSpace(row)) continue;
                    if(table.Count+map.Count>=4096) throw new ArgumentException("CSV exceeds 4096 rows.");
                    var cells=row.Split(',');
                    if(cells.Length!=columns.Length) throw new ArgumentException("CSV line "+line+": incorrect number of columns.");
                    double Read(string key)
                    {
                        int index=Array.IndexOf(columns,key);
                        if(!double.TryParse(cells[index].Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out double value) ||
                            double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value)>1e12)
                            throw new ArgumentException("CSV line "+line+": invalid SI number in "+key+".");
                        return value;
                    }
                    if(model=="RpmTable") table.Add(new RpmPerformancePoint { rpm=Read("rpm"),thrustN=Read("thrustN"),torqueNm=Read("torqueNm"),currentA=columns.Contains(optional) ? (double?)Read(optional) : null });
                    else map.Add(new PerformanceMapPoint { rpm=Read("rpm"),advanceRatio=Read("advanceRatio"),ct=Read("ct"),cq=Read("cq"),reynolds=columns.Contains(optional) ? Read(optional) : 0 });
                    if(model=="PerformanceMap" && columns.Contains(optional) && map.Last().reynolds<=0) throw new ArgumentException("Reynolds metadata must be positive.");
                }
            }
            if(model=="RpmTable") result.rpmTable=table.OrderBy(x=>x.rpm).ToArray();
            else result.performanceMap=map.OrderBy(x=>x.rpm).ThenBy(x=>x.advanceRatio).ToArray();
            var errors=PerformanceValidation.Check(result).ToArray();
            if(errors.Length>0) throw new ArgumentException(string.Join("\n",errors));
            if(model=="RpmTable" && (referenceDensity<=0 || double.IsNaN(referenceDensity) || double.IsInfinity(referenceDensity)))
                throw new ArgumentException("Positive reference density required.");
            return result;
        }
    }
}
