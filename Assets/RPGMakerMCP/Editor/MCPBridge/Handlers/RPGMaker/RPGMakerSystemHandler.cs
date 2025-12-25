using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Services;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker system management handler.
    /// Handles operations for system settings, game variables, switches, and save data.
    /// Uses EditorDataService for CRUD operations via the RPGMaker Editor API.
    /// </summary>
    public class RPGMakerSystemHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerSystem";
        public override string Version => "1.0.0";

        // Access the EditorDataService singleton for system operations
        private EditorDataService DataService => EditorDataService.Instance;

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getSystemInfo",
            "getGameVariables",
            "setGameVariable",
            "getSwitches",
            "setSwitch",
            "getSystemSettings",
            "updateSystemSettings",
            "getSaveData",
            "createSaveData",
            "loadSaveData",
            "deleteSaveData"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getSystemInfo" => GetSystemInfo(),
                "getGameVariables" => GetGameVariables(),
                "setGameVariable" => SetGameVariable(payload),
                "getSwitches" => GetSwitches(),
                "setSwitch" => SetSwitch(payload),
                "getSystemSettings" => GetSystemSettings(),
                "updateSystemSettings" => UpdateSystemSettings(payload),
                "getSaveData" => GetSaveData(payload),
                "createSaveData" => CreateSaveData(payload),
                "loadSaveData" => LoadSaveData(payload),
                "deleteSaveData" => DeleteSaveData(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] { "getSystemInfo", "getGameVariables", "getSwitches", "getSystemSettings", "getSaveData", "loadSaveData" };
            return !readOnlyOperations.Contains(operation);
        }

        #region System Info

        private object GetSystemInfo()
        {
            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON");
            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");

            string persistentDataPath = null;
            try
            {
                persistentDataPath = Application.persistentDataPath;
            }
            catch
            {
                persistentDataPath = "unavailable (not on main thread)";
            }

            // Load flags to get counts
            var flags = DataService.LoadFlags();
            var variableCount = flags?.variables?.Count ?? 0;
            var switchCount = flags?.switches?.Count ?? 0;

            return CreateSuccessResponse(
                ("bridgeVersion", "1.0.0"),
                ("unityVersion", Application.unityVersion),
                ("rpgmakerVersion", "Unite 1.0"),
                ("systemPath", systemPath),
                ("flagsPath", flagsPath),
                ("saveDataPath", saveDataPath),
                ("systemFilesCount", Directory.Exists(systemPath) ? Directory.GetFiles(systemPath, "*.json").Length : 0),
                ("variableCount", variableCount),
                ("switchCount", switchCount),
                ("saveDataFilesCount", Directory.Exists(saveDataPath) ? Directory.GetFiles(saveDataPath, "*.json").Length : 0),
                ("platform", Application.platform.ToString()),
                ("dataPath", Application.dataPath),
                ("persistentDataPath", persistentDataPath),
                ("streamingAssetsPath", Application.streamingAssetsPath),
                ("isEditor", Application.isEditor),
                ("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            );
        }

        #endregion

        #region Game Variables

        private object GetGameVariables()
        {
            var flags = DataService.LoadFlags();
            var variables = flags?.variables?.Select(v => new Dictionary<string, object>
            {
                ["id"] = v.id,
                ["name"] = v.name ?? "Unnamed Variable",
                ["eventCount"] = v.events?.Count ?? 0
            }).ToList() ?? new List<Dictionary<string, object>>();

            return CreateSuccessResponse(
                ("variables", variables),
                ("count", variables.Count)
            );
        }

        private object SetGameVariable(Dictionary<string, object> payload)
        {
            var variableId = GetString(payload, "variableId");
            var name = GetString(payload, "name");

            if (string.IsNullOrEmpty(variableId))
            {
                // Create a new variable if no ID is provided
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException("Variable ID or name is required.");
                }

                var newVariable = DataService.CreateVariable(name);
                return CreateSuccessResponse(
                    ("id", newVariable.id),
                    ("name", newVariable.name),
                    ("message", "Variable created successfully.")
                );
            }

            // Update existing variable
            var variable = DataService.GetVariableById(variableId);
            if (variable == null)
            {
                throw new InvalidOperationException($"Variable with ID '{variableId}' not found.");
            }

            DataService.UpdateVariable(variableId, v =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    v.name = name;
                }
            });

            return CreateSuccessResponse(
                ("id", variableId),
                ("message", "Variable updated successfully.")
            );
        }

        #endregion

        #region Switches

        private object GetSwitches()
        {
            var flags = DataService.LoadFlags();
            var switches = flags?.switches?.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.id,
                ["name"] = s.name ?? "Unnamed Switch",
                ["eventCount"] = s.events?.Count ?? 0
            }).ToList() ?? new List<Dictionary<string, object>>();

            return CreateSuccessResponse(
                ("switches", switches),
                ("count", switches.Count)
            );
        }

        private object SetSwitch(Dictionary<string, object> payload)
        {
            var switchId = GetString(payload, "switchId");
            var name = GetString(payload, "name");

            if (string.IsNullOrEmpty(switchId))
            {
                // Create a new switch if no ID is provided
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException("Switch ID or name is required.");
                }

                var newSwitch = DataService.CreateSwitch(name);
                return CreateSuccessResponse(
                    ("id", newSwitch.id),
                    ("name", newSwitch.name),
                    ("message", "Switch created successfully.")
                );
            }

            // Update existing switch
            var switchData = DataService.GetSwitchById(switchId);
            if (switchData == null)
            {
                throw new InvalidOperationException($"Switch with ID '{switchId}' not found.");
            }

            DataService.UpdateSwitch(switchId, s =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    s.name = name;
                }
            });

            return CreateSuccessResponse(
                ("id", switchId),
                ("message", "Switch updated successfully.")
            );
        }

        #endregion

        #region System Settings

        private object GetSystemSettings()
        {
            var settings = DataService.LoadSystemSettings();
            if (settings == null)
            {
                return CreateSuccessResponse(
                    ("settings", new Dictionary<string, object>()),
                    ("message", "No system settings found.")
                );
            }

            return CreateSuccessResponse(
                ("settings", DataModelMapper.ToDict(settings))
            );
        }

        private object UpdateSystemSettings(Dictionary<string, object> payload)
        {
            var systemSettings = GetPayloadValue<Dictionary<string, object>>(payload, "systemSettings");

            if (systemSettings == null)
            {
                throw new InvalidOperationException("System settings are required.");
            }

            DataService.UpdateSystemSettings(settings =>
            {
                DataModelMapper.ApplyPartialUpdate(settings, systemSettings);
            });

            return CreateSuccessResponse(("message", "System settings updated successfully."));
        }

        #endregion

        #region Save Data Operations

        private object GetSaveData(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");
            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");

            if (!Directory.Exists(saveDataPath))
            {
                return CreateSuccessResponse(("saveData", new List<object>()), ("message", "No save data found."));
            }

            if (!string.IsNullOrEmpty(slotId))
            {
                var filePath = Path.Combine(saveDataPath, $"save_{slotId}.json");
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException($"Save data for slot '{slotId}' not found.");
                }

                var content = File.ReadAllText(filePath);
                var saveData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                return CreateSuccessResponse(("saveData", saveData), ("slotId", slotId));
            }
            else
            {
                var saveFiles = Directory.GetFiles(saveDataPath, "save_*.json");
                var saveDataList = new List<Dictionary<string, object>>();

                foreach (var file in saveFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var saveData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                        saveDataList.Add(new Dictionary<string, object>
                        {
                            ["filename"] = Path.GetFileNameWithoutExtension(file),
                            ["data"] = saveData
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to parse save file {file}: {ex.Message}");
                    }
                }

                return CreateSuccessResponse(
                    ("saveData", saveDataList),
                    ("count", saveDataList.Count)
                );
            }
        }

        private object CreateSaveData(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");
            var saveData = GetPayloadValue<Dictionary<string, object>>(payload, "saveData");

            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("Slot ID is required.");
            }

            if (saveData == null)
            {
                throw new InvalidOperationException("Save data is required.");
            }

            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");
            Directory.CreateDirectory(saveDataPath);

            var filePath = Path.Combine(saveDataPath, $"save_{slotId}.json");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(saveData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);

            AssetDatabase.Refresh();

            return CreateSuccessResponse(("message", $"Save data for slot '{slotId}' created successfully."));
        }

        private object LoadSaveData(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");

            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("Slot ID is required.");
            }

            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");
            var filePath = Path.Combine(saveDataPath, $"save_{slotId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Save data for slot '{slotId}' not found.");
            }

            var content = File.ReadAllText(filePath);
            var saveData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            return CreateSuccessResponse(
                ("saveData", saveData),
                ("slotId", slotId),
                ("message", $"Save data for slot '{slotId}' loaded successfully.")
            );
        }

        private object DeleteSaveData(Dictionary<string, object> payload)
        {
            var slotId = GetString(payload, "slotId");

            if (string.IsNullOrEmpty(slotId))
            {
                throw new InvalidOperationException("Slot ID is required.");
            }

            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");
            var filePath = Path.Combine(saveDataPath, $"save_{slotId}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Save data for slot '{slotId}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();

            return CreateSuccessResponse(("message", $"Save data for slot '{slotId}' deleted successfully."));
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
