using System;

namespace DroneLab.Physics
{
    // Version 1: body-relative stick commands; throttle is a fraction of maximum thrust, not RPM.
    public readonly struct FlightInput
    {
        public const string ContractVersion="1.0.0";
        public readonly double Roll, Pitch, Yaw, Climb, Throttle;
        public FlightInput(double roll, double pitch, double yaw, double climb, double throttle)
        {
            Roll=Axis(roll); Pitch=Axis(pitch); Yaw=Axis(yaw); Climb=Axis(climb);
            Throttle=PhysicsMath.Clamp(Finite(throttle),0,1);
        }
        private static double Finite(double x) => double.IsNaN(x) || double.IsInfinity(x) ? 0 : x;
        private static double Axis(double x) => PhysicsMath.Clamp(Finite(x),-1,1);
    }
}
