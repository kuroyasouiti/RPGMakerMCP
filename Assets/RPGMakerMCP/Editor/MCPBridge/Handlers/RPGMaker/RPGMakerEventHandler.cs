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
    /// RPGMaker event management handler.
    /// Handles operations for common events, map events, and event commands.
    /// </summary>
    public class RPGMakerEventHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerEvent";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getCommonEvents",
            "createCommonEvent",
            "updateCommonEvent",
            "deleteCommonEvent",
            "getEventCommands",
            "createEventCommand",
            "updateEventCommand",
            "deleteEventCommand",
            "getEventPages",
            "createEventPage",
            "updateEventPage",
            "deleteEventPage",
            "copyEvent",
            "moveEvent",
            "validateEvent"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getCommonEvents" => GetCommonEvents(payload),
                "createCommonEvent" => CreateCommonEvent(payload),
                "updateCommonEvent" => UpdateCommonEvent(payload),
                "deleteCommonEvent" => DeleteCommonEvent(payload),
                "getEventCommands" => GetEventCommands(payload),
                "createEventCommand" => CreateEventCommand(payload),
                "updateEventCommand" => UpdateEventCommand(payload),
                "deleteEventCommand" => DeleteEventCommand(payload),
                "getEventPages" => GetEventPages(payload),
                "createEventPage" => CreateEventPage(payload),
                "updateEventPage" => UpdateEventPage(payload),
                "deleteEventPage" => DeleteEventPage(payload),
                "copyEvent" => CopyEvent(payload),
                "moveEvent" => MoveEvent(payload),
                "validateEvent" => ValidateEvent(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] { "getCommonEvents", "getEventCommands", "getEventPages", "validateEvent" };
            return !readOnlyOperations.Contains(operation);
        }

        #region Common Event Operations

        private object GetCommonEvents(Dictionary<string, object> payload)
        {
            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            if (!Directory.Exists(eventPath))
            {
                return CreateSuccessResponse(("events", new List<object>()), ("message", "No common event data found."));
            }

            var eventFiles = Directory.GetFiles(eventPath, "*.json");
            var events = new List<Dictionary<string, object>>();

            foreach (var file in eventFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    events.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = eventData?.ContainsKey("name") == true ? eventData["name"]?.ToString() : "Unnamed Event",
                        ["trigger"] = eventData?.ContainsKey("trigger") == true ? eventData["trigger"] : null,
                        ["switchId"] = eventData?.ContainsKey("switchId") == true ? eventData["switchId"] : null,
                        ["data"] = eventData
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse event file {file}: {ex.Message}");
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
            var filename = GetString(payload, "filename");

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"event_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            else
            {
                filename = SanitizeFilename(filename);
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            Directory.CreateDirectory(eventPath);

            var filePath = Path.Combine(eventPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(eventData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("path", filePath),
                ("message", $"Common event '{filename}' created successfully.")
            );
        }

        private object UpdateCommonEvent(Dictionary<string, object> payload)
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

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Common event file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Common event '{filename}' updated successfully."));
        }

        private object DeleteCommonEvent(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Common event file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Common event '{filename}' deleted successfully."));
        }

        #endregion

        #region Event Command Operations

        private object GetEventCommands(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", 0);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            var pages = eventData?.ContainsKey("pages") == true ? eventData["pages"] as List<object> : null;
            if (pages == null || pageIndex >= pages.Count)
            {
                return CreateSuccessResponse(
                    ("commands", new List<object>()),
                    ("message", "No commands found for the specified page.")
                );
            }

            var page = pages[pageIndex] as Dictionary<string, object>;
            var commands = page?.ContainsKey("list") == true ? page["list"] : new List<object>();

            return CreateSuccessResponse(
                ("commands", commands),
                ("filename", filename),
                ("pageIndex", pageIndex)
            );
        }

        private object CreateEventCommand(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (commandData == null)
            {
                throw new InvalidOperationException("Command data is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            if (!eventData.ContainsKey("pages") || eventData["pages"] == null)
            {
                eventData["pages"] = new List<object> { new Dictionary<string, object> { ["list"] = new List<object>() } };
            }

            var pages = eventData["pages"] as List<object>;
            while (pages.Count <= pageIndex)
            {
                pages.Add(new Dictionary<string, object> { ["list"] = new List<object>() });
            }

            var page = pages[pageIndex] as Dictionary<string, object>;
            if (!page.ContainsKey("list") || page["list"] == null)
            {
                page["list"] = new List<object>();
            }

            var commands = page["list"] as List<object>;
            commands.Add(commandData);

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Command added to event '{filename}' page {pageIndex} successfully."));
        }

        private object UpdateEventCommand(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandIndex = GetInt(payload, "commandIndex", -1);
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
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
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var pages = eventData?["pages"] as List<object>;

            if (pages == null || pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("Invalid page index.");
            }

            var page = pages[pageIndex] as Dictionary<string, object>;
            var commands = page?["list"] as List<object>;

            if (commands == null || commandIndex >= commands.Count)
            {
                throw new InvalidOperationException("Invalid command index.");
            }

            commands[commandIndex] = commandData;

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Command {commandIndex} in event '{filename}' page {pageIndex} updated successfully."));
        }

        private object DeleteEventCommand(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", 0);
            var commandIndex = GetInt(payload, "commandIndex", -1);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (commandIndex < 0)
            {
                throw new InvalidOperationException("Valid command index is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var pages = eventData?["pages"] as List<object>;

            if (pages == null || pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("Invalid page index.");
            }

            var page = pages[pageIndex] as Dictionary<string, object>;
            var commands = page?["list"] as List<object>;

            if (commands == null || commandIndex >= commands.Count)
            {
                throw new InvalidOperationException("Invalid command index.");
            }

            commands.RemoveAt(commandIndex);

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Command {commandIndex} deleted from event '{filename}' page {pageIndex} successfully."));
        }

        #endregion

        #region Event Page Operations

        private object GetEventPages(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var pages = eventData?.ContainsKey("pages") == true ? eventData["pages"] : new List<object>();

            return CreateSuccessResponse(
                ("pages", pages),
                ("count", (pages as List<object>)?.Count ?? 0),
                ("filename", filename)
            );
        }

        private object CreateEventPage(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageData = GetPayloadValue<Dictionary<string, object>>(payload, "pageData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
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
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            if (!eventData.ContainsKey("pages") || eventData["pages"] == null)
            {
                eventData["pages"] = new List<object>();
            }

            var pages = eventData["pages"] as List<object>;
            pages.Add(pageData);

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("pageIndex", pages.Count - 1),
                ("message", $"Event page added to '{filename}' successfully.")
            );
        }

        private object UpdateEventPage(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", -1);
            var pageData = GetPayloadValue<Dictionary<string, object>>(payload, "pageData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
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
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var pages = eventData?["pages"] as List<object>;

            if (pages == null || pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("Invalid page index.");
            }

            pages[pageIndex] = pageData;

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event page {pageIndex} in '{filename}' updated successfully."));
        }

        private object DeleteEventPage(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var pageIndex = GetInt(payload, "pageIndex", -1);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (pageIndex < 0)
            {
                throw new InvalidOperationException("Valid page index is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;
            var pages = eventData?["pages"] as List<object>;

            if (pages == null || pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("Invalid page index.");
            }

            pages.RemoveAt(pageIndex);

            File.WriteAllText(filePath, MiniJson.Serialize(eventData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event page {pageIndex} deleted from '{filename}' successfully."));
        }

        #endregion

        #region Copy/Move/Validate

        private object CopyEvent(Dictionary<string, object> payload)
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

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var sourceFilePath = Path.Combine(eventPath, $"{sourceFilename}.json");
            var targetFilePath = Path.Combine(eventPath, $"{targetFilename}.json");

            if (!File.Exists(sourceFilePath))
            {
                throw new InvalidOperationException($"Source event file '{sourceFilename}' not found.");
            }

            File.Copy(sourceFilePath, targetFilePath, true);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event '{sourceFilename}' copied to '{targetFilename}' successfully."));
        }

        private object MoveEvent(Dictionary<string, object> payload)
        {
            var sourceFilename = GetString(payload, "sourceFilename");
            var targetFilename = GetString(payload, "targetFilename");

            if (string.IsNullOrEmpty(sourceFilename))
            {
                throw new InvalidOperationException("Source filename is required.");
            }

            if (string.IsNullOrEmpty(targetFilename))
            {
                throw new InvalidOperationException("Target filename is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var sourceFilePath = Path.Combine(eventPath, $"{sourceFilename}.json");
            var targetFilePath = Path.Combine(eventPath, $"{targetFilename}.json");

            if (!File.Exists(sourceFilePath))
            {
                throw new InvalidOperationException($"Source event file '{sourceFilename}' not found.");
            }

            File.Move(sourceFilePath, targetFilePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Event '{sourceFilename}' moved to '{targetFilename}' successfully."));
        }

        private object ValidateEvent(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var eventPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Event", "JSON");
            var filePath = Path.Combine(eventPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Event file '{filename}' not found.");
            }

            var issues = new List<string>();
            var content = File.ReadAllText(filePath);
            var eventData = MiniJson.Deserialize(content) as Dictionary<string, object>;

            if (eventData == null)
            {
                issues.Add("Event data is null or invalid JSON.");
            }
            else
            {
                if (!eventData.ContainsKey("name") || string.IsNullOrEmpty(eventData["name"]?.ToString()))
                {
                    issues.Add("Event has no name.");
                }

                if (!eventData.ContainsKey("pages") || eventData["pages"] == null)
                {
                    issues.Add("Event has no pages.");
                }
                else
                {
                    var pages = eventData["pages"] as List<object>;
                    if (pages != null)
                    {
                        for (int i = 0; i < pages.Count; i++)
                        {
                            var page = pages[i] as Dictionary<string, object>;
                            if (page == null)
                            {
                                issues.Add($"Page {i} is null or invalid.");
                            }
                            else if (!page.ContainsKey("list") || page["list"] == null)
                            {
                                issues.Add($"Page {i} has no command list.");
                            }
                        }
                    }
                }
            }

            return CreateSuccessResponse(
                ("valid", issues.Count == 0),
                ("issues", issues),
                ("issueCount", issues.Count),
                ("filename", filename)
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
