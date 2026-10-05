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
            int count=p.DragModel=="Surfaces" ? p.Surfaces.Count : 1;
            for(int i=0;i<count;i++)
            {
                var point=p.DragModel=="Surfaces" ? p.Surfaces[i].Position : p.DragPoint;
                var velocity=airVelocityAtCom+DVector3.Cross(angularVelocity,point-p.CenterOfMass);
                var w=EvaluatePoint(p,velocity,p.Density,i); force+=w.Force; torque+=w.Torque; area=w.ProjectedArea;
            }
            return new AeroWrench(force,torque,area);
        }
        // Point velocity already relative to wind at this point, in body-local axes.
        public static AeroWrench EvaluatePoint(RuntimeDroneParameters p,DVector3 airVelocity,double density,int surfaceIndex=0)
        {
            if(!p.BodyDrag) return default;
            EnvironmentMath.Finite(airVelocity); EnvironmentMath.Finite(density);
            if(density<=0) throw new ArgumentOutOfRangeException(nameof(density));
            DVector3 force,point; double area=0;
            if(p.DragModel=="Surfaces")
            {
                var surface=p.Surfaces[surfaceIndex]; point=surface.Position;
                force=SurfaceDrag(airVelocity,surface.Normal,density,surface.Cd,surface.Area);
            }
            else
            {
                point=p.DragPoint;
                if(p.DragModel=="AxisApproximation") force=PhysicsMath.AxisDrag(airVelocity,density,p.DragCd,p.DragArea);
                else if(airVelocity.Length>1e-12)
                {
                    area=p.ProjectedAreaMode=="AxisApproximation" ?
                        DVector3.Dot(p.ProjectedReferenceArea,new DVector3(Math.Abs(airVelocity.Normalized.X),
                            Math.Abs(airVelocity.Normalized.Y),Math.Abs(airVelocity.Normalized.Z))) : LookupArea(p.AreaSamples,airVelocity);
                    force=ProjectedDrag(airVelocity,density,p.ProjectedCd,area);
                }
                else force=default;
            }
            return new AeroWrench(force,DVector3.Cross(point-p.CenterOfMass,force),area);
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
