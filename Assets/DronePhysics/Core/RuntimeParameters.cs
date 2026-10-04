using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public sealed class RuntimeRotorParameters
    {
        public readonly string Id;
        public readonly DVector3 Position, Axis;
        public readonly double ReactionSign, MaxOmega, MinOmega, IdleOmega, TauUp, TauDown, KT, KQ, Diameter;
        public double MaxThrust => PhysicsMath.Thrust(MaxOmega, KT);
        internal RuntimeRotorParameters(RotorProfile p, double rho, bool response)
        {
            Id=p.rotorId; Position=DVector3.From(p.geometry.positionLocalM); Axis=DVector3.From(p.geometry.thrustAxisLocal).Normalized;
            // Viewed from the +thrust-axis tip towards the hub. Unity +Y rotates clockwise from above.
            // Body reaction is opposite rotor spin: CW -> -axis, CCW -> +axis.
            ReactionSign=p.geometry.spinDirection == "CW" ? -1 : 1;
            MaxOmega=PhysicsMath.RpmToOmega(p.motor.maxRpm); MinOmega=PhysicsMath.RpmToOmega(p.motor.minRpm);
            IdleOmega=PhysicsMath.RpmToOmega(p.motor.idleRpm);
            TauUp=response ? p.motor.responseTimeUpS : 0; TauDown=response ? p.motor.responseTimeDownS : 0;
            Diameter=p.propeller.diameterM;
            if (p.performance.model == "CtCq")
            {
                KT=p.performance.ct*rho*Math.Pow(Diameter,4)/(4*Math.PI*Math.PI);
                KQ=p.performance.cq*rho*Math.Pow(Diameter,5)/(4*Math.PI*Math.PI);
            }
            else { KT=p.performance.kThrustNPerRadPerSecSquared; KQ=p.performance.kTorqueNmPerRadPerSecSquared; }
        }
    }
    public sealed class RuntimeDroneParameters
    {
        public readonly double Mass, Gravity, Density;
        public readonly DVector3 CenterOfMass, Inertia, Dimensions, DragCd, DragArea, DragPoint, Wind;
        public readonly double RotationX, RotationY, RotationZ, RotationW;
        public readonly bool BodyDrag;
        public readonly IReadOnlyList<RuntimeRotorParameters> Rotors;
        public readonly double MaxTotalThrust;
        public double ThrustToWeight => MaxTotalThrust/(Mass*Gravity);
        internal RuntimeDroneParameters(DroneProfile p, EnvironmentProfile env)
        {
            Mass=p.massProperties.massKg; Gravity=env.gravityMps2; Density=env.airDensityKgM3;
            CenterOfMass=DVector3.From(p.massProperties.centerOfMassLocalM); Dimensions=DVector3.From(p.massProperties.dimensionsM);
            var inertia=p.massProperties.inertia;
            Inertia=inertia.mode == "AutoBox" ? PhysicsMath.BoxInertia(Mass,Dimensions) : DVector3.From(inertia.principalMomentsKgM2);
            var q=inertia.mode == "AutoBox" ? new double[]{0,0,0,1} : inertia.principalAxesRotationXyzw;
            RotationX=q[0]; RotationY=q[1]; RotationZ=q[2]; RotationW=q[3];
            BodyDrag=p.physicsConfiguration.modules.bodyDrag;
            DragCd=BodyDrag ? DVector3.From(p.bodyAerodynamics.dragCd) : default;
            DragArea=BodyDrag ? DVector3.From(p.bodyAerodynamics.referenceAreaM2) : default;
            DragPoint=p.bodyAerodynamics.dragApplicationPointLocalM == null ? CenterOfMass : DVector3.From(p.bodyAerodynamics.dragApplicationPointLocalM);
            Wind=p.physicsConfiguration.modules.windInteraction && env.windMode != "None" ? DVector3.From(env.windVelocityWorldMps) : default;
            var rotors=new List<RuntimeRotorParameters>();
            foreach(var r in p.rotors) { var runtime=new RuntimeRotorParameters(r,Density,p.physicsConfiguration.modules.motorResponse); rotors.Add(runtime); MaxTotalThrust+=runtime.MaxThrust; }
            Rotors=rotors.AsReadOnly();
        }
    }
}
