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
    /// RPGMaker game state management handler.
    /// Handles operations for player data, party, inventory, and progress flags.
    /// </summary>
    public class RPGMakerGameStateHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerGameState";
        public override string Version => "1.0.0";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "getGameState",
            "setGameState",
            "getPlayerData",
            "updatePlayerData",
            "getPartyData",
            "updatePartyData",
            "getInventory",
            "updateInventory",
            "addItemToInventory",
            "removeItemFromInventory",
            "getProgressFlags",
            "setProgressFlag",
            "getCurrentMap",
            "setCurrentMap",
            "teleportPlayer",
            "resetGameState"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "getGameState" => GetGameState(),
                "setGameState" => SetGameState(payload),
                "getPlayerData" => GetPlayerData(),
                "updatePlayerData" => UpdatePlayerData(payload),
                "getPartyData" => GetPartyData(),
                "updatePartyData" => UpdatePartyData(payload),
                "getInventory" => GetInventory(),
                "updateInventory" => UpdateInventory(payload),
                "addItemToInventory" => AddItemToInventory(payload),
                "removeItemFromInventory" => RemoveItemFromInventory(payload),
                "getProgressFlags" => GetProgressFlags(),
                "setProgressFlag" => SetProgressFlag(payload),
                "getCurrentMap" => GetCurrentMap(),
                "setCurrentMap" => SetCurrentMap(payload),
                "teleportPlayer" => TeleportPlayer(payload),
                "resetGameState" => ResetGameState(),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] { "getGameState", "getPlayerData", "getPartyData", "getInventory", "getProgressFlags", "getCurrentMap" };
            return !readOnlyOperations.Contains(operation);
        }

        #region Game State

        private object GetGameState()
        {
            var gameStatePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "gamestate.json");

            if (!File.Exists(gameStatePath))
            {
                return CreateSuccessResponse(("gameState", new Dictionary<string, object>()), ("message", "No game state found."));
            }

            var content = File.ReadAllText(gameStatePath);
            var gameState = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("gameState", gameState));
        }

        private object SetGameState(Dictionary<string, object> payload)
        {
            var gameStateData = GetPayloadValue<Dictionary<string, object>>(payload, "gameStateData");

            if (gameStateData == null)
            {
                throw new InvalidOperationException("Game state data is required.");
            }

            var gameStatePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "gamestate.json");
            Directory.CreateDirectory(Path.GetDirectoryName(gameStatePath));

            File.WriteAllText(gameStatePath, MiniJson.Serialize(gameStateData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Game state updated successfully."));
        }

        #endregion

        #region Player Data

        private object GetPlayerData()
        {
            var playerDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "player.json");

            if (!File.Exists(playerDataPath))
            {
                return CreateSuccessResponse(("playerData", new Dictionary<string, object>()), ("message", "No player data found."));
            }

            var content = File.ReadAllText(playerDataPath);
            var playerData = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("playerData", playerData));
        }

        private object UpdatePlayerData(Dictionary<string, object> payload)
        {
            var playerData = GetPayloadValue<Dictionary<string, object>>(payload, "playerData");

            if (playerData == null)
            {
                throw new InvalidOperationException("Player data is required.");
            }

            var playerDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "player.json");
            Directory.CreateDirectory(Path.GetDirectoryName(playerDataPath));

            File.WriteAllText(playerDataPath, MiniJson.Serialize(playerData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Player data updated successfully."));
        }

        #endregion

        #region Party Data

        private object GetPartyData()
        {
            var partyDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "party.json");

            if (!File.Exists(partyDataPath))
            {
                return CreateSuccessResponse(("partyData", new Dictionary<string, object>()), ("message", "No party data found."));
            }

            var content = File.ReadAllText(partyDataPath);
            var partyData = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("partyData", partyData));
        }

        private object UpdatePartyData(Dictionary<string, object> payload)
        {
            var partyData = GetPayloadValue<Dictionary<string, object>>(payload, "partyData");

            if (partyData == null)
            {
                throw new InvalidOperationException("Party data is required.");
            }

            var partyDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "party.json");
            Directory.CreateDirectory(Path.GetDirectoryName(partyDataPath));

            File.WriteAllText(partyDataPath, MiniJson.Serialize(partyData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Party data updated successfully."));
        }

        #endregion

        #region Inventory

        private object GetInventory()
        {
            var inventoryPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "inventory.json");

            if (!File.Exists(inventoryPath))
            {
                return CreateSuccessResponse(("inventory", new Dictionary<string, object>()), ("message", "No inventory data found."));
            }

            var content = File.ReadAllText(inventoryPath);
            var inventory = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("inventory", inventory));
        }

        private object UpdateInventory(Dictionary<string, object> payload)
        {
            var inventoryData = GetPayloadValue<Dictionary<string, object>>(payload, "inventoryData");

            if (inventoryData == null)
            {
                throw new InvalidOperationException("Inventory data is required.");
            }

            var inventoryPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "inventory.json");
            Directory.CreateDirectory(Path.GetDirectoryName(inventoryPath));

            File.WriteAllText(inventoryPath, MiniJson.Serialize(inventoryData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Inventory updated successfully."));
        }

        private object AddItemToInventory(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            var quantity = GetInt(payload, "quantity", 1);

            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("Item ID is required.");
            }

            var inventoryPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "inventory.json");
            Dictionary<string, object> inventory;

            if (File.Exists(inventoryPath))
            {
                var content = File.ReadAllText(inventoryPath);
                inventory = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                inventory = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(inventoryPath));
            }

            if (inventory.ContainsKey(itemId))
            {
                var currentQuantity = Convert.ToInt32(inventory[itemId]);
                inventory[itemId] = currentQuantity + quantity;
            }
            else
            {
                inventory[itemId] = quantity;
            }

            File.WriteAllText(inventoryPath, MiniJson.Serialize(inventory));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Added {quantity} of item '{itemId}' to inventory."));
        }

        private object RemoveItemFromInventory(Dictionary<string, object> payload)
        {
            var itemId = GetString(payload, "itemId");
            var quantity = GetInt(payload, "quantity", 1);

            if (string.IsNullOrEmpty(itemId))
            {
                throw new InvalidOperationException("Item ID is required.");
            }

            var inventoryPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "inventory.json");

            if (!File.Exists(inventoryPath))
            {
                throw new InvalidOperationException("Inventory not found.");
            }

            var content = File.ReadAllText(inventoryPath);
            var inventory = MiniJson.Deserialize(content) as Dictionary<string, object>;

            if (inventory == null || !inventory.ContainsKey(itemId))
            {
                throw new InvalidOperationException($"Item '{itemId}' not found in inventory.");
            }

            var currentQuantity = Convert.ToInt32(inventory[itemId]);
            var newQuantity = Math.Max(0, currentQuantity - quantity);

            if (newQuantity == 0)
            {
                inventory.Remove(itemId);
            }
            else
            {
                inventory[itemId] = newQuantity;
            }

            File.WriteAllText(inventoryPath, MiniJson.Serialize(inventory));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Removed {quantity} of item '{itemId}' from inventory."));
        }

        #endregion

        #region Progress Flags

        private object GetProgressFlags()
        {
            var flagsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON");
            var progressFlags = new Dictionary<string, object>();

            if (Directory.Exists(flagsPath))
            {
                var flagFiles = Directory.GetFiles(flagsPath, "*.json");
                foreach (var file in flagFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var flagData = MiniJson.Deserialize(content);
                        var filename = Path.GetFileNameWithoutExtension(file);
                        progressFlags[filename] = flagData;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to parse flag file {file}: {ex.Message}");
                    }
                }
            }

            return CreateSuccessResponse(("progressFlags", progressFlags));
        }

        private object SetProgressFlag(Dictionary<string, object> payload)
        {
            var flagType = GetString(payload, "flagType");
            var flagId = GetString(payload, "flagId");
            object value = null;
            if (payload.TryGetValue("value", out var v))
            {
                value = v;
            }

            if (string.IsNullOrEmpty(flagType) || (flagType != "switches" && flagType != "variables"))
            {
                throw new InvalidOperationException("Flag type must be 'switches' or 'variables'.");
            }

            if (string.IsNullOrEmpty(flagId))
            {
                throw new InvalidOperationException("Flag ID is required.");
            }

            var flagPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Flags", "JSON", $"{flagType}.json");
            Dictionary<string, object> flags;

            if (File.Exists(flagPath))
            {
                var content = File.ReadAllText(flagPath);
                flags = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                flags = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(flagPath));
            }

            flags[flagId] = value;
            File.WriteAllText(flagPath, MiniJson.Serialize(flags));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Progress flag '{flagId}' set successfully."));
        }

        #endregion

        #region Current Map

        private object GetCurrentMap()
        {
            var currentMapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "currentmap.json");

            if (!File.Exists(currentMapPath))
            {
                return CreateSuccessResponse(("currentMap", new Dictionary<string, object>()), ("message", "No current map data found."));
            }

            var content = File.ReadAllText(currentMapPath);
            var currentMap = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("currentMap", currentMap));
        }

        private object SetCurrentMap(Dictionary<string, object> payload)
        {
            var mapId = GetString(payload, "mapId");

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            var currentMapData = new Dictionary<string, object>
            {
                ["mapId"] = mapId,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            var currentMapPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "currentmap.json");
            Directory.CreateDirectory(Path.GetDirectoryName(currentMapPath));

            File.WriteAllText(currentMapPath, MiniJson.Serialize(currentMapData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Current map set to '{mapId}'."));
        }

        #endregion

        #region Teleport

        private object TeleportPlayer(Dictionary<string, object> payload)
        {
            var mapId = GetString(payload, "mapId");
            var x = GetInt(payload, "x", -1);
            var y = GetInt(payload, "y", -1);
            var direction = GetInt(payload, "direction", 2);

            if (string.IsNullOrEmpty(mapId))
            {
                throw new InvalidOperationException("Map ID is required.");
            }

            if (x < 0 || y < 0)
            {
                throw new InvalidOperationException("X and Y coordinates are required.");
            }

            var teleportData = new Dictionary<string, object>
            {
                ["mapId"] = mapId,
                ["x"] = x,
                ["y"] = y,
                ["direction"] = direction,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            var playerDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "player.json");
            Dictionary<string, object> playerData;

            if (File.Exists(playerDataPath))
            {
                var content = File.ReadAllText(playerDataPath);
                playerData = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                playerData = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(playerDataPath));
            }

            playerData["position"] = teleportData;
            File.WriteAllText(playerDataPath, MiniJson.Serialize(playerData));

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Player teleported to map '{mapId}' at position ({x}, {y})."));
        }

        #endregion

        #region Reset

        private object ResetGameState()
        {
            var saveDataPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData");

            if (Directory.Exists(saveDataPath))
            {
                var backupPath = Path.Combine(Application.dataPath, "..", $"RPGMaker_GameState_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(backupPath);

                foreach (var file in Directory.GetFiles(saveDataPath))
                {
                    var backupFile = Path.Combine(backupPath, Path.GetFileName(file));
                    File.Copy(file, backupFile, true);
                }

                Directory.Delete(saveDataPath, true);
            }

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Game state reset successfully. Backup created."));
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
