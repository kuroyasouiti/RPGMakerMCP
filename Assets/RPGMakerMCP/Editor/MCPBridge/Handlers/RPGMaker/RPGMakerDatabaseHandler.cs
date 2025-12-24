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
            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            if (!Directory.Exists(characterPath))
            {
                return CreatePaginatedResponse("characters", new List<Dictionary<string, object>>(), payload);
            }

            var characterFiles = Directory.GetFiles(characterPath, "*.json");
            var characters = new List<Dictionary<string, object>>();

            foreach (var file in characterFiles)
            {
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
                            var name = item["basic"]?["name"]?.ToString() ?? item["name"]?.ToString() ?? "Unnamed";
                            if (!string.IsNullOrEmpty(uuId))
                            {
                                characters.Add(new Dictionary<string, object>
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
                    Debug.LogWarning($"Failed to parse character file {file}: {ex.Message}");
                }
            }

            return CreatePaginatedResponse("characters", characters, payload);
        }

        private object GetCharacterById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            if (!Directory.Exists(characterPath))
            {
                throw new InvalidOperationException("Character directory not found.");
            }

            var characterFiles = Directory.GetFiles(characterPath, "*.json");

            foreach (var file in characterFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemUuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
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

            throw new InvalidOperationException($"Character with uuId '{uuId}' not found.");
        }

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
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
                            if (!string.IsNullOrEmpty(uuId))
                            {
                                characters.Add(new Dictionary<string, object>
                                {
                                    ["uuId"] = uuId,
                                    ["name"] = item["basic"]?["name"]?.ToString() ?? item["name"]?.ToString() ?? "Unnamed",
                                    ["filename"] = filename,
                                    ["data"] = ConvertJTokenToObject(item)
                                });
                            }
                        }
                    }
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
            var targetFile = GetString(payload, "filename") ?? "characterActor";

            if (characterData == null)
            {
                throw new InvalidOperationException("Character data is required.");
            }

            // Generate UUID if not provided
            if (!characterData.ContainsKey("uuId") || string.IsNullOrEmpty(characterData["uuId"]?.ToString()))
            {
                characterData["uuId"] = Guid.NewGuid().ToString();
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(characterPath);

            var filePath = Path.Combine(characterPath, $"{targetFile}.json");

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

            array.Add(JObject.FromObject(characterData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("uuId", characterData["uuId"]),
                ("filename", targetFile),
                ("message", $"Character created successfully.")
            );
        }

        private object UpdateCharacter(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var characterData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "characterData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (characterData == null)
            {
                throw new InvalidOperationException("Character data is required.");
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var characterFiles = Directory.GetFiles(characterPath, "*.json");

            foreach (var file in characterFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemUuId = array[i]["uuId"]?.ToString() ?? array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            if (partialUpdate)
                            {
                                var existing = array[i] as JObject;
                                existing?.Merge(JObject.FromObject(characterData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace,
                                    MergeNullValueHandling = MergeNullValueHandling.Merge
                                });
                            }
                            else
                            {
                                characterData["uuId"] = uuId; // Preserve UUID
                                array[i] = JObject.FromObject(characterData);
                            }

                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Character updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Character with uuId '{uuId}' not found.");
        }

        private object DeleteCharacter(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var characterPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var characterFiles = Directory.GetFiles(characterPath, "*.json");

            foreach (var file in characterFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = array.Count - 1; i >= 0; i--)
                    {
                        var itemUuId = array[i]["uuId"]?.ToString() ?? array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            array.RemoveAt(i);
                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Character deleted successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Character with uuId '{uuId}' not found.");
        }

        #endregion

        #region Item Operations

        private object ListItems(Dictionary<string, object> payload)
        {
            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            if (!Directory.Exists(itemPath))
            {
                return CreatePaginatedResponse("items", new List<Dictionary<string, object>>(), payload);
            }

            var itemFiles = Directory.GetFiles(itemPath, "*.json");
            var items = new List<Dictionary<string, object>>();

            foreach (var file in itemFiles)
            {
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
                            var name = item["basic"]?["name"]?.ToString() ?? item["name"]?.ToString() ?? "Unnamed";
                            if (!string.IsNullOrEmpty(uuId))
                            {
                                items.Add(new Dictionary<string, object>
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
                    Debug.LogWarning($"Failed to parse item file {file}: {ex.Message}");
                }
            }

            return CreatePaginatedResponse("items", items, payload);
        }

        private object GetItemById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            if (!Directory.Exists(itemPath))
            {
                throw new InvalidOperationException("Item directory not found.");
            }

            var itemFiles = Directory.GetFiles(itemPath, "*.json");

            foreach (var file in itemFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var itemUuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
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

            throw new InvalidOperationException($"Item with uuId '{uuId}' not found.");
        }

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
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);
                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["uuId"]?.ToString() ?? item["id"]?.ToString();
                            items.Add(new Dictionary<string, object>
                            {
                                ["uuId"] = uuId,
                                ["filename"] = filename,
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
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
            var targetFile = GetString(payload, "filename") ?? "item";

            if (itemData == null)
            {
                throw new InvalidOperationException("Item data is required.");
            }

            if (!itemData.ContainsKey("uuId") || string.IsNullOrEmpty(itemData["uuId"]?.ToString()))
            {
                itemData["uuId"] = Guid.NewGuid().ToString();
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            Directory.CreateDirectory(itemPath);

            var filePath = Path.Combine(itemPath, $"{targetFile}.json");

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

            array.Add(JObject.FromObject(itemData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("uuId", itemData["uuId"]),
                ("filename", targetFile),
                ("message", $"Item created successfully.")
            );
        }

        private object UpdateItem(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var itemData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (itemData == null)
            {
                throw new InvalidOperationException("Item data is required.");
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var itemFiles = Directory.GetFiles(itemPath, "*.json");

            foreach (var file in itemFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemUuId = array[i]["uuId"]?.ToString() ?? array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            if (partialUpdate)
                            {
                                var existing = array[i] as JObject;
                                existing?.Merge(JObject.FromObject(itemData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace,
                                    MergeNullValueHandling = MergeNullValueHandling.Merge
                                });
                            }
                            else
                            {
                                itemData["uuId"] = uuId;
                                array[i] = JObject.FromObject(itemData);
                            }

                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Item updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Item with uuId '{uuId}' not found.");
        }

        private object DeleteItem(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var itemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var itemFiles = Directory.GetFiles(itemPath, "*.json");

            foreach (var file in itemFiles)
            {
                var data = ReadJsonFile(file);
                if (data is JArray array)
                {
                    for (int i = array.Count - 1; i >= 0; i--)
                    {
                        var itemUuId = array[i]["uuId"]?.ToString() ?? array[i]["id"]?.ToString();
                        if (itemUuId == uuId)
                        {
                            array.RemoveAt(i);
                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Item deleted successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Item with uuId '{uuId}' not found.");
        }

        #endregion

        #region Animation Operations

        private object ListAnimations(Dictionary<string, object> payload)
        {
            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            if (!Directory.Exists(animationPath))
            {
                return CreatePaginatedResponse("animations", new List<Dictionary<string, object>>(), payload);
            }

            var animationFiles = Directory.GetFiles(animationPath, "*.json");
            var animations = new List<Dictionary<string, object>>();

            foreach (var file in animationFiles)
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
                            var name = item["particleName"]?.ToString() ?? item["name"]?.ToString() ?? "Unnamed";
                            if (!string.IsNullOrEmpty(uuId))
                            {
                                animations.Add(new Dictionary<string, object>
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
                    Debug.LogWarning($"Failed to parse animation file {file}: {ex.Message}");
                }
            }

            return CreatePaginatedResponse("animations", animations, payload);
        }

        private object GetAnimationById(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            if (!Directory.Exists(animationPath))
            {
                throw new InvalidOperationException("Animation directory not found.");
            }

            var animationFiles = Directory.GetFiles(animationPath, "*.json");

            foreach (var file in animationFiles)
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

            throw new InvalidOperationException($"Animation with uuId '{uuId}' not found.");
        }

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
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);
                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var uuId = item["id"]?.ToString();
                            animations.Add(new Dictionary<string, object>
                            {
                                ["uuId"] = uuId,
                                ["filename"] = filename,
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
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
            var targetFile = GetString(payload, "filename") ?? "animation";

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            if (!animationData.ContainsKey("id") || string.IsNullOrEmpty(animationData["id"]?.ToString()))
            {
                animationData["id"] = Guid.NewGuid().ToString();
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            Directory.CreateDirectory(animationPath);

            var filePath = Path.Combine(animationPath, $"{targetFile}.json");

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

            array.Add(JObject.FromObject(animationData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("uuId", animationData["id"]),
                ("filename", targetFile),
                ("message", $"Animation created successfully.")
            );
        }

        private object UpdateAnimation(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "itemData")
                ?? GetPayloadValue<Dictionary<string, object>>(payload, "animationData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            var animationFiles = Directory.GetFiles(animationPath, "*.json");

            foreach (var file in animationFiles)
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
                                existing?.Merge(JObject.FromObject(animationData), new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace,
                                    MergeNullValueHandling = MergeNullValueHandling.Merge
                                });
                            }
                            else
                            {
                                animationData["id"] = uuId;
                                array[i] = JObject.FromObject(animationData);
                            }

                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("uuId", uuId),
                                ("message", $"Animation updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Animation with uuId '{uuId}' not found.");
        }

        private object DeleteAnimation(Dictionary<string, object> payload)
        {
            var uuId = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(uuId))
            {
                throw new InvalidOperationException("uuId is required.");
            }

            var animationPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            var animationFiles = Directory.GetFiles(animationPath, "*.json");

            foreach (var file in animationFiles)
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
                                ("message", $"Animation deleted successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Animation with uuId '{uuId}' not found.");
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
                    var id = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);
                    systemSettings[id] = ConvertJTokenToObject(data);
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
            var id = GetId(payload);
            var settingData = GetPayloadValue<Dictionary<string, object>>(payload, "settingData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID is required.");
            }

            if (settingData == null)
            {
                throw new InvalidOperationException("Setting data is required.");
            }

            var systemPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            Directory.CreateDirectory(systemPath);

            var filePath = Path.Combine(systemPath, $"{id}.json");

            JToken finalData;
            if (partialUpdate && File.Exists(filePath))
            {
                var existing = ReadJsonFile(filePath);
                finalData = MergeData(existing, settingData);
            }
            else
            {
                finalData = JToken.FromObject(settingData);
            }

            WriteJsonFile(filePath, finalData);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"System setting '{id}' updated successfully.")
            );
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
