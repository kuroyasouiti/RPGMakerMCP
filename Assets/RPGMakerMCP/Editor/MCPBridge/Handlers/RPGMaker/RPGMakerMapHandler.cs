using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker map management handler.
    /// Handles operations for maps, map events, and tilesets.
    /// </summary>
    public class RPGMakerMapHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerMap";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getMaps",
            "createMap",
            "updateMap",
            "deleteMap",
            "getMapData",
            "setMapData",
            "getMapEvents",
            "createMapEvent",
            "updateMapEvent",
            "deleteMapEvent",
            "getTilesets",
            "setTileset",
            "getMapSettings",
            "updateMapSettings",
            "copyMap",
            "exportMap",
            "importMap"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getMaps" => GetMaps(payload),
                "createMap" => CreateMap(payload),
                "updateMap" => UpdateMap(payload),
                "deleteMap" => DeleteMap(payload),
                "getMapData" => GetMapData(payload),
                "setMapData" => SetMapData(payload),
                "getMapEvents" => GetMapEvents(payload),
                "createMapEvent" => CreateMapEvent(payload),
                "updateMapEvent" => UpdateMapEvent(payload),
                "deleteMapEvent" => DeleteMapEvent(payload),
                "getTilesets" => GetTilesets(),
                "setTileset" => SetTileset(payload),
                "getMapSettings" => GetMapSettings(payload),
                "updateMapSettings" => UpdateMapSettings(payload),
                "copyMap" => CopyMap(payload),
                "exportMap" => ExportMap(payload),
                "importMap" => ImportMap(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] { "getMaps", "getMapData", "getMapEvents", "getTilesets", "getMapSettings" };
            return !readOnlyOperations.Contains(operation);
        }

        #region Map Operations

        private object GetMaps(Dictionary<string, object> payload)
        {
            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            if (!Directory.Exists(mapPath))
            {
                return CreateSuccessResponse(("maps", new List<object>()), ("message", "No map data found."));
            }

            var mapFiles = Directory.GetFiles(mapPath, "*.json");
            var maps = new List<Dictionary<string, object>>();

            foreach (var file in mapFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    maps.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = mapData?.ContainsKey("name") == true ? mapData["name"]?.ToString() : "Unnamed Map",
                        ["width"] = mapData?.ContainsKey("width") == true ? mapData["width"] : 0,
                        ["height"] = mapData?.ContainsKey("height") == true ? mapData["height"] : 0,
                        ["tilesetId"] = mapData?.ContainsKey("tilesetId") == true ? mapData["tilesetId"]?.ToString() : null,
                        ["data"] = mapData
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse map file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("maps", maps),
                ("count", maps.Count)
            );
        }

        private object CreateMap(Dictionary<string, object> payload)
        {
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");
            var filename = GetString(payload, "filename");

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"map_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            else
            {
                filename = SanitizeFilename(filename);
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            Directory.CreateDirectory(mapPath);

            var filePath = Path.Combine(mapPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(mapData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("path", filePath),
                ("message", $"Map '{filename}' created successfully.")
            );
        }

        private object UpdateMap(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Map '{filename}' updated successfully."));
        }

        private object DeleteMap(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Map '{filename}' deleted successfully."));
        }

        private object GetMapData(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content);

            return CreateSuccessResponse(
                ("mapData", mapData),
                ("filename", filename)
            );
        }

        private object SetMapData(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Map data for '{filename}' updated successfully."));
        }

        #endregion

        #region Map Event Operations

        private object GetMapEvents(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var events = mapData?.ContainsKey("events") == true ? mapData["events"] : new List<object>();

            return CreateSuccessResponse(
                ("events", events),
                ("filename", filename)
            );
        }

        private object CreateMapEvent(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            if (!mapData.ContainsKey("events") || mapData["events"] == null)
            {
                mapData["events"] = new List<object>();
            }

            var events = mapData["events"] as List<object> ?? new List<object>();
            events.Add(eventData);
            mapData["events"] = events;

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event added to map '{filename}' successfully."));
        }

        private object UpdateMapEvent(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var eventId = GetString(payload, "eventId");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var events = mapData?["events"] as List<object>;

            if (events == null)
            {
                throw new InvalidOperationException("No events found in map.");
            }

            bool eventFound = false;
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i] as Dictionary<string, object>;
                if (evt?.ContainsKey("id") == true && evt["id"]?.ToString() == eventId)
                {
                    events[i] = eventData;
                    eventFound = true;
                    break;
                }
            }

            if (!eventFound)
            {
                throw new InvalidOperationException($"Event with ID '{eventId}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event '{eventId}' in map '{filename}' updated successfully."));
        }

        private object DeleteMapEvent(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var eventId = GetString(payload, "eventId");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var events = mapData?["events"] as List<object>;

            if (events == null)
            {
                throw new InvalidOperationException("No events found in map.");
            }

            bool eventFound = false;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var evt = events[i] as Dictionary<string, object>;
                if (evt?.ContainsKey("id") == true && evt["id"]?.ToString() == eventId)
                {
                    events.RemoveAt(i);
                    eventFound = true;
                    break;
                }
            }

            if (!eventFound)
            {
                throw new InvalidOperationException($"Event with ID '{eventId}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event '{eventId}' deleted from map '{filename}' successfully."));
        }

        #endregion

        #region Tileset Operations

        private object GetTilesets()
        {
            var tilesetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");
            var tilesets = new List<Dictionary<string, object>>();

            if (Directory.Exists(tilesetPath))
            {
                var tilesetDirs = Directory.GetDirectories(tilesetPath);
                foreach (var dir in tilesetDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    var files = Directory.GetFiles(dir, "*.png")
                        .Concat(Directory.GetFiles(dir, "*.jpg"))
                        .Concat(Directory.GetFiles(dir, "*.jpeg"))
                        .ToArray();

                    var filesList = files.Select(f => new Dictionary<string, object>
                    {
                        ["name"] = Path.GetFileNameWithoutExtension(f),
                        ["path"] = f.Replace(Application.dataPath, "Assets")
                    }).ToList();

                    tilesets.Add(new Dictionary<string, object>
                    {
                        ["category"] = dirName,
                        ["files"] = filesList
                    });
                }
            }

            return CreateSuccessResponse(
                ("tilesets", tilesets),
                ("count", tilesets.Count)
            );
        }

        private object SetTileset(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var tilesetId = GetString(payload, "tilesetId");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(tilesetId))
            {
                throw new InvalidOperationException("Tileset ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            mapData["tilesetId"] = tilesetId;

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Tileset for map '{filename}' set to '{tilesetId}'."));
        }

        #endregion

        #region Map Settings

        private object GetMapSettings(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            var settings = new Dictionary<string, object>
            {
                ["name"] = mapData?.ContainsKey("name") == true ? mapData["name"] : null,
                ["width"] = mapData?.ContainsKey("width") == true ? mapData["width"] : null,
                ["height"] = mapData?.ContainsKey("height") == true ? mapData["height"] : null,
                ["tilesetId"] = mapData?.ContainsKey("tilesetId") == true ? mapData["tilesetId"] : null,
                ["parallaxName"] = mapData?.ContainsKey("parallaxName") == true ? mapData["parallaxName"] : null,
                ["battleback1Name"] = mapData?.ContainsKey("battleback1Name") == true ? mapData["battleback1Name"] : null,
                ["battleback2Name"] = mapData?.ContainsKey("battleback2Name") == true ? mapData["battleback2Name"] : null,
                ["bgm"] = mapData?.ContainsKey("bgm") == true ? mapData["bgm"] : null,
                ["bgs"] = mapData?.ContainsKey("bgs") == true ? mapData["bgs"] : null,
                ["encounterList"] = mapData?.ContainsKey("encounterList") == true ? mapData["encounterList"] : null,
                ["encounterStep"] = mapData?.ContainsKey("encounterStep") == true ? mapData["encounterStep"] : null
            };

            return CreateSuccessResponse(
                ("settings", settings),
                ("filename", filename)
            );
        }

        private object UpdateMapSettings(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var settings = GetPayloadValue<Dictionary<string, object>>(payload, "settings");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Settings data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var mapData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            foreach (var kvp in settings)
            {
                mapData[kvp.Key] = kvp.Value;
            }

            File.WriteAllText(filePath, MiniJson.Serialize(mapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Settings for map '{filename}' updated successfully."));
        }

        #endregion

        #region Copy/Export/Import

        private object CopyMap(Dictionary<string, object> payload)
        {
            var sourceFilename = GetString(payload, "sourceFilename");
            var targetFilename = GetString(payload, "targetFilename");

            if (string.IsNullOrEmpty(sourceFilename))
            {
                throw new InvalidOperationException("Source filename is required.");
            }

            if (string.IsNullOrEmpty(targetFilename))
            {
                targetFilename = $"{sourceFilename}_copy_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var sourceFilePath = Path.Combine(mapPath, $"{sourceFilename}.json");
            var targetFilePath = Path.Combine(mapPath, $"{targetFilename}.json");

            if (!File.Exists(sourceFilePath))
            {
                throw new InvalidOperationException($"Source map file '{sourceFilename}' not found.");
            }

            if (File.Exists(targetFilePath))
            {
                throw new InvalidOperationException($"Target map file '{targetFilename}' already exists.");
            }

            File.Copy(sourceFilePath, targetFilePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Map '{sourceFilename}' copied to '{targetFilename}' successfully."));
        }

        private object ExportMap(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var exportPath = GetString(payload, "exportPath");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = Path.Combine(Application.dataPath, "..", "Map_Exports");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var sourceFilePath = Path.Combine(mapPath, $"{filename}.json");

            if (!File.Exists(sourceFilePath))
            {
                throw new InvalidOperationException($"Map file '{filename}' not found.");
            }

            Directory.CreateDirectory(exportPath);
            var targetFilePath = Path.Combine(exportPath, $"{filename}.json");
            File.Copy(sourceFilePath, targetFilePath, true);

            return CreateSuccessResponse(
                ("exportPath", exportPath),
                ("message", $"Map '{filename}' exported to '{exportPath}' successfully.")
            );
        }

        private object ImportMap(Dictionary<string, object> payload)
        {
            var importFilePath = GetString(payload, "importFilePath");
            var targetFilename = GetString(payload, "targetFilename");

            if (string.IsNullOrEmpty(importFilePath) || !File.Exists(importFilePath))
            {
                throw new InvalidOperationException("Valid import file path is required.");
            }

            if (string.IsNullOrEmpty(targetFilename))
            {
                targetFilename = Path.GetFileNameWithoutExtension(importFilePath);
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var targetFilePath = Path.Combine(mapPath, $"{targetFilename}.json");

            Directory.CreateDirectory(mapPath);
            File.Copy(importFilePath, targetFilePath, true);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Map imported as '{targetFilename}' successfully."));
        }

        #endregion

        #region Helper Methods

        private T GetPayloadValue<T>(Dictionary<string, object> payload, string key) where T : class
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                return value as T;
            }
            return null;
        }

        private string SanitizeFilename(string filename)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                filename = filename.Replace(c, '_');
            }
            return filename;
        }

        private void RefreshHierarchy()
        {
            try
            {
                var hierarchyType = Type.GetType("RPGMaker.Codebase.Editor.Hierarchy.Hierarchy, Assembly-CSharp-Editor");
                if (hierarchyType != null)
                {
                    var refreshMethod = hierarchyType.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    refreshMethod?.Invoke(null, null);
                }
            }
            catch
            {
                // Ignore if hierarchy refresh is not available
            }
        }

        #endregion
    }
}
