using System;

namespace DroneLab.Physics
{
    public static class PilotMath
    {
        public static double SmoothCommand(double previous, double target, double tau, double dt)
            => tau<=0 ? target : target+(previous-target)*Math.Exp(-dt/tau);
        // Legacy proportional angular-rate reference, retained for stage-1 regression tests.
        // This requests angular acceleration, not a persistent torque for a held key.
        public static DVector3 RateAcceleration(DVector3 desiredRate, DVector3 measuredRate, double gain)
            => (desiredRate - measuredRate) * gain;
    }
}
