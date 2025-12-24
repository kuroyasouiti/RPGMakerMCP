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
    /// RPGMaker database management handler.
    /// Handles operations for characters, items, animations, and system settings.
    /// </summary>
    public class RPGMakerDatabaseHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerDatabase";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getDatabaseInfo",
            "getCharacters",
            "createCharacter",
            "updateCharacter",
            "deleteCharacter",
            "getItems",
            "createItem",
            "updateItem",
            "deleteItem",
            "getAnimations",
            "createAnimation",
            "updateAnimation",
            "deleteAnimation",
            "getSystemSettings",
            "updateSystemSettings",
            "exportDatabase",
            "importDatabase",
            "backupDatabase",
            "restoreDatabase"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getDatabaseInfo" => GetDatabaseInfo(),
                "getCharacters" => GetCharacters(payload),
                "createCharacter" => CreateCharacter(payload),
                "updateCharacter" => UpdateCharacter(payload),
                "deleteCharacter" => DeleteCharacter(payload),
                "getItems" => GetItems(payload),
                "createItem" => CreateItem(payload),
                "updateItem" => UpdateItem(payload),
                "deleteItem" => DeleteItem(payload),
                "getAnimations" => GetAnimations(payload),
                "createAnimation" => CreateAnimation(payload),
                "updateAnimation" => UpdateAnimation(payload),
                "deleteAnimation" => DeleteAnimation(payload),
                "getSystemSettings" => GetSystemSettings(),
                "updateSystemSettings" => UpdateSystemSettings(payload),
                "exportDatabase" => ExportDatabase(payload),
                "importDatabase" => ImportDatabase(payload),
                "backupDatabase" => BackupDatabase(payload),
                "restoreDatabase" => RestoreDatabase(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] { "getDatabaseInfo", "getCharacters", "getItems", "getAnimations", "getSystemSettings" };
            return !readOnlyOperations.Contains(operation);
        }

        #region Database Info

        private object GetDatabaseInfo()
        {
            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");

            return CreateSuccessResponse(
                ("characterCount", Directory.Exists(characterPath) ? Directory.GetFiles(characterPath, "*.json").Length : 0),
                ("itemCount", Directory.Exists(itemPath) ? Directory.GetFiles(itemPath, "*.json").Length : 0),
                ("animationCount", Directory.Exists(animationPath) ? Directory.GetFiles(animationPath, "*.json").Length : 0),
                ("systemConfigExists", Directory.Exists(systemPath)),
                ("paths", new Dictionary<string, string>
                {
                    ["characters"] = characterPath,
                    ["items"] = itemPath,
                    ["animations"] = animationPath,
                    ["system"] = systemPath
                })
            );
        }

        #endregion

        #region Character Operations

        private object GetCharacters(Dictionary<string, object> payload)
        {
            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            if (!Directory.Exists(characterPath))
            {
                return CreateSuccessResponse(("characters", new List<object>()), ("message", "No character data found."));
            }

            var characterFiles = Directory.GetFiles(characterPath, "*.json");
            var characters = new List<Dictionary<string, object>>();

            foreach (var file in characterFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    characters.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["data"] = MiniJson.Deserialize(content)
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse character file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("characters", characters),
                ("count", characters.Count)
            );
        }

        private object CreateCharacter(Dictionary<string, object> payload)
        {
            var characterData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "characterData");
            var filename = GetString(payload, "filename");

            if (characterData == null)
            {
                throw new InvalidOperationException("Character data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"character_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            else
            {
                filename = SanitizeFilename(filename);
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(characterPath);

            var filePath = Path.Combine(characterPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(characterData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("path", filePath),
                ("message", $"Character '{filename}' created successfully.")
            );
        }

        private object UpdateCharacter(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var characterData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "characterData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (characterData == null)
            {
                throw new InvalidOperationException("Character data is required.");
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(characterPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Character file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(characterData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Character '{filename}' updated successfully."));
        }

        private object DeleteCharacter(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(characterPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Character file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Character '{filename}' deleted successfully."));
        }

        #endregion

        #region Item Operations

        private object GetItems(Dictionary<string, object> payload)
        {
            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            if (!Directory.Exists(itemPath))
            {
                return CreateSuccessResponse(("items", new List<object>()), ("message", "No item data found."));
            }

            var itemFiles = Directory.GetFiles(itemPath, "*.json");
            var items = new List<Dictionary<string, object>>();

            foreach (var file in itemFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    items.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["data"] = MiniJson.Deserialize(content)
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse item file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("items", items),
                ("count", items.Count)
            );
        }

        private object CreateItem(Dictionary<string, object> payload)
        {
            var itemData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData");
            var filename = GetString(payload, "filename");

            if (itemData == null)
            {
                throw new InvalidOperationException("Item data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"item_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            else
            {
                filename = SanitizeFilename(filename);
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            Directory.CreateDirectory(itemPath);

            var filePath = Path.Combine(itemPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(itemData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("path", filePath),
                ("message", $"Item '{filename}' created successfully.")
            );
        }

        private object UpdateItem(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var itemData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (itemData == null)
            {
                throw new InvalidOperationException("Item data is required.");
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var filePath = Path.Combine(itemPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Item file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(itemData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Item '{filename}' updated successfully."));
        }

        private object DeleteItem(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var filePath = Path.Combine(itemPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Item file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Item '{filename}' deleted successfully."));
        }

        #endregion

        #region Animation Operations

        private object GetAnimations(Dictionary<string, object> payload)
        {
            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            if (!Directory.Exists(animationPath))
            {
                return CreateSuccessResponse(("animations", new List<object>()), ("message", "No animation data found."));
            }

            var animationFiles = Directory.GetFiles(animationPath, "*.json");
            var animations = new List<Dictionary<string, object>>();

            foreach (var file in animationFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    animations.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["data"] = MiniJson.Deserialize(content)
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse animation file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("animations", animations),
                ("count", animations.Count)
            );
        }

        private object CreateAnimation(Dictionary<string, object> payload)
        {
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "animationData");
            var filename = GetString(payload, "filename");

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"animation_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            else
            {
                filename = SanitizeFilename(filename);
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            Directory.CreateDirectory(animationPath);

            var filePath = Path.Combine(animationPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(animationData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("path", filePath),
                ("message", $"Animation '{filename}' created successfully.")
            );
        }

        private object UpdateAnimation(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            var filePath = Path.Combine(animationPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Animation file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(animationData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Animation '{filename}' updated successfully."));
        }

        private object DeleteAnimation(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            var filePath = Path.Combine(animationPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Animation file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Animation '{filename}' deleted successfully."));
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
            var systemSettings = new Dictionary<string, object>();

            foreach (var file in systemFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var filename = Path.GetFileNameWithoutExtension(file);
                    systemSettings[filename] = MiniJson.Deserialize(content);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse system file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(("settings", systemSettings));
        }

        private object UpdateSystemSettings(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var settingData = GetPayloadValue<Dictionary<string, object>>(payload, "settingData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (settingData == null)
            {
                throw new InvalidOperationException("Setting data is required.");
            }

            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            Directory.CreateDirectory(systemPath);

            var filePath = Path.Combine(systemPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(settingData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"System setting '{filename}' updated successfully."));
        }

        #endregion

        #region Export/Import/Backup

        private object ExportDatabase(Dictionary<string, object> payload)
        {
            var exportPath = GetString(payload, "exportPath");
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = Path.Combine(Application.dataPath, "..", "RPGMaker_Database_Export");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            if (!Directory.Exists(sourcePath))
            {
                throw new InvalidOperationException("RPGMaker storage directory not found.");
            }

            Directory.CreateDirectory(exportPath);
            CopyDirectory(sourcePath, exportPath);

            return CreateSuccessResponse(
                ("exportPath", exportPath),
                ("message", $"Database exported to '{exportPath}'.")
            );
        }

        private object ImportDatabase(Dictionary<string, object> payload)
        {
            var importPath = GetString(payload, "importPath");

            if (string.IsNullOrEmpty(importPath) || !Directory.Exists(importPath))
            {
                throw new InvalidOperationException("Valid import path is required.");
            }

            var targetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            // Create backup before import
            var backupPath = Path.Combine(Application.dataPath, "..", $"RPGMaker_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            if (Directory.Exists(targetPath))
            {
                CopyDirectory(targetPath, backupPath);
            }

            CopyDirectory(importPath, targetPath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("importPath", importPath),
                ("backupPath", backupPath),
                ("message", $"Database imported from '{importPath}'. Backup created at '{backupPath}'.")
            );
        }

        private object BackupDatabase(Dictionary<string, object> payload)
        {
            var backupPath = GetString(payload, "backupPath");
            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(Application.dataPath, "..", $"RPGMaker_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            if (!Directory.Exists(sourcePath))
            {
                throw new InvalidOperationException("RPGMaker storage directory not found.");
            }

            Directory.CreateDirectory(backupPath);
            CopyDirectory(sourcePath, backupPath);

            return CreateSuccessResponse(
                ("backupPath", backupPath),
                ("message", $"Database backed up to '{backupPath}'.")
            );
        }

        private object RestoreDatabase(Dictionary<string, object> payload)
        {
            var backupPath = GetString(payload, "backupPath");

            if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
            {
                throw new InvalidOperationException("Valid backup path is required.");
            }

            var targetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            // Create backup of current state before restore
            var currentBackupPath = Path.Combine(Application.dataPath, "..", $"RPGMaker_PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}");
            if (Directory.Exists(targetPath))
            {
                CopyDirectory(targetPath, currentBackupPath);
            }

            CopyDirectory(backupPath, targetPath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("backupPath", backupPath),
                ("currentBackupPath", currentBackupPath),
                ("message", $"Database restored from '{backupPath}'. Current state backed up to '{currentBackupPath}'.")
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

        private string SanitizeFilename(string filename)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                filename = filename.Replace(c, '_');
            }
            return filename;
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, targetSubDir);
            }
        }

        private void RefreshHierarchy()
        {
            // Attempt to refresh RPGMaker hierarchy if available
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
