using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor
{
    /// <summary>
    /// Helper methods for McpCommandProcessor.
    /// Includes payload parsing, serialization, type resolution, and validation utilities.
    /// </summary>
    internal static partial class McpCommandProcessor
    {
        #region Utility Helper Methods

        /// <summary>
        /// Gets a string value from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The string value, or null if the key doesn't exist or the value is null.</returns>
        private static string GetString(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return null;
            }

            var value = payload[key];
            return value?.ToString();
        }

        /// <summary>
        /// Gets a string value from a dictionary payload with a default value.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The string value, or the default value if the key doesn't exist.</returns>
        private static string GetString(Dictionary<string, object> payload, string key, string defaultValue)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }

            var value = payload[key];
            return string.IsNullOrEmpty(value?.ToString()) ? defaultValue : value.ToString();
        }

        /// <summary>
        /// Gets a boolean value from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The boolean value, or false if the key doesn't exist.</returns>
        private static bool GetBool(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return false;
            }

            var value = payload[key];
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string strValue)
            {
                return bool.TryParse(strValue, out var result) && result;
            }

            return false;
        }

        /// <summary>
        /// Gets a boolean value from a dictionary payload with a default value.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The boolean value, or the default value if the key doesn't exist.</returns>
        private static bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue)
        {
            if (payload == null || !payload.ContainsKey(key))
            {
                return defaultValue;
            }

            var value = payload[key];
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string strValue)
            {
                return bool.TryParse(strValue, out var result) ? result : defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a list of strings from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The list of strings, or null if the key doesn't exist.</returns>
        private static List<string> GetStringList(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is List<object> listObj)
            {
                return listObj.Select(o => o?.ToString()).Where(s => s != null).ToList();
            }

            if (value is string[] strArray)
            {
                return strArray.ToList();
            }

            if (value is List<string> strList)
            {
                return strList;
            }

            return null;
        }

        /// <summary>
        /// Gets an integer value from a dictionary payload with a default value.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The integer value, or the default value if the key doesn't exist.</returns>
        private static int GetInt(Dictionary<string, object> payload, string key, int defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is long longValue)
            {
                return (int)longValue;
            }

            if (value is double doubleValue)
            {
                return (int)doubleValue;
            }

            if (value is string strValue && int.TryParse(strValue, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a float value from a dictionary payload (nullable version).
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The float value, or null if the key doesn't exist.</returns>
        private static float? GetFloat(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            if (value is double d)
            {
                return (float)d;
            }
            if (value is float f)
            {
                return f;
            }
            if (value is int i)
            {
                return i;
            }
            if (value is long l)
            {
                return (float)l;
            }

            return null;
        }

        /// <summary>
        /// Gets a float value from a dictionary payload with a default value.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The float value, or the default value if the key doesn't exist.</returns>
        private static float GetFloat(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }

            if (value is int intValue)
            {
                return (float)intValue;
            }

            if (value is long longValue)
            {
                return (float)longValue;
            }

            if (value is string strValue && float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a list of objects from a dictionary payload.
        /// </summary>
        /// <param name="payload">The payload dictionary.</param>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The list of objects, or null if the key doesn't exist.</returns>
        private static List<object> GetList(Dictionary<string, object> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return value as List<object>;
        }

        /// <summary>
        /// Resolves a GameObject by its hierarchy path.
        /// </summary>
        /// <param name="path">The hierarchy path (e.g., "Canvas/Panel/Button").</param>
        /// <returns>The resolved GameObject.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the GameObject is not found.</exception>
        private static GameObject ResolveGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("GameObject path is required");
            }

            var go = GameObject.Find(path);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: {path}");
            }

            return go;
        }

        /// <summary>
        /// Gets the full hierarchy path of a GameObject.
        /// </summary>
        /// <param name="go">The GameObject.</param>
        /// <returns>The full hierarchy path (e.g., "Canvas/Panel/Button").</returns>
        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var path = go.name;
            var parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Serializes a value for JSON output.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A serialized representation suitable for JSON.</returns>
        private static object SerializeValue(object value)
        {
            return SerializeValue(value, 0, 3, 50);
        }

        /// <summary>
        /// Serializes a value for JSON output with depth and size limits.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="depth">Current recursion depth.</param>
        /// <param name="maxDepth">Maximum recursion depth (default: 3).</param>
        /// <param name="maxItems">Maximum items in collections (default: 50).</param>
        /// <returns>A serialized representation suitable for JSON.</returns>
        private static object SerializeValue(object value, int depth, int maxDepth, int maxItems)
        {
            if (value == null)
            {
                return null;
            }

            // Check depth limit
            if (depth >= maxDepth)
            {
                return $"<MaxDepthReached:{value.GetType().Name}>";
            }

            // Handle Unity Object references
            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null) // Unity's null check
                {
                    return null;
                }

                var result = new Dictionary<string, object>
                {
                    ["type"] = value.GetType().FullName,
                    ["name"] = unityObj.name,
                };

                // Add asset path if it's an asset
                var assetPath = AssetDatabase.GetAssetPath(unityObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    result["assetPath"] = assetPath;
                    result["guid"] = AssetDatabase.AssetPathToGUID(assetPath);
                }

                // Add instance ID for scene objects
                if (unityObj is GameObject || unityObj is Component)
                {
                    result["instanceID"] = unityObj.GetInstanceID();
                }

                return result;
            }

            // Handle Vector2
            if (value is Vector2 v2)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v2.x,
                    ["y"] = v2.y,
                };
            }

            // Handle Vector3
            if (value is Vector3 v3)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v3.x,
                    ["y"] = v3.y,
                    ["z"] = v3.z,
                };
            }

            // Handle Vector4
            if (value is Vector4 v4)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = v4.x,
                    ["y"] = v4.y,
                    ["z"] = v4.z,
                    ["w"] = v4.w,
                };
            }

            // Handle Quaternion
            if (value is Quaternion quat)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = quat.x,
                    ["y"] = quat.y,
                    ["z"] = quat.z,
                    ["w"] = quat.w,
                };
            }

            // Handle Matrix4x4 (skip to avoid large output)
            if (value is Matrix4x4 matrix)
            {
                return $"<Matrix4x4>";
            }

            // Handle Color
            if (value is Color color)
            {
                return new Dictionary<string, object>
                {
                    ["r"] = color.r,
                    ["g"] = color.g,
                    ["b"] = color.b,
                    ["a"] = color.a,
                };
            }

            // Handle Rect
            if (value is Rect rect)
            {
                return new Dictionary<string, object>
                {
                    ["x"] = rect.x,
                    ["y"] = rect.y,
                    ["width"] = rect.width,
                    ["height"] = rect.height,
                };
            }

            // Handle enums
            if (value.GetType().IsEnum)
            {
                return value.ToString();
            }

            // Handle arrays and lists with size limit
            if (value is IEnumerable enumerable && !(value is string))
            {
                var list = new List<object>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= maxItems)
                    {
                        list.Add($"<TruncatedAt{maxItems}Items>");
                        break;
                    }
                    list.Add(SerializeValue(item, depth + 1, maxDepth, maxItems));
                    count++;
                }
                return list;
            }

            // Return primitives and strings as-is
            return value;
        }

        /// <summary>
        /// Resolves a type by its name, searching common Unity namespaces.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "UnityEngine.UI.Button").</param>
        /// <returns>The resolved Type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type cannot be resolved.</exception>
        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new InvalidOperationException("Type name is required");
            }

            // Try direct resolution first
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            // Try with common Unity assemblies
            var assemblies = new[]
            {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.CoreModule",
                "Assembly-CSharp",
            };

            foreach (var assembly in assemblies)
            {
                type = Type.GetType($"{typeName}, {assembly}");
                if (type != null)
                {
                    return type;
                }
            }

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException($"Type not found: {typeName}");
        }

        /// <summary>
        /// Resolves asset path from either path or GUID.
        /// </summary>
        /// <param name="assetPath">The asset path (can be null or empty).</param>
        /// <param name="assetGuid">The asset GUID (can be null or empty).</param>
        /// <param name="parameterName">The name of the parameter for error messages. Defaults to "assetPath".</param>
        /// <returns>The resolved asset path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when neither path nor GUID is provided, or GUID cannot be resolved.</exception>
        private static string ResolveAssetPath(string assetPath, string assetGuid, string parameterName = "assetPath")
        {
            if (string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(assetGuid))
            {
                throw new InvalidOperationException($"Either {parameterName} or {parameterName}Guid must be provided");
            }

            // If GUID is provided, resolve it to a path
            if (!string.IsNullOrEmpty(assetGuid))
            {
                var resolvedPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    throw new InvalidOperationException($"Asset not found with GUID: {assetGuid}");
                }
                return resolvedPath;
            }

            return assetPath;
        }

        /// <summary>
        /// Validates and sanitizes an asset path to prevent path traversal attacks.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="mustStartWith">The required path prefix (e.g., "Assets/").</param>
        /// <param name="mustEndWith">The required file extension (e.g., ".asset"). Can be null.</param>
        /// <exception cref="InvalidOperationException">Thrown when the path is invalid.</exception>
        private static void ValidateAssetPath(string path, string mustStartWith = "Assets/", string mustEndWith = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Asset path cannot be null or empty");
            }

            // Check prefix
            if (!string.IsNullOrEmpty(mustStartWith) && !path.StartsWith(mustStartWith))
            {
                throw new InvalidOperationException($"Asset path must start with '{mustStartWith}'");
            }

            // Check extension
            if (!string.IsNullOrEmpty(mustEndWith) && !path.EndsWith(mustEndWith))
            {
                throw new InvalidOperationException($"Asset path must end with '{mustEndWith}'");
            }

            // Prevent path traversal attacks
            if (path.Contains("..") || path.Contains("~"))
            {
                throw new InvalidOperationException("Asset path cannot contain '..' or '~' (path traversal detected)");
            }

            // Normalize and verify the path is within the project
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                var projectPath = Path.GetFullPath(mustStartWith ?? "Assets/");

                if (!normalizedPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Asset path must be within the project directory");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid asset path: {ex.Message}");
            }
        }

        /// <summary>
        /// Serializes object properties to a dictionary for JSON output.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="propertyFilter">Optional list of property names to include. If null, all properties are included.</param>
        /// <returns>Dictionary of property names to serialized values.</returns>
        private static Dictionary<string, object> SerializeObjectProperties(UnityEngine.Object obj, List<string> propertyFilter)
        {
            var properties = new Dictionary<string, object>();
            var type = obj.GetType();

            // Properties that cause memory leaks in edit mode (use shared versions instead)
            var dangerousProperties = new HashSet<string>
            {
                "mesh",      // Use sharedMesh instead
                "material",  // Use sharedMaterial instead
                "materials", // Use sharedMaterials instead
            };

            // Serialize public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && !prop.GetIndexParameters().Any())
                {
                    // Apply property filter if specified
                    if (propertyFilter != null && !propertyFilter.Contains(prop.Name))
                    {
                        continue;
                    }

                    // Skip dangerous properties that cause memory leaks
                    if (dangerousProperties.Contains(prop.Name))
                    {
                        properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(obj);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch
                    {
                        // Skip properties that can't be read
                        properties[prop.Name] = "<ReadError>";
                    }
                }
            }

            // Serialize public fields (including those with [SerializeField])
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // For non-public fields, require SerializeField attribute
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }

                // Apply property filter if specified
                if (propertyFilter != null && !propertyFilter.Contains(field.Name))
                {
                    continue;
                }

                try
                {
                    var value = field.GetValue(obj);
                    properties[field.Name] = SerializeValue(value);
                }
                catch
                {
                    // Skip fields that can't be read
                    properties[field.Name] = "<ReadError>";
                }
            }

            return properties;
        }

        /// <summary>
        /// Resolves a GameObject from a payload dictionary, supporting both path and GlobalObjectId.
        /// </summary>
        /// <param name="payload">The payload containing gameObjectPath or gameObjectGlobalObjectId.</param>
        /// <returns>The resolved GameObject.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the GameObject cannot be resolved.</exception>
        private static GameObject ResolveGameObjectFromPayload(Dictionary<string, object> payload)
        {
            // Try GlobalObjectId first (more reliable)
            var globalObjectIdStr = GetString(payload, "gameObjectGlobalObjectId");
            if (!string.IsNullOrEmpty(globalObjectIdStr))
            {
                if (GlobalObjectId.TryParse(globalObjectIdStr, out var globalObjectId))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
                    if (obj is GameObject go)
                    {
                        return go;
                    }
                    if (obj is Component comp)
                    {
                        return comp.gameObject;
                    }
                }
                throw new InvalidOperationException($"Could not resolve GameObject from GlobalObjectId: {globalObjectIdStr}");
            }

            // Fall back to gameObjectPath
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Either gameObjectPath or gameObjectGlobalObjectId is required");
            }

            return ResolveGameObject(path);
        }

        /// <summary>
        /// Resolves an asset path from a payload dictionary, supporting both path and GUID.
        /// </summary>
        /// <param name="payload">The payload containing assetPath or assetGuid.</param>
        /// <returns>The resolved asset path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the asset cannot be resolved.</exception>
        private static string ResolveAssetPathFromPayload(Dictionary<string, object> payload)
        {
            // Try GUID first (more reliable)
            var guid = GetString(payload, "assetGuid");
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException($"Could not resolve asset from GUID: {guid}");
                }
                return path;
            }

            // Fall back to assetPath
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("Either assetPath or assetGuid is required");
            }

            return assetPath;
        }

        /// <summary>
        /// Applies property changes to an asset's importer settings.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <param name="propertyChanges">Dictionary of property name/value pairs to apply.</param>
        private static void ApplyAssetImporterProperties(string assetPath, Dictionary<string, object> propertyChanges)
        {
            if (propertyChanges == null || propertyChanges.Count == 0)
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not get AssetImporter for: {assetPath}");
            }

            foreach (var kvp in propertyChanges)
            {
                ApplyProperty(importer, kvp.Key, kvp.Value);
            }

            importer.SaveAndReimport();
        }

        /// <summary>
        /// Describes a component by serializing its public properties and fields.
        /// </summary>
        /// <param name="component">The component to describe.</param>
        /// <returns>A dictionary containing component information.</returns>
        private static Dictionary<string, object> DescribeComponent(Component component)
        {
            if (component == null)
            {
                return null;
            }

            var result = new Dictionary<string, object>
            {
                ["type"] = component.GetType().FullName,
                ["typeName"] = component.GetType().Name,
                ["gameObjectPath"] = GetHierarchyPath(component.gameObject),
                ["instanceID"] = component.GetInstanceID(),
            };

            // Add GlobalObjectId if available
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(component.gameObject);
            result["globalObjectId"] = globalObjectId.ToString();

            var properties = new Dictionary<string, object>();
            var type = component.GetType();

            // Properties that cause memory leaks in edit mode (use shared versions instead)
            var dangerousProperties = new HashSet<string>
            {
                "mesh",      // Use sharedMesh instead
                "material",  // Use sharedMaterial instead
                "materials", // Use sharedMaterials instead
            };

            // Serialize public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && !prop.GetIndexParameters().Any())
                {
                    // Skip dangerous properties that cause memory leaks
                    if (dangerousProperties.Contains(prop.Name))
                    {
                        properties[prop.Name] = $"<Skipped:{prop.Name}:UseSharedVersion>";
                        continue;
                    }

                    try
                    {
                        var value = prop.GetValue(component);
                        properties[prop.Name] = SerializeValue(value);
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }

            result["properties"] = properties;
            return result;
        }

        /// <summary>
        /// Describes an asset by its path.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <returns>A dictionary containing asset information.</returns>
        private static Dictionary<string, object> DescribeAsset(string assetPath)
        {
            var result = new Dictionary<string, object>
            {
                ["path"] = assetPath,
                ["exists"] = File.Exists(assetPath),
                ["guid"] = AssetDatabase.AssetPathToGUID(assetPath),
            };

            if (File.Exists(assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null)
                {
                    result["name"] = asset.name;
                    result["type"] = asset.GetType().FullName;
                }

                var fileInfo = new FileInfo(assetPath);
                result["size"] = fileInfo.Length;
                result["modified"] = fileInfo.LastWriteTimeUtc.ToString("o");
            }

            return result;
        }

        /// <summary>
        /// Applies a property value to a component.
        /// </summary>
        /// <param name="component">The component to modify.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value to set.</param>
        private static void ApplyProperty(Component component, string propertyName, object value)
        {
            ApplyPropertyToObject(component, propertyName, value);
        }

        private static void ApplyProperty(UnityEngine.Object obj, string propertyName, object value)
        {
            ApplyPropertyToObject(obj, propertyName, value);
        }

        private static void ApplyPropertyToObject(UnityEngine.Object obj, string propertyName, object value)
        {
            var type = obj.GetType();

            // Try property first
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    // IMPORTANT: Record undo BEFORE making changes
                    Undo.RecordObject(obj, $"Set {propertyName}");
                    var converted = ConvertValue(value, prop.PropertyType);

                    Debug.Log($"[MCP] Setting {type.Name}.{propertyName} = {converted} (converted from {value})");

                    prop.SetValue(obj, converted);

                    // Verify the value was actually set
                    var actualValue = prop.GetValue(obj);
                    Debug.Log($"[MCP] After set, {type.Name}.{propertyName} = {actualValue}");

                    // For RectTransform, force canvas update
                    if (obj is RectTransform rectTransform)
                    {
                        Canvas.ForceUpdateCanvases();
                        Debug.Log($"[MCP] Forced Canvas update for RectTransform");
                    }

                    EditorUtility.SetDirty(obj);
                    return;
                }
                catch (Exception ex)
                {
                    var valueStr = value?.ToString() ?? "null";
                    var valueType = value?.GetType().Name ?? "null";
                    throw new InvalidOperationException(
                        $"Failed to set property '{propertyName}' on {type.Name}. " +
                        $"Target type: {prop.PropertyType.Name}, " +
                        $"Provided value: '{valueStr}' ({valueType}). " +
                        $"Error: {ex.Message}");
                }
            }

            // Try field (including private fields with [SerializeField] attribute)
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                // For private fields, require SerializeField attribute
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                {
                    // Private field without SerializeField - skip and continue to error message
                    field = null;
                }
            }

            if (field != null)
            {
                try
                {
                    // IMPORTANT: Record undo BEFORE making changes
                    Undo.RecordObject(obj, $"Set {propertyName}");
                    var converted = ConvertValue(value, field.FieldType);
                    field.SetValue(obj, converted);
                    EditorUtility.SetDirty(obj);
                    return;
                }
                catch (Exception ex)
                {
                    var valueStr = value?.ToString() ?? "null";
                    var valueType = value?.GetType().Name ?? "null";
                    throw new InvalidOperationException(
                        $"Failed to set field '{propertyName}' on {type.Name}. " +
                        $"Target type: {field.FieldType.Name}, " +
                        $"Provided value: '{valueStr}' ({valueType}). " +
                        $"Error: {ex.Message}");
                }
            }

            // Property/field not found - provide helpful suggestions
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToList();
            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Select(f => f.Name)
                .ToList();
            var allMembers = allProperties.Concat(allFields).OrderBy(n => n).ToList();

            var suggestions = "";
            if (allMembers.Count > 0)
            {
                // Find similar names (case-insensitive)
                var similarNames = allMembers
                    .Where(n => n.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (similarNames.Count > 0)
                {
                    suggestions = $" Did you mean: {string.Join(", ", similarNames)}?";
                }
                else
                {
                    suggestions = $" Available members: {string.Join(", ", allMembers.Take(10))}";
                    if (allMembers.Count > 10)
                    {
                        suggestions += $" (and {allMembers.Count - 10} more)";
                    }
                }
            }

            throw new InvalidOperationException($"Property or field '{propertyName}' not found on {type.FullName}.{suggestions}");
        }

        /// <summary>
        /// Converts a value to a target type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The converted value.</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            // Direct assignment if types match
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            // Handle Vector2
            if (targetType == typeof(Vector2) && value is Dictionary<string, object> v2Dict)
            {
                Debug.Log($"[MCP] Converting Dictionary to Vector2. Keys: {string.Join(", ", v2Dict.Keys)}");
                var x = GetFloat(v2Dict, "x");
                var y = GetFloat(v2Dict, "y");
                Debug.Log($"[MCP] Vector2 x={x}, y={y}");

                if (!x.HasValue || !y.HasValue)
                {
                    Debug.LogWarning($"[MCP] Failed to get x or y from Dictionary. x={x}, y={y}. Dict contents: {MiniJson.Serialize(v2Dict)}");
                }

                return new Vector2(
                    x ?? 0f,
                    y ?? 0f
                );
            }

            // Handle Vector3
            if (targetType == typeof(Vector3) && value is Dictionary<string, object> v3Dict)
            {
                return new Vector3(
                    GetFloat(v3Dict, "x") ?? 0f,
                    GetFloat(v3Dict, "y") ?? 0f,
                    GetFloat(v3Dict, "z") ?? 0f
                );
            }

            // Handle Vector4
            if (targetType == typeof(Vector4) && value is Dictionary<string, object> v4Dict)
            {
                return new Vector4(
                    GetFloat(v4Dict, "x") ?? 0f,
                    GetFloat(v4Dict, "y") ?? 0f,
                    GetFloat(v4Dict, "z") ?? 0f,
                    GetFloat(v4Dict, "w") ?? 0f
                );
            }

            // Handle Color
            if (targetType == typeof(Color))
            {
                // Dictionary format: {r:1, g:0, b:0, a:1}
                if (value is Dictionary<string, object> colorDict)
                {
                    return new Color(
                        GetFloat(colorDict, "r") ?? 0f,
                        GetFloat(colorDict, "g") ?? 0f,
                        GetFloat(colorDict, "b") ?? 0f,
                        GetFloat(colorDict, "a") ?? 1f
                    );
                }

                // String color name: "red", "blue", etc.
                if (value is string colorName)
                {
                    try
                    {
                        var rgba = McpConstantConverter.ColorNameToRGBA(colorName);
                        return new Color(
                            (float)rgba["r"],
                            (float)rgba["g"],
                            (float)rgba["b"],
                            (float)rgba["a"]
                        );
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidOperationException($"Invalid color name '{colorName}': {ex.Message}. " +
                            $"Valid colors are: {string.Join(", ", McpConstantConverter.ListColorNames())}");
                    }
                }
            }

            // Handle Rect
            if (targetType == typeof(Rect) && value is Dictionary<string, object> rectDict)
            {
                return new Rect(
                    GetFloat(rectDict, "x") ?? 0f,
                    GetFloat(rectDict, "y") ?? 0f,
                    GetFloat(rectDict, "width") ?? 0f,
                    GetFloat(rectDict, "height") ?? 0f
                );
            }

            // Handle enums
            if (targetType.IsEnum)
            {
                if (value is string strValue)
                {
                    try
                    {
                        // Try exact match first
                        if (Enum.IsDefined(targetType, strValue))
                        {
                            return Enum.Parse(targetType, strValue, false);
                        }

                        // Try case-insensitive match
                        var names = Enum.GetNames(targetType);
                        var matchedName = names.FirstOrDefault(n =>
                            string.Equals(n, strValue, StringComparison.OrdinalIgnoreCase));

                        if (matchedName != null)
                        {
                            return Enum.Parse(targetType, matchedName, false);
                        }

                        // No match found
                        throw new InvalidOperationException(
                            $"Enum value '{strValue}' is not valid for type {targetType.Name}. " +
                            $"Valid values are: {string.Join(", ", names)}");
                    }
                    catch (ArgumentException ex)
                    {
                        var names = Enum.GetNames(targetType);
                        throw new InvalidOperationException(
                            $"Failed to parse enum value '{strValue}' for type {targetType.Name}: {ex.Message}. " +
                            $"Valid values are: {string.Join(", ", names)}");
                    }
                }

                if (value is int intValue)
                {
                    if (Enum.IsDefined(targetType, intValue))
                    {
                        return Enum.ToObject(targetType, intValue);
                    }
                    else
                    {
                        // Allow undefined values but log warning
                        Debug.LogWarning($"Enum value {intValue} is not defined in {targetType.Name}. " +
                            $"Valid values are: {string.Join(", ", Enum.GetValues(targetType).Cast<int>())}");
                        return Enum.ToObject(targetType, intValue);
                    }
                }

                // Try to convert other numeric types to int
                if (value is long || value is short || value is byte || value is sbyte ||
                    value is uint || value is ushort || value is ulong)
                {
                    try
                    {
                        var enumIntValue = Convert.ToInt32(value);
                        if (Enum.IsDefined(targetType, enumIntValue))
                        {
                            return Enum.ToObject(targetType, enumIntValue);
                        }
                        else
                        {
                            Debug.LogWarning($"Enum value {enumIntValue} is not defined in {targetType.Name}. " +
                                $"Valid values are: {string.Join(", ", Enum.GetValues(targetType).Cast<int>())}");
                            return Enum.ToObject(targetType, enumIntValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to convert {value} ({value.GetType().Name}) to enum {targetType.Name}: {ex.Message}");
                    }
                }
            }

            // Handle Unity Object references
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                if (value is Dictionary<string, object> refDict && refDict.ContainsKey("_ref"))
                {
                    var refType = refDict["_ref"].ToString();
                    if (refType == "asset")
                    {
                        // Try GUID first
                        if (refDict.ContainsKey("guid"))
                        {
                            var guid = refDict["guid"].ToString();
                            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                return AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                            }
                        }

                        // Fall back to path
                        if (refDict.ContainsKey("path"))
                        {
                            var path = refDict["path"].ToString();
                            return AssetDatabase.LoadAssetAtPath(path, targetType);
                        }
                    }
                }

                // Direct asset path
                if (value is string assetPathStr)
                {
                    // Handle built-in resources
                    if (assetPathStr.StartsWith("Library/unity default resources::"))
                    {
                        var resourceName = assetPathStr.Substring("Library/unity default resources::".Length);
                        var builtIn = AssetDatabase.GetBuiltinExtraResource(targetType, resourceName);
                        if (builtIn != null)
                        {
                            return builtIn;
                        }
                    }

                    // Regular asset
                    return AssetDatabase.LoadAssetAtPath(assetPathStr, targetType);
                }
            }

            // Primitive type conversions
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                // Provide detailed error message with conversion context
                var valueStr = value?.ToString() ?? "null";
                var valueType = value?.GetType().Name ?? "null";

                throw new InvalidOperationException(
                    $"Cannot convert value '{valueStr}' of type {valueType} to {targetType.Name}. " +
                    $"Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures a value is not null or empty, throwing an exception if it is.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>The original value if it's valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is null or empty.</exception>
        private static string EnsureValue(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"{paramName} is required and cannot be null or empty");
            }
            return value;
        }

        /// <summary>
        /// Ensures the directory for a given file path exists, creating it if necessary.
        /// </summary>
        /// <param name="filePath">The file path whose directory should exist.</param>
        private static void EnsureDirectoryExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        #endregion
    }
}

