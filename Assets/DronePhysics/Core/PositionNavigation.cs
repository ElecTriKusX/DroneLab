using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public interface INavigationFeedback
    {
        bool TryHorizontal(out DVector3 position, out DVector3 velocity);
    }

    /// <summary>Horizontal position -> velocity -> acceleration. Output enters the existing attitude loop.</summary>
    public sealed class PositionController
    {
        private DVector3 trim;
        public void Reset() => trim = default;
        public DVector3 Acceleration(DVector3 target, DVector3 position, DVector3 velocity,
            double dt, double speed, double accelerationLimit, bool integrate)
        {
            if (!(dt > 0) || double.IsInfinity(dt) || !(speed > 0) || !(accelerationLimit > 0)) throw new ArgumentOutOfRangeException();
            var error = new DVector3(target.X-position.X, 0, target.Z-position.Z);
            var desiredVelocity = Limit(error * .45, speed);
            var velocityError = desiredVelocity - new DVector3(velocity.X, 0, velocity.Z);
            var requested = velocityError * 1.1 + trim;
            if (integrate && requested.Length < accelerationLimit)
                trim = Limit(trim + velocityError * (.15 * dt), accelerationLimit * .6);
            return Limit(velocityError * 1.1 + trim, accelerationLimit);
        }
        private static DVector3 Limit(DVector3 value, double limit) => value.Length > limit ? value * (limit / value.Length) : value;
    }

    /// <summary>Constant-velocity alpha/beta estimate for navigation. Raw GPS readings remain available separately.</summary>
    public sealed class GpsNavigationFilter
    {
        public DVector3 Position { get; private set; }
        public DVector3 Velocity { get; private set; }
        public double CapturedAt { get; private set; }
        public bool Ready { get; private set; }
        public void Reset() { Ready=false; Position=Velocity=default; }
        public void Feed(DVector3 measured,double time,double noiseStd)
        {
            if(!Ready || time<=CapturedAt) { Position=measured; Velocity=default; CapturedAt=time; Ready=true; return; }
            double dt=time-CapturedAt;
            var innovation=measured-(Position+Velocity*dt);
            double alpha=noiseStd<=0 ? 1 : 1-Math.Exp(-dt/.12);
            double beta=noiseStd<=0 ? 1 : Math.Min(.2,alpha*alpha*.3);
            Position=Position+Velocity*dt+innovation*alpha;
            Velocity+=innovation*(beta/dt); CapturedAt=time;
        }
        public DVector3 Predict(double time) => Position+Velocity*Math.Max(0,Math.Min(2,time-CapturedAt));
    }

    public sealed class WaypointMission
    {
        private readonly List<DVector3> points = new List<DVector3>();
        private double dwell;
        public double ArrivalRadiusM { get; set; }=.8;
        public double ArrivalSpeedMps { get; set; }=.7;
        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public int Index { get; private set; }
        public int Count => points.Count;
        public DVector3 Target => points[Index];
        public void Start(IReadOnlyList<DVector3> route)
        {
            if (!Finite(ArrivalRadiusM) || ArrivalRadiusM<=0 || !Finite(ArrivalSpeedMps) || ArrivalSpeedMps<=0) throw new ArgumentOutOfRangeException();
            if (route == null || route.Count == 0 || route.Count > 64) throw new ArgumentException("Route needs 1..64 points.");
            foreach (var p in route) if (!Finite(p.X) || !Finite(p.Y) || !Finite(p.Z)) throw new ArgumentException("Non-finite waypoint.");
            points.Clear(); for (int i=0;i<route.Count;i++) points.Add(route[i]);
            Index=0; dwell=0; Active=true; Completed=false;
        }
        public void Stop() { Active=false; Completed=false; dwell=0; }
        public bool Step(DVector3 position, double speed, double dt, bool available)
        {
            if (!(dt > 0) || !Finite(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (!Active) return false;
            if (!available) { Stop(); return false; }
            bool arrived=(Target-position).Length <= ArrivalRadiusM && speed <= ArrivalSpeedMps;
            dwell=arrived ? dwell+dt : 0;
            if (dwell < .75) return false;
            dwell=0;
            if (Index+1 < Count) Index++; else { Active=false; Completed=true; }
            return true;
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }

    /// <summary>One row per interval in simulation time; restarting the clock starts a new cadence.</summary>
    public sealed class RecordingCadence
    {
        private double previous=-1, next;
        public void Reset() { previous=-1; next=0; }
        public bool Due(double time, double interval)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0 || !(interval > 0) || double.IsInfinity(interval)) throw new ArgumentOutOfRangeException();
            if (time < previous) next=time;
            previous=time;
            if (time+1e-9 < next) return false;
            next=time+interval; return true;
        }
    }
}
