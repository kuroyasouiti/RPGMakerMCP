using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Services;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventCommon;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker event management handler.
    /// Handles operations for common events, map events, and event commands.
    /// Uses EditorDataService for CRUD operations via the RPGMaker Editor API.
    /// </summary>
    public class RPGMakerEventHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerEvent";
        public override string Version => "1.0.0";

        // Access the EditorDataService singleton for event operations
        private EditorDataService DataService => EditorDataService.Instance;

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
                "listCommonEvents" => ListCommonEvents(payload),
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

        private object ListCommonEvents(Dictionary<string, object> payload)
        {
            var events = DataService.LoadCommonEvents();
            var result = events.Select(e => new Dictionary<string, object>
            {
                ["id"] = e.eventId,
                ["uuId"] = e.eventId,
                ["name"] = e.name ?? "Unnamed Event"
            }).ToList();

            return CreatePaginatedResponse("events", result, payload);
        }

        private object GetCommonEventById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var commonEvent = DataService.LoadCommonEventById(id);
            if (commonEvent == null)
            {
                throw new InvalidOperationException($"Common event with id '{id}' not found.");
            }

            // Also load the event data containing commands
            var eventData = DataService.LoadEventById(id);

            return CreateSuccessResponse(
                ("id", commonEvent.eventId),
                ("uuId", commonEvent.eventId),
                ("data", new Dictionary<string, object>
                {
                    ["eventId"] = commonEvent.eventId,
                    ["name"] = commonEvent.name,
                    ["conditions"] = commonEvent.conditions.Select(c => new Dictionary<string, object>
                    {
                        ["trigger"] = c.trigger,
                        ["switchId"] = c.switchId
                    }).ToList(),
                    ["eventCommands"] = eventData?.eventCommands?.Select(c => new Dictionary<string, object>
                    {
                        ["code"] = c.code,
                        ["indent"] = c.indent,
                        ["parameters"] = c.parameters,
                        ["route"] = c.route?.Select(r => new Dictionary<string, object>
                        {
                            ["code"] = r.code,
                            ["codeIndex"] = r.codeIndex,
                            ["parameters"] = r.parameters
                        }).ToList() ?? new List<Dictionary<string, object>>()
                    }).ToList() ?? new List<Dictionary<string, object>>()
                })
            );
        }

        private object GetCommonEvents(Dictionary<string, object> payload)
        {
            var events = DataService.LoadCommonEvents();
            var result = events.Select(e =>
            {
                var eventData = DataService.LoadEventById(e.eventId);
                return new Dictionary<string, object>
                {
                    ["id"] = e.eventId,
                    ["uuId"] = e.eventId,
                    ["name"] = e.name ?? "Unnamed Event",
                    ["data"] = new Dictionary<string, object>
                    {
                        ["eventId"] = e.eventId,
                        ["name"] = e.name,
                        ["conditions"] = e.conditions.Select(c => new Dictionary<string, object>
                        {
                            ["trigger"] = c.trigger,
                            ["switchId"] = c.switchId
                        }).ToList(),
                        ["commandCount"] = eventData?.eventCommands?.Count ?? 0
                    }
                };
            }).ToList();

            return CreateSuccessResponse(
                ("events", result),
                ("count", result.Count)
            );
        }

        private object CreateCommonEvent(Dictionary<string, object> payload)
        {
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            // Get optional name from payload
            string name = null;
            if (eventData != null && eventData.TryGetValue("name", out var nameObj))
            {
                name = nameObj?.ToString();
            }

            // Create common event using EditorDataService
            var newEvent = DataService.CreateCommonEvent(name);

            // Apply any additional data from payload (like conditions)
            if (eventData != null && eventData.Count > 0)
            {
                DataService.UpdateCommonEvent(newEvent.eventId, e =>
                {
                    DataModelMapper.ApplyPartialUpdate(e, eventData);
                });
            }

            return CreateSuccessResponse(
                ("id", newEvent.eventId),
                ("uuId", newEvent.eventId),
                ("message", "Common event created successfully.")
            );
        }

        private object UpdateCommonEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var eventData = GetPayloadValue<Dictionary<string, object>>(payload, "eventData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (eventData == null)
            {
                throw new InvalidOperationException("Event data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateCommonEvent(id, e =>
            {
                DataModelMapper.ApplyPartialUpdate(e, eventData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Common event updated successfully.")
            );
        }

        private object DeleteCommonEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteCommonEvent(id);

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Common event deleted successfully.")
            );
        }

        #endregion

        #region Event Command Operations

        private object GetEventCommands(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var eventData = DataService.LoadEventById(id);
            if (eventData == null)
            {
                throw new InvalidOperationException($"Event with id '{id}' not found.");
            }

            var commands = eventData.eventCommands?.Select((c, index) => new Dictionary<string, object>
            {
                ["index"] = index,
                ["code"] = c.code,
                ["indent"] = c.indent,
                ["parameters"] = c.parameters,
                ["route"] = c.route?.Select(r => new Dictionary<string, object>
                {
                    ["code"] = r.code,
                    ["codeIndex"] = r.codeIndex,
                    ["parameters"] = r.parameters
                }).ToList() ?? new List<Dictionary<string, object>>()
            }).ToList() ?? new List<Dictionary<string, object>>();

            return CreateSuccessResponse(
                ("commands", commands),
                ("count", commands.Count),
                ("id", id)
            );
        }

        private object CreateEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");
            var insertIndex = GetInt(payload, "commandIndex", -1);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (commandData == null)
            {
                throw new InvalidOperationException("Command data is required.");
            }

            DataService.UpdateEvent(id, eventData =>
            {
                var newCommand = CreateEventCommandFromDict(commandData);

                if (insertIndex >= 0 && insertIndex < eventData.eventCommands.Count)
                {
                    eventData.eventCommands.Insert(insertIndex, newCommand);
                }
                else
                {
                    eventData.eventCommands.Add(newCommand);
                }
            });

            return CreateSuccessResponse(
                ("id", id),
                ("message", "Event command created successfully.")
            );
        }

        private object UpdateEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var commandIndex = GetInt(payload, "commandIndex", -1);
            var commandData = GetPayloadValue<Dictionary<string, object>>(payload, "commandData");

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

            DataService.UpdateEvent(id, eventData =>
            {
                if (commandIndex >= eventData.eventCommands.Count)
                {
                    throw new InvalidOperationException("Invalid command index.");
                }

                var existingCommand = eventData.eventCommands[commandIndex];

                // Update command properties
                if (commandData.TryGetValue("code", out var codeObj))
                    existingCommand.code = Convert.ToInt32(codeObj);
                if (commandData.TryGetValue("indent", out var indentObj))
                    existingCommand.indent = Convert.ToInt32(indentObj);
                if (commandData.TryGetValue("parameters", out var paramsObj) && paramsObj is IEnumerable<object> paramsList)
                    existingCommand.parameters = paramsList.Select(p => p?.ToString() ?? "").ToList();
            });

            return CreateSuccessResponse(
                ("id", id),
                ("commandIndex", commandIndex),
                ("message", $"Event command {commandIndex} updated successfully.")
            );
        }

        private object DeleteEventCommand(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var commandIndex = GetInt(payload, "commandIndex", -1);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (commandIndex < 0)
            {
                throw new InvalidOperationException("Valid command index is required.");
            }

            DataService.UpdateEvent(id, eventData =>
            {
                if (commandIndex >= eventData.eventCommands.Count)
                {
                    throw new InvalidOperationException("Invalid command index.");
                }

                eventData.eventCommands.RemoveAt(commandIndex);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("commandIndex", commandIndex),
                ("message", $"Event command {commandIndex} deleted successfully.")
            );
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

            // For common events, there's typically one "page" (the event itself)
            // This is a simplified view - the actual event data contains commands
            var eventData = DataService.LoadEventById(id);
            if (eventData == null)
            {
                throw new InvalidOperationException($"Event with id '{id}' not found.");
            }

            // Return a single "page" representing the event
            var pages = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["index"] = 0,
                    ["page"] = eventData.page,
                    ["type"] = eventData.type,
                    ["commandCount"] = eventData.eventCommands?.Count ?? 0
                }
            };

            return CreateSuccessResponse(
                ("pages", pages),
                ("count", pages.Count),
                ("id", id)
            );
        }

        private object CreateEventPage(Dictionary<string, object> payload)
        {
            // Note: Common events in RPGMaker Unite typically have a single page structure
            // This operation is kept for API compatibility but may have limited use
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            return CreateSuccessResponse(
                ("id", id),
                ("message", "Common events have a single page structure. Use createEventCommand to add commands.")
            );
        }

        private object UpdateEventPage(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var pageData = GetPayloadValue<Dictionary<string, object>>(payload, "pageData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (pageData == null)
            {
                throw new InvalidOperationException("Page data is required.");
            }

            DataService.UpdateEvent(id, eventData =>
            {
                if (pageData.TryGetValue("page", out var pageObj))
                    eventData.page = Convert.ToInt32(pageObj);
                if (pageData.TryGetValue("type", out var typeObj))
                    eventData.type = Convert.ToInt32(typeObj);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("message", "Event page updated successfully.")
            );
        }

        private object DeleteEventPage(Dictionary<string, object> payload)
        {
            // Note: Common events cannot have pages deleted - they are the event itself
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            return CreateSuccessResponse(
                ("id", id),
                ("message", "Cannot delete the page of a common event. Use deleteCommonEvent to delete the entire event.")
            );
        }

        #endregion

        #region Copy/Move/Validate

        private object CopyEvent(Dictionary<string, object> payload)
        {
            var sourceId = GetString(payload, "uuId") ?? GetString(payload, "sourceId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(sourceId))
            {
                throw new InvalidOperationException("Source ID (uuId) is required.");
            }

            // Use EditorDataService to duplicate the event
            var duplicated = DataService.DuplicateCommonEvent(sourceId);

            return CreateSuccessResponse(
                ("sourceId", sourceId),
                ("newId", duplicated.eventId),
                ("newName", duplicated.name),
                ("message", $"Event '{sourceId}' copied successfully.")
            );
        }

        private object MoveEvent(Dictionary<string, object> payload)
        {
            // Move operation doesn't apply to common events in the same way as file-based operations
            // Common events are stored in a unified structure
            var sourceId = GetString(payload, "uuId") ?? GetString(payload, "sourceId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(sourceId))
            {
                throw new InvalidOperationException("Source ID (uuId) is required.");
            }

            return CreateSuccessResponse(
                ("id", sourceId),
                ("message", "Move operation is not applicable to common events. They are managed as a unified list.")
            );
        }

        private object ValidateEvent(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var commonEvent = DataService.LoadCommonEventById(id);
            if (commonEvent == null)
            {
                throw new InvalidOperationException($"Common event with id '{id}' not found.");
            }

            var eventData = DataService.LoadEventById(id);
            var issues = new List<string>();

            // Validate common event
            if (string.IsNullOrEmpty(commonEvent.name))
            {
                issues.Add("Event has no name.");
            }

            if (commonEvent.conditions == null || commonEvent.conditions.Count == 0)
            {
                issues.Add("Event has no trigger conditions.");
            }

            // Validate event data
            if (eventData == null)
            {
                issues.Add("Event data is missing.");
            }
            else if (eventData.eventCommands == null || eventData.eventCommands.Count == 0)
            {
                issues.Add("Event has no commands.");
            }
            else
            {
                // Validate commands
                for (int i = 0; i < eventData.eventCommands.Count; i++)
                {
                    var cmd = eventData.eventCommands[i];
                    if (cmd.code < 0)
                    {
                        issues.Add($"Command {i} has invalid code: {cmd.code}");
                    }
                }
            }

            return CreateSuccessResponse(
                ("valid", issues.Count == 0),
                ("issues", issues),
                ("issueCount", issues.Count),
                ("id", id)
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

        private EventDataModel.EventCommand CreateEventCommandFromDict(Dictionary<string, object> dict)
        {
            var code = 0;
            var indent = 0;
            var parameters = new List<string>();
            var route = new List<EventDataModel.EventCommandMoveRoute>();

            if (dict.TryGetValue("code", out var codeObj))
                code = Convert.ToInt32(codeObj);
            if (dict.TryGetValue("indent", out var indentObj))
                indent = Convert.ToInt32(indentObj);
            if (dict.TryGetValue("parameters", out var paramsObj) && paramsObj is IEnumerable<object> paramsList)
                parameters = paramsList.Select(p => p?.ToString() ?? "").ToList();
            if (dict.TryGetValue("route", out var routeObj) && routeObj is IEnumerable<object> routeList)
            {
                foreach (var r in routeList)
                {
                    if (r is Dictionary<string, object> routeDict)
                    {
                        var routeCode = 0;
                        var routeCodeIndex = 0;
                        var routeParams = new List<string>();

                        if (routeDict.TryGetValue("code", out var rcObj))
                            routeCode = Convert.ToInt32(rcObj);
                        if (routeDict.TryGetValue("codeIndex", out var rciObj))
                            routeCodeIndex = Convert.ToInt32(rciObj);
                        if (routeDict.TryGetValue("parameters", out var rpObj) && rpObj is IEnumerable<object> rpList)
                            routeParams = rpList.Select(p => p?.ToString() ?? "").ToList();

                        route.Add(new EventDataModel.EventCommandMoveRoute(routeCode, routeParams, routeCodeIndex));
                    }
                }
            }

            return new EventDataModel.EventCommand(code, parameters, route, indent);
        }

        #endregion
    }
}
