using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    public static class DroneProfileEdits
    {
        public static void SetOptionalText(JObject value,string key,string text)
        {
            if(string.IsNullOrWhiteSpace(text))value.Remove(key);else value[key]=text;
        }
        private static bool Finite(double value)=>!double.IsNaN(value) && !double.IsInfinity(value);
        public static double UniformScaleToDimensions(JArray dimensions,JArray currentBounds,double currentScale)
        {
            if(dimensions==null || currentBounds==null || dimensions.Count!=3 || currentBounds.Count!=3)throw new ArgumentException("Нужны три габарита X/Y/Z.");
            var target=dimensions.Select(v=>(double)v).ToArray();var source=currentBounds.Select(v=>(double)v).ToArray();
            if(target.Any(v=>!Finite(v)||v<=0) || source.Any(v=>!Finite(v)||v<0) || !Finite(currentScale)||currentScale<=0 || source.Max()<=1e-9)throw new ArgumentException("Проверьте габариты и масштаб модели: значения должны быть конечными, габариты профиля и масштаб — положительными.");
            double result=currentScale*target.Max()/source.Max();
            if(!Finite(result)||result<=0 || result>1e12)throw new ArgumentException("Не удалось получить допустимый масштаб модели.");
            return result;
        }
        public static bool TryNormalize(JArray value,out JArray normalized,out string error)
        {
            normalized=null;error=null;
            if(value==null || value.Count==0){error="Пустой вектор нельзя нормализовать.";return false;}
            var components=value.Select(v=>(double)v).ToArray();double length=Math.Sqrt(components.Sum(v=>v*v));
            if(double.IsNaN(length)||double.IsInfinity(length)||length<=1e-12){error="Для нормализации нужен ненулевой конечный вектор или кватернион.";return false;}
            normalized=new JArray(components.Select(v=>v/length));return true;
        }
        // A successful check applies only to the exact document shown in the results window.
        public static bool MatchesValidation(DroneProfileDocument document,string snapshot)=>
            document!=null && snapshot!=null && document.unfinishedInputs.Count==0 && document.Snapshot()==snapshot;
    }
}
