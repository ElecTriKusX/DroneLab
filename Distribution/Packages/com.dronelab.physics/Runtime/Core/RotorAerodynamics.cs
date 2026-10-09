using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public sealed class RuntimeGroundEffect
    {
        public readonly double Coefficient,MinHeightRadiusRatio,MaxMultiplier;
        internal RuntimeGroundEffect(GroundEffectProfile p)
        { Coefficient=p.coefficient; MinHeightRadiusRatio=p.minHeightRadiusRatio; MaxMultiplier=p.maxMultiplier; }
    }

    public static class RotorAerodynamics
    {
        // Finite collider probe: retain the formula through 45R, fade smoothly to zero at 50R.
        public const double ProbeRadiusLimit=50;
        public const double ProbeFadeStartRadius=45;
        public const double MinimumNormalAlignment=0.5;
        // Height is axial rotor-centre -> surface distance. Infinity means no hit.
        // Fade near 60-degree incidence; use the original bounded model on an aligned plane.
        public static double GroundMultiplier(RuntimeGroundEffect p,double radius,double height,double normalAlignment=1)
        {
            if(p==null) return 1;
            if(radius<=0 || double.IsNaN(radius) || double.IsInfinity(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
            if(double.IsNaN(height) || double.IsNaN(normalAlignment) || double.IsInfinity(normalAlignment)) throw new ArgumentException("Invalid surface query.");
            if(height<0 || double.IsInfinity(height) || height>ProbeRadiusLimit*radius || p.Coefficient==0 || p.MaxMultiplier==1) return 1;
            double fade=PhysicsMath.Clamp((normalAlignment-MinimumNormalAlignment)/(1-MinimumNormalAlignment),0,1);
            if(fade==0) return 1;
            double ratio=Math.Max(height/radius,p.MinHeightRadiusRatio);
            double rawGain=p.Coefficient*Math.Pow(1/(4*ratio),2);
            // Bound before fading so even a very small h cannot overflow the final multiplier.
            double farT=PhysicsMath.Clamp((height/radius-ProbeFadeStartRadius)/(ProbeRadiusLimit-ProbeFadeStartRadius),0,1);
            double distanceFade=1-farT*farT*(3-2*farT);
            return 1+Math.Min(p.MaxMultiplier-1,rawGain)*fade*fade*distanceFade;
        }
        public static double ThrustWithGroundEffect(double thrust,double multiplier) => thrust>0 ? thrust*multiplier : thrust;
        // Empirical per-rotor drag: K [kg/rad], omega [rad/s], Vperp [m/s] -> N.
        // No axial force, implicit density correction, thrust boost or rolling-moment term.
        public static DVector3 Drag(DVector3 airVelocity,DVector3 unitAxis,double omega,double coefficient)
        {
            if(omega<0 || coefficient<0 || double.IsNaN(omega) || double.IsInfinity(omega) ||
                double.IsNaN(coefficient) || double.IsInfinity(coefficient)) throw new ArgumentOutOfRangeException();
            var perpendicular=airVelocity-unitAxis*DVector3.Dot(airVelocity,unitAxis);
            return perpendicular*(-coefficient*omega);
        }
        public static AeroWrench DragWrench(RuntimeDroneParameters p,IReadOnlyList<double> omega,DVector3 airVelocityAtCom,DVector3 angularVelocity)
        {
            if(!p.RotorDrag) return default;
            if(omega==null || omega.Count!=p.Rotors.Count) throw new ArgumentException("One omega per rotor is required.");
            DVector3 force=default,torque=default;
            for(int i=0;i<p.Rotors.Count;i++)
            {
                var r=p.Rotors[i]; var arm=r.Position-p.CenterOfMass;
                var velocity=airVelocityAtCom+DVector3.Cross(angularVelocity,arm);
                var f=Drag(velocity,r.Axis,omega[i],r.RotorDragCoefficient);
                force+=f; torque+=DVector3.Cross(arm,f);
            }
            return new AeroWrench(force,torque);
        }
    }
}
