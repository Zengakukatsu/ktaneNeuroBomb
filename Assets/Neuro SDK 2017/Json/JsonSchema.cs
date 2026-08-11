
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NeuroSdk.Json
{
    public class JsonSchema
    {
        [JsonIgnore]
    public Dictionary<string, JsonSchema> Properties
    {
        get
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, JsonSchema>();
            }

            return _properties;
        }
        set
        {
            _properties = value;
        }
    }

        [JsonIgnore]
        public JsonSchemaType Type
        {
            get
            {
                switch (_type)
                {
                    case "string":
                        return JsonSchemaType.String;
                    case "number":
                        return JsonSchemaType.Float;
                    case "integer":
                        return JsonSchemaType.Integer;
                    case "boolean":
                        return JsonSchemaType.Boolean;
                    case "object":
                        return JsonSchemaType.Object;
                    case "array":
                        return JsonSchemaType.Array;
                    case "null":
                        return JsonSchemaType.Null;
                    default:
                        return JsonSchemaType.None;
                }
            }
            set
            {
                switch (value)
                {
                    case JsonSchemaType.String:
                        _type = "string";
                        break;
                    case JsonSchemaType.Float:
                        _type = "number";
                        break;
                    case JsonSchemaType.Integer:
                        _type = "integer";
                        break;
                    case JsonSchemaType.Boolean:
                        _type = "boolean";
                        break;
                    case JsonSchemaType.Object:
                        _type = "object";
                        break;
                    case JsonSchemaType.Array:
                        _type = "array";
                        break;
                    case JsonSchemaType.Null:
                        _type = "null";
                        break;
                    default:
                        _type = null;
                        break;
                }
            }
        }

        [JsonIgnore]
        public List<object> Enum
        {
            get
            {
                if (_enum == null)
                {
                    _enum = new List<object>();
                }

                return _enum;
            }
            set
            {
                _enum = value;
            }
        }

        [JsonIgnore]
        public List<string> Required
        {
            get
            {
                if (_required == null)
                {
                    _required = new List<string>();
                }

                return _required;
            }
            set
            {
                _required = value;
            }
        }

        #region Keywords

        [JsonProperty("properties")]
        private Dictionary<string, JsonSchema> _properties;

        [JsonProperty("items")]
        public JsonSchema Items { get; set; }

        [JsonProperty("type")]
        private string _type;

        [JsonProperty("enum")]
        private List<object> _enum;

        [JsonProperty("const")]
        public virtual object Const { get; set; }

        [JsonProperty("minLength")]
        public int? MinLength { get; set; }

        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        [JsonProperty("maxLength")]
        public int? MaxLength { get; set; }

        [JsonProperty("maximum")]
        public float? Maximum { get; set; }

        [JsonProperty("exclusiveMinimum")]
        public float? ExclusiveMinimum { get; set; }

        [JsonProperty("exclusiveMaximum")]
        public float? ExclusiveMaximum { get; set; }

        [JsonProperty("minimum")]
        public float? Minimum { get; set; }

        [JsonProperty("required")]
        private List<string> _required;

        [JsonProperty("minItems")]
        public int? MinItems { get; set; }

        [JsonProperty("maxItems")]
        public int? MaxItems { get; set; }

        [JsonProperty("uniqueItems")]
        public bool? UniqueItems { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        #endregion
        
        internal sealed class ConstNull : JsonSchema
        {
            [JsonProperty("const", NullValueHandling = NullValueHandling.Include)]
            public override object Const { get; set; }
        }
    }
}
