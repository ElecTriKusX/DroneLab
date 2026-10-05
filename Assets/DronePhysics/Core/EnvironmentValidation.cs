using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    internal static class EnvironmentValidation
    {
        public static IEnumerable<ValidationIssue> Check(EnvironmentProfile e,JObject json,DroneProfile drone)
        {
            if(drone.powerSystem.thermalEnabled && e.airDensityMode=="Constant" &&
                (json["temperatureK"]==null || e.temperatureK<100 || e.temperatureK>500))
                yield return new ValidationIssue("environment.temperatureK","Thermal mode requires explicit constant ambient temperature 100..500 K.");
            if(e.weather!=null)
            {
                if((e.weather.precipitation=="None")!=(e.weather.intensityMmPerHour==0))
                    yield return new ValidationIssue("environment.weather","None requires zero intensity; Rain/Snow/Hail require positive intensity.");
            }
            if(e.airDensityMode=="StandardAtmosphere")
            {
                if(e.altitudeM< -500 || e.altitudeM>11000 || (e.temperatureK!=0 && (e.temperatureK<200 || e.temperatureK>330)) ||
                    (e.pressurePa!=0 && (e.pressurePa<1000 || e.pressurePa>200000)))
                    yield return new ValidationIssue("environment","Troposphere supports -500..11000 m, sea-level temperature 200..330 K and pressure 1000..200000 Pa.");
                for(int i=0;i<drone.rotors.Length;i++)
                    if(drone.rotors[i].performance.model=="OmegaSquared" || drone.rotors[i].performance.model=="RpmTable")
                        yield return new ValidationIssue("rotors["+i+"].performance","Variable atmosphere requires CtCq or PerformanceMap; measured kT/kQ and RPM tables remain tied to reference density.");
            }
            bool fluctuations=e.gustEnabled || e.windMode=="Turbulence" || e.windMode=="Gust";
            if(e.windMode=="Gust" && !e.gustEnabled)
                yield return new ValidationIssue("environment.gustEnabled","Gust mode requires gustEnabled=true.");
            if((e.windMode=="None" || e.windMode=="CustomField") && e.gustEnabled)
                yield return new ValidationIssue("environment.gustEnabled","Gust overlay requires Constant, Gust or Turbulence mode; CustomField supplies its own fluctuations.");
            if(fluctuations)
            {
                if(json["gustIntensityMps"]==null || json["gustTimeScaleS"]==null)
                    yield return new ValidationIssue("environment","Fluctuations require explicit gustIntensityMps and gustTimeScaleS.");
                if(e.gustTimeScaleS<.01 || e.gustTimeScaleS>1e6 || e.gustIntensityMps>100)
                    yield return new ValidationIssue("environment","Fluctuations require timescale 0.01..1e6 s and intensity 0..100 m/s.");
            }
            if(e.windMode=="Turbulence" && json["turbulenceSeed"]==null)
                yield return new ValidationIssue("environment.turbulenceSeed","Explicit seed required for reproducible turbulence.");
        }
    }
}
