
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroSdk.Json
{
    /// <summary>
    /// Utility class for generating quick JSON schemas
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class QJS
    {
        private static JsonSchema Const<T>(T value)
        {
            return new JsonSchema
            {
                Const = value
            };
        }
        private static JsonSchema Enum<T>(IEnumerable<T> values)
        {
            return new JsonSchema
            {
                Enum = values.Cast<object>().ToList()
            };
        }

        public static JsonSchema Const(string value){
            return Const<string>(value);
        }
        public static JsonSchema Const(int value){
            return Const<int>(value);
        }
        public static JsonSchema Const(float value){
            return Const<float>(value);
        }
        public static JsonSchema Const(bool value){
            return Const<bool>(value);
        }
        public static JsonSchema Const(IEnumerable<string> values){
            return Const<IEnumerable<string>>(values);
        }
        public static JsonSchema Const(IEnumerable<int> values){
            return Const<IEnumerable<int>>(values);
        }
        public static JsonSchema Const(IEnumerable<float> values){
            return Const<IEnumerable<float>>(values);
        }
        public static JsonSchema Const(IEnumerable<bool> values){
            return Const<IEnumerable<bool>>(values);
        }

        public static JsonSchema ConstEmptyArray
        {
            get
            {
                return Const(new object[0]);
            }
        }
        public static JsonSchema ConstNull
        {
            get
            {
                return new JsonSchema.ConstNull();
            }
        }

        public static JsonSchema Enum(IEnumerable<string> values){
            return Enum<string>(values);
        }
        public static JsonSchema Enum(IEnumerable<int> values){
            return Enum<int>(values);
        }
        public static JsonSchema Enum(IEnumerable<float> values){
            return Enum<float>(values);
        }

        public static JsonSchema Type(JsonSchemaType type)
        {
            return new JsonSchema
            {
                Type = type
            };
        }

        public static JsonSchema WrapObject(IDictionary<string, JsonSchema> properties, bool makePropertiesRequired = true)
        {
            JsonSchema result = new JsonSchema()
            {
                Type = JsonSchemaType.Object,
                Properties = properties.ToDictionary(x => x.Key, x => x.Value)
            };

            if (makePropertiesRequired) result.Required = properties.Keys.ToList();

            return result;
        }
    }
}
