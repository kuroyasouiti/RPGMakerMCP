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
    /// RPGMaker event management handler.
    /// Handles operations for common events, map events, and event commands.
    /// </summary>
    public class RPGMakerEventHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerEvent";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            // Common event operations
            "listCommonEvents",
            "getCommonEventById",
            "getCommonEvents",
            "createCommonEvent",
            "updateCommonEvent",
            "deleteCommonEvent",
            // Event command operations
            "getEventCommands",
            "createEventCommand",
            "updateEventCommand",
            "deleteEventCommand",
            // Event page operations
            "getEventPages",
            "createEventPage",
            "updateEventPage",
            "deleteEventPage",
            // Utility
            "copyEvent",
            "moveEvent",
            "validateEvent"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                // Common event operations
                "listCommonEvents" => ListCommonEvents(),
                "getCommonEventById" => GetCommonEventById(payload),
                "getCommonEvents" => GetCommonEvents(payload),
                "createCommonEvent" => CreateCommonEvent(payload),
                "updateCommonEvent" => UpdateCommonEvent(payload),
                "deleteCommonEvent" => DeleteCommonEvent(payload),
                // Event command operations
                "getEventCommands" => GetEventCommands(payload),
                "createEventCommand" => CreateEventCommand(payload),
                "updateEventCommand" => UpdateEventCommand(payload),
                "deleteEventCommand" => DeleteEventCommand(payload),
                // Event page operations
                "getEventPages" => GetEventPages(payload),
                "createEventPage" => CreateEventPage(payload),
                "updateEventPage" => UpdateEventPage(payload),
                "deleteEventPage" => DeleteEventPage(payload),
                // Utility
                "copyEvent" => CopyEvent(payload),
                "moveEvent" => MoveEvent(payload),
                "validateEvent" => ValidateEvent(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] {
                "listCommonEvents", "getCommonEventById", "getCommonEvents",
                "getEventCommands", "getEventPages", "validateEvent"
            };
            return !readOnlyOperations.Contains(operation);
        }

        #region Common Event Operations

        private object ListCommonEvents()
        {
            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            if (!Directory.Exists(eventPath))
            {
                return CreateSuccessResponse(("events", new List<object>()), ("count", 0));
            }

            var events = new List<Dictionary<string, object>>();
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                try
                {
                    var filename = Path.GetFileNameWithoutExtension(filePath);
                    var data = ReadJsonFile(filePath);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var id = item["id"]?.ToString();
                            var name = item["name"]?.ToString() ?? "Unnamed Event";
                            if (!string.IsNullOrEmpty(id))
                            {
                                events.Add(new Dictionary<string, object>
                                {
                                    ["id"] = id,
                                    ["name"] = name,
                                    ["filename"] = filename
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse event file {eventFile}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(("events", events), ("count", events.Count));
        }

        private object GetCommonEventById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemId = item["id"]?.ToString();
                        if (itemId == id)
                        {
                            return CreateSuccessResponse(
                                ("id", id),
                                ("filename", Path.GetFileNameWithoutExtension(filePath)),
                                ("data", ConvertJTokenToObject(item))
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Common event with id '{id}' not found.");
        }

        private object GetCommonEvents(Dictionary<string, object> payload)
        {
            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            if (!Directory.Exists(eventPath))
            {
                return CreateSuccessResponse(("events", new List<object>()), ("message", "No common event data found."));
            }

            var events = new List<Dictionary<string, object>>();
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                try
                {
                    var filename = Path.GetFileNameWithoutExtension(filePath);
                    var data = ReadJsonFile(filePath);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var id = item["id"]?.ToString();
                            if (!string.IsNullOrEmpty(id))
                            {
                                events.Add(new Dictionary<string, object>
                                {
                                    ["id"] = id,
                                    ["name"] = item["name"]?.ToString() ?? "Unnamed Event",
                                    ["filename"] = filename,
                                    ["data"] = ConvertJTokenToObject(item)
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse event file {eventFile}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("events", events),
                ("count", events.Count)
            );
        }

        private object CreateCommonEvent(Dictionary<string, object> payload)
        {
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");
            var targetFile = GetString(payload, "filename") ?? "eventCommon";

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            // Generate UUID if not provided
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }
            eventData["id"] = id;

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            Directory.CreateDirectory(eventPath);

            var filePath = Path.Combine(eventPath, $"{targetFile}.json");

            JArray array;
            if (File.Exists(filePath))
            {
                var existingData = ReadJsonFile(filePath);
                array = existingData as JArray ?? new JArray();
            }
            else
            {
                array = new JArray();
            }

            array.Add(JObject.FromObject(eventData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("filename", targetFile),
                ("message", $"Common event '{id}' created successfully.")
            );
        }

        private object UpdateCommonEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            JToken finalData;
                            if (partialUpdate)
                            {
                                finalData = MergeData(array[i], eventData);
                            }
                            else
                            {
                                eventData["id"] = id; // Preserve UUID
                                finalData = JToken.FromObject(eventData);
                            }
                            array[i] = finalData;

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("filename", Path.GetFileNameWithoutExtension(filePath)),
                                ("message", $"Common event '{id}' updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Common event with id '{id}' not found.");
        }

        private object DeleteCommonEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            array.RemoveAt(i);

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("filename", Path.GetFileNameWithoutExtension(filePath)),
                                ("message", $"Common event '{id}' deleted successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Common event with id '{id}' not found.");
        }

        #endregion

        #region Event Command Operations

        private object GetEventCommands(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", 0);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemId = item["id"]?.ToString();
                        if (itemId == id)
                        {
                            var pages = item["pages"] as JArray ?? item["eventCommands"] as JArray;

                            if (pages == null || pageIndex >= pages.Count)
                            {
                                return CreateSuccessResponse(
                                    ("commands", new List<object>()),
                                    ("message", "No commands found for the specified page.")
                                );
                            }

                            var page = pages[pageIndex] as JObject;
                            var commands = page?["list"]?.ToObject<List<object>>() ?? page?.ToObject<List<object>>() ?? new List<object>();

                            return CreateSuccessResponse(
                                ("commands", commands),
                                ("id", id),
                                ("pageIndex", pageIndex)
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object CreateEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (commandData == null)
            {
                throw new InvalidOperationException("Command data is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray ?? new JArray();

                            while (pages.Count <= pageIndex)
                            {
                                pages.Add(JObject.FromObject(new Dictionary<string, object> { ["list"] = new List<object>() }));
                            }

                            var page = pages[pageIndex] as JObject;
                            var commands = page["list"] as JArray ?? new JArray();
                            commands.Add(JObject.FromObject(commandData));
                            page["list"] = commands;
                            eventItem["pages"] = pages;

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pageIndex),
                                ("message", $"Command added to event '{id}' page {pageIndex} successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object UpdateEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandIndex = GetInt(payload, "commandIndex", -1);
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (commandIndex < 0)
            {
                throw new InvalidOperationException("Valid command index is required.");
            }

            if (commandData == null)
            {
                throw new InvalidOperationException("Command data is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray;

                            if (pages == null || pageIndex >= pages.Count)
                            {
                                throw new InvalidOperationException("Invalid page index.");
                            }

                            var page = pages[pageIndex] as JObject;
                            var commands = page?["list"] as JArray;

                            if (commands == null || commandIndex >= commands.Count)
                            {
                                throw new InvalidOperationException("Invalid command index.");
                            }

                            if (partialUpdate)
                            {
                                var existing = commands[commandIndex] as JObject;
                                existing?.Merge(JObject.FromObject(commandData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace
                                });
                            }
                            else
                            {
                                commands[commandIndex] = JObject.FromObject(commandData);
                            }

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pageIndex),
                                ("commandIndex", commandIndex),
                                ("message", $"Command {commandIndex} in event '{id}' page {pageIndex} updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object DeleteEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandIndex = GetInt(payload, "commandIndex", -1);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (commandIndex < 0)
            {
                throw new InvalidOperationException("Valid command index is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray;

                            if (pages == null || pageIndex >= pages.Count)
                            {
                                throw new InvalidOperationException("Invalid page index.");
                            }

                            var page = pages[pageIndex] as JObject;
                            var commands = page?["list"] as JArray;

                            if (commands == null || commandIndex >= commands.Count)
                            {
                                throw new InvalidOperationException("Invalid command index.");
                            }

                            commands.RemoveAt(commandIndex);

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pageIndex),
                                ("commandIndex", commandIndex),
                                ("message", $"Command {commandIndex} deleted from event '{id}' page {pageIndex} successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        #endregion

        #region Event Page Operations

        private object GetEventPages(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemId = item["id"]?.ToString();
                        if (itemId == id)
                        {
                            var pages = item["pages"]?.ToObject<List<object>>() ?? new List<object>();

                            return CreateSuccessResponse(
                                ("pages", pages),
                                ("count", pages.Count),
                                ("id", id)
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object CreateEventPage(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageData = GetPayloadValue<Dictionary<string, object>>(payload, "pageData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (pageData == null)
            {
                pageData = new Dictionary<string, object>
                {
                    ["conditions"] = new Dictionary<string, object>(),
                    ["list"] = new List<object>()
                };
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray ?? new JArray();
                            pages.Add(JObject.FromObject(pageData));
                            eventItem["pages"] = pages;

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pages.Count - 1),
                                ("message", $"Event page added to '{id}' successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object UpdateEventPage(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", -1);
            var pageData = GetPayloadValue<Dictionary<string, object>>(payload, "pageData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (pageIndex < 0)
            {
                throw new InvalidOperationException("Valid page index is required.");
            }

            if (pageData == null)
            {
                throw new InvalidOperationException("Page data is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray;

                            if (pages == null || pageIndex >= pages.Count)
                            {
                                throw new InvalidOperationException("Invalid page index.");
                            }

                            if (partialUpdate)
                            {
                                var existing = pages[pageIndex] as JObject;
                                existing?.Merge(JObject.FromObject(pageData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace
                                });
                            }
                            else
                            {
                                pages[pageIndex] = JObject.FromObject(pageData);
                            }

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pageIndex),
                                ("message", $"Event page {pageIndex} in '{id}' updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        private object DeleteEventPage(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageIndex = GetInt(payload, "pageIndex", -1);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (pageIndex < 0)
            {
                throw new InvalidOperationException("Valid page index is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            var eventItem = array[i] as JObject;
                            var pages = eventItem["pages"] as JArray;

                            if (pages == null || pageIndex >= pages.Count)
                            {
                                throw new InvalidOperationException("Invalid page index.");
                            }

                            pages.RemoveAt(pageIndex);

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("pageIndex", pageIndex),
                                ("message", $"Event page {pageIndex} deleted from '{id}' successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
        }

        #endregion

        #region Copy/Move/Validate

        private object CopyEvent(Dictionary<string, object> payload)
        {
            var sourceId = GetString(payload, "uuId") ?? GetString(payload, "sourceId") ?? GetString(payload, "id");
            var newId = GetString(payload, "targetId") ?? Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(sourceId))
            {
                throw new InvalidOperationException("Source ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemId = item["id"]?.ToString();
                        if (itemId == sourceId)
                        {
                            // Clone the event with a new ID
                            var copiedEvent = item.DeepClone() as JObject;
                            copiedEvent["id"] = newId;
                            var originalName = copiedEvent["name"]?.ToString() ?? "Unnamed";
                            copiedEvent["name"] = $"{originalName} (Copy)";

                            array.Add(copiedEvent);

                            WriteJsonFile(filePath, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("sourceId", sourceId),
                                ("newId", newId),
                                ("filename", Path.GetFileNameWithoutExtension(filePath)),
                                ("message", $"Event '{sourceId}' copied with new ID '{newId}' successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Source event with id '{sourceId}' not found.");
        }

        private object MoveEvent(Dictionary<string, object> payload)
        {
            var sourceId = GetString(payload, "uuId") ?? GetString(payload, "sourceId") ?? GetString(payload, "id");
            var targetFile = GetString(payload, "targetFilename") ?? GetString(payload, "targetFile");

            if (string.IsNullOrEmpty(sourceId))
            {
                throw new InvalidOperationException("Source ID (uuId) is required.");
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                throw new InvalidOperationException("Target filename (eventCommon, eventBattle, or eventMap) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };
            var targetFilePath = Path.Combine(eventPath, $"{targetFile}.json");

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == sourceId)
                        {
                            var eventItem = array[i].DeepClone();
                            array.RemoveAt(i);
                            WriteJsonFile(filePath, array);

                            // Add to target file
                            JArray targetArray;
                            if (File.Exists(targetFilePath))
                            {
                                var targetData = ReadJsonFile(targetFilePath);
                                targetArray = targetData as JArray ?? new JArray();
                            }
                            else
                            {
                                targetArray = new JArray();
                            }

                            targetArray.Add(eventItem);
                            WriteJsonFile(targetFilePath, targetArray);

                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", sourceId),
                                ("sourceFile", Path.GetFileNameWithoutExtension(filePath)),
                                ("targetFile", targetFile),
                                ("message", $"Event '{sourceId}' moved to '{targetFile}' successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Source event with id '{sourceId}' not found.");
        }

        private object ValidateEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var eventFiles = new[] { "eventCommon.json", "eventBattle.json", "eventMap.json" };

            foreach (var eventFile in eventFiles)
            {
                var filePath = Path.Combine(eventPath, eventFile);
                if (!File.Exists(filePath)) continue;

                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemId = item["id"]?.ToString();
                        if (itemId == id)
                        {
                            var issues = new List<string>();
                            try
                            {
                                if (string.IsNullOrEmpty(item["name"]?.ToString()))
                                {
                                    issues.Add("Event has no name.");
                                }

                                var pages = item["pages"] as JArray;
                                if (pages == null || pages.Count == 0)
                                {
                                    issues.Add("Event has no pages.");
                                }
                                else
                                {
                                    for (int i = 0; i < pages.Count; i++)
                                    {
                                        var page = pages[i] as JObject;
                                        if (page == null)
                                        {
                                            issues.Add($"Page {i} is null or invalid.");
                                        }
                                        else if (page["list"] == null)
                                        {
                                            issues.Add($"Page {i} has no command list.");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                issues.Add($"Event data is invalid: {ex.Message}");
                            }

                            return CreateSuccessResponse(
                                ("valid", issues.Count == 0),
                                ("issues", issues),
                                ("issueCount", issues.Count),
                                ("id", id),
                                ("filename", Path.GetFileNameWithoutExtension(filePath))
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Event with id '{id}' not found.");
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

        private int GetInt(Dictionary<string, object> payload, string key, int defaultValue = 0)
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                if (value is long longVal) return (int)longVal;
                if (value is int intVal) return intVal;
                if (value is double doubleVal) return (int)doubleVal;
                if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
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
