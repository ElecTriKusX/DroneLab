namespace DroneLab.Physics
{
    public static class PilotMath
    {
        // Proportional angular-rate loop used by the development pilot.
        // This requests angular acceleration, not a persistent torque for a held key.
        public static DVector3 RateAcceleration(DVector3 desiredRate, DVector3 measuredRate, double gain)
            => (desiredRate - measuredRate) * gain;
    }
}
