using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Services;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker map management handler.
    /// Handles operations for maps, map events, and tilesets.
    /// Uses EditorDataService for CRUD operations via the RPGMaker Editor API.
    /// </summary>
    public class RPGMakerMapHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerMap";
        public override string Version => "1.0.0";

        // Access the EditorDataService singleton for map operations
        private EditorDataService DataService => EditorDataService.Instance;

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
            var maps = DataService.LoadMaps();
            var result = maps.Select(m => new Dictionary<string, object>
            {
                ["id"] = m.id,
                ["uuId"] = m.id,
                ["name"] = m.name ?? "Unnamed Map",
                ["displayName"] = m.displayName ?? m.name ?? "Unnamed Map",
                ["width"] = m.width,
                ["height"] = m.height
            }).ToList();

            return CreatePaginatedResponse("maps", result, payload);
        }

        private object GetMapById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var map = DataService.LoadMapById(id);
            if (map == null)
            {
                throw new InvalidOperationException($"Map with id '{id}' not found.");
            }

            return CreateSuccessResponse(
                ("id", map.id),
                ("uuId", map.id),
                ("data", DataModelMapper.ToDict(map))
            );
        }

        private object GetMaps(Dictionary<string, object> payload)
        {
            var maps = DataService.LoadMaps();
            var result = maps.Select(m => new Dictionary<string, object>
            {
                ["id"] = m.id,
                ["uuId"] = m.id,
                ["name"] = m.name ?? "Unnamed Map",
                ["data"] = DataModelMapper.ToDict(m)
            }).ToList();

            return CreateSuccessResponse(
                ("maps", result),
                ("count", result.Count)
            );
        }

        private object CreateMap(Dictionary<string, object> payload)
        {
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");

            // Get optional name from payload
            string name = null;
            if (mapData != null && mapData.TryGetValue("name", out var nameObj))
            {
                name = nameObj?.ToString();
            }

            // Create map using EditorDataService
            var newMap = DataService.CreateMap(name);

            // Apply any additional data from payload
            if (mapData != null && mapData.Count > 0)
            {
                DataService.UpdateMap(newMap.id, m =>
                {
                    DataModelMapper.ApplyPartialUpdate(m, mapData);
                });
            }

            return CreateSuccessResponse(
                ("id", newMap.id),
                ("uuId", newMap.id),
                ("message", "Map created successfully.")
            );
        }

        private object UpdateMap(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateMap(id, m =>
            {
                DataModelMapper.ApplyPartialUpdate(m, mapData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Map updated successfully.")
            );
        }

        private object DeleteMap(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteMap(id);

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Map deleted successfully.")
            );
        }

        private object GetMapData(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID is required.");
            }

            var map = DataService.LoadMapById(id);
            if (map == null)
            {
                throw new InvalidOperationException($"Map with id '{id}' not found.");
            }

            return CreateSuccessResponse(
                ("id", id),
                ("mapData", DataModelMapper.ToDict(map))
            );
        }

        private object SetMapData(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var mapData = GetPayloadValue<Dictionary<string, object>>(payload, "mapData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID is required.");
            }

            if (mapData == null)
            {
                throw new InvalidOperationException("Map data is required.");
            }

            DataService.UpdateMap(id, m =>
            {
                DataModelMapper.ApplyPartialUpdate(m, mapData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"Map data for '{id}' updated successfully.")
            );
        }

        #endregion

        #region Map Event Operations

        private object ListMapEvents(Dictionary<string, object> payload)
        {
            var mapId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var events = DataService.LoadMapEventsByMapId(mapId);
            var result = events.Select(e => new Dictionary<string, object>
            {
                ["id"] = e.eventId,
                ["eventId"] = e.eventId,
                ["name"] = e.name ?? "Unnamed Event",
                ["x"] = e.x,
                ["y"] = e.y,
                ["pageCount"] = e.pages?.Count ?? 0
            }).ToList();

            return CreateSuccessResponse(
                ("events", result),
                ("count", result.Count),
                ("mapId", mapId)
            );
        }

        private object GetMapEventById(Dictionary<string, object> payload)
        {
            var eventId = GetString(payload, "eventId") ?? GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            var mapEvent = DataService.LoadMapEventById(eventId);
            if (mapEvent == null)
            {
                throw new InvalidOperationException($"Map event with eventId '{eventId}' not found.");
            }

            return CreateSuccessResponse(
                ("eventId", mapEvent.eventId),
                ("mapId", mapEvent.mapId),
                ("data", DataModelMapper.ToDict(mapEvent))
            );
        }

        private object GetMapEvents(Dictionary<string, object> payload)
        {
            var mapId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var events = DataService.LoadMapEventsByMapId(mapId);
            var result = events.Select(e => new Dictionary<string, object>
            {
                ["eventId"] = e.eventId,
                ["name"] = e.name ?? "Unnamed Event",
                ["data"] = DataModelMapper.ToDict(e)
            }).ToList();

            return CreateSuccessResponse(
                ("events", result),
                ("count", result.Count),
                ("mapId", mapId)
            );
        }

        private object CreateMapEvent(Dictionary<string, object> payload)
        {
            var mapId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            // Get position from payload
            int x = 0, y = 0;
            if (eventData != null)
            {
                if (eventData.TryGetValue("x", out var xObj))
                    x = Convert.ToInt32(xObj);
                if (eventData.TryGetValue("y", out var yObj))
                    y = Convert.ToInt32(yObj);
            }

            // Create map event using EditorDataService
            var newEvent = DataService.CreateMapEvent(mapId, x, y);

            // Apply any additional data from payload
            if (eventData != null && eventData.Count > 0)
            {
                DataService.UpdateMapEvent(newEvent.eventId, e =>
                {
                    DataModelMapper.ApplyPartialUpdate(e, eventData);
                });
            }

            return CreateSuccessResponse(
                ("eventId", newEvent.eventId),
                ("mapId", mapId),
                ("message", "Map event created successfully.")
            );
        }

        private object UpdateMapEvent(Dictionary<string, object> payload)
        {
            var eventId = GetString(payload, "eventId") ?? GetString(payload, "uuId") ?? GetString(payload, "id");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            DataService.UpdateMapEvent(eventId, e =>
            {
                DataModelMapper.ApplyPartialUpdate(e, eventData);
            });

            return CreateSuccessResponse(
                ("eventId", eventId),
                ("message", "Map event updated successfully.")
            );
        }

        private object DeleteMapEvent(Dictionary<string, object> payload)
        {
            var eventId = GetString(payload, "eventId") ?? GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(eventId))
            {
                throw new InvalidOperationException("Event ID is required.");
            }

            DataService.DeleteMapEvent(eventId);

            return CreateSuccessResponse(
                ("eventId", eventId),
                ("message", "Map event deleted successfully.")
            );
        }

        #endregion

        #region Tileset Operations

        private object ListTilesets(Dictionary<string, object> payload)
        {
            // Tilesets are stored as image directories
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
            var tilesetId = GetString(payload, "tilesetId") ?? GetString(payload, "id");

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
                        ["files"] = filesList,
                        ["count"] = filesList.Count
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
            var mapId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");
            var tilesetId = GetString(payload, "tilesetId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (string.IsNullOrEmpty(tilesetId))
            {
                throw new InvalidOperationException("Tileset ID is required.");
            }

            // Note: Tileset assignment in RPGMaker Unite is more complex
            // This is a simplified implementation
            return CreateSuccessResponse(
                ("mapId", mapId),
                ("tilesetId", tilesetId),
                ("message", "Tileset assignment noted. Full tileset support requires additional map layer operations.")
            );
        }

        #endregion

        #region Map Settings

        private object GetMapSettings(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var map = DataService.LoadMapById(id);
            if (map == null)
            {
                throw new InvalidOperationException($"Map with id '{id}' not found.");
            }

            var settings = new Dictionary<string, object>
            {
                ["name"] = map.name,
                ["displayName"] = map.displayName,
                ["width"] = map.width,
                ["height"] = map.height,
                ["scrollType"] = (int)map.scrollType,
                ["autoPlayBGM"] = map.autoPlayBGM,
                ["bgmID"] = map.bgmID,
                ["autoPlayBgs"] = map.autoPlayBgs,
                ["bgsID"] = map.bgsID,
                ["forbidDash"] = map.forbidDash,
                ["memo"] = map.memo
            };

            return CreateSuccessResponse(
                ("settings", settings),
                ("mapId", id)
            );
        }

        private object UpdateMapSettings(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "mapId");
            var settings = GetPayloadValue<Dictionary<string, object>>(payload, "settings");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Settings data is required.");
            }

            DataService.UpdateMap(id, map =>
            {
                DataModelMapper.ApplyPartialUpdate(map, settings);
            });

            return CreateSuccessResponse(
                ("mapId", id),
                ("message", "Map settings updated successfully.")
            );
        }

        #endregion

        #region Copy/Export/Import

        private object CopyMap(Dictionary<string, object> payload)
        {
            var sourceId = GetString(payload, "sourceFilename") ?? GetString(payload, "uuId") ?? GetString(payload, "id");
            var targetName = GetString(payload, "targetFilename");

            if (string.IsNullOrEmpty(sourceId))
            {
                throw new InvalidOperationException("Source map ID is required.");
            }

            var sourceMap = DataService.LoadMapById(sourceId);
            if (sourceMap == null)
            {
                throw new InvalidOperationException($"Source map '{sourceId}' not found.");
            }

            // Create a new map based on the source
            var newMapName = targetName ?? $"{sourceMap.name} (Copy)";
            var newMap = DataService.CreateMap(newMapName);

            // Copy basic properties (note: full copy would require copying tile data too)
            DataService.UpdateMap(newMap.id, m =>
            {
                m.width = sourceMap.width;
                m.height = sourceMap.height;
                m.scrollType = sourceMap.scrollType;
                m.autoPlayBGM = sourceMap.autoPlayBGM;
                m.bgmID = sourceMap.bgmID;
                m.autoPlayBgs = sourceMap.autoPlayBgs;
                m.bgsID = sourceMap.bgsID;
                m.forbidDash = sourceMap.forbidDash;
                m.memo = sourceMap.memo;
            });

            return CreateSuccessResponse(
                ("sourceId", sourceId),
                ("newId", newMap.id),
                ("message", $"Map copied successfully. Note: Tile data requires manual copy.")
            );
        }

        private object ExportMap(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "filename") ?? GetString(payload, "uuId") ?? GetString(payload, "id");
            var exportPath = GetString(payload, "exportPath");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var map = DataService.LoadMapById(id);
            if (map == null)
            {
                throw new InvalidOperationException($"Map '{id}' not found.");
            }

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = Path.Combine(Application.dataPath, "..", "Map_Exports");
            }

            Directory.CreateDirectory(exportPath);
            var targetFilePath = Path.Combine(exportPath, $"{map.name ?? id}.json");

            var mapDict = DataModelMapper.ToDict(map);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(mapDict, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(targetFilePath, json);

            return CreateSuccessResponse(
                ("exportPath", targetFilePath),
                ("message", $"Map '{map.name}' exported successfully.")
            );
        }

        private object ImportMap(Dictionary<string, object> payload)
        {
            var importFilePath = GetString(payload, "importFilePath");
            var targetName = GetString(payload, "targetFilename");

            if (string.IsNullOrEmpty(importFilePath) || !File.Exists(importFilePath))
            {
                throw new InvalidOperationException("Valid import file path is required.");
            }

            // Read and parse the JSON file
            var json = File.ReadAllText(importFilePath);
            var mapData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Create a new map with the imported data
            var name = targetName ?? mapData?.GetValueOrDefault("name")?.ToString() ?? Path.GetFileNameWithoutExtension(importFilePath);
            var newMap = DataService.CreateMap(name);

            // Apply imported data
            if (mapData != null)
            {
                // Remove id to preserve the new ID
                mapData.Remove("id");
                DataService.UpdateMap(newMap.id, m =>
                {
                    DataModelMapper.ApplyPartialUpdate(m, mapData);
                });
            }

            return CreateSuccessResponse(
                ("newId", newMap.id),
                ("message", $"Map imported as '{name}' successfully.")
            );
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

        #endregion
    }
}
