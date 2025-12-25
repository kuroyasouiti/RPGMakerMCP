using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Config;
using MCP.Editor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker database management handler.
    /// Handles operations for characters, items, animations, and system settings.
    /// Uses EditorDataService for CRUD operations via the RPGMaker Editor API.
    /// </summary>
    public class RPGMakerDatabaseHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerDatabase";
        public override string Version => "1.0.0";

        // Access the EditorDataService singleton for database operations
        private EditorDataService DataService => EditorDataService.Instance;

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getDatabaseInfo",
            // Character operations
            "listCharacters",
            "getCharacterById",
            "getCharacters",
            "createCharacter",
            "updateCharacter",
            "deleteCharacter",
            // Item operations
            "listItems",
            "getItemById",
            "getItems",
            "createItem",
            "updateItem",
            "deleteItem",
            // Animation operations
            "listAnimations",
            "getAnimationById",
            "getAnimations",
            "createAnimation",
            "updateAnimation",
            "deleteAnimation",
            // System operations
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
                // Character operations
                "listCharacters" => ListCharacters(payload),
                "getCharacterById" => GetCharacterById(payload),
                "getCharacters" => GetCharacters(payload),
                "createCharacter" => CreateCharacter(payload),
                "updateCharacter" => UpdateCharacter(payload),
                "deleteCharacter" => DeleteCharacter(payload),
                // Item operations
                "listItems" => ListItems(payload),
                "getItemById" => GetItemById(payload),
                "getItems" => GetItems(payload),
                "createItem" => CreateItem(payload),
                "updateItem" => UpdateItem(payload),
                "deleteItem" => DeleteItem(payload),
                // Animation operations
                "listAnimations" => ListAnimations(payload),
                "getAnimationById" => GetAnimationById(payload),
                "getAnimations" => GetAnimations(payload),
                "createAnimation" => CreateAnimation(payload),
                "updateAnimation" => UpdateAnimation(payload),
                "deleteAnimation" => DeleteAnimation(payload),
                // System operations
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
            var readOnlyOperations = new[] {
                "getDatabaseInfo",
                "listCharacters", "getCharacterById", "getCharacters",
                "listItems", "getItemById", "getItems",
                "listAnimations", "getAnimationById", "getAnimations",
                "getSystemSettings"
            };
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

        private object ListCharacters(Dictionary<string, object> payload)
        {
            var characters = DataService.LoadCharacters();
            var result = characters.Select(c => new Dictionary<string, object>
            {
                ["uuId"] = c.uuId,
                ["name"] = c.basic?.name ?? "Unnamed",
                ["charaType"] = c.charaType,
                ["filename"] = "characterActor"
            }).ToList();

            return CreatePaginatedResponse("characters", result, payload);
        }

        private object GetCharacterById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var character = DataService.LoadCharacterById(uuId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with uuId '{uuId}' not found.");
            }

            return CreateSuccessResponse(
                ("uuId", character.uuId),
                ("filename", "characterActor"),
                ("data", DataModelMapper.ToDict(character))
            );
        }

        private object GetCharacters(Dictionary<string, object> payload)
        {
            var characters = DataService.LoadCharacters();
            var result = characters.Select(c => new Dictionary<string, object>
            {
                ["uuId"] = c.uuId,
                ["name"] = c.basic?.name ?? "Unnamed",
                ["charaType"] = c.charaType,
                ["filename"] = "characterActor",
                ["data"] = DataModelMapper.ToDict(c)
            }).ToList();

            return CreateSuccessResponse(
                ("characters", result),
                ("count", result.Count)
            );
        }

        private object CreateCharacter(Dictionary<string, object> payload)
        {
            var characterData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "characterData");

            // Get optional name and type from payload
            string name = null;
            int charaType = (int)ActorTypeEnum.ACTOR;

            if (characterData != null)
            {
                if (characterData.TryGetValue("basic", out var basicObj) && basicObj is Dictionary<string, object> basic)
                {
                    if (basic.TryGetValue("name", out var nameObj))
                        name = nameObj?.ToString();
                }
                if (characterData.TryGetValue("charaType", out var typeObj))
                    charaType = Convert.ToInt32(typeObj);
            }

            // Create character using EditorDataService
            var newCharacter = DataService.CreateCharacter(name, charaType);

            // Apply any additional data from payload using partial update
            if (characterData != null && characterData.Count > 0)
            {
                DataService.UpdateCharacter(newCharacter.uuId, c =>
                {
                    DataModelMapper.ApplyPartialUpdate(c, characterData);
                });
            }

            return CreateSuccessResponse(
                ("uuId", newCharacter.uuId),
                ("filename", "characterActor"),
                ("message", "Character created successfully.")
            );
        }

        private object UpdateCharacter(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var characterData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "characterData");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (characterData == null)
            {
                throw new InvalidOperationException("Character data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateCharacter(uuId, c =>
            {
                DataModelMapper.ApplyPartialUpdate(c, characterData);
            });

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Character updated successfully.")
            );
        }

        private object DeleteCharacter(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteCharacter(uuId);

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Character deleted successfully.")
            );
        }

        #endregion

        #region Item Operations

        private object ListItems(Dictionary<string, object> payload)
        {
            var items = DataService.LoadItems();
            var result = items.Select(i => new Dictionary<string, object>
            {
                ["uuId"] = i.basic.id,
                ["name"] = i.basic.name ?? "Unnamed",
                ["filename"] = "item"
            }).ToList();

            return CreatePaginatedResponse("items", result, payload);
        }

        private object GetItemById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var item = DataService.LoadItemById(uuId);
            if (item == null)
            {
                throw new InvalidOperationException($"Item with uuId '{uuId}' not found.");
            }

            return CreateSuccessResponse(
                ("uuId", item.basic.id),
                ("filename", "item"),
                ("data", DataModelMapper.ToDict(item))
            );
        }

        private object GetItems(Dictionary<string, object> payload)
        {
            var items = DataService.LoadItems();
            var result = items.Select(i => new Dictionary<string, object>
            {
                ["uuId"] = i.basic.id,
                ["name"] = i.basic.name ?? "Unnamed",
                ["filename"] = "item",
                ["data"] = DataModelMapper.ToDict(i)
            }).ToList();

            return CreateSuccessResponse(
                ("items", result),
                ("count", result.Count)
            );
        }

        private object CreateItem(Dictionary<string, object> payload)
        {
            var itemData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData");

            // Get optional name from payload
            string name = null;
            if (itemData != null)
            {
                if (itemData.TryGetValue("basic", out var basicObj) && basicObj is Dictionary<string, object> basic)
                {
                    if (basic.TryGetValue("name", out var nameObj))
                        name = nameObj?.ToString();
                }
            }

            // Create item using EditorDataService
            var newItem = DataService.CreateItem(name);

            // Apply any additional data from payload
            if (itemData != null && itemData.Count > 0)
            {
                DataService.UpdateItem(newItem.basic.id, i =>
                {
                    DataModelMapper.ApplyPartialUpdate(i, itemData);
                });
            }

            return CreateSuccessResponse(
                ("uuId", newItem.basic.id),
                ("filename", "item"),
                ("message", "Item created successfully.")
            );
        }

        private object UpdateItem(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var itemData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (itemData == null)
            {
                throw new InvalidOperationException("Item data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateItem(uuId, i =>
            {
                DataModelMapper.ApplyPartialUpdate(i, itemData);
            });

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Item updated successfully.")
            );
        }

        private object DeleteItem(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteItem(uuId);

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Item deleted successfully.")
            );
        }

        #endregion

        #region Animation Operations

        private object ListAnimations(Dictionary<string, object> payload)
        {
            var animations = DataService.LoadAnimations();
            var result = animations.Select(a => new Dictionary<string, object>
            {
                ["uuId"] = a.id,
                ["name"] = a.particleName ?? "Unnamed",
                ["filename"] = "animation"
            }).ToList();

            return CreatePaginatedResponse("animations", result, payload);
        }

        private object GetAnimationById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var animation = DataService.LoadAnimationById(uuId);
            if (animation == null)
            {
                throw new InvalidOperationException($"Animation with uuId '{uuId}' not found.");
            }

            return CreateSuccessResponse(
                ("uuId", animation.id),
                ("filename", "animation"),
                ("data", DataModelMapper.ToDict(animation))
            );
        }

        private object GetAnimations(Dictionary<string, object> payload)
        {
            var animations = DataService.LoadAnimations();
            var result = animations.Select(a => new Dictionary<string, object>
            {
                ["uuId"] = a.id,
                ["name"] = a.particleName ?? "Unnamed",
                ["filename"] = "animation",
                ["data"] = DataModelMapper.ToDict(a)
            }).ToList();

            return CreateSuccessResponse(
                ("animations", result),
                ("count", result.Count)
            );
        }

        private object CreateAnimation(Dictionary<string, object> payload)
        {
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            // Get optional name from payload
            string name = null;
            if (animationData != null)
            {
                if (animationData.TryGetValue("particleName", out var nameObj))
                    name = nameObj?.ToString();
            }

            // Create animation using EditorDataService
            var newAnimation = DataService.CreateAnimation(name);

            // Apply any additional data from payload
            if (animationData != null && animationData.Count > 0)
            {
                DataService.UpdateAnimation(newAnimation.id, a =>
                {
                    DataModelMapper.ApplyPartialUpdate(a, animationData);
                });
            }

            return CreateSuccessResponse(
                ("uuId", newAnimation.id),
                ("filename", "animation"),
                ("message", "Animation created successfully.")
            );
        }

        private object UpdateAnimation(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateAnimation(uuId, a =>
            {
                DataModelMapper.ApplyPartialUpdate(a, animationData);
            });

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Animation updated successfully.")
            );
        }

        private object DeleteAnimation(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteAnimation(uuId);

            return CreateSuccessResponse(
                ("uuId", uuId),
                ("message", "Animation deleted successfully.")
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
            var settingData = GetPayloadValue<Dictionary<string, object>>(payload, "settingData");

            if (settingData == null)
            {
                throw new InvalidOperationException("Setting data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateSystemSettings(s =>
            {
                DataModelMapper.ApplyPartialUpdate(s, settingData);
            });

            return CreateSuccessResponse(
                ("message", "System settings updated successfully.")
            );
        }

        #endregion

        #region Export/Import/Backup

        private object ExportDatabase(Dictionary<string, object> payload)
        {
            var exportPath = GetString(payload, "exportPath");
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = Path.Combine(projectRoot, "RPGMaker_Database_Export");
            }
            else
            {
                // Validate export path is within project directory
                exportPath = McpBridgeConstants.ValidateAndNormalizePath(exportPath, projectRoot);
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
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (string.IsNullOrEmpty(importPath))
            {
                throw new InvalidOperationException("Import path is required.");
            }

            // Validate import path is within project directory
            importPath = McpBridgeConstants.ValidateAndNormalizePath(importPath, projectRoot);

            if (!Directory.Exists(importPath))
            {
                throw new InvalidOperationException($"Import path does not exist: '{importPath}'.");
            }

            var targetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            // Create backup before import
            var backupPath = Path.Combine(projectRoot, $"RPGMaker_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
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
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(projectRoot, $"RPGMaker_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            }
            else
            {
                // Validate backup path is within project directory
                backupPath = McpBridgeConstants.ValidateAndNormalizePath(backupPath, projectRoot);
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
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (string.IsNullOrEmpty(backupPath))
            {
                throw new InvalidOperationException("Backup path is required.");
            }

            // Validate backup path is within project directory
            backupPath = McpBridgeConstants.ValidateAndNormalizePath(backupPath, projectRoot);

            if (!Directory.Exists(backupPath))
            {
                throw new InvalidOperationException($"Backup path does not exist: '{backupPath}'.");
            }

            var targetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            // Create backup of current state before restore
            var currentBackupPath = Path.Combine(projectRoot, $"RPGMaker_PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}");
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

        /// <summary>
        /// Get ID from payload (supports both "id" and "filename" parameters).
        /// </summary>
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

        /// <summary>
        /// Read JSON file and return as JToken (supports both arrays and objects).
        /// </summary>
        private JToken ReadJsonFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return JToken.Parse(content);
        }

        /// <summary>
        /// Convert JToken to appropriate .NET object (Dictionary or List).
        /// </summary>
        private object ConvertJTokenToObject(JToken token)
        {
            return token.ToObject<object>();
        }

        /// <summary>
        /// Write JToken to JSON file with formatting.
        /// </summary>
        private void WriteJsonFile(string filePath, JToken data)
        {
            var json = data.ToString(Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Merge update data into existing data (partial update support).
        /// </summary>
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
            // For arrays, replace entirely
            return JToken.FromObject(updates);
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
