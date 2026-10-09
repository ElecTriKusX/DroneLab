using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>UI bounds come from the accepted physics contract, not an independent schema.</summary>
    public static class DroneParameterSchema
    {
        private static JObject schema, help, examples;
        public static JObject Schema => schema ??= JObject.Parse(DroneProfileLibrary.Resource("drone-profile.schema"));
        public static JObject Help => help ??= JObject.Parse(Resources.Load<TextAsset>("DroneLab/DroneParameters").text);
        public static JObject Examples => examples ??= JObject.Parse(Resources.Load<TextAsset>("DroneLab/DroneParameterDefaults").text);
        public static JObject Resolve(JToken rule) => rule?["$ref"] == null ? (JObject)rule : (JObject)Schema["$defs"][(string)rule["$ref"].ToString().Split('/').Last()];
        public static string Definition(JToken rule) => rule?["$ref"]?.ToString().Split('/').Last() ?? "";
        public static JObject ObjectRule(string type) => (JObject)Schema["$defs"][type];
        public static string Name(string key) => key == null ? "Выберите модель" : (string)Help[key]?["name"] ?? key;
        public static string Unit(string key) => key == null ? "" : (string)Help[key]?["unit"] ?? "";
        public static JToken ComponentRule(string key,JToken rule,string path)
        {
            var result=(JObject)Resolve(rule).DeepClone();
            if(key=="dimensionsM" || key=="principalMomentsKgM2" || (key=="referenceAreaM2" && !path.Contains("projectedArea")))result["exclusiveMinimum"]=0;
            if(key=="dragCd" || (key=="referenceAreaM2" && path.Contains("projectedArea")))result["minimum"]=0;
            if(key=="thrustAxisLocal" || key=="normalLocal" || key=="directionLocal" || key=="principalAxesRotationXyzw"){result["minimum"]=-1;result["maximum"]=1;}
            return result;
        }
        public static JToken Default(JToken unresolved,string key = null)
        {
            string definition = Definition(unresolved);
            if (definition.Length > 0 && Examples["$types"]?[definition] != null) return Examples["$types"][definition].DeepClone();
            if (key != null && Examples[key] != null) return Examples[key].DeepClone();
            var rule = Resolve(unresolved); string type = (string)rule["type"];
            if (rule["enum"] is JArray choices) return choices[0].DeepClone();
            if (type == "object") {
                var result = new JObject();
                foreach (var p in ((JObject)rule["properties"]).Properties()) if (((JArray)rule["required"]).Any(x=>(string)x == p.Name)) result[p.Name] = Default(p.Value,p.Name);
                return result;
            }
            if (type == "array") {
                var result = new JArray(); for(int i=0;i<((int?)rule["minItems"]??0);i++) result.Add(Default(rule["items"]));
                if (key == "principalAxesRotationXyzw" && result.Count == 4) result[3] = 1.0;
                return result;
            }
            if (type == "boolean") return new JValue(false);
            if (type == "string") return new JValue("Новый элемент");
            double value = Math.Max((double?)rule["minimum"] ?? 0, (double?)rule["exclusiveMinimum"] != null ? 1 : 0);
            return type == "integer" ? new JValue((int)value) : new JValue(value);
        }
        public static string Range(JToken unresolved)
        {
            var rule = Resolve(unresolved);
            if (rule["enum"] is JArray choices) return string.Join(" / ",choices.Select(x=>Name((string)x)));
            if ((string)rule["type"] == "boolean") return "Включено / выключено";
            if ((string)rule["type"] == "object") return "Параметры выбранной физической модели";
            if ((string)rule["type"] == "string") return (int?)rule["minLength"]>0 ? "Непустой текст" : "Свободный текст";
            if ((string)rule["type"] == "array") return rule["maxItems"] != null ? rule["minItems"] + " компоненты; " + Range(rule["items"]) : "Строк: от " + (rule["minItems"] ?? 0);
            string lo = rule["minimum"]?.ToString() ?? rule["exclusiveMinimum"]?.ToString();
            string hi = rule["maximum"]?.ToString() ?? "1e12 (численный предел runtime)";
            return (lo == null ? "−1e12" : ((rule["exclusiveMinimum"] != null ? "> " : "≥ ") + lo)) + "; ≤ " + hi;
        }
        public static string Tooltip(string key,JToken rule)
        {
            var effective=(JObject)Resolve(rule).DeepClone();
            if((string)effective["type"]=="array" && (string)Resolve(effective["items"])["type"]=="number")effective["items"]=ComponentRule(key,effective["items"],key);
            var example=Examples[key];
            string defaultValue=example==null?"не задано; добавляется при выборе соответствующей модели":
                example is JObject?"параметры стартового шаблона; раскрываются отдельно":
                example is JArray array && array.FirstOrDefault() is JObject?"таблица стартового шаблона, строк: "+array.Count:
                example.Type==JTokenType.String && effective["enum"]!=null?Name((string)example):example.ToString(Newtonsoft.Json.Formatting.None);
            return Name(key) + (Unit(key).Length == 0 ? "" : " [" + Unit(key) + "]") + "\n\n" +
                ((string)Help[key]?["description"] ?? "Параметр действующего цифрового профиля.") +
                "\n\nПо умолчанию: " + defaultValue + ". Числа стартового профиля — оценочные, замените их измерениями своего дрона." +
                "\nДиапазон: " + Range(effective) + ". Связанные условия проверяются при проверке профиля.";
        }
        public static bool TryNumber(string text,JToken rule,out JToken value,out string error)
        {
            value=null; error=null; var resolved=Resolve(rule);
            if (!double.TryParse((text ?? "").Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double number) || double.IsNaN(number) || double.IsInfinity(number))
                { error="Введите конечное число, например 0,14 или 1e-5."; return false; }
            if (Math.Abs(number)>1e12 || (resolved["minimum"] != null && number<(double)resolved["minimum"]) ||
                (resolved["maximum"] != null && number>(double)resolved["maximum"]) ||
                (resolved["exclusiveMinimum"] != null && number<=(double)resolved["exclusiveMinimum"]))
                { error="Допустимо: " + Range(rule); return false; }
            if ((string)resolved["type"] == "integer") {
                if (number != Math.Truncate(number) || number<int.MinValue || number>int.MaxValue) { error="Введите целое число."; return false; }
                value=new JValue((int)number);
            } else value=new JValue(number);
            return true;
        }
    }
}
