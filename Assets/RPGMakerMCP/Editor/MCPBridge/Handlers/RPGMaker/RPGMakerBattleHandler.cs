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
    /// RPGMaker battle system management handler.
    /// Handles operations for enemies, troops, skills, and battle settings.
    /// </summary>
    public class RPGMakerBattleHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerBattle";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getBattleSettings",
            "updateBattleSettings",
            "listEnemies",
            "getEnemyById",
            "getEnemies",
            "createEnemy",
            "updateEnemy",
            "deleteEnemy",
            "listTroops",
            "getTroopById",
            "getTroops",
            "createTroop",
            "updateTroop",
            "deleteTroop",
            "listSkills",
            "getSkillById",
            "getSkills",
            "createSkill",
            "updateSkill",
            "deleteSkill",
            "getBattleAnimations",
            "updateBattleAnimation"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getBattleSettings" => GetBattleSettings(),
                "updateBattleSettings" => UpdateBattleSettings(payload),
                "listEnemies" => ListEnemies(payload),
                "getEnemyById" => GetEnemyById(payload),
                "getEnemies" => GetEnemies(payload),
                "createEnemy" => CreateEnemy(payload),
                "updateEnemy" => UpdateEnemy(payload),
                "deleteEnemy" => DeleteEnemy(payload),
                "listTroops" => ListTroops(payload),
                "getTroopById" => GetTroopById(payload),
                "getTroops" => GetTroops(payload),
                "createTroop" => CreateTroop(payload),
                "updateTroop" => UpdateTroop(payload),
                "deleteTroop" => DeleteTroop(payload),
                "listSkills" => ListSkills(payload),
                "getSkillById" => GetSkillById(payload),
                "getSkills" => GetSkills(payload),
                "createSkill" => CreateSkill(payload),
                "updateSkill" => UpdateSkill(payload),
                "deleteSkill" => DeleteSkill(payload),
                "getBattleAnimations" => GetBattleAnimations(payload),
                "updateBattleAnimation" => UpdateBattleAnimation(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] {
                "getBattleSettings",
                "listEnemies", "getEnemyById", "getEnemies",
                "listTroops", "getTroopById", "getTroops",
                "listSkills", "getSkillById", "getSkills",
                "getBattleAnimations"
            };
            return !readOnlyOperations.Contains(operation);
        }

        #region Battle Settings

        private object GetBattleSettings()
        {
            var battlePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            var battleFiles = Directory.Exists(battlePath)
                ? Directory.GetFiles(battlePath, "*battle*.json")
                : new string[0];

            var settings = new Dictionary<string, object>();
            foreach (var file in battleFiles)
            {
                try
                {
                    var data = ReadJsonFile(file);
                    settings[Path.GetFileNameWithoutExtension(file)] = ConvertJTokenToObject(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse battle file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(("settings", settings));
        }

        private object UpdateBattleSettings(Dictionary<string, object> payload)
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

            var battlePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System");
            Directory.CreateDirectory(battlePath);

            var filePath = Path.Combine(battlePath, $"{filename}.json");
            WriteJsonFile(filePath, JToken.FromObject(settingData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Battle setting '{filename}' updated successfully."));
        }

        #endregion

        #region Enemy Operations

        private object ListEnemies(Dictionary<string, object> payload)
        {
            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, "enemy.json");

            if (!File.Exists(filePath))
            {
                return CreatePaginatedResponse("enemies", new List<Dictionary<string, object>>(), payload);
            }

            var enemies = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["id"]?.ToString();
                        var name = item["name"]?.ToString() ?? "Unnamed Enemy";
                        if (!string.IsNullOrEmpty(id))
                        {
                            enemies.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse enemy.json: {ex.Message}");
            }

            return CreatePaginatedResponse("enemies", enemies, payload);
        }

        private object GetEnemyById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, "enemy.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Enemy data file not found.");
            }

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
                            ("data", ConvertJTokenToObject(item))
                        );
                    }
                }
            }

            throw new InvalidOperationException($"Enemy with id '{id}' not found.");
        }

        private object GetEnemies(Dictionary<string, object> payload)
        {
            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, "enemy.json");

            if (!File.Exists(filePath))
            {
                return CreateSuccessResponse(("enemies", new List<object>()), ("message", "No enemy data found."));
            }

            var enemies = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["id"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            enemies.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = item["name"]?.ToString() ?? "Unnamed Enemy",
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse enemy.json: {ex.Message}");
            }

            return CreateSuccessResponse(
                ("enemies", enemies),
                ("count", enemies.Count)
            );
        }

        private object CreateEnemy(Dictionary<string, object> payload)
        {
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");

            if (enemyData == null)
            {
                throw new InvalidOperationException("Enemy data is required.");
            }

            // Generate UUID if not provided
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }
            enemyData["id"] = id;

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(enemyPath);

            var filePath = Path.Combine(enemyPath, "enemy.json");

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

            array.Add(JObject.FromObject(enemyData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"Enemy '{id}' created successfully.")
            );
        }

        private object UpdateEnemy(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (enemyData == null)
            {
                throw new InvalidOperationException("Enemy data is required.");
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, "enemy.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Enemy data file not found.");
            }

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
                            finalData = MergeData(array[i], enemyData);
                        }
                        else
                        {
                            enemyData["id"] = id; // Preserve UUID
                            finalData = JToken.FromObject(enemyData);
                        }
                        array[i] = finalData;

                        WriteJsonFile(filePath, array);
                        AssetDatabase.Refresh();
                        RefreshHierarchy();

                        return CreateSuccessResponse(("id", id), ("message", $"Enemy '{id}' updated successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Enemy with id '{id}' not found.");
        }

        private object DeleteEnemy(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, "enemy.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Enemy data file not found.");
            }

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

                        return CreateSuccessResponse(("id", id), ("message", $"Enemy '{id}' deleted successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Enemy with id '{id}' not found.");
        }

        #endregion

        #region Troop Operations

        private object ListTroops(Dictionary<string, object> payload)
        {
            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, "troop.json");

            if (!File.Exists(filePath))
            {
                return CreatePaginatedResponse("troops", new List<Dictionary<string, object>>(), payload);
            }

            var troops = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["id"]?.ToString();
                        var name = item["name"]?.ToString() ?? "Unnamed Troop";
                        if (!string.IsNullOrEmpty(id))
                        {
                            troops.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse troop.json: {ex.Message}");
            }

            return CreatePaginatedResponse("troops", troops, payload);
        }

        private object GetTroopById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, "troop.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Troop data file not found.");
            }

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
                            ("data", ConvertJTokenToObject(item))
                        );
                    }
                }
            }

            throw new InvalidOperationException($"Troop with id '{id}' not found.");
        }

        private object GetTroops(Dictionary<string, object> payload)
        {
            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, "troop.json");

            if (!File.Exists(filePath))
            {
                return CreateSuccessResponse(("troops", new List<object>()), ("message", "No troop data found."));
            }

            var troops = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["id"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            troops.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = item["name"]?.ToString() ?? "Unnamed Troop",
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse troop.json: {ex.Message}");
            }

            return CreateSuccessResponse(
                ("troops", troops),
                ("count", troops.Count)
            );
        }

        private object CreateTroop(Dictionary<string, object> payload)
        {
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");

            if (troopData == null)
            {
                throw new InvalidOperationException("Troop data is required.");
            }

            // Generate UUID if not provided
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }
            troopData["id"] = id;

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(troopPath);

            var filePath = Path.Combine(troopPath, "troop.json");

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

            array.Add(JObject.FromObject(troopData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"Troop '{id}' created successfully.")
            );
        }

        private object UpdateTroop(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (troopData == null)
            {
                throw new InvalidOperationException("Troop data is required.");
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, "troop.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Troop data file not found.");
            }

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
                            finalData = MergeData(array[i], troopData);
                        }
                        else
                        {
                            troopData["id"] = id; // Preserve UUID
                            finalData = JToken.FromObject(troopData);
                        }
                        array[i] = finalData;

                        WriteJsonFile(filePath, array);
                        AssetDatabase.Refresh();
                        RefreshHierarchy();

                        return CreateSuccessResponse(("id", id), ("message", $"Troop '{id}' updated successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Troop with id '{id}' not found.");
        }

        private object DeleteTroop(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, "troop.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Troop data file not found.");
            }

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

                        return CreateSuccessResponse(("id", id), ("message", $"Troop '{id}' deleted successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Troop with id '{id}' not found.");
        }

        #endregion

        #region Skill Operations

        private object ListSkills(Dictionary<string, object> payload)
        {
            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            var filePath = Path.Combine(skillPath, "skillCustom.json");

            if (!File.Exists(filePath))
            {
                return CreatePaginatedResponse("skills", new List<Dictionary<string, object>>(), payload);
            }

            var skills = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["basic"]?["id"]?.ToString();
                        var name = item["basic"]?["name"]?.ToString() ?? "Unnamed Skill";
                        if (!string.IsNullOrEmpty(id))
                        {
                            skills.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse skillCustom.json: {ex.Message}");
            }

            return CreatePaginatedResponse("skills", skills, payload);
        }

        private object GetSkillById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            var filePath = Path.Combine(skillPath, "skillCustom.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Skill data file not found.");
            }

            var data = ReadJsonFile(filePath);
            if (data is JArray array)
            {
                foreach (var item in array)
                {
                    var itemId = item["basic"]?["id"]?.ToString();
                    if (itemId == id)
                    {
                        return CreateSuccessResponse(
                            ("id", id),
                            ("data", ConvertJTokenToObject(item))
                        );
                    }
                }
            }

            throw new InvalidOperationException($"Skill with id '{id}' not found.");
        }

        private object GetSkills(Dictionary<string, object> payload)
        {
            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            var filePath = Path.Combine(skillPath, "skillCustom.json");

            if (!File.Exists(filePath))
            {
                return CreateSuccessResponse(("skills", new List<object>()), ("message", "No skill data found."));
            }

            var skills = new List<Dictionary<string, object>>();
            try
            {
                var data = ReadJsonFile(filePath);
                if (data is JArray array)
                {
                    foreach (var item in array)
                    {
                        var id = item["basic"]?["id"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            skills.Add(new Dictionary<string, object>
                            {
                                ["id"] = id,
                                ["name"] = item["basic"]?["name"]?.ToString() ?? "Unnamed Skill",
                                ["data"] = ConvertJTokenToObject(item)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse skillCustom.json: {ex.Message}");
            }

            return CreateSuccessResponse(
                ("skills", skills),
                ("count", skills.Count)
            );
        }

        private object CreateSkill(Dictionary<string, object> payload)
        {
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");

            if (skillData == null)
            {
                throw new InvalidOperationException("Skill data is required.");
            }

            // Generate UUID if not provided
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }

            // Ensure basic object exists and set id
            if (!skillData.ContainsKey("basic"))
            {
                skillData["basic"] = new Dictionary<string, object>();
            }
            if (skillData["basic"] is Dictionary<string, object> basic)
            {
                basic["id"] = id;
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            Directory.CreateDirectory(skillPath);

            var filePath = Path.Combine(skillPath, "skillCustom.json");

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

            array.Add(JObject.FromObject(skillData));
            WriteJsonFile(filePath, array);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("id", id),
                ("message", $"Skill '{id}' created successfully.")
            );
        }

        private object UpdateSkill(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");
            var partialUpdate = GetBool(payload, "partial", false);

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (skillData == null)
            {
                throw new InvalidOperationException("Skill data is required.");
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            var filePath = Path.Combine(skillPath, "skillCustom.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Skill data file not found.");
            }

            var data = ReadJsonFile(filePath);
            if (data is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var itemId = array[i]["basic"]?["id"]?.ToString();
                    if (itemId == id)
                    {
                        JToken finalData;
                        if (partialUpdate)
                        {
                            finalData = MergeData(array[i], skillData);
                        }
                        else
                        {
                            // Ensure basic.id is preserved
                            if (!skillData.ContainsKey("basic"))
                            {
                                skillData["basic"] = new Dictionary<string, object>();
                            }
                            if (skillData["basic"] is Dictionary<string, object> basic)
                            {
                                basic["id"] = id;
                            }
                            finalData = JToken.FromObject(skillData);
                        }
                        array[i] = finalData;

                        WriteJsonFile(filePath, array);
                        AssetDatabase.Refresh();
                        RefreshHierarchy();

                        return CreateSuccessResponse(("id", id), ("message", $"Skill '{id}' updated successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Skill with id '{id}' not found.");
        }

        private object DeleteSkill(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Initializations", "JSON");
            var filePath = Path.Combine(skillPath, "skillCustom.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Skill data file not found.");
            }

            var data = ReadJsonFile(filePath);
            if (data is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var itemId = array[i]["basic"]?["id"]?.ToString();
                    if (itemId == id)
                    {
                        array.RemoveAt(i);

                        WriteJsonFile(filePath, array);
                        AssetDatabase.Refresh();
                        RefreshHierarchy();

                        return CreateSuccessResponse(("id", id), ("message", $"Skill '{id}' deleted successfully."));
                    }
                }
            }

            throw new InvalidOperationException($"Skill with id '{id}' not found.");
        }

        #endregion

        #region Battle Animation Operations

        private object GetBattleAnimations(Dictionary<string, object> payload)
        {
            var animPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            if (!Directory.Exists(animPath))
            {
                return CreateSuccessResponse(("animations", new List<object>()), ("message", "No battle animation data found."));
            }

            var animations = new List<Dictionary<string, object>>();
            var animFiles = Directory.GetFiles(animPath, "*.json");

            foreach (var file in animFiles)
            {
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var data = ReadJsonFile(file);

                    if (data is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var id = item["id"]?.ToString();
                            var name = item["particleName"]?.ToString() ?? item["name"]?.ToString() ?? "Unnamed Animation";
                            if (!string.IsNullOrEmpty(id))
                            {
                                animations.Add(new Dictionary<string, object>
                                {
                                    ["id"] = id,
                                    ["name"] = name,
                                    ["filename"] = filename
                                });
                            }
                        }
                    }
                    else if (data is JObject obj)
                    {
                        var id = obj["id"]?.ToString();
                        animations.Add(new Dictionary<string, object>
                        {
                            ["id"] = id ?? filename,
                            ["name"] = obj["particleName"]?.ToString() ?? obj["name"]?.ToString() ?? "Unnamed Animation",
                            ["filename"] = filename
                        });
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

        private object UpdateBattleAnimation(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id") ?? GetString(payload, "filename");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            var animPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            if (!Directory.Exists(animPath))
            {
                throw new InvalidOperationException("Animation data directory not found.");
            }

            var animFiles = Directory.GetFiles(animPath, "*.json");
            foreach (var file in animFiles)
            {
                var data = ReadJsonFile(file);

                if (data is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var itemId = array[i]["id"]?.ToString();
                        if (itemId == id)
                        {
                            animationData["id"] = id; // Preserve UUID
                            array[i] = JToken.FromObject(animationData);

                            WriteJsonFile(file, array);
                            AssetDatabase.Refresh();
                            RefreshHierarchy();

                            return CreateSuccessResponse(
                                ("id", id),
                                ("filename", Path.GetFileNameWithoutExtension(file)),
                                ("message", $"Battle animation '{id}' updated successfully.")
                            );
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Battle animation with id '{id}' not found.");
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

        private bool GetBool(Dictionary<string, object> payload, string key, bool defaultValue = false)
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                if (value is bool boolVal) return boolVal;
                if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
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
