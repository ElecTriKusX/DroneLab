using System;

namespace DroneLab.Physics
{
    public readonly struct DVector3
    {
        public readonly double X, Y, Z;
        public DVector3(double x, double y, double z) { X=x; Y=y; Z=z; }
        public static DVector3 From(double[] a) => new DVector3(a[0], a[1], a[2]);
        public double Length => Math.Sqrt(X*X+Y*Y+Z*Z);
        public DVector3 Normalized => Length > 1e-12 ? this / Length : default;
        public static DVector3 operator +(DVector3 a,DVector3 b) => new DVector3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static DVector3 operator -(DVector3 a,DVector3 b) => new DVector3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static DVector3 operator *(DVector3 a,double k) => new DVector3(a.X*k,a.Y*k,a.Z*k);
        public static DVector3 operator /(DVector3 a,double k) => a*(1/k);
        public static double Dot(DVector3 a,DVector3 b) => a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        public static DVector3 Cross(DVector3 a,DVector3 b) => new DVector3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    }

    public static class PhysicsMath
    {
        public static double Clamp(double x,double lo,double hi) => Math.Max(lo, Math.Min(hi,x));
        public static double RpmToOmega(double rpm) => rpm * (2*Math.PI/60);
        public static double OmegaToRpm(double omega) => omega * (60/(2*Math.PI));
        public static double MotorStep(double omega,double target,double tau,double dt)
        {
            if (dt < 0 || tau < 0 || omega < 0 || target < 0) throw new ArgumentOutOfRangeException();
            if (dt == 0) return omega;
            return tau == 0 ? target : target+(omega-target)*Math.Exp(-dt/tau);
        }
        public static double Thrust(double omega,double kT) => kT*omega*omega;
        public static double Torque(double omega,double kQ) => kQ*omega*omega;
        public static double CtThrust(double rpm,double ct,double rho,double diameter)
            => ct*rho*(rpm/60)*(rpm/60)*Math.Pow(diameter,4);
        public static double CqTorque(double rpm,double cq,double rho,double diameter)
            => cq*rho*(rpm/60)*(rpm/60)*Math.Pow(diameter,5);
        // Axis-separated empirical drag; guaranteed nonpositive F dot V.
        public static DVector3 AxisDrag(DVector3 airVelocity,double rho,DVector3 cd,DVector3 area)
            => new DVector3(-0.5*rho*cd.X*area.X*airVelocity.X*Math.Abs(airVelocity.X),
                -0.5*rho*cd.Y*area.Y*airVelocity.Y*Math.Abs(airVelocity.Y),
                -0.5*rho*cd.Z*area.Z*airVelocity.Z*Math.Abs(airVelocity.Z));
        public static DVector3 BoxInertia(double mass,DVector3 size)
            => new DVector3(mass*(size.Y*size.Y+size.Z*size.Z)/12,
                mass*(size.X*size.X+size.Z*size.Z)/12,mass*(size.X*size.X+size.Y*size.Y)/12);
        public static double InducedHoverVelocity(double thrust,double rho,double diameter)
            => Math.Sqrt(Math.Max(0,thrust)/(2*rho*Math.PI*diameter*diameter/4));
    }
}
