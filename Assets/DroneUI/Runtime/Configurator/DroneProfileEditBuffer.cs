using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    /// <summary>A modal edits a detached subtree. Nothing is published until BuildValue succeeds.</summary>
    public sealed class DroneProfileEditBuffer
    {
        public JToken Value { get; }
        public string Path { get; }
        public JToken Rule { get; }
        public Dictionary<string,string> Errors { get; } = new Dictionary<string,string>();
        public Dictionary<string,string> Inputs { get; } = new Dictionary<string,string>();
        private readonly JToken original;
        private readonly Dictionary<string,string> originalInputs;

        public DroneProfileEditBuffer(JToken source,JToken rule,string path,Dictionary<string,string> errors,Dictionary<string,string> inputs)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            Value=source.DeepClone();original=source.DeepClone();Rule=rule;Path=path;
            foreach(var pair in errors.Where(p=>Contains(p.Key)))Errors[pair.Key]=pair.Value;
            foreach(var pair in inputs.Where(p=>Contains(p.Key)))Inputs[pair.Key]=pair.Value;
            originalInputs=new Dictionary<string,string>(Inputs);
        }
        private bool Contains(string key)=>key==Path || key.StartsWith(Path+".",StringComparison.Ordinal) || key.StartsWith(Path+"[",StringComparison.Ordinal);
        public bool Dirty=>!JToken.DeepEquals(original,Value) || Inputs.Count!=originalInputs.Count || Inputs.Any(p=>!originalInputs.TryGetValue(p.Key,out var before)||before!=p.Value);
        public string ValidationError()
        {
            if(Errors.Count>0)return Errors.First().Value;
            return Validate(Value,Rule,Path);
        }
        public JToken BuildValue()
        {
            string error=ValidationError();if(error!=null)throw new ArgumentException(error);
            return Value.DeepClone();
        }
        private static string Validate(JToken value,JToken unresolved,string path)
        {
            var rule=DroneParameterSchema.Resolve(unresolved);string type=(string)rule["type"];
            if(value==null)return "Заполните обязательное поле: "+DroneValidationText.Path(path);
            if(rule["enum"] is JArray choices && !choices.Any(c=>JToken.DeepEquals(c,value)))return "Выберите допустимое значение: "+DroneValidationText.Path(path);
            if(type=="object") {
                if(value is not JObject obj)return "Ожидается группа параметров: "+DroneValidationText.Path(path);
                foreach(string required in (rule["required"] as JArray ?? new JArray()).Select(v=>(string)v))
                    if(obj[required]==null)return "Заполните обязательное поле: "+DroneValidationText.Path(path+"."+required);
                foreach(var property in obj.Properties())if(rule["properties"]?[property.Name]!=null) {
                    string error=Validate(property.Value,rule["properties"][property.Name],path+"."+property.Name);if(error!=null)return error;
                }
            } else if(type=="array") {
                if(value is not JArray array)return "Ожидается таблица или вектор: "+DroneValidationText.Path(path);
                if((int?)rule["minItems"] is int minimum && array.Count<minimum)return $"Добавьте строки: требуется не менее {minimum}.";
                if((int?)rule["maxItems"] is int maximum && array.Count>maximum)return $"Слишком много строк или компонентов: допустимо не более {maximum}.";
                for(int i=0;i<array.Count;i++){string error=Validate(array[i],rule["items"],path+"["+i+"]");if(error!=null)return error;}
            } else if(type=="number" || type=="integer") {
                if(value.Type!=JTokenType.Integer && value.Type!=JTokenType.Float)return "Введите число: "+DroneValidationText.Path(path);
                if(!DroneParameterSchema.TryNumber(value.ToString(Newtonsoft.Json.Formatting.None),rule,out _,out var error))return DroneValidationText.Path(path)+": "+error;
            } else if(type=="string") {
                if(value.Type!=JTokenType.String)return "Введите текст: "+DroneValidationText.Path(path);
                if((int?)rule["minLength"] is int minimum && ((string)value).Trim().Length<minimum)return "Заполните текст: "+DroneValidationText.Path(path);
            } else if(type=="boolean" && value.Type!=JTokenType.Boolean)return "Выберите состояние переключателя: "+DroneValidationText.Path(path);
            return null;
        }
    }
}
