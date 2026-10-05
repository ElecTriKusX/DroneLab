using System;

namespace DroneLab.Physics
{
    public readonly struct AeroWrench
    {
        public readonly DVector3 Force, Torque;
        public readonly double ProjectedArea;
        public AeroWrench(DVector3 force,DVector3 torque,double projectedArea=0) { Force=force; Torque=torque; ProjectedArea=projectedArea; }
    }
    public static class BodyAerodynamics
    {
        public static double BoxProjectedArea(DVector3 dimensions,DVector3 direction)
        {
            var d=direction.Normalized;
            return dimensions.Y*dimensions.Z*Math.Abs(d.X)+dimensions.X*dimensions.Z*Math.Abs(d.Y)+
                dimensions.X*dimensions.Y*Math.Abs(d.Z);
        }
        // Directional samples describe a silhouette: A(d) == A(-d).
        // Positive, continuous inverse-distance weights; exact sample hits remain exact.
        public static double LookupArea(System.Collections.Generic.IReadOnlyList<RuntimeAreaSample> samples,DVector3 direction)
        {
            var d=direction.Normalized; double weighted=0,weights=0;
            foreach(var sample in samples)
            {
                double distance=1-Math.Min(1,Math.Abs(DVector3.Dot(d,sample.Direction)));
                if(distance<1e-12) return sample.Area;
                double weight=1/(distance*distance);
                weighted+=weight*sample.Area; weights+=weight;
            }
            return weighted/weights;
        }
        public static DVector3 ProjectedDrag(DVector3 velocity,double rho,double cd,double area)
            => velocity*(-0.5*rho*cd*area*velocity.Length);
        // Two-sided pressure patch. Tangential flow produces no force; force is normal to the patch.
        public static DVector3 SurfaceDrag(DVector3 velocity,DVector3 normal,double rho,double cd,double area)
        {
            double vn=DVector3.Dot(velocity,normal);
            return normal*(-0.5*rho*cd*area*vn*Math.Abs(vn));
        }
        // Velocity at COM relative to constant wind, and angular velocity, all in body-local SI units.
        public static AeroWrench Evaluate(RuntimeDroneParameters p,DVector3 airVelocityAtCom,DVector3 angularVelocity)
        {
            if(!p.BodyDrag) return default;
            DVector3 force=default,torque=default; double area=0;
            if(p.DragModel=="Surfaces")
            {
                foreach(var surface in p.Surfaces)
                {
                    var r=surface.Position-p.CenterOfMass;
                    var velocity=airVelocityAtCom+DVector3.Cross(angularVelocity,r);
                    var f=SurfaceDrag(velocity,surface.Normal,p.Density,surface.Cd,surface.Area);
                    force+=f; torque+=DVector3.Cross(r,f);
                }
            }
            else
            {
                var r=p.DragPoint-p.CenterOfMass;
                var velocity=airVelocityAtCom+DVector3.Cross(angularVelocity,r);
                if(p.DragModel=="AxisApproximation") force=PhysicsMath.AxisDrag(velocity,p.Density,p.DragCd,p.DragArea);
                else if(velocity.Length>1e-12)
                {
                    area=p.ProjectedAreaMode=="AxisApproximation" ?
                        DVector3.Dot(p.ProjectedReferenceArea,new DVector3(Math.Abs(velocity.Normalized.X),
                            Math.Abs(velocity.Normalized.Y),Math.Abs(velocity.Normalized.Z))) : LookupArea(p.AreaSamples,velocity);
                    force=ProjectedDrag(velocity,p.Density,p.ProjectedCd,area);
                }
                torque=DVector3.Cross(r,force);
            }
            return new AeroWrench(force,torque,area);
        }
    }
    public sealed class RuntimeAreaSample
    {
        public readonly DVector3 Direction;
        public readonly double Area;
        public RuntimeAreaSample(DVector3 direction,double area) { Direction=direction.Normalized; Area=area; }
    }
    public sealed class RuntimeAeroSurface
    {
        public readonly string Id;
        public readonly DVector3 Position,Normal;
        public readonly double Area,Cd;
        internal RuntimeAeroSurface(AeroSurfaceProfile s)
        { Id=s.surfaceId; Position=DVector3.From(s.positionLocalM); Normal=DVector3.From(s.normalLocal).Normalized; Area=s.areaM2; Cd=s.dragCoefficient; }
    }
}
