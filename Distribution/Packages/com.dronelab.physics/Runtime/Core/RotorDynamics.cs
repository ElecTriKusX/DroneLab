using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public static class RotorDynamics
    {
        // Implicit midpoint coast: Jr*(next-previous)/dt = -Q((next+previous)/2,Vaxial).
        // Q is nonnegative and monotone on the validated power envelope.
        public static double Coast(RuntimeRotorParameters rotor,double previous,double dt,double axial,double density)
        {
            if(!rotor.Inertial || previous<0 || !Finite(previous) || dt<=0 || !Finite(dt)) throw new ArgumentOutOfRangeException();
            if(previous==0) return 0;
            if(rotor.Performance.Evaluate(previous/2,axial,density).Torque>=rotor.RotatingInertia*previous/dt) return 0;
            double lo=0,hi=previous;
            for(int k=0;k<56;k++)
            {
                double mid=(lo+hi)/2;
                double residual=rotor.RotatingInertia*(mid-previous)/dt+rotor.Performance.Evaluate((mid+previous)/2,axial,density).Torque;
                if(residual>0) hi=mid; else lo=mid;
            }
            return (lo+hi)/2;
        }
        public static double SpinEnergy(RuntimeRotorParameters rotor,double omega)=>.5*rotor.RotatingInertia*omega*omega;
        // H uses rotor spin, opposite to the sign of its reaction torque on the body.
        public static DVector3 Momentum(RuntimeDroneParameters p,IReadOnlyList<double> omega)
        {
            if(omega==null || omega.Count!=p.Rotors.Count) throw new ArgumentException("One speed per rotor is required.");
            DVector3 h=default;
            for(int i=0;i<omega.Count;i++)
            {
                if(omega[i]<0 || !Finite(omega[i])) throw new ArgumentOutOfRangeException(nameof(omega));
                var r=p.Rotors[i]; h+=r.Axis*(-r.ReactionSign*r.RotatingInertia*omega[i]);
            }
            return h;
        }
        public static DVector3 GyroscopicMoment(RuntimeDroneParameters p,IReadOnlyList<double> omega,DVector3 bodyRate)
        { EnvironmentMath.Finite(bodyRate); return p.GyroscopicRotors ? DVector3.Cross(bodyRate,Momentum(p,omega))*(-1) : default; }
        private static bool Finite(double x)=>!double.IsNaN(x) && !double.IsInfinity(x);
    }
}
