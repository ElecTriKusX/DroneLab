using System;
using DroneLab.Physics;

namespace DroneLab.Weather
{
    // Observed, applied Enviro state. Wetness/snow/turbulence are visual ratios, not rainfall or m/s.
    public sealed class WeatherSnapshot
    {
        public readonly string WeatherName;
        public readonly DVector3 WindWorldMps;
        public readonly double TemperatureK, Wetness, SnowCover, VisualTurbulence, CapturedAtS;
        public readonly AtmosphereColumn AirColumn;
        public readonly WeatherSample Weather;
        public WeatherSnapshot(string name, DVector3 wind, double temperatureK, double wetness,
            double snowCover, double visualTurbulence, double capturedAtS, AtmosphereColumn airColumn = null,
            WeatherSample? weather = null)
        {
            EnvironmentMath.Finite(wind); EnvironmentMath.Finite(temperatureK);
            EnvironmentMath.Finite(capturedAtS);
            if (temperatureK <= 0 || capturedAtS < 0) throw new ArgumentOutOfRangeException();
            WindConversion.Ratio(wetness); WindConversion.Ratio(snowCover); WindConversion.Ratio(visualTurbulence);
            if(airColumn!=null && Math.Abs(airColumn.ReferenceTemperatureK-temperatureK)>1e-8)
                throw new ArgumentException("Snapshot and air column must use the same reference temperature.");
            WeatherName = name ?? ""; WindWorldMps = wind; TemperatureK = temperatureK;
            Wetness = wetness; SnowCover = snowCover; VisualTurbulence = visualTurbulence; CapturedAtS = capturedAtS;
            AirColumn=airColumn; var metadata=weather ?? new WeatherSample("None",0);
            Weather=new WeatherSample(metadata.Precipitation,metadata.IntensityMmPerHour);
        }
    }
    public sealed class WeatherState : IWindProvider, IAirProvider, IWeatherProvider
    {
        public WeatherSnapshot Current { get; private set; }
        public void Publish(WeatherSnapshot snapshot)
        {
            if(snapshot==null) throw new ArgumentNullException(nameof(snapshot));
            if(snapshot.AirColumn==null) throw new ArgumentException("A complete weather state requires an air column.");
            Current=snapshot;
        }
        public DVector3 Sample(DVector3 position,double time)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            if(time<0) throw new ArgumentOutOfRangeException(nameof(time));
            return Current?.WindWorldMps ?? default;
        }
        public bool TrySampleAir(DVector3 position,double time,out AirSample air)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            if(time<0) throw new ArgumentOutOfRangeException(nameof(time));
            var snapshot=Current;
            if(snapshot!=null) return snapshot.AirColumn.TrySampleAir(position,time,out air);
            air=default; return false;
        }
        public bool TrySampleWeather(out WeatherSample weather)
        {
            weather=Current?.Weather ?? new WeatherSample("None",0); return Current!=null;
        }
    }

    public readonly struct EnviroWindSettings
    {
        public readonly double Strength, DirectionX, DirectionZ;
        public EnviroWindSettings(double strength, double x, double z)
        { Strength = strength; DirectionX = x; DirectionZ = z; }
    }

    public static class WindConversion
    {
        public static void Ratio(double value)
        {
            EnvironmentMath.Finite(value);
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
        }
        private static void Scale(double fullStrengthMps)
        {
            EnvironmentMath.Finite(fullStrengthMps);
            if (fullStrengthMps <= 0) throw new ArgumentOutOfRangeException(nameof(fullStrengthMps));
        }
        public static double CelsiusToKelvin(double celsius)
        {
            EnvironmentMath.Finite(celsius);
            double kelvin = celsius + 273.15;
            EnvironmentMath.Finite(kelvin);
            if (kelvin <= 0) throw new ArgumentOutOfRangeException(nameof(celsius));
            return kelvin;
        }
        public static DVector3 FromEnviro(double strength, double directionX, double directionZ, double fullStrengthMps)
        {
            Ratio(strength); Scale(fullStrengthMps);
            EnvironmentMath.Finite(directionX); EnvironmentMath.Finite(directionZ);
            if (Math.Abs(directionX) > 1 || Math.Abs(directionZ) > 1) throw new ArgumentOutOfRangeException();
            // EnviroEnvironmentModule.UpdateWindZone uses NEGATIVE X/Y; its Y is world Z.
            var direction = new DVector3(-directionX, 0, -directionZ);
            return direction.Normalized * (strength * fullStrengthMps);
        }
        public static DVector3 FromMeteorological(double speedMps, double fromDegrees, double northYawDegrees = 0)
        {
            EnvironmentMath.Finite(speedMps); EnvironmentMath.Finite(fromDegrees); EnvironmentMath.Finite(northYawDegrees);
            if (speedMps < 0) throw new ArgumentOutOfRangeException(nameof(speedMps));
            // Bearing is clockwise FROM scene north; +Z north and +X east at northYaw=0.
            double angle = ((fromDegrees % 360 + northYawDegrees % 360) % 360) * Math.PI / 180;
            return new DVector3(-Math.Sin(angle) * speedMps, 0, -Math.Cos(angle) * speedMps);
        }
        public static EnviroWindSettings ToEnviro(DVector3 velocity, double fullStrengthMps)
        {
            EnvironmentMath.Finite(velocity); Scale(fullStrengthMps);
            double speed = velocity.Length;
            EnvironmentMath.Finite(speed);
            if (Math.Abs(velocity.Y) > 1e-10 || speed > fullStrengthMps * (1 + 1e-12))
                throw new ArgumentOutOfRangeException(nameof(velocity), "Horizontal wind must fit the configured Enviro scale.");
            if (speed < 1e-12) return new EnviroWindSettings(0, 0, 1);
            return new EnviroWindSettings(Math.Min(1, speed / fullStrengthMps), -velocity.X / speed, -velocity.Z / speed);
        }
    }
}
