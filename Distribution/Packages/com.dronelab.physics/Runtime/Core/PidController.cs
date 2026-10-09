using System;

namespace DroneLab.Physics
{
    [Serializable]
    public sealed class PidTerms
    {
        public double integralGain, derivativeGain, integralLimit, derivativeFilterSeconds;
        public PidTerms(double i, double d, double limit, double filter=0.04)
        { integralGain=i; derivativeGain=d; integralLimit=limit; derivativeFilterSeconds=filter; }
    }

    // P is kept separate to preserve the existing serialized pilot gains.
    // Integral is stored in output units. D acts on measurement, avoiding setpoint kick.
    public sealed class PidController
    {
        public double Integral { get; private set; }
        private double previousMeasurement, filteredDerivative;
        private bool primed;
        public void Reset() { Integral=0; previousMeasurement=filteredDerivative=0; primed=false; }
        public double Step(double target, double measurement, double dt, double p, PidTerms terms,
            double outputLimit, bool integrate=true)
        {
            if(dt<=0 || double.IsNaN(dt) || double.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            double derivative=primed ? (measurement-previousMeasurement)/dt : 0;
            previousMeasurement=measurement; primed=true;
            return StepError(target-measurement,derivative,dt,p,terms,outputLimit,integrate);
        }
        // For attitude/position loops, angular/linear velocity is already the measured derivative.
        public double StepError(double error, double measurementDerivative, double dt, double p,
            PidTerms terms, double outputLimit, bool integrate=true)
        {
            if(dt<=0 || double.IsNaN(dt) || double.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            double alpha=terms.derivativeFilterSeconds<=0 ? 1 : 1-Math.Exp(-dt/terms.derivativeFilterSeconds);
            filteredDerivative+=alpha*(measurementDerivative-filteredDerivative);
            double limit=Math.Max(0,terms.integralLimit);
            Integral=PhysicsMath.Clamp(Integral,-limit,limit);
            double delta=integrate ? terms.integralGain*error*dt : 0;
            double candidate=PhysicsMath.Clamp(Integral+delta,-limit,limit);
            double pd=p*error-terms.derivativeGain*filteredDerivative;
            // Permit unwinding, but reject integration that pushes an already limited output further.
            if(Math.Abs(pd+candidate)<=outputLimit || (pd+candidate)*delta<=0) Integral=candidate;
            return PhysicsMath.Clamp(pd+Integral,-outputLimit,outputLimit);
        }
    }
}
