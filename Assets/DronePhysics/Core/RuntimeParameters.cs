using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public sealed class RuntimeRotorParameters
    {
        public readonly string Id;
        public readonly DVector3 Position, Axis;
        // KT/KQ are meaningful only for the two constant-coefficient models.
        public readonly double ReactionSign, MaxOmega, MinOmega, IdleOmega, TauUp, TauDown, KT, KQ, Diameter, RotorDragCoefficient;
        public readonly PropellerPerformance Performance;
        public readonly RuntimeMotorPower Power;
        public readonly double MaxThrust;
        internal RuntimeRotorParameters(RotorProfile p, double rho, bool response, bool rotorDrag, bool power)
        {
            Id=p.rotorId; Position=DVector3.From(p.geometry.positionLocalM); Axis=DVector3.From(p.geometry.thrustAxisLocal).Normalized;
            Power=power ? new RuntimeMotorPower(p.motor.electrical) : null;
            // Viewed from the +thrust-axis tip towards the hub. Unity +Y rotates clockwise from above.
            // Body reaction is opposite rotor spin: CW -> -axis, CCW -> +axis.
            ReactionSign=p.geometry.spinDirection == "CW" ? -1 : 1;
            MaxOmega=PhysicsMath.RpmToOmega(p.motor.maxRpm); MinOmega=PhysicsMath.RpmToOmega(p.motor.minRpm);
            IdleOmega=PhysicsMath.RpmToOmega(p.motor.idleRpm);
            TauUp=response ? p.motor.responseTimeUpS : 0; TauDown=response ? p.motor.responseTimeDownS : 0;
            Diameter=p.propeller.diameterM;
            RotorDragCoefficient=rotorDrag ? p.advancedAerodynamics.rotorDragCoefficientKgPerRad : 0;
            Performance=new PropellerPerformance(p.performance,rho,Diameter);
            if (p.performance.model == "CtCq")
            {
                KT=p.performance.ct*rho*Math.Pow(Diameter,4)/(4*Math.PI*Math.PI);
                KQ=p.performance.cq*rho*Math.Pow(Diameter,5)/(4*Math.PI*Math.PI);
            }
            else { KT=p.performance.kThrustNPerRadPerSecSquared; KQ=p.performance.kTorqueNmPerRadPerSecSquared; }
            MaxThrust=Performance.StaticPeakThrust(MaxOmega);
        }
    }
    public sealed class RuntimeDroneParameters
    {
        public readonly double Mass, Gravity, Density;
        public readonly RuntimeEnvironment Environment;
        public readonly DVector3 CenterOfMass, Inertia, Dimensions, DragCd, DragArea, DragPoint, Wind;
        public readonly double RotationX, RotationY, RotationZ, RotationW;
        public readonly bool BodyDrag,RotorDrag;
        public readonly RuntimeGroundEffect GroundEffect;
        public readonly RuntimeBattery Battery;
        public readonly string DragModel,ProjectedAreaMode;
        public readonly double ProjectedCd;
        public readonly DVector3 ProjectedReferenceArea;
        public readonly IReadOnlyList<RuntimeAreaSample> AreaSamples;
        public readonly IReadOnlyList<RuntimeAeroSurface> Surfaces;
        public readonly IReadOnlyList<RuntimeRotorParameters> Rotors;
        public readonly double MaxTotalThrust;
        public double ThrustToWeight => MaxTotalThrust/(Mass*Gravity);
        internal RuntimeDroneParameters(DroneProfile p, EnvironmentProfile env)
        {
            Environment=new RuntimeEnvironment(env,p.physicsConfiguration.modules.windInteraction);
            Mass=p.massProperties.massKg; Gravity=env.gravityMps2; Density=Environment.SampleAir(0).Density;
            CenterOfMass=DVector3.From(p.massProperties.centerOfMassLocalM); Dimensions=DVector3.From(p.massProperties.dimensionsM);
            var inertia=p.massProperties.inertia;
            Inertia=inertia.mode == "AutoBox" ? PhysicsMath.BoxInertia(Mass,Dimensions) : DVector3.From(inertia.principalMomentsKgM2);
            var q=inertia.mode == "AutoBox" ? new double[]{0,0,0,1} : inertia.principalAxesRotationXyzw;
            RotationX=q[0]; RotationY=q[1]; RotationZ=q[2]; RotationW=q[3];
            BodyDrag=p.physicsConfiguration.modules.bodyDrag;
            RotorDrag=p.physicsConfiguration.modules.rotorAerodynamics;
            GroundEffect=p.physicsConfiguration.modules.groundEffect ? new RuntimeGroundEffect(p.groundEffect) : null;
            Battery=p.powerSystem.battery.mode=="None" ? null : new RuntimeBattery(p.powerSystem.battery,p.physicsConfiguration.modules);
            var aero=p.bodyAerodynamics; DragModel=aero.model;
            DragCd=BodyDrag && DragModel=="AxisApproximation" ? DVector3.From(aero.dragCd) : default;
            DragArea=BodyDrag && DragModel=="AxisApproximation" ? DVector3.From(aero.referenceAreaM2) : default;
            ProjectedCd=aero.dragCoefficient;
            ProjectedAreaMode=aero.projectedArea?.mode;
            ProjectedReferenceArea=BodyDrag && DragModel=="ProjectedArea" && ProjectedAreaMode=="AxisApproximation" ?
                DVector3.From(aero.projectedArea.referenceAreaM2) : default;
            var samples=new List<RuntimeAreaSample>(); var surfaces=new List<RuntimeAeroSurface>();
            if(BodyDrag && DragModel=="ProjectedArea" && aero.projectedArea.samples!=null)
                foreach(var sample in aero.projectedArea.samples) samples.Add(new RuntimeAreaSample(DVector3.From(sample.directionLocal),sample.areaM2));
            if(BodyDrag && DragModel=="Surfaces") foreach(var surface in aero.surfaces) surfaces.Add(new RuntimeAeroSurface(surface));
            AreaSamples=samples.AsReadOnly(); Surfaces=surfaces.AsReadOnly();
            DragPoint=p.bodyAerodynamics.dragApplicationPointLocalM == null ? CenterOfMass : DVector3.From(p.bodyAerodynamics.dragApplicationPointLocalM);
            Wind=p.physicsConfiguration.modules.windInteraction && env.windMode != "None" ? DVector3.From(env.windVelocityWorldMps) : default;
            var rotors=new List<RuntimeRotorParameters>();
            foreach(var r in p.rotors) { var runtime=new RuntimeRotorParameters(r,Density,p.physicsConfiguration.modules.motorResponse,RotorDrag,Battery!=null); rotors.Add(runtime); MaxTotalThrust+=runtime.MaxThrust; }
            Rotors=rotors.AsReadOnly();
        }
    }
}
