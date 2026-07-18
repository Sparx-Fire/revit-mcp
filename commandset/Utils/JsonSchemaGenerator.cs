using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RevitMCPCommandSet.Utils
{
    public static class JsonSchemaGenerator
    {
        /// <summary>
        /// </summary>
        /// <typeparam name="T">Type to generate the schema for</typeparam>
        /// <param name="mainPropertyName">Name of the main property in the generated schema</param>
        /// <returns>The generated JSON schema string</returns>
        public static string GenerateTransformedSchema<T>(string mainPropertyName)
        {
            return GenerateTransformedSchema<T>(mainPropertyName, false);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T">Type to generate the schema for</typeparam>
        /// <param name="mainPropertyName">Name of the main property in the generated schema</param>
        /// <param name="includeThinkingProcess">Whether to add a ThinkingProcess property</param>
        /// <returns>The generated JSON schema string</returns>
        public static string GenerateTransformedSchema<T>(string mainPropertyName, bool includeThinkingProcess)
        {
            if (string.IsNullOrWhiteSpace(mainPropertyName))
                throw new ArgumentException("Main property name cannot be null or empty.", nameof(mainPropertyName));

            // Create the root schema
            JObject rootSchema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["required"] = new JArray(),
                ["additionalProperties"] = false
            };

            // Optionally add the ThinkingProcess property
            if (includeThinkingProcess)
            {
                AddProperty(rootSchema, "ThinkingProcess", new JObject { ["type"] = "string" }, true);
            }

            // Generate the schema for the target property
            JObject mainPropertySchema = GenerateSchema(typeof(T));
            AddProperty(rootSchema, mainPropertyName, mainPropertySchema, true);

            // Recursively add "additionalProperties": false to all objects
            AddAdditionalPropertiesFalse(rootSchema);

            // Return the formatted JSON schema
            return JsonConvert.SerializeObject(rootSchema, Formatting.Indented);
        }

        /// <summary>
        /// </summary>
        private static JObject GenerateSchema(Type type)
        {
            if (type == typeof(string)) return new JObject { ["type"] = "string" };
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return new JObject { ["type"] = "integer" };
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return new JObject { ["type"] = "number" };
            if (type == typeof(bool)) return new JObject { ["type"] = "boolean" };

            // Handle Dictionary types first
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return HandleDictionary(type);

            // Handle array/collection types
            if (type.IsArray || (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType))
            {
                Type itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = GenerateSchema(itemType)
                };
            }

            // Handle class types
            if (type.IsClass)
            {
                var schema = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject(),
                    ["required"] = new JArray(),
                    ["additionalProperties"] = false
                };

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AddProperty(schema, prop.Name, GenerateSchema(prop.PropertyType), isRequired: true);
                }
                return schema;
            }

            // Default to string
            return new JObject { ["type"] = "string" };
        }

        /// <summary>
        /// </summary>
        private static JObject HandleDictionary(Type type)
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            if (keyType != typeof(string))
            {
                throw new NotSupportedException("JSON Schema only supports dictionaries with string keys.");
            }

            return new JObject
            {
                ["type"] = "object",
                ["additionalProperties"] = GenerateSchema(valueType)
            };
        }

        /// <summary>
        /// </summary>
        private static void AddProperty(JObject schema, string propertyName, JToken propertySchema, bool isRequired)
        {
            ((JObject)schema["properties"]).Add(propertyName, propertySchema);

            if (isRequired)
            {
                ((JArray)schema["required"]).Add(propertyName);
            }
        }

        /// <summary>
        /// </summary>
        private static void AddAdditionalPropertiesFalse(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj["required"] != null && obj["additionalProperties"] == null)
                {
                    obj["additionalProperties"] = false;
                }

                foreach (var property in obj.Properties())
                {
                    AddAdditionalPropertiesFalse(property.Value);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    AddAdditionalPropertiesFalse(item);
                }
            }
        }
    }
}
