using System;
using System.Linq;

namespace DroneLab.Physics
{
    public readonly struct PropellerSample
    {
        public readonly double Thrust, Torque, AdvanceRatio;
        public readonly double? Current;
        public readonly bool Clamped;
        public PropellerSample(double thrust,double torque,double advanceRatio=0,double? current=null,bool clamped=false)
        { Thrust=thrust; Torque=torque; AdvanceRatio=advanceRatio; Current=current; Clamped=clamped; }
    }

    // Compiled once: sorted private arrays; no DTO references, mesh access or per-query allocations.
    public sealed class PropellerPerformance
    {
        public readonly string Model, Policy;
        private readonly double rho, diameter, kt, kq;
        private readonly double[] rpm, j, thrust, torque, current, ct, cq;
        private readonly bool staticQuadratic;
        public bool IsStaticQuadratic => IsQuadratic || staticQuadratic;
        public bool IsQuadratic => Model=="OmegaSquared" || Model=="CtCq";
        public PropellerPerformance(RotorPerformanceProfile p,double density,double diameterM)
        {
            Model=p.model; Policy=p.outOfRangePolicy; rho=density; diameter=diameterM;
            if(Model=="CtCq")
            { kt=p.ct*rho*Math.Pow(diameter,4)/(4*Math.PI*Math.PI); kq=p.cq*rho*Math.Pow(diameter,5)/(4*Math.PI*Math.PI); }
            else if(Model=="OmegaSquared") { kt=p.kThrustNPerRadPerSecSquared; kq=p.kTorqueNmPerRadPerSecSquared; }
            else if(Model=="RpmTable")
            {
                var rows=p.rpmTable.OrderBy(x=>x.rpm).ToArray();
                rpm=rows.Select(x=>x.rpm).ToArray(); thrust=rows.Select(x=>x.thrustN).ToArray(); torque=rows.Select(x=>x.torqueNm).ToArray();
                if(rows.All(x=>x.currentA.HasValue)) current=rows.Select(x=>x.currentA.Value).ToArray();
            }
            else
            {
                rpm=p.performanceMap.Select(x=>x.rpm).Distinct().OrderBy(x=>x).ToArray();
                j=p.performanceMap.Select(x=>x.advanceRatio).Distinct().OrderBy(x=>x).ToArray();
                ct=new double[rpm.Length*j.Length]; cq=new double[ct.Length];
                foreach(var row in p.performanceMap)
                { int index=Array.BinarySearch(rpm,row.rpm)*j.Length+Array.BinarySearch(j,row.advanceRatio); ct[index]=row.ct; cq[index]=row.cq; }
                int zero=Array.BinarySearch(j,0.0);
                staticQuadratic=Enumerable.Range(0,rpm.Length).All(k=>ct[k*j.Length+zero]==ct[zero] && cq[k*j.Length+zero]==cq[zero]);
                if(staticQuadratic)
                { kt=ct[zero]*rho*Math.Pow(diameter,4)/(4*Math.PI*Math.PI); kq=cq[zero]*rho*Math.Pow(diameter,5)/(4*Math.PI*Math.PI); }
            }
        }
        private static void Finite(double x)
        { if(double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("Finite propeller query required."); }
        private double Bound(double value,double lo,double hi,ref bool clamped)
        {
            if(value>=lo-1e-9*Math.Max(1,Math.Abs(lo)) && value<=hi+1e-9*Math.Max(1,Math.Abs(hi))) return PhysicsMath.Clamp(value,lo,hi);
            if(Policy=="Reject") throw new ArgumentOutOfRangeException(nameof(value),"Propeller "+Model+" query outside supplied data range.");
            clamped=true; return PhysicsMath.Clamp(value,lo,hi);
        }
        private static int Interval(double[] axis,double x)
        {
            if(axis.Length==1) return 0;
            int index=Array.BinarySearch(axis,x);
            return index>=0 ? Math.Min(index,axis.Length-2) : Math.Max(0,Math.Min(~index-1,axis.Length-2));
        }
        private static double Lerp(double a,double b,double t) => a+(b-a)*t;
        private double Grid(double[] values,int r,int s,double tr,double tj)
        {
            int r1=Math.Min(r+1,rpm.Length-1);
            return Lerp(Lerp(values[r*j.Length+s],values[r*j.Length+s+1],tj),
                Lerp(values[r1*j.Length+s],values[r1*j.Length+s+1],tj),tr);
        }
        // Positive axial velocity = rotor motion along +thrust relative to air (advancing flow).
        // J uses actual RPM, including when the coefficient lookup is clamped.
        public PropellerSample Evaluate(double omega,double axialVelocity=0)
        {
            Finite(omega); Finite(axialVelocity); if(omega<0) throw new ArgumentOutOfRangeException(nameof(omega));
            if(omega==0) return new PropellerSample(0,0,current:current==null ? (double?)null : 0);
            if(IsQuadratic) return new PropellerSample(kt*omega*omega,kq*omega*omega);
            bool bounded=false; double actualRpm=PhysicsMath.OmegaToRpm(omega);
            double lookupRpm=Bound(actualRpm,rpm[0],rpm[rpm.Length-1],ref bounded);
            int r=Interval(rpm,lookupRpm);
            double tr=rpm.Length==1 ? 0 : (lookupRpm-rpm[r])/(rpm[r+1]-rpm[r]);
            if(Model=="RpmTable")
                return new PropellerSample(Lerp(thrust[r],thrust[r+1],tr),Lerp(torque[r],torque[r+1],tr),
                    current:current==null ? (double?)null : Lerp(current[r],current[r+1],tr),clamped:bounded);
            double advance=axialVelocity/(actualRpm/60*diameter);
            double lookupJ=Bound(advance,j[0],j[j.Length-1],ref bounded);
            int s=Interval(j,lookupJ); double tj=(lookupJ-j[s])/(j[s+1]-j[s]);
            return new PropellerSample(PhysicsMath.CtThrust(actualRpm,Grid(ct,r,s,tr,tj),rho,diameter),
                PhysicsMath.CqTorque(actualRpm,Grid(cq,r,s,tr,tj),rho,diameter),advance,clamped:bounded);
        }
        // Static curve inversion for the test pilot. A map is evaluated at J=0 here.
        public double OmegaForStaticThrust(double requested,double maxOmega)
        {
            if(requested<=0) return 0;
            if(IsStaticQuadratic) return Math.Sqrt(requested/kt);
            if(Model=="RpmTable")
            {
                int index=Interval(thrust,requested);
                double t=(requested-thrust[index])/(thrust[index+1]-thrust[index]);
                return PhysicsMath.RpmToOmega(Lerp(rpm[index],rpm[index+1],PhysicsMath.Clamp(t,0,1)));
            }
            double lo=0,hi=maxOmega;
            for(int i=0;i<40;i++) { double mid=(lo+hi)/2; if(Evaluate(mid).Thrust<requested) lo=mid; else hi=mid; }
            return (lo+hi)/2;
        }
        public double StaticPeakThrust(double maxOmega)
        {
            double peak=Evaluate(maxOmega).Thrust;
            if(!IsQuadratic) foreach(double value in rpm)
                if(PhysicsMath.RpmToOmega(value)<=maxOmega) peak=Math.Max(peak,Evaluate(PhysicsMath.RpmToOmega(value)).Thrust);
            if(Model=="PerformanceMap")
            {
                int zero=Array.BinarySearch(j,0.0);
                for(int k=0;k<rpm.Length-1;k++)
                {
                    double slope=(ct[(k+1)*j.Length+zero]-ct[k*j.Length+zero])/(rpm[k+1]-rpm[k]);
                    if(slope==0) continue;
                    double intercept=ct[k*j.Length+zero]-slope*rpm[k];
                    double stationary=-2*intercept/(3*slope);
                    if(stationary>rpm[k] && stationary<rpm[k+1] && stationary<=PhysicsMath.OmegaToRpm(maxOmega))
                        peak=Math.Max(peak,Evaluate(PhysicsMath.RpmToOmega(stationary)).Thrust);
                }
            }
            return peak;
        }
        public void RequirePilotCurve(double maxOmega)
        {
            if(IsQuadratic) return;
            if(Model=="PerformanceMap" && Policy=="Reject")
                throw new ArgumentException("Test pilot needs Clamp for map startup below its first positive RPM.");
            double previousT=0,previousQ=0;
            // At J=0, Ct interpolation is affine in RPM; derivative of RPM²(a+b RPM)
            // has at most one interior extremum. Check both endpoints of every segment.
            if(Model=="PerformanceMap")
            {
                int zero=Array.BinarySearch(j,0.0);
                for(int k=0;k<rpm.Length-1;k++)
                {
                    double a=ct[k*j.Length+zero],b=ct[(k+1)*j.Length+zero];
                    double slope=(b-a)/(rpm[k+1]-rpm[k]);
                    if(a<=0 || b<=0 || 2*a+rpm[k]*slope<=0 || 2*b+rpm[k+1]*slope<=0)
                        throw new ArgumentException("Test pilot requires strictly increasing positive static thrust.");
                    a=cq[k*j.Length+zero]; b=cq[(k+1)*j.Length+zero]; slope=(b-a)/(rpm[k+1]-rpm[k]);
                    if(a<0 || b<0 || 2*a+rpm[k]*slope<0 || 2*b+rpm[k+1]*slope<0)
                        throw new ArgumentException("Test pilot requires nonnegative nondecreasing static torque.");
                }
            }
            foreach(double value in rpm)
            {
                var sample=Evaluate(PhysicsMath.RpmToOmega(value));
                if(value>0 && (sample.Thrust<=previousT || sample.Torque<previousQ || sample.Torque<0))
                    throw new ArgumentException("Test pilot requires increasing thrust and nondecreasing nonnegative torque.");
                previousT=sample.Thrust; previousQ=sample.Torque;
            }
            if(Evaluate(maxOmega).Thrust<=0 || Evaluate(maxOmega).Torque<=0)
                throw new ArgumentException("Test pilot requires positive maximum static thrust and torque for yaw control.");
        }
    }
}
