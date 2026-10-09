using System;

namespace DroneLab.Physics
{
    // Coefficients at reference density. These are empirical corrections, not BEMT/VRS.
    public sealed class RuntimeRotorFlow
    {
        public readonly double AxialCoefficient,FlappingCoefficient,LiftCoefficient;
        public readonly double ReferenceDensity,MaxAirSpeed,MaxThrustFraction,MaxMomentRatio;
        internal RuntimeRotorFlow(RotorAerodynamicsProfile p,PhysicsModulesProfile modules)
        {
            AxialCoefficient=modules.inducedDrag ? p.inducedDragCoefficient : 0;
            FlappingCoefficient=modules.bladeFlapping ? p.bladeFlappingCoefficient : 0;
            LiftCoefficient=modules.rotorAerodynamics ? p.translationalLiftCoefficientKgPerM : 0;
            ReferenceDensity=p.referenceAirDensityKgM3; MaxAirSpeed=p.maxAirSpeedMps;
            MaxThrustFraction=p.maxThrustCorrectionFraction; MaxMomentRatio=p.maxFlappingMomentRatio;
        }
    }

    public readonly struct RotorFlowSample
    {
        public readonly double ThrustCorrection;
        // Pure hub moment, excludes lever-arm moments from any force.
        public readonly DVector3 FlappingMoment;
        public readonly bool Clamped;
        public RotorFlowSample(double correction,DVector3 moment,bool clamped)
        { ThrustCorrection=correction; FlappingMoment=moment; Clamped=clamped; }
    }

    public static class RotorFlow
    {
        public static RotorFlowSample Evaluate(RuntimeRotorParameters rotor,double omega,double freeAirThrust,
            DVector3 airVelocity,DVector3 unitAxis,double density)
        {
            EnvironmentMath.Finite(airVelocity); EnvironmentMath.Finite(unitAxis);
            if(!Finite(omega) || omega<0 || !Finite(freeAirThrust) || !Finite(density) || density<=0 || Math.Abs(unitAxis.Length-1)>1e-5)
                throw new ArgumentOutOfRangeException("Invalid rotor airflow query.");
            var p=rotor.Flow;
            // No extrapolated lift/torque at motor stop or on a windmilling/negative-thrust branch.
            if(p==null || omega==0 || freeAirThrust<=0) return default;
            double speed=airVelocity.Length; bool clamped=speed>p.MaxAirSpeed;
            var v=clamped ? airVelocity*(p.MaxAirSpeed/speed) : airVelocity;
            double axial=DVector3.Dot(v,unitAxis),ratio=density/p.ReferenceDensity;
            var perpendicular=v-unitAxis*axial;
            double raw=ratio*(p.LiftCoefficient*DVector3.Dot(perpendicular,perpendicular)-p.AxialCoefficient*omega*axial);
            double cap=p.MaxThrustFraction*freeAirThrust;
            double correction=PhysicsMath.Clamp(raw,-cap,cap);
            var moment=DVector3.Cross(v,unitAxis)*(-p.FlappingCoefficient*omega*ratio);
            double momentCap=p.MaxMomentRatio*freeAirThrust*rotor.Diameter/2;
            double magnitude=moment.Length;
            if(magnitude>momentCap) { moment=moment*(momentCap/magnitude); clamped=true; }
            if(correction!=raw) clamped=true;
            if(!Finite(correction)) throw new ArgumentOutOfRangeException("Nonfinite rotor correction.");
            EnvironmentMath.Finite(moment);
            return new RotorFlowSample(correction,moment,clamped);
        }
        private static bool Finite(double value)=>!double.IsNaN(value) && !double.IsInfinity(value);
    }
}
