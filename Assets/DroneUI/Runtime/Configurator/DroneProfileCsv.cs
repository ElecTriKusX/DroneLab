using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    /// <summary>Transactional CSV import: no rows are replaced until every cell has been parsed.</summary>
    public static class DroneProfileCsv
    {
        public static JArray Read(string csv,JObject itemRule)
        {
            using var reader=new StringReader(csv);string first=ReadRecord(reader);if(first==null)throw new ArgumentException("CSV пуст.");
            char separator=first.Contains(';')?';':',';var header=Split(first.TrimStart('\uFEFF'),separator);
            var properties=(JObject)itemRule["properties"];
            if(header.Count==0 || header.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=header.Count)throw new ArgumentException("Заголовки CSV должны быть уникальными.");
            var keys=header.Select(h=>properties.Properties().FirstOrDefault(p=>string.Equals(p.Name,h.Trim(),StringComparison.OrdinalIgnoreCase))?.Name??throw new ArgumentException("Неизвестный столбец: "+h)).ToArray();
            if(keys.Distinct().Count()!=keys.Length)throw new ArgumentException("Заголовки CSV должны быть уникальными.");
            foreach(var required in ((JArray)itemRule["required"]).Select(x=>(string)x))if(!keys.Contains(required))throw new ArgumentException("Нет обязательного столбца: "+required);
            var result=new JArray();string line;int row=1;
            while((line=ReadRecord(reader))!=null) {
                row++;if(string.IsNullOrWhiteSpace(line))continue;if(result.Count>=10000)throw new ArgumentException("CSV превышает 10000 строк.");
                var cells=Split(line,separator);if(cells.Count!=keys.Length)throw new ArgumentException("Строка "+row+": число столбцов не совпадает с заголовком.");
                var item=new JObject();
                for(int i=0;i<keys.Length;i++) {
                    string key=keys[i];var rule=properties[key];string value=cells[i].Trim();
                    if(value.Length==0 && !((JArray)itemRule["required"]).Any(x=>(string)x==key))continue;
                    string type=(string)rule["type"];
                    if(type=="number" || type=="integer") {
                        if(!DroneParameterSchema.TryNumber(value,rule,out var number,out var error))throw new ArgumentException("Строка "+row+", "+key+": "+error);
                        item[key]=number;
                    } else if(type=="array") {
                        try {var vector=JArray.Parse(value);if(rule["maxItems"]!=null && vector.Count!=(int)rule["maxItems"])throw new ArgumentException();
                            foreach(var v in vector)if((v.Type!=JTokenType.Float && v.Type!=JTokenType.Integer) || !DroneParameterSchema.TryNumber(v.ToString(),rule["items"],out _,out _))throw new ArgumentException();item[key]=vector;
                        }catch(Exception){throw new ArgumentException("Строка "+row+", "+key+": ожидается JSON-вектор, например [0,1,0].");}
                    } else if(type=="string") {
                        if(rule["enum"] is JArray choices && !choices.Any(x=>(string)x==value))throw new ArgumentException("Строка "+row+": неверный вариант "+key);
                        if((int?)rule["minLength"]>0 && value.Length==0)throw new ArgumentException("Строка "+row+": пустой "+key);item[key]=value;
                    } else throw new ArgumentException("Для этого типа таблицы CSV не поддерживается.");
                }
                result.Add(item);
            }
            if(result.Count==0)throw new ArgumentException("CSV не содержит строк данных.");return result;
        }
        public static string Write(JArray rows,JObject itemRule)
        {
            var keys=((JObject)itemRule["properties"]).Properties().Select(x=>x.Name).ToArray();var b=new StringBuilder();b.AppendLine(string.Join(",",keys));
            foreach(var row in rows)b.AppendLine(string.Join(",",keys.Select(k=>Quote(row[k]?.Type==JTokenType.String?(string)row[k]:row[k]?.ToString(Newtonsoft.Json.Formatting.None)??""))));
            return b.ToString();
        }
        private static string Quote(string value)=>value.Contains(',')||value.Contains('"')||value.Contains('\n')||value.Contains('\r')?"\""+value.Replace("\"","\"\"")+"\"":value;
        private static string ReadRecord(StringReader reader)
        {
            var b=new StringBuilder();bool quoted=false;int next;
            while((next=reader.Read())>=0) {
                char c=(char)next;
                if(c=='"'){b.Append(c);if(quoted && reader.Peek()=='"'){b.Append((char)reader.Read());continue;}quoted=!quoted;continue;}
                if(!quoted && (c=='\n' || c=='\r')){if(c=='\r' && reader.Peek()=='\n')reader.Read();return b.ToString();}
                b.Append(c);
            }
            if(quoted)throw new ArgumentException("Незакрытая кавычка в CSV.");return b.Length==0?null:b.ToString();
        }
        private static List<string> Split(string line,char separator)
        {
            var values=new List<string>();var b=new StringBuilder();bool quoted=false;
            for(int i=0;i<line.Length;i++) {char c=line[i];if(c=='"') {if(quoted && i+1<line.Length && line[i+1]=='"'){b.Append('"');i++;}else quoted=!quoted;}
                else if(c==separator && !quoted){values.Add(b.ToString());b.Clear();}else b.Append(c);}
            if(quoted)throw new ArgumentException("Незакрытая кавычка в CSV.");values.Add(b.ToString());return values;
        }
    }
}
