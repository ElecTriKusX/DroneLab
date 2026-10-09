using System;

namespace DroneLab.Physics
{
    public interface IWindProvider
    {
        // World axes, metres, seconds, m/s. Must not depend on query order or change simulation state.
        DVector3 Sample(DVector3 worldPositionM,double timeS);
    }
    public interface IAirProvider
    {
        // Return false before a valid snapshot exists; consumer uses its JSON environment.
        // Read-only, order-independent SI query. No reset, stepping or network calls.
        bool TrySampleAir(DVector3 worldPositionM,double timeS,out AirSample air);
    }
    public interface IWeatherProvider
    {
        bool TrySampleWeather(out WeatherSample weather);
    }
    public readonly struct WeatherSample
    {
        public readonly string Precipitation;
        public readonly double IntensityMmPerHour;
        public WeatherSample(string precipitation,double intensityMmPerHour)
        {
            EnvironmentMath.Finite(intensityMmPerHour);
            if(precipitation!="None" && precipitation!="Rain" && precipitation!="Snow" && precipitation!="Hail")
                throw new ArgumentException("Unknown precipitation kind.",nameof(precipitation));
            if(intensityMmPerHour<0 || intensityMmPerHour>200 || (precipitation=="None")!=(intensityMmPerHour==0))
                throw new ArgumentOutOfRangeException(nameof(intensityMmPerHour));
            Precipitation=precipitation; IntensityMmPerHour=intensityMmPerHour;
        }
    }
    public readonly struct AirSample
    {
        public readonly double Density,TemperatureK,PressurePa,AltitudeM;
        public AirSample(double density,double temperature,double pressure,double altitude)
        { Density=density; TemperatureK=temperature; PressurePa=pressure; AltitudeM=altitude; }
    }
    public static class Atmosphere
    {
        public const double GasConstant=287.05287,Lapse=.0065,Gravity=9.80665;
        public static AirSample Troposphere(double altitudeM,double seaLevelTemperatureK=288.15,double seaLevelPressurePa=101325)
        {
            EnvironmentMath.Finite(altitudeM); EnvironmentMath.Finite(seaLevelTemperatureK); EnvironmentMath.Finite(seaLevelPressurePa);
            if(altitudeM< -500 || altitudeM>11000 || seaLevelTemperatureK<200 || seaLevelTemperatureK>330 || seaLevelPressurePa<1000 || seaLevelPressurePa>200000)
                throw new ArgumentOutOfRangeException(nameof(altitudeM),"Troposphere: -500..11000 m, sea-level T=200..330 K, P=1000..200000 Pa.");
            double t=seaLevelTemperatureK-Lapse*altitudeM;
            double p=seaLevelPressurePa*Math.Pow(t/seaLevelTemperatureK,Gravity/(GasConstant*Lapse));
            return new AirSample(p/(GasConstant*t),t,p,altitudeM);
        }
    }
    // Dry hydrostatic column anchored to LOCAL temperature/pressure at a surveyed scene altitude.
    // Unlike Troposphere's sea-level inputs, reference measurements here are at ReferenceAltitudeM.
    public sealed class AtmosphereColumn : IAirProvider
    {
        public readonly double ReferenceTemperatureK,ReferencePressurePa,ReferenceAltitudeM,ReferenceWorldY;
        public AtmosphereColumn(double temperatureK,double pressurePa,double altitudeM=0,double worldY=0)
        {
            EnvironmentMath.Finite(temperatureK); EnvironmentMath.Finite(pressurePa);
            EnvironmentMath.Finite(altitudeM); EnvironmentMath.Finite(worldY);
            if(temperatureK<200 || temperatureK>330 || pressurePa<1000 || pressurePa>200000 || altitudeM< -500 || altitudeM>11000)
                throw new ArgumentOutOfRangeException("Reference air requires 200..330 K, 1000..200000 Pa, -500..11000 m.");
            ReferenceTemperatureK=temperatureK; ReferencePressurePa=pressurePa;
            ReferenceAltitudeM=altitudeM; ReferenceWorldY=worldY;
        }
        public bool TrySampleAir(DVector3 worldPositionM,double timeS,out AirSample air)
        {
            EnvironmentMath.Finite(worldPositionM); EnvironmentMath.Finite(timeS);
            if(timeS<0) throw new ArgumentOutOfRangeException(nameof(timeS));
            double height=worldPositionM.Y-ReferenceWorldY,altitude=ReferenceAltitudeM+height;
            if(altitude< -500 || altitude>11000) throw new ArgumentOutOfRangeException(nameof(worldPositionM));
            double t=ReferenceTemperatureK-Atmosphere.Lapse*height;
            double p=ReferencePressurePa*Math.Pow(t/ReferenceTemperatureK,Atmosphere.Gravity/(Atmosphere.GasConstant*Atmosphere.Lapse));
            air=new AirSample(p/(Atmosphere.GasConstant*t),t,p,altitude);
            LiveAirValidation.Validate(air); return true;
        }
    }
    public static class LiveAirValidation
    {
        public static void RequireCompatible(RuntimeDroneParameters parameters)
        {
            if(parameters==null) throw new ArgumentNullException(nameof(parameters));
            foreach(var rotor in parameters.Rotors)
                if(rotor.Performance.Model!="CtCq" && rotor.Performance.Model!="PerformanceMap")
                    throw new ArgumentException("Live air requires CtCq or PerformanceMap. Measured rotor data cannot be silently scaled with density.");
        }
        public static void Validate(AirSample air)
        {
            EnvironmentMath.Finite(air.Density); EnvironmentMath.Finite(air.TemperatureK);
            EnvironmentMath.Finite(air.PressurePa); EnvironmentMath.Finite(air.AltitudeM);
            if(air.Density<=0 || air.Density>100 || air.TemperatureK<100 || air.TemperatureK>500 ||
                air.PressurePa<=0 || air.PressurePa>2000000 || air.AltitudeM< -500 || air.AltitudeM>11000)
                throw new ArgumentOutOfRangeException(nameof(air),"Live air sample outside runtime bounds.");
        }
    }
    public static class EnvironmentMath
    {
        public static void Finite(double x)
        { if(double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x),"Environment query must be finite."); }
        public static void Finite(DVector3 v) { Finite(v.X); Finite(v.Y); Finite(v.Z); }
        public static DVector3 Limit(DVector3 v,double maximum)
        { return v.Length>maximum ? v.Normalized*maximum : v; }
    }
    public sealed class LinearWindField : IWindProvider
    {
        private readonly DVector3 mean,origin,x,y,z;
        private readonly double maxDelta;
        public LinearWindField(DVector3 mean,DVector3 origin,DVector3 gradientX,DVector3 gradientY,DVector3 gradientZ,double maxDeltaMps)
        {
            EnvironmentMath.Finite(mean); EnvironmentMath.Finite(origin); EnvironmentMath.Finite(gradientX);
            EnvironmentMath.Finite(gradientY); EnvironmentMath.Finite(gradientZ); EnvironmentMath.Finite(maxDeltaMps);
            if(maxDeltaMps<0) throw new ArgumentOutOfRangeException(nameof(maxDeltaMps));
            this.mean=mean; this.origin=origin; x=gradientX; y=gradientY; z=gradientZ; maxDelta=maxDeltaMps;
        }
        public DVector3 Sample(DVector3 position,double time)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            var d=position-origin;
            return mean+EnvironmentMath.Limit(x*d.X+y*d.Y+z*d.Z,maxDelta);
        }
    }
    // Stateless environment: legacy bounded Fourier disturbances or optional Dryden frozen-line spectrum.
    public sealed class RuntimeEnvironment : IWindProvider
    {
        public readonly string DensityMode,WindMode;
        public readonly string Precipitation;
        public readonly double PrecipitationIntensityMmPerHour;
        public readonly bool WindEnabled,GustEnabled;
        public readonly DVector3 MeanWind;
        public readonly double ReferenceAltitudeM,IntensityMps,TimeScaleS,SpatialScaleM;
        public readonly DrydenFrozenField Dryden;
        private readonly double density,temperature,pressure;
        private readonly DVector3[] waves,amplitudes;
        private readonly double[] phases;
        private readonly DVector3 advection;
        internal RuntimeEnvironment(EnvironmentProfile p,bool windEnabled)
        {
            DensityMode=p.airDensityMode; WindMode=p.windMode; WindEnabled=windEnabled;
            Precipitation=p.weather?.precipitation ?? "None";
            PrecipitationIntensityMmPerHour=p.weather?.intensityMmPerHour ?? 0;
            MeanWind=p.windMode=="None" ? default : DVector3.From(p.windVelocityWorldMps);
            GustEnabled=p.gustEnabled; ReferenceAltitudeM=p.altitudeM;
            density=p.airDensityKgM3; temperature=p.temperatureK; pressure=p.pressurePa;
            IntensityMps=p.gustIntensityMps; TimeScaleS=p.gustTimeScaleS;
            if(WindMode=="DrydenFrozen") Dryden=new DrydenFrozenField(p.dryden,p.turbulenceSeed,MeanWind);
            if(WindMode!="Turbulence") return;
            SpatialScaleM=Math.Max(1,MeanWind.Length)*TimeScaleS;
            advection=MeanWind.Length>=1 ? MeanWind : new DVector3(1,0,0);
            waves=new DVector3[8]; amplitudes=new DVector3[8]; phases=new double[8];
            uint state=unchecked((uint)p.turbulenceSeed)^0x9e3779b9u;
            if(state==0) state=1;
            for(int i=0;i<8;i++)
            {
                var direction=new DVector3(Next(ref state)*2-1,Next(ref state)*2-1,Next(ref state)*2-1).Normalized;
                var other=new DVector3(Next(ref state)*2-1,Next(ref state)*2-1,Next(ref state)*2-1);
                var transverse=DVector3.Cross(direction,other).Normalized;
                if(transverse.Length<.5) transverse=DVector3.Cross(direction,Math.Abs(direction.X)<.9 ? new DVector3(1,0,0) : new DVector3(0,1,0)).Normalized;
                waves[i]=direction*(2*Math.PI*(.5+1.5*Next(ref state))/SpatialScaleM);
                amplitudes[i]=transverse*(IntensityMps/8); phases[i]=Next(ref state)*2*Math.PI;
            }
        }
        private static double Next(ref uint state)
        { unchecked { state^=state<<13; state^=state>>17; state^=state<<5; } return state/(double)uint.MaxValue; }
        public double? WindSamplingRatio(DVector3 pointVelocity,double dt)
        {
            if(!WindEnabled || Dryden==null) return null;
            double ratio=Dryden.SamplingRatio(pointVelocity,dt);
            return GustEnabled && IntensityMps>0 ? Math.Max(ratio,2*dt/TimeScaleS) : ratio;
        }
        public AirSample SampleAir(double heightChangeM)
        {
            EnvironmentMath.Finite(heightChangeM);
            double altitude=ReferenceAltitudeM+heightChangeM;
            return DensityMode=="StandardAtmosphere" ? Atmosphere.Troposphere(altitude,temperature>0 ? temperature : 288.15,pressure>0 ? pressure : 101325) :
                new AirSample(density,temperature,pressure,altitude);
        }
        public DVector3 Sample(DVector3 position,double time)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            if(time<0) throw new ArgumentOutOfRangeException(nameof(time));
            if(!WindEnabled || WindMode=="None") return default;
            if(WindMode=="CustomField") throw new InvalidOperationException("CustomField requires an IWindProvider supplied by the Unity adapter.");
            var wind=MeanWind;
            if(Dryden!=null) wind=Dryden.Sample(position,time);
            if(WindMode=="Turbulence")
                for(int i=0;i<waves.Length;i++) wind+=amplitudes[i]*Math.Sin(DVector3.Dot(waves[i],position-advection*time)+phases[i]);
            if(GustEnabled)
            {
                var axis=MeanWind.Length>1e-12 ? MeanWind.Normalized : new DVector3(1,0,0);
                wind+=axis*(.5*IntensityMps*(1-Math.Cos(2*Math.PI*time/TimeScaleS)));
            }
            return wind;
        }
    }
}
