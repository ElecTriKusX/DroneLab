using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    internal static class DroneVisualBindings
    {
        public static IEnumerable<string> Paths(JToken token)=>token is JArray array ? array.Values<string>() : token?.Type==JTokenType.String ? new[]{(string)token} : Array.Empty<string>();
        public static void Validate(JObject bindings)
        {
            foreach(var property in bindings.Properties()){
                var value=property.Value;
                if(value.Type!=JTokenType.String && (value is not JArray array || array.Any(p=>p.Type!=JTokenType.String)))throw new ArgumentException("Привязка ротора должна содержать путь узла или список путей: "+property.Name);
                var paths=Paths(value).ToArray();
                if(paths.Any(string.IsNullOrWhiteSpace) || paths.Distinct(StringComparer.Ordinal).Count()!=paths.Length)throw new ArgumentException("Пустой или повторяющийся путь узла: "+property.Name);
                foreach(string path in paths)if(path.StartsWith("@/",StringComparison.Ordinal) && path.Substring(2).Split('/').Any(s=>!int.TryParse(s,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int index) || index<0))throw new ArgumentException("Некорректный путь узла: "+property.Name);
            }
        }
        // CW as seen from the positive thrust axis towards the hub; opposite to reaction torque.
        public static double AdvancePhase(double phase,double omega,double deltaTime,string spin)
        {
            if(double.IsNaN(omega) || double.IsInfinity(omega) || deltaTime<0)return phase;
            return (phase+(spin=="CW"?1:-1)*omega*deltaTime*180/Math.PI)%360;
        }
    }
}
