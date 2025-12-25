using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using MCP.Editor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker battle system management handler.
    /// Handles operations for enemies, troops, skills, and battle settings.
    /// Uses EditorDataService for CRUD operations via the RPGMaker Editor API.
    /// </summary>
    public class RPGMakerBattleHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerBattle";
        public override string Version => "1.0.0";

        // Access the EditorDataService singleton for database operations
        private EditorDataService DataService => EditorDataService.Instance;

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
            // Load system settings which include battle configuration
            var settings = DataService.LoadSystemSettings();
            if (settings == null)
            {
                return CreateSuccessResponse(
                    ("settings", new Dictionary<string, object>()),
                    ("message", "No battle settings found.")
                );
            }

            return CreateSuccessResponse(
                ("settings", DataModelMapper.ToDict(settings))
            );
        }

        private object UpdateBattleSettings(Dictionary<string, object> payload)
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

            return CreateSuccessResponse(("message", "Battle settings updated successfully."));
        }

        #endregion

        #region Enemy Operations

        private object ListEnemies(Dictionary<string, object> payload)
        {
            var enemies = DataService.LoadEnemies();
            var result = enemies.Select(e => new Dictionary<string, object>
            {
                ["id"] = e.id,
                ["uuId"] = e.id,
                ["name"] = e.name ?? "Unnamed Enemy"
            }).ToList();

            return CreatePaginatedResponse("enemies", result, payload);
        }

        private object GetEnemyById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var enemy = DataService.LoadEnemyById(id);
            if (enemy == null)
            {
                throw new InvalidOperationException($"Enemy with id '{id}' not found.");
            }

            return CreateSuccessResponse(
                ("id", enemy.id),
                ("uuId", enemy.id),
                ("data", DataModelMapper.ToDict(enemy))
            );
        }

        private object GetEnemies(Dictionary<string, object> payload)
        {
            var enemies = DataService.LoadEnemies();
            var result = enemies.Select(e => new Dictionary<string, object>
            {
                ["id"] = e.id,
                ["uuId"] = e.id,
                ["name"] = e.name ?? "Unnamed Enemy",
                ["data"] = DataModelMapper.ToDict(e)
            }).ToList();

            return CreateSuccessResponse(
                ("enemies", result),
                ("count", result.Count)
            );
        }

        private object CreateEnemy(Dictionary<string, object> payload)
        {
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");

            // Get optional name from payload
            string name = null;
            if (enemyData != null && enemyData.TryGetValue("name", out var nameObj))
            {
                name = nameObj?.ToString();
            }

            // Create enemy using EditorDataService
            var newEnemy = DataService.CreateEnemy(name);

            // Apply any additional data from payload
            if (enemyData != null && enemyData.Count > 0)
            {
                DataService.UpdateEnemy(newEnemy.id, e =>
                {
                    DataModelMapper.ApplyPartialUpdate(e, enemyData);
                });
            }

            return CreateSuccessResponse(
                ("id", newEnemy.id),
                ("uuId", newEnemy.id),
                ("message", "Enemy created successfully.")
            );
        }

        private object UpdateEnemy(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var enemyData = GetPayloadValue<Dictionary<string, object>>(payload, "enemyData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (enemyData == null)
            {
                throw new InvalidOperationException("Enemy data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateEnemy(id, e =>
            {
                DataModelMapper.ApplyPartialUpdate(e, enemyData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Enemy updated successfully.")
            );
        }

        private object DeleteEnemy(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteEnemy(id);

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Enemy deleted successfully.")
            );
        }

        #endregion

        #region Troop Operations

        private object ListTroops(Dictionary<string, object> payload)
        {
            var troops = DataService.LoadTroops();
            var result = troops.Select(t => new Dictionary<string, object>
            {
                ["id"] = t.id,
                ["uuId"] = t.id,
                ["name"] = t.name ?? "Unnamed Troop"
            }).ToList();

            return CreatePaginatedResponse("troops", result, payload);
        }

        private object GetTroopById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var troop = DataService.LoadTroopById(id);
            if (troop == null)
            {
                throw new InvalidOperationException($"Troop with id '{id}' not found.");
            }

            return CreateSuccessResponse(
                ("id", troop.id),
                ("uuId", troop.id),
                ("data", DataModelMapper.ToDict(troop))
            );
        }

        private object GetTroops(Dictionary<string, object> payload)
        {
            var troops = DataService.LoadTroops();
            var result = troops.Select(t => new Dictionary<string, object>
            {
                ["id"] = t.id,
                ["uuId"] = t.id,
                ["name"] = t.name ?? "Unnamed Troop",
                ["data"] = DataModelMapper.ToDict(t)
            }).ToList();

            return CreateSuccessResponse(
                ("troops", result),
                ("count", result.Count)
            );
        }

        private object CreateTroop(Dictionary<string, object> payload)
        {
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");

            // Get optional name from payload
            string name = null;
            if (troopData != null && troopData.TryGetValue("name", out var nameObj))
            {
                name = nameObj?.ToString();
            }

            // Create troop using EditorDataService
            var newTroop = DataService.CreateTroop(name);

            // Apply any additional data from payload
            if (troopData != null && troopData.Count > 0)
            {
                DataService.UpdateTroop(newTroop.id, t =>
                {
                    DataModelMapper.ApplyPartialUpdate(t, troopData);
                });
            }

            return CreateSuccessResponse(
                ("id", newTroop.id),
                ("uuId", newTroop.id),
                ("message", "Troop created successfully.")
            );
        }

        private object UpdateTroop(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var troopData = GetPayloadValue<Dictionary<string, object>>(payload, "troopData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (troopData == null)
            {
                throw new InvalidOperationException("Troop data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateTroop(id, t =>
            {
                DataModelMapper.ApplyPartialUpdate(t, troopData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Troop updated successfully.")
            );
        }

        private object DeleteTroop(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteTroop(id);

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Troop deleted successfully.")
            );
        }

        #endregion

        #region Skill Operations

        private object ListSkills(Dictionary<string, object> payload)
        {
            var skills = DataService.LoadSkills();
            var result = skills.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.basic.id,
                ["uuId"] = s.basic.id,
                ["name"] = s.basic.name ?? "Unnamed Skill"
            }).ToList();

            return CreatePaginatedResponse("skills", result, payload);
        }

        private object GetSkillById(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            var skill = DataService.LoadSkillById(id);
            if (skill == null)
            {
                throw new InvalidOperationException($"Skill with id '{id}' not found.");
            }

            return CreateSuccessResponse(
                ("id", skill.basic.id),
                ("uuId", skill.basic.id),
                ("data", DataModelMapper.ToDict(skill))
            );
        }

        private object GetSkills(Dictionary<string, object> payload)
        {
            var skills = DataService.LoadSkills();
            var result = skills.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.basic.id,
                ["uuId"] = s.basic.id,
                ["name"] = s.basic.name ?? "Unnamed Skill",
                ["data"] = DataModelMapper.ToDict(s)
            }).ToList();

            return CreateSuccessResponse(
                ("skills", result),
                ("count", result.Count)
            );
        }

        private object CreateSkill(Dictionary<string, object> payload)
        {
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");

            // Get optional name from payload
            string name = null;
            if (skillData != null)
            {
                if (skillData.TryGetValue("basic", out var basicObj) && basicObj is Dictionary<string, object> basic)
                {
                    if (basic.TryGetValue("name", out var nameObj))
                        name = nameObj?.ToString();
                }
            }

            // Create skill using EditorDataService
            var newSkill = DataService.CreateSkill(name);

            // Apply any additional data from payload
            if (skillData != null && skillData.Count > 0)
            {
                DataService.UpdateSkill(newSkill.basic.id, s =>
                {
                    DataModelMapper.ApplyPartialUpdate(s, skillData);
                });
            }

            return CreateSuccessResponse(
                ("id", newSkill.basic.id),
                ("uuId", newSkill.basic.id),
                ("message", "Skill created successfully.")
            );
        }

        private object UpdateSkill(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var skillData = GetPayloadValue<Dictionary<string, object>>(payload, "skillData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (skillData == null)
            {
                throw new InvalidOperationException("Skill data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateSkill(id, s =>
            {
                DataModelMapper.ApplyPartialUpdate(s, skillData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Skill updated successfully.")
            );
        }

        private object DeleteSkill(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            // Delete using EditorDataService
            DataService.DeleteSkill(id);

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Skill deleted successfully.")
            );
        }

        #endregion

        #region Battle Animations

        private object GetBattleAnimations(Dictionary<string, object> payload)
        {
            // Use EditorDataService for animations
            var animations = DataService.LoadAnimations();
            var result = animations.Select(a => new Dictionary<string, object>
            {
                ["id"] = a.id,
                ["uuId"] = a.id,
                ["name"] = a.particleName ?? "Unnamed Animation",
                ["data"] = DataModelMapper.ToDict(a)
            }).ToList();

            return CreateSuccessResponse(
                ("animations", result),
                ("count", result.Count)
            );
        }

        private object UpdateBattleAnimation(Dictionary<string, object> payload)
        {
            var id = GetString(payload, "uuId") ?? GetString(payload, "id");
            var animationData = GetPayloadValue<Dictionary<string, object>>(payload, "animationData");

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("ID (uuId) is required.");
            }

            if (animationData == null)
            {
                throw new InvalidOperationException("Animation data is required.");
            }

            // Update using EditorDataService
            DataService.UpdateAnimation(id, a =>
            {
                DataModelMapper.ApplyPartialUpdate(a, animationData);
            });

            return CreateSuccessResponse(
                ("id", id),
                ("uuId", id),
                ("message", "Battle animation updated successfully.")
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

        #endregion
    }
}
