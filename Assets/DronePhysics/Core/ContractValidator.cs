using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    public sealed class ValidationIssue
    {
        public readonly string Path, Severity, Message;
        public ValidationIssue(string path, string message, string severity = "Error")
        { Path = path; Message = message; Severity = severity; }
        public override string ToString() => $"{Severity}: {Path}: {Message}";
    }

    // Deliberately implements only the keywords emitted by our versioned generator,
    // not a general-purpose JSON Schema engine. External tools can use full Draft 2020-12.
    public static class ContractValidator
    {
        public static List<ValidationIssue> Validate(JToken data, JObject schema)
        {
            var issues = new List<ValidationIssue>();
            Visit(data, schema, schema, "$", issues);
            return issues;
        }
        private static void Visit(JToken value, JToken rule, JObject root, string path, List<ValidationIssue> issues)
        {
            if (rule["$ref"] != null)
            { Visit(value, root["$defs"][(string)rule["$ref"].ToString().Split('/').Last()], root, path, issues); return; }
            string type = (string)rule["type"];
            bool valid = value != null && (type == "object" ? value.Type == JTokenType.Object :
                type == "array" ? value.Type == JTokenType.Array : type == "string" ? value.Type == JTokenType.String :
                type == "boolean" ? value.Type == JTokenType.Boolean : type == "integer" ? value.Type == JTokenType.Integer :
                type == "number" && (value.Type == JTokenType.Float || value.Type == JTokenType.Integer));
            if (!valid) { issues.Add(new ValidationIssue(path, "Expected " + type)); return; }
            if (type == "object")
            {
                var props = (JObject)rule["properties"];
                foreach (string key in rule["required"].Values<string>())
                    if (value[key] == null) issues.Add(new ValidationIssue(path + "." + key, "Required field is missing."));
                foreach (var prop in ((JObject)value).Properties())
                    if (props[prop.Name] == null) issues.Add(new ValidationIssue(path + "." + prop.Name, "Unknown field."));
                    else Visit(prop.Value, props[prop.Name], root, path + "." + prop.Name, issues);
            }
            else if (type == "array")
            {
                int count = ((JArray)value).Count;
                if (count < (int?)rule["minItems"] || count > (int?)rule["maxItems"])
                    issues.Add(new ValidationIssue(path, "Invalid array length."));
                for (int i = 0; i < count; i++) Visit(value[i], rule["items"], root, path + "[" + i + "]", issues);
            }
            else if (type == "number" || type == "integer")
            {
                double n = (double)value;
                if (double.IsNaN(n) || double.IsInfinity(n) || n < (double?)rule["minimum"] ||
                    n > (double?)rule["maximum"] || n <= (double?)rule["exclusiveMinimum"] ||
                    (type == "integer" && (n > int.MaxValue || n < int.MinValue)))
                    issues.Add(new ValidationIssue(path, "Number is not finite or is outside the allowed range."));
            }
            else if (type == "string")
            {
                if (((string)value).Length < (int?)rule["minLength"])
                    issues.Add(new ValidationIssue(path, "String must not be empty."));
                if (rule["enum"] is JArray allowed && !allowed.Any(x => JToken.DeepEquals(x, value)))
                    issues.Add(new ValidationIssue(path, "Unknown value: " + value));
            }
        }
    }
}
