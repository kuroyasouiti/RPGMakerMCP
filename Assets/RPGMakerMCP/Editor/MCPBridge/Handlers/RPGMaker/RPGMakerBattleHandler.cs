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
            "getEnemies",
            "createEnemy",
            "updateEnemy",
            "deleteEnemy",
            "getTroops",
            "createTroop",
            "updateTroop",
            "deleteTroop",
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
                "getEnemies" => GetEnemies(payload),
                "createEnemy" => CreateEnemy(payload),
                "updateEnemy" => UpdateEnemy(payload),
                "deleteEnemy" => DeleteEnemy(payload),
                "getTroops" => GetTroops(payload),
                "createTroop" => CreateTroop(payload),
                "updateTroop" => UpdateTroop(payload),
                "deleteTroop" => DeleteTroop(payload),
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
            var readOnlyOperations = new[] { "getBattleSettings", "getEnemies", "getTroops", "getSkills", "getBattleAnimations" };
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
                    var content = File.ReadAllText(file);
                    var settingData = MiniJson.Deserialize(content);
                    settings[Path.GetFileNameWithoutExtension(file)] = settingData;
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
            File.WriteAllText(filePath, MiniJson.Serialize(settingData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Battle setting '{filename}' updated successfully."));
        }

        #endregion

        #region Enemy Operations

        private object GetEnemies(Dictionary<string, object> payload)
        {
            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            if (!Directory.Exists(enemyPath))
            {
                return CreateSuccessResponse(("enemies", new List<object>()), ("message", "No enemy data found."));
            }

            var enemyFiles = Directory.GetFiles(enemyPath, "*enemy*.json");
            var enemies = new List<Dictionary<string, object>>();

            foreach (var file in enemyFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var enemyData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    enemies.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = enemyData?.ContainsKey("name") == true ? enemyData["name"]?.ToString() : "Unnamed Enemy",
                        ["data"] = enemyData
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse enemy file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("enemies", enemies),
                ("count", enemies.Count)
            );
        }

        private object CreateEnemy(Dictionary<string, object> payload)
        {
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");
            var filename = GetString(payload, "filename");

            if (enemyData == null)
            {
                throw new InvalidOperationException("Enemy data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"enemy_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(enemyPath);

            var filePath = Path.Combine(enemyPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(enemyData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("message", $"Enemy '{filename}' created successfully.")
            );
        }

        private object UpdateEnemy(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (enemyData == null)
            {
                throw new InvalidOperationException("Enemy data is required.");
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Enemy file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(enemyData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Enemy '{filename}' updated successfully."));
        }

        private object DeleteEnemy(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var enemyPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(enemyPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Enemy file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Enemy '{filename}' deleted successfully."));
        }

        #endregion

        #region Troop Operations

        private object GetTroops(Dictionary<string, object> payload)
        {
            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            if (!Directory.Exists(troopPath))
            {
                return CreateSuccessResponse(("troops", new List<object>()), ("message", "No troop data found."));
            }

            var troopFiles = Directory.GetFiles(troopPath, "*troop*.json");
            var troops = new List<Dictionary<string, object>>();

            foreach (var file in troopFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var troopData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    troops.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = troopData?.ContainsKey("name") == true ? troopData["name"]?.ToString() : "Unnamed Troop",
                        ["data"] = troopData
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse troop file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("troops", troops),
                ("count", troops.Count)
            );
        }

        private object CreateTroop(Dictionary<string, object> payload)
        {
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");
            var filename = GetString(payload, "filename");

            if (troopData == null)
            {
                throw new InvalidOperationException("Troop data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"troop_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            Directory.CreateDirectory(troopPath);

            var filePath = Path.Combine(troopPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(troopData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("message", $"Troop '{filename}' created successfully.")
            );
        }

        private object UpdateTroop(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (troopData == null)
            {
                throw new InvalidOperationException("Troop data is required.");
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Troop file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(troopData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Troop '{filename}' updated successfully."));
        }

        private object DeleteTroop(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var troopPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Character", "JSON");
            var filePath = Path.Combine(troopPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Troop file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Troop '{filename}' deleted successfully."));
        }

        #endregion

        #region Skill Operations

        private object GetSkills(Dictionary<string, object> payload)
        {
            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            if (!Directory.Exists(skillPath))
            {
                return CreateSuccessResponse(("skills", new List<object>()), ("message", "No skill data found."));
            }

            var skillFiles = Directory.GetFiles(skillPath, "*skill*.json");
            var skills = new List<Dictionary<string, object>>();

            foreach (var file in skillFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var skillData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    skills.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = skillData?.ContainsKey("name") == true ? skillData["name"]?.ToString() : "Unnamed Skill",
                        ["data"] = skillData
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse skill file {file}: {ex.Message}");
                }
            }

            return CreateSuccessResponse(
                ("skills", skills),
                ("count", skills.Count)
            );
        }

        private object CreateSkill(Dictionary<string, object> payload)
        {
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");
            var filename = GetString(payload, "filename");

            if (skillData == null)
            {
                throw new InvalidOperationException("Skill data is required.");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = $"skill_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            Directory.CreateDirectory(skillPath);

            var filePath = Path.Combine(skillPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(skillData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("filename", filename),
                ("message", $"Skill '{filename}' created successfully.")
            );
        }

        private object UpdateSkill(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (skillData == null)
            {
                throw new InvalidOperationException("Skill data is required.");
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var filePath = Path.Combine(skillPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Skill file '{filename}' not found.");
            }

            File.WriteAllText(filePath, MiniJson.Serialize(skillData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Skill '{filename}' updated successfully."));
        }

        private object DeleteSkill(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var skillPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Item", "JSON");
            var filePath = Path.Combine(skillPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Skill file '{filename}' not found.");
            }

            File.Delete(filePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Skill '{filename}' deleted successfully."));
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

            var animFiles = Directory.GetFiles(animPath, "*.json");
            var animations = new List<Dictionary<string, object>>();

            foreach (var file in animFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var animData = MiniJson.Deserialize(content) as Dictionary<string, object>;
                    animations.Add(new Dictionary<string, object>
                    {
                        ["filename"] = Path.GetFileNameWithoutExtension(file),
                        ["name"] = animData?.ContainsKey("name") == true ? animData["name"]?.ToString() : "Unnamed Animation",
                        ["data"] = animData
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

        private object UpdateBattleAnimation(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            var animPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Animation", "JSON");
            Directory.CreateDirectory(animPath);

            var filePath = Path.Combine(animPath, $"{filename}.json");
            File.WriteAllText(filePath, MiniJson.Serialize(animationData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Battle animation '{filename}' updated successfully."));
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
