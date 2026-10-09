using System;
using System.Collections.Generic;
using System.Linq;

namespace DroneLab.Physics
{
    public static class PerformanceValidation
    {
        public static IEnumerable<string> Check(RotorPerformanceProfile p)
        {
            if(p.outOfRangePolicy!="Clamp" && p.outOfRangePolicy!="Reject") yield return "Explicit outOfRangePolicy Clamp or Reject is required.";
            if(p.model=="RpmTable")
            {
                if(p.rpmTable==null || p.rpmTable.Length<2 || p.rpmTable.Length>4096)
                { yield return "rpmTable requires 2..4096 rows."; yield break; }
                var rows=p.rpmTable.OrderBy(x=>x.rpm).ToArray();
                if(rows.Any(x=>!Finite(x.rpm) || !Finite(x.thrustN) || !Finite(x.torqueNm) || x.rpm<0 || x.thrustN<0 || x.torqueNm<0 ||
                    (x.currentA.HasValue && (!Finite(x.currentA.Value) || x.currentA.Value<0)))) yield return "RPM, thrust, torque and current must be finite and nonnegative.";
                if(rows.Select(x=>x.rpm).Distinct().Count()!=rows.Length) yield return "Duplicate RPM rows are not allowed.";
                if(rows[0].rpm!=0 || rows[0].thrustN!=0 || rows[0].torqueNm!=0 || (rows[0].currentA.HasValue && rows[0].currentA!=0))
                    yield return "An explicit zero RPM / zero thrust / zero torque (and zero current if supplied) origin is required.";
                if(rows.Any(x=>x.currentA.HasValue) && rows.Any(x=>!x.currentA.HasValue)) yield return "currentA must be supplied for all rows or omitted for all rows.";
            }
            else if(p.model=="PerformanceMap")
            {
                if(p.performanceMap==null || p.performanceMap.Length<2 || p.performanceMap.Length>4096)
                { yield return "performanceMap requires 2..4096 rows."; yield break; }
                var rows=p.performanceMap;
                if(rows.Any(x=>!Finite(x.rpm) || x.rpm<=0 || !Finite(x.advanceRatio) || !Finite(x.ct) || !Finite(x.cq)))
                    yield return "Map RPM must be positive; RPM/J/Ct/Cq must be finite.";
                int nr=rows.Select(x=>x.rpm).Distinct().Count(), nj=rows.Select(x=>x.advanceRatio).Distinct().Count();
                if(nj<2 || (long)nr*nj!=rows.Length || rows.Select(x=>Tuple.Create(x.rpm,x.advanceRatio)).Distinct().Count()!=rows.Length)
                    yield return "Map requires a complete rectangular RPM x J grid with at least two J values and no duplicate cells.";
                if(!rows.Any(x=>x.advanceRatio==0)) yield return "Map must include a J=0 column for static capacity/reference calculations.";
            }
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }
}
