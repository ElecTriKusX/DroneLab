using System;

namespace DroneLab.Physics
{
    public sealed class RuntimeRotorEnvelope
    {
        public readonly double MaxClimb,MaxDescent,MaxLateral;
        internal RuntimeRotorEnvelope(RotorOperatingEnvelopeProfile p)
        { MaxClimb=p.maxAxialClimbSpeedMps; MaxDescent=p.maxAxialDescentSpeedMps; MaxLateral=p.maxLateralSpeedMps; }
    }
    public enum RotorFlowRegime { Stopped=0,NonPositiveThrust=1,HoverOrClimb=2,PositiveThrustDescent=3 }
    public readonly struct RotorFlightEnvelopeSample
    {
        public readonly double AxialSpeed,LateralSpeed;
        public readonly double? DescentToHoverInflowRatio;
        public readonly bool? Exceeded;
        public readonly RotorFlowRegime Regime;
        public RotorFlightEnvelopeSample(double axial,double lateral,double? ratio,bool? exceeded,RotorFlowRegime regime)
        { AxialSpeed=axial; LateralSpeed=lateral; DescentToHoverInflowRatio=ratio; Exceeded=exceeded; Regime=regime; }
    }
    public static class RotorFlightEnvelope
    {
        // Raw local flow, BEFORE flow clipping and ground effect. Diagnostics only: no VRS forces.
        public static RotorFlightEnvelopeSample Evaluate(RuntimeRotorParameters r,double omega,double freeAirThrust,
            DVector3 pointAirVelocity,DVector3 unitAxis,double density)
        {
            EnvironmentMath.Finite(pointAirVelocity); EnvironmentMath.Finite(unitAxis);
            EnvironmentMath.Finite(omega); EnvironmentMath.Finite(freeAirThrust); EnvironmentMath.Finite(density);
            if(r==null || omega<0 || density<=0 || Math.Abs(unitAxis.Length-1)>1e-5) throw new ArgumentOutOfRangeException(nameof(omega));
            double axial=DVector3.Dot(pointAirVelocity,unitAxis),lateral=(pointAirVelocity-unitAxis*axial).Length;
            var regime=omega==0 ? RotorFlowRegime.Stopped : freeAirThrust<=0 ? RotorFlowRegime.NonPositiveThrust :
                axial<0 ? RotorFlowRegime.PositiveThrustDescent : RotorFlowRegime.HoverOrClimb;
            double vi=omega>0 ? PhysicsMath.InducedHoverVelocity(freeAirThrust,density,r.Diameter) : 0;
            double? ratio=vi>1e-6 ? (double?)(-axial/vi) : null;
            bool? exceeded=r.Envelope==null ? (bool?)null : axial>r.Envelope.MaxClimb || -axial>r.Envelope.MaxDescent || lateral>r.Envelope.MaxLateral;
            return new RotorFlightEnvelopeSample(axial,lateral,ratio,exceeded,regime);
        }
    }
}
