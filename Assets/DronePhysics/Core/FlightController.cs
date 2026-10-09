namespace DroneLab.Physics
{
    // Cascaded attitude -> rate -> acceleration and height -> vertical speed -> acceleration.
    // Geometry, sensor sampling, inertia multiplication and actuator commands remain in adapters.
    public sealed class FlightController
    {
        private readonly PidController[] rates={new PidController(),new PidController(),new PidController()};
        private readonly PidController[] attitudes={new PidController(),new PidController(),new PidController()};
        private readonly PidController height=new PidController(), verticalSpeed=new PidController();
        public void Reset()
        {
            foreach(var pid in rates) pid.Reset();
            foreach(var pid in attitudes) pid.Reset();
            height.Reset(); verticalSpeed.Reset();
        }
        public DVector3 RateAcceleration(DVector3 desired, DVector3 measured, double dt,
            double p, PidTerms terms, double limit, bool integrate)
            => new DVector3(rates[0].Step(desired.X,measured.X,dt,p,terms,limit,integrate),
                rates[1].Step(desired.Y,measured.Y,dt,p,terms,limit,integrate),
                rates[2].Step(desired.Z,measured.Z,dt,p,terms,limit,integrate));
        public DVector3 AttitudeRate(DVector3 error, DVector3 measuredRate, double dt,
            double p, PidTerms terms, double limit, bool integrate)
            => new DVector3(attitudes[0].StepError(error.X,measuredRate.X,dt,p,terms,limit,integrate),
                attitudes[1].StepError(error.Y,measuredRate.Y,dt,p,terms,limit,integrate),
                attitudes[2].StepError(error.Z,measuredRate.Z,dt,p,terms,limit,integrate));
        public double ClimbAcceleration(double heightError, double velocity, double dt,
            double heightP, PidTerms heightTerms, double velocityP, PidTerms velocityTerms,
            double climbLimit, double accelerationLimit, bool integrate)
        {
            double desired=height.StepError(heightError,velocity,dt,heightP,heightTerms,climbLimit,integrate);
            return verticalSpeed.Step(desired,velocity,dt,velocityP,velocityTerms,accelerationLimit,integrate);
        }
    }
}
