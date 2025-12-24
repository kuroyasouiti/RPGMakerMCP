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
    /// RPGMaker system management handler.
    /// Handles operations for system settings, game variables, switches, and save data.
    /// </summary>
    public class RPGMakerSystemHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerSystem";
        public override string Version => "1.0.0";

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

            return CreateSuccessResponse(
                ("bridgeVersion", "1.0.0"),
                ("unityVersion", Application.unityVersion),
                ("rpgmakerVersion", "Unite 1.0"),
                ("systemPath", systemPath),
                ("flagsPath", flagsPath),
                ("saveDataPath", saveDataPath),
                ("systemFilesCount", Directory.Exists(systemPath) ? Directory.GetFiles(systemPath, "*.json").Length : 0),
                ("flagsFilesCount", Directory.Exists(flagsPath) ? Directory.GetFiles(flagsPath, "*.json").Length : 0),
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
            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON", "variables.json");

            if (!File.Exists(flagsPath))
            {
                return CreateSuccessResponse(("variables", new Dictionary<string, object>()), ("message", "No variables file found."));
            }

            var content = File.ReadAllText(flagsPath);
            var variables = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("variables", variables));
        }

        private object SetGameVariable(Dictionary<string, object> payload)
        {
            var variableId = GetString(payload, "variableId");
            object value = null;
            if (payload.TryGetValue("value", out var v))
            {
                value = v;
            }

            if (string.IsNullOrEmpty(variableId))
            {
                throw new InvalidOperationException("Variable ID is required.");
            }

            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON", "variables.json");
            Dictionary<string, object> variables;

            if (File.Exists(flagsPath))
            {
                var content = File.ReadAllText(flagsPath);
                variables = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                variables = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(flagsPath));
            }

            variables[variableId] = value;
            File.WriteAllText(flagsPath, MiniJson.Serialize(variables));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Variable '{variableId}' set successfully."));
        }

        #endregion

        #region Switches

        private object GetSwitches()
        {
            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON", "switches.json");

            if (!File.Exists(flagsPath))
            {
                return CreateSuccessResponse(("switches", new Dictionary<string, object>()), ("message", "No switches file found."));
            }

            var content = File.ReadAllText(flagsPath);
            var switches = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("switches", switches));
        }

        private object SetSwitch(Dictionary<string, object> payload)
        {
            var switchId = GetString(payload, "switchId");
            var value = GetBool(payload, "value");

            if (string.IsNullOrEmpty(switchId))
            {
                throw new InvalidOperationException("Switch ID is required.");
            }

            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON", "switches.json");
            Dictionary<string, object> switches;

            if (File.Exists(flagsPath))
            {
                var content = File.ReadAllText(flagsPath);
                switches = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                switches = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(flagsPath));
            }

            switches[switchId] = value;
            File.WriteAllText(flagsPath, MiniJson.Serialize(switches));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Switch '{switchId}' set to {value}."));
        }

        #endregion

        #region System Settings

        private object GetSystemSettings()
        {
            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            if (!Directory.Exists(systemPath))
            {
                return CreateSuccessResponse(("settings", new Dictionary<string, object>()), ("message", "No system settings found."));
            }

            var systemFiles = Directory.GetFiles(systemPath, "*.json");
            var settings = new Dictionary<string, object>();

            foreach (var file in systemFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var settingData = MiniJson.Deserialize(content);
                    var filename = Path.GetFileNameWithoutExtension(file);
                    settings[filename] = settingData;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse system file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(("settings", settings));
        }

        private object UpdateSystemSettings(Dictionary<string, object> payload)
        {
            var systemSettings = GetPayloadValue<Dictionary<string, object>>(payload, "systemSettings");

            if (systemSettings == null)
            {
                throw new InvalidOperationException("System settings are required.");
            }

            var filename = systemSettings.ContainsKey("filename") ? systemSettings["filename"]?.ToString() : null;
            var settingData = systemSettings.ContainsKey("settingData")
                ? systemSettings["settingData"] as Dictionary<string, object>
                : null;

            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            Directory.CreateDirectory(systemPath);

            if (!string.IsNullOrEmpty(filename) && settingData != null)
            {
                var filePath = Path.Combine(systemPath, $"{filename}.json");
                File.WriteAllText(filePath, MiniJson.Serialize(settingData));

                AssetDatabase.Refresh();
                RefreshHierarchy();

                return CreateSuccessResponse(("message", $"System setting '{filename}' updated successfully."));
            }
            else
            {
                var filePath = Path.Combine(systemPath, "GameSettings.json");
                File.WriteAllText(filePath, MiniJson.Serialize(systemSettings));

                AssetDatabase.Refresh();
                RefreshHierarchy();

                return CreateSuccessResponse(("message", "System settings updated successfully."));
            }
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
                var saveData = MiniJson.Deserialize(content);
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
                        var saveData = MiniJson.Deserialize(content);
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
            File.WriteAllText(filePath, MiniJson.Serialize(saveData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

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
            var saveData = MiniJson.Deserialize(content);

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
            RefreshHierarchy();

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

        private bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue = false)
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                if (value is bool boolVal) return boolVal;
                if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
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
