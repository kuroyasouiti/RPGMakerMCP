using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCP.Editor.Services
{
    /// <summary>
    /// Utility class for mapping between Dictionary data from MCP and typed DataModels.
    /// Handles partial updates by merging with existing data.
    /// </summary>
    public static class DataModelMapper
    {
        /// <summary>
        /// Apply partial updates from a dictionary to an existing data model.
        /// Uses reflection to update only the fields present in the updates dictionary.
        /// </summary>
        public static void ApplyPartialUpdate<T>(T target, Dictionary<string, object> updates) where T : class
        {
            if (target == null || updates == null) return;

            var type = typeof(T);
            foreach (var kvp in updates)
            {
                ApplyFieldUpdate(target, type, kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Apply a single field update to an object.
        /// For nested objects (Dictionary values), recursively updates fields instead of replacing the entire object.
        /// </summary>
        private static void ApplyFieldUpdate(object target, Type type, string fieldName, object value)
        {
            if (target == null || value == null) return;

            // Try to find a field with the given name
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    // IMPORTANT: For nested objects (Dictionary values), recursively update fields
                    // instead of replacing the entire object to preserve existing field values
                    if (value is Dictionary<string, object> nestedDict &&
                        field.FieldType.IsClass &&
                        field.FieldType != typeof(string) &&
                        !typeof(IList).IsAssignableFrom(field.FieldType))
                    {
                        var existingObject = field.GetValue(target);
                        if (existingObject != null)
                        {
                            // Recursively update each field in the nested object
                            foreach (var nestedKvp in nestedDict)
                            {
                                ApplyFieldUpdate(existingObject, field.FieldType, nestedKvp.Key, nestedKvp.Value);
                            }
                            return;
                        }
                        // If existing object is null, fall through to create new object
                    }

                    var convertedValue = ConvertValue(value, field.FieldType);
                    if (convertedValue != null || !field.FieldType.IsValueType)
                    {
                        field.SetValue(target, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to set field '{fieldName}': {ex.Message}");
                }
                return;
            }

            // Try to find a property with the given name
            var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                try
                {
                    // IMPORTANT: For nested objects (Dictionary values), recursively update fields
                    // instead of replacing the entire object to preserve existing field values
                    if (value is Dictionary<string, object> nestedDict &&
                        property.PropertyType.IsClass &&
                        property.PropertyType != typeof(string) &&
                        !typeof(IList).IsAssignableFrom(property.PropertyType))
                    {
                        var existingObject = property.GetValue(target);
                        if (existingObject != null)
                        {
                            // Recursively update each field in the nested object
                            foreach (var nestedKvp in nestedDict)
                            {
                                ApplyFieldUpdate(existingObject, property.PropertyType, nestedKvp.Key, nestedKvp.Value);
                            }
                            return;
                        }
                        // If existing object is null, fall through to create new object
                    }

                    var convertedValue = ConvertValue(value, property.PropertyType);
                    if (convertedValue != null || !property.PropertyType.IsValueType)
                    {
                        property.SetValue(target, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to set property '{fieldName}': {ex.Message}");
                }
                return;
            }
        }

        /// <summary>
        /// Convert a value to the target type.
        /// </summary>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;

            var valueType = value.GetType();

            // If the value is already the correct type, return it
            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            // Handle JToken types from Newtonsoft.Json
            if (value is JToken jToken)
            {
                try
                {
                    return jToken.ToObject(targetType);
                }
                catch
                {
                    // Fall through to other conversion methods
                }
            }

            // Handle Dictionary to object conversion
            if (value is Dictionary<string, object> dict && targetType.IsClass && targetType != typeof(string))
            {
                try
                {
                    var json = JsonConvert.SerializeObject(dict);
                    return JsonConvert.DeserializeObject(json, targetType);
                }
                catch
                {
                    // Fall through
                }
            }

            // Handle IList conversions
            if (value is IList list && typeof(IList).IsAssignableFrom(targetType))
            {
                try
                {
                    var json = JsonConvert.SerializeObject(list);
                    return JsonConvert.DeserializeObject(json, targetType);
                }
                catch
                {
                    // Fall through
                }
            }

            // Handle primitive type conversions
            if (targetType == typeof(string))
            {
                return value.ToString();
            }

            if (targetType == typeof(int))
            {
                return Convert.ToInt32(value);
            }

            if (targetType == typeof(long))
            {
                return Convert.ToInt64(value);
            }

            if (targetType == typeof(float))
            {
                return Convert.ToSingle(value);
            }

            if (targetType == typeof(double))
            {
                return Convert.ToDouble(value);
            }

            if (targetType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }

            // Try ChangeType for other conversions
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // Final fallback: try JSON serialization/deserialization
                try
                {
                    var json = JsonConvert.SerializeObject(value);
                    return JsonConvert.DeserializeObject(json, targetType);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Convert a data model to a dictionary for MCP response.
        /// </summary>
        public static Dictionary<string, object> ToDict<T>(T dataModel) where T : class
        {
            if (dataModel == null) return null;

            try
            {
                var json = JsonConvert.SerializeObject(dataModel);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert data model to dictionary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a data model from a dictionary.
        /// </summary>
        public static T FromDict<T>(Dictionary<string, object> dict) where T : class, new()
        {
            if (dict == null) return null;

            try
            {
                var json = JsonConvert.SerializeObject(dict);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to create data model from dictionary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Merge two dictionaries, with the second dictionary taking precedence.
        /// </summary>
        public static Dictionary<string, object> MergeDicts(
            Dictionary<string, object> original,
            Dictionary<string, object> updates)
        {
            if (original == null) return updates;
            if (updates == null) return original;

            var result = new Dictionary<string, object>(original);

            foreach (var kvp in updates)
            {
                if (kvp.Value is Dictionary<string, object> nestedUpdates &&
                    result.TryGetValue(kvp.Key, out var existingValue) &&
                    existingValue is Dictionary<string, object> existingDict)
                {
                    // Recursively merge nested dictionaries
                    result[kvp.Key] = MergeDicts(existingDict, nestedUpdates);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Get a nested value from a dictionary using dot notation.
        /// Example: GetNestedValue(dict, "basic.name") returns dict["basic"]["name"]
        /// </summary>
        public static object GetNestedValue(Dictionary<string, object> dict, string path)
        {
            if (dict == null || string.IsNullOrEmpty(path)) return null;

            var parts = path.Split('.');
            object current = dict;

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> currentDict)
                {
                    if (!currentDict.TryGetValue(part, out current))
                    {
                        return null;
                    }
                }
                else if (current is JObject jObject)
                {
                    current = jObject[part];
                    if (current == null) return null;
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// Set a nested value in a dictionary using dot notation.
        /// Creates intermediate dictionaries as needed.
        /// </summary>
        public static void SetNestedValue(Dictionary<string, object> dict, string path, object value)
        {
            if (dict == null || string.IsNullOrEmpty(path)) return;

            var parts = path.Split('.');

            Dictionary<string, object> current = dict;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (!current.TryGetValue(part, out var next))
                {
                    next = new Dictionary<string, object>();
                    current[part] = next;
                }

                if (next is Dictionary<string, object> nextDict)
                {
                    current = nextDict;
                }
                else
                {
                    // Cannot set nested value if intermediate is not a dictionary
                    return;
                }
            }

            current[parts[parts.Length - 1]] = value;
        }

        /// <summary>
        /// Extract specific fields from a data model for lightweight listing.
        /// </summary>
        public static Dictionary<string, object> ExtractFields<T>(T dataModel, params string[] fieldPaths) where T : class
        {
            if (dataModel == null) return null;

            var fullDict = ToDict(dataModel);
            if (fullDict == null) return null;

            var result = new Dictionary<string, object>();
            foreach (var path in fieldPaths)
            {
                var value = GetNestedValue(fullDict, path);
                if (value != null)
                {
                    // Use the last part of the path as the key
                    var key = path.Contains('.') ? path.Split('.').Last() : path;
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
