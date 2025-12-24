using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            // Map operations
            "listMaps",
            "getMapById",
            "getMaps",
            "createMap",
            "updateMap",
            "deleteMap",
            "getMapData",
            "setMapData",
            // Event operations
            "listMapEvents",
            "getMapEventById",
            "getMapEvents",
            "createMapEvent",
            "updateMapEvent",
            "deleteMapEvent",
            // Tileset operations
            "listTilesets",
            "getTilesetById",
            "getTilesets",
            "setTileset",
            // Settings and utility
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
                // Map operations
                "listMaps" => ListMaps(payload),
                "getMapById" => GetMapById(payload),
                "getMaps" => GetMaps(payload),
                "createMap" => CreateMap(payload),
                "updateMap" => UpdateMap(payload),
                "deleteMap" => DeleteMap(payload),
                "getMapData" => GetMapData(payload),
                "setMapData" => SetMapData(payload),
                // Event operations
                "listMapEvents" => ListMapEvents(payload),
                "getMapEventById" => GetMapEventById(payload),
                "getMapEvents" => GetMapEvents(payload),
                "createMapEvent" => CreateMapEvent(payload),
                "updateMapEvent" => UpdateMapEvent(payload),
                "deleteMapEvent" => DeleteMapEvent(payload),
                // Tileset operations
                "listTilesets" => ListTilesets(payload),
                "getTilesetById" => GetTilesetById(payload),
                "getTilesets" => GetTilesets(payload),
                "setTileset" => SetTileset(payload),
                // Settings and utility
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
            var readOnlyOperations = new[] {
                "listMaps", "getMapById", "getMaps", "getMapData",
                "listMapEvents", "getMapEventById", "getMapEvents",
                "listTilesets", "getTilesetById", "getTilesets",
                "getMapSettings"
            };
            return !readOnlyOperations.Contains(operation);
        }

        #region Map Operations

        private object ListMaps(Dictionary<string, object> payload)
        {
            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            if (!Directory.Exists(mapPath))
            {
                return CreatePaginatedResponse("maps", new List<Dictionary<string, object>>(), payload);
            }

            var mapFiles = Directory.GetFiles(mapPath, "*.json");
            var maps = new List<Dictionary<string, object>>();

            foreach (var file in mapFiles)
            {
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["id"]?.ToString();
                            var name = item["name"]?.ToString() ?? "Unnamed";
                            if (!string.IsNullOrEmpty(uuId))
                            {
                                maps.Add(new Dictionary<string, object>
                                {
                                    ["uuId"] = uuId,
                                    ["name"] = name,
                                    ["filename"] = filename
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse map file {file}: {ex.Message}");
                }
            }

            return CreatePaginatedResponse("maps", maps, payload);
        }

        private object GetMapById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            if (!Directory.Exists(mapPath))
            {
                throw new InvalidOperationException("Map directory not found.");
            }

            var mapFiles = Directory.GetFiles(mapPath, "*.json");

            foreach (var file in mapFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemUuId = item["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("filename", Path.GetFileNameWithoutExtension(file)),
                                ("data", ConvertJTokenToObject(item))
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Map with uuId '{uuId}' not found.");
        }

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
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);
                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["id"]?.ToString();
                            maps.Add(new Dictionary<string, object>
                            {
                                ["uuId"] = uuId,
                                ["filename"] = filename,
                                ["name"] = item["name"]?.ToString() ?? "Unnamed Map",
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
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
            var targetFile = GetString(payload, "filename") ?? "mapInfo";

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            if (!mapData.ContainsKey("id") || string.IsNullOrEmpty(mapData["id"]?.ToString()))
            {
                mapData["id"] = Guid.NewGuid().ToString();
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            Directory.CreateDirectory(mapPath);

            var filePath = Path.Combine(mapPath, $"{targetFile}.json");

            JArray array;
            if (File.Exists(filePath))
            {
                var existing = ReadJsonFile(filePath);
                array = existing as JArray ?? new JArray();
            }
            else
            {
                array = new JArray();
            }

            array.Add(JObject.FromObject(mapData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("uuId", mapData["id"]),
                ("filename", targetFile),
                ("message", $"Map created successfully.")
            );
        }

        private object UpdateMap(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var mapFiles = Directory.GetFiles(mapPath, "*.json");

            foreach (var file in mapFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemUuId = array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            if (partialUpdate)
                            {
                                var existing = array[i] as JObject;
                                existing?.Merge(JObject.FromObject(mapData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace,
                                    MergeNullValueHandling = MergeNullValueHandling.Merge
                                });
                            }
                            else
                            {
                                mapData["id"] = uuId;
                                array[i] = JObject.FromObject(mapData);
                            }

                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Map updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Map with uuId '{uuId}' not found.");
        }

        private object DeleteMap(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var mapFiles = Directory.GetFiles(mapPath, "*.json");

            foreach (var file in mapFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = array.Count - 1; i >= 0; i--)
                    {
                        var itemUuId = array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            array.RemoveAt(i);
                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Map deleted successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Map with uuId '{uuId}' not found.");
        }

        private object GetMapData(Dictionary<string, object> payload)
        {
            var id = GetId(payload);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{id}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{id}' not found.");
            }

            var data = ReadJsonFile(filePath);

            return CreateSuccessResponse(
                ("id", id),
                ("mapData", ConvertJTokenToObject(data))
            );
        }

        private object SetMapData(Dictionary<string, object> payload)
        {
            var id = GetId(payload);
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{id}.json");

            JToken finalData;
            if (partialUpdate && File.Exists(filePath))
            {
                var existing = ReadJsonFile(filePath);
                finalData = MergeData(existing, mapData);
            }
            else
            {
                finalData = JToken.FromObject(mapData);
            }

            WriteJsonFile(filePath, finalData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"Map data for '{id}' updated successfully.")
            );
        }

        #endregion

        #region Map Event Operations

        private object ListMapEvents(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"] as JArray ?? new JArray();

            var eventList = new List<Dictionary<string, object>>();
            foreach (var evt in events)
            {
                eventList.Add(new Dictionary<string, object>
                {
                    ["id"] = evt["id"]?.ToString(),
                    ["name"] = evt["name"]?.ToString() ?? "Unnamed"
                });
            }

            return CreateSuccessResponse(
                ("events", eventList),
                ("count", eventList.Count),
                ("mapId", mapId)
            );
        }

        private object GetMapEventById(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var eventId = GetString(payload, "eventId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"] as JArray;

            if (events == null)
            {
                throw new InvalidOperationException("No events found in map.");
            }

            foreach (var evt in events)
            {
                if (evt["id"]?.ToString() == eventId)
                {
                    return CreateSuccessResponse(
                        ("eventId", eventId),
                        ("data", ConvertJTokenToObject(evt)),
                        ("mapId", mapId)
                    );
                }
            }

            throw new InvalidOperationException($"Event '{eventId}' not found.");
        }

        private object GetMapEvents(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"]?.ToObject<List<Dictionary<string, object>>>() ?? new List<Dictionary<string, object>>();

            return CreateSuccessResponse(
                ("events", events),
                ("mapId", mapId)
            );
        }

        private object CreateMapEvent(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"] as JArray ?? new JArray();
            events.Add(JObject.FromObject(eventData));
            mapData["events"] = events;

            WriteJsonFile(filePath, mapData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("mapId", mapId),
                ("message", $"Event added to map '{mapId}' successfully.")
            );
        }

        private object UpdateMapEvent(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var eventId = GetString(payload, "eventId");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
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
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"] as JArray;

            if (events == null)
            {
                throw new InvalidOperationException("No events found in map.");
            }

            bool eventFound = false;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i]["id"]?.ToString() == eventId)
                {
                    if (partialUpdate)
                    {
                        var existing = events[i] as JObject;
                        existing.Merge(JObject.FromObject(eventData), new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Replace
                        });
                    }
                    else
                    {
                        events[i] = JObject.FromObject(eventData);
                    }
                    eventFound = true;
                    break;
                }
            }

            if (!eventFound)
            {
                throw new InvalidOperationException($"Event '{eventId}' not found.");
            }

            WriteJsonFile(filePath, mapData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("eventId", eventId),
                ("mapId", mapId),
                ("message", $"Event '{eventId}' in map '{mapId}' updated successfully.")
            );
        }

        private object DeleteMapEvent(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var eventId = GetString(payload, "eventId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            var events = mapData["events"] as JArray;

            if (events == null)
            {
                throw new InvalidOperationException("No events found in map.");
            }

            bool eventFound = false;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i]["id"]?.ToString() == eventId)
                {
                    events.RemoveAt(i);
                    eventFound = true;
                    break;
                }
            }

            if (!eventFound)
            {
                throw new InvalidOperationException($"Event '{eventId}' not found.");
            }

            WriteJsonFile(filePath, mapData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("eventId", eventId),
                ("mapId", mapId),
                ("message", $"Event '{eventId}' deleted from map '{mapId}' successfully.")
            );
        }

        #endregion

        #region Tileset Operations

        private object ListTilesets(Dictionary<string, object> payload)
        {
            var tilesetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");
            var tilesets = new List<Dictionary<string, object>>();

            if (Directory.Exists(tilesetPath))
            {
                var tilesetDirs = Directory.GetDirectories(tilesetPath);
                foreach (var dir in tilesetDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    tilesets.Add(new Dictionary<string, object>
                    {
                        ["id"] = dirName,
                        ["category"] = dirName
                    });
                }
            }

            return CreatePaginatedResponse("tilesets", tilesets, payload);
        }

        private object GetTilesetById(Dictionary<string, object> payload)
        {
            var tilesetId = GetString(payload, "tilesetId");

            if (string.IsNullOrEmpty(tilesetId))
            {
                throw new InvalidOperationException("Tileset ID is required.");
            }

            var tilesetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images", tilesetId);

            if (!Directory.Exists(tilesetPath))
            {
                throw new InvalidOperationException($"Tileset '{tilesetId}' not found.");
            }

            var files = Directory.GetFiles(tilesetPath, "*.png")
                .Concat(Directory.GetFiles(tilesetPath, "*.jpg"))
                .Concat(Directory.GetFiles(tilesetPath, "*.jpeg"))
                .ToArray();

            var filesList = files.Select(f => new Dictionary<string, object>
            {
                ["name"] = Path.GetFileNameWithoutExtension(f),
                ["filename"] = Path.GetFileName(f),
                ["path"] = f.Replace(Application.dataPath, "Assets")
            }).ToList();

            return CreateSuccessResponse(
                ("tilesetId", tilesetId),
                ("files", filesList),
                ("count", filesList.Count)
            );
        }

        private object GetTilesets(Dictionary<string, object> payload)
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

            return CreatePaginatedResponse("tilesets", tilesets, payload);
        }

        private object SetTileset(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var tilesetId = GetString(payload, "tilesetId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (string.IsNullOrEmpty(tilesetId))
            {
                throw new InvalidOperationException("Tileset ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            mapData["tilesetId"] = tilesetId;

            WriteJsonFile(filePath, mapData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("mapId", mapId),
                ("tilesetId", tilesetId),
                ("message", $"Tileset for map '{mapId}' set to '{tilesetId}'.")
            );
        }

        #endregion

        #region Map Settings

        private object GetMapSettings(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);

            var settings = new Dictionary<string, object>
            {
                ["name"] = mapData["name"]?.ToString(),
                ["width"] = mapData["width"]?.ToObject<int?>(),
                ["height"] = mapData["height"]?.ToObject<int?>(),
                ["tilesetId"] = mapData["tilesetId"]?.ToString(),
                ["parallaxName"] = mapData["parallaxName"]?.ToString(),
                ["battleback1Name"] = mapData["battleback1Name"]?.ToString(),
                ["battleback2Name"] = mapData["battleback2Name"]?.ToString(),
                ["bgm"] = mapData["bgm"]?.ToObject<Dictionary<string, object>>(),
                ["bgs"] = mapData["bgs"]?.ToObject<Dictionary<string, object>>(),
                ["encounterList"] = mapData["encounterList"]?.ToObject<List<object>>(),
                ["encounterStep"] = mapData["encounterStep"]?.ToObject<int?>()
            };

            return CreateSuccessResponse(
                ("settings", settings),
                ("mapId", mapId)
            );
        }

        private object UpdateMapSettings(Dictionary<string, object> payload)
        {
            var mapId = GetId(payload);
            var settings = GetPayloadValue<Dictionary<string, object>>(payload, "settings");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Settings data is required.");
            }

            var mapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Map", "JSON");
            var filePath = Path.Combine(mapPath, $"{mapId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Map '{mapId}' not found.");
            }

            var mapData = ReadJsonFile(filePath);
            mapData = MergeData(mapData, settings);

            WriteJsonFile(filePath, mapData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("mapId", mapId),
                ("message", $"Settings for map '{mapId}' updated successfully.")
            );
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

        private string GetId(Dictionary<string, object> payload)
        {
            return GetString(payload, "id") ?? GetString(payload, "filename");
        }

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

        private JToken ReadJsonFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return JToken.Parse(content);
        }

        private object ConvertJTokenToObject(JToken token)
        {
            return token.ToObject<object>();
        }

        private void WriteJsonFile(string filePath, JToken data)
        {
            var json = data.ToString(Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private JToken MergeData(JToken existing, Dictionary<string, object> updates)
        {
            if (existing is JObject existingObj)
            {
                var updateJson = JObject.FromObject(updates);
                existingObj.Merge(updateJson, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                });
                return existingObj;
            }
            return JToken.FromObject(updates);
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
