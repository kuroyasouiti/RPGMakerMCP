"""
RPGMaker Unite MCP Tools
Provides tools for managing RPGMaker Unite game data through the MCP protocol.
"""

from __future__ import annotations

from typing import Any

import mcp.types as types


def _schema_with_required(schema: dict[str, Any], required: list[str]) -> dict[str, Any]:
    enriched = dict(schema)
    enriched["required"] = required
    enriched["additionalProperties"] = False
    return enriched


# Common pagination parameters for list operations
PAGINATION_PROPERTIES = {
    "offset": {
        "type": "integer",
        "minimum": 0,
        "default": 0,
        "description": "Number of items to skip from the beginning of the list (0-based). Default: 0.",
    },
    "limit": {
        "type": "integer",
        "minimum": 1,
        "default": 100,
        "description": "Maximum number of items to return. Default: 100. Use -1 for no limit.",
    },
}


# ============================================================
# RPGMaker Database Tool Schema
# ============================================================
rpgmaker_database_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    "getDatabaseInfo",
                    # Character operations
                    "listCharacters",
                    "getCharacterById",
                    "getCharacters",
                    "createCharacter",
                    "updateCharacter",
                    "deleteCharacter",
                    # Item operations
                    "listItems",
                    "getItemById",
                    "getItems",
                    "createItem",
                    "updateItem",
                    "deleteItem",
                    # Animation operations
                    "listAnimations",
                    "getAnimationById",
                    "getAnimations",
                    "createAnimation",
                    "updateAnimation",
                    "deleteAnimation",
                    # System operations
                    "getSystemSettings",
                    "updateSystemSettings",
                    "exportDatabase",
                    "importDatabase",
                    "backupDatabase",
                    "restoreDatabase",
                ],
                "description": (
                    "Database operation. "
                    "Recommended: 'list*' (lightweight UUID list) + 'get*ById' (full data by UUID). "
                    "Deprecated: 'get*' (all records) - use list + getById instead for large datasets."
                ),
            },
            "uuId": {
                "type": "string",
                "description": "UUID of the record. Required for getById/update/delete operations.",
            },
            "filename": {
                "type": "string",
                "description": (
                    "Target JSON filename (without extension) for create operations. "
                    "Optional - defaults to 'characterActor', 'item', or 'animation' based on operation."
                ),
            },
            "itemData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Item data for create/update. "
                    "If 'uuId' is omitted, it will be auto-generated on create."
                ),
            },
            "characterData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Character data for create/update. "
                    "If 'uuId' is omitted, it will be auto-generated on create."
                ),
            },
            "animationData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Animation data for create/update. "
                    "If 'id' is omitted, it will be auto-generated on create."
                ),
            },
            "settingData": {
                "type": "object",
                "additionalProperties": True,
                "description": "System setting data for updating.",
            },
            "exportPath": {
                "type": "string",
                "description": "Path for exporting database. Optional - defaults to project root.",
            },
            "importPath": {
                "type": "string",
                "description": "Path for importing database.",
            },
            "backupPath": {
                "type": "string",
                "description": "Path for backup/restore operations. Optional for backup.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker Map Tool Schema
# ============================================================
rpgmaker_map_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    # Map operations
                    "listMaps",
                    "getMapById",
                    "getMaps",
                    "createMap",
                    "updateMap",
                    "deleteMap",
                    "getMapData",
                    "setMapData",
                    # Event operations
                    "listMapEvents",
                    "getMapEventById",
                    "getMapEvents",
                    "createMapEvent",
                    "updateMapEvent",
                    "deleteMapEvent",
                    # Tileset operations
                    "listTilesets",
                    "getTilesetById",
                    "getTilesets",
                    "setTileset",
                    # Settings and utility
                    "getMapSettings",
                    "updateMapSettings",
                    "copyMap",
                    "exportMap",
                    "importMap",
                ],
                "description": (
                    "Map operation. "
                    "Recommended: 'list*' (lightweight UUID list) + 'get*ById' (full data by UUID). "
                    "Deprecated: 'get*' (all records) - use list + getById instead for large datasets."
                ),
            },
            "uuId": {
                "type": "string",
                "description": "UUID of the map/event record. Required for getById/update/delete operations.",
            },
            "filename": {
                "type": "string",
                "description": (
                    "Target JSON filename (without extension) for create operations. "
                    "Optional - defaults to 'mapInfo'."
                ),
            },
            "mapData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Map data for create/update. "
                    "If 'id' is omitted, it will be auto-generated on create."
                ),
            },
            "eventId": {
                "type": "string",
                "description": "Event UUID for map event operations.",
            },
            "eventData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Map event data for create/update. "
                    "If 'id' is omitted, it will be auto-generated on create."
                ),
            },
            "tilesetId": {
                "type": "string",
                "description": "Tileset UUID for tileset operations.",
            },
            "settings": {
                "type": "object",
                "additionalProperties": True,
                "description": "Map settings for updating.",
            },
            "sourceFilename": {
                "type": "string",
                "description": "Source filename for copy operations.",
            },
            "targetFilename": {
                "type": "string",
                "description": "Target filename for copy operations.",
            },
            "exportPath": {
                "type": "string",
                "description": "Export path for map export. Optional.",
            },
            "importFilePath": {
                "type": "string",
                "description": "Import file path for map import.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker Event Tool Schema
# ============================================================
rpgmaker_event_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    # Common event operations
                    "listCommonEvents",
                    "getCommonEventById",
                    "getCommonEvents",
                    "createCommonEvent",
                    "updateCommonEvent",
                    "deleteCommonEvent",
                    # Event command operations
                    "getEventCommands",
                    "createEventCommand",
                    "updateEventCommand",
                    "deleteEventCommand",
                    # Event page operations
                    "getEventPages",
                    "createEventPage",
                    "updateEventPage",
                    "deleteEventPage",
                    # Utility
                    "copyEvent",
                    "moveEvent",
                    "validateEvent",
                ],
                "description": (
                    "Event operation. "
                    "Recommended: 'list*' (lightweight UUID list) + 'get*ById' (full data by UUID). "
                    "Deprecated: 'get*' (all records) - use list + getById instead for large datasets."
                ),
            },
            "uuId": {
                "type": "string",
                "description": "UUID of the event record. Required for getById/update/delete/command/page operations.",
            },
            "filename": {
                "type": "string",
                "description": (
                    "Target JSON filename (without extension) for create operations. "
                    "Optional - defaults to 'commonEvent'."
                ),
            },
            "eventData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Event data for create/update. "
                    "If 'id' is omitted, it will be auto-generated on create."
                ),
            },
            "pageIndex": {
                "type": "integer",
                "description": "Page index for page operations (0-based).",
            },
            "pageData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Page data for creating/updating.",
            },
            "commandIndex": {
                "type": "integer",
                "description": "Command index for command operations (0-based).",
            },
            "commandData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Command data for creating/updating.",
            },
            "sourceFilename": {
                "type": "string",
                "description": "Source filename for copy/move operations.",
            },
            "targetFilename": {
                "type": "string",
                "description": "Target filename for copy/move operations.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker Battle Tool Schema
# ============================================================
rpgmaker_battle_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    "getBattleSettings",
                    "updateBattleSettings",
                    # Enemy operations
                    "listEnemies",
                    "getEnemyById",
                    "getEnemies",
                    "createEnemy",
                    "updateEnemy",
                    "deleteEnemy",
                    # Troop operations
                    "listTroops",
                    "getTroopById",
                    "getTroops",
                    "createTroop",
                    "updateTroop",
                    "deleteTroop",
                    # Skill operations
                    "listSkills",
                    "getSkillById",
                    "getSkills",
                    "createSkill",
                    "updateSkill",
                    "deleteSkill",
                    # Animation operations
                    "getBattleAnimations",
                    "updateBattleAnimation",
                ],
                "description": (
                    "Battle system operation. "
                    "Recommended: 'list*' (lightweight UUID list) + 'get*ById' (full data by UUID). "
                    "Deprecated: 'get*' (all records) - use list + getById instead for large datasets."
                ),
            },
            "uuId": {
                "type": "string",
                "description": "UUID of the battle record. Required for getById/update/delete operations.",
            },
            "filename": {
                "type": "string",
                "description": (
                    "Target JSON filename (without extension) for create operations. "
                    "Optional - defaults to 'enemy', 'troop', or 'skill' based on operation."
                ),
            },
            "settingData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Battle setting data for updating.",
            },
            "enemyData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Enemy data for create/update. "
                    "If 'uuId' is omitted, it will be auto-generated on create."
                ),
            },
            "troopData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Troop data for create/update. "
                    "If 'uuId' is omitted, it will be auto-generated on create."
                ),
            },
            "skillData": {
                "type": "object",
                "additionalProperties": True,
                "description": (
                    "Skill data for create/update. "
                    "If 'uuId' is omitted, it will be auto-generated on create."
                ),
            },
            "animationData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Animation data for updating.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker System Tool Schema
# ============================================================
rpgmaker_system_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
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
                    "deleteSaveData",
                ],
                "description": "System operation to perform.",
            },
            "variableId": {
                "type": "string",
                "description": "Variable ID for variable operations.",
            },
            "value": {
                "oneOf": [
                    {"type": "string"},
                    {"type": "number"},
                    {"type": "boolean"},
                    {"type": "integer"},
                ],
                "description": "Value to set for variable/switch operations.",
            },
            "switchId": {
                "type": "string",
                "description": "Switch ID for switch operations.",
            },
            "systemSettings": {
                "type": "object",
                "additionalProperties": True,
                "description": "System settings for updating.",
            },
            "slotId": {
                "type": "string",
                "description": "Save slot ID for save data operations.",
            },
            "saveData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Save data for creating.",
            },
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker Assets Tool Schema
# ============================================================
rpgmaker_assets_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    # Image operations
                    "listImages",
                    "getImageById",
                    "getImages",
                    "importImage",
                    "exportImage",
                    "deleteImage",
                    # Sound operations
                    "listSounds",
                    "getSoundById",
                    "getSounds",
                    "importSound",
                    "exportSound",
                    "deleteSound",
                    # Asset management
                    "getAssetInfo",
                    "organizeAssets",
                    "validateAssets",
                    "backupAssets",
                    "restoreAssets",
                ],
                "description": (
                    "Asset operation. "
                    "Recommended: 'list*' (lightweight file list) + 'get*ById' (full data by filename). "
                    "Deprecated: 'get*' (all records) - use list + getById instead for large datasets."
                ),
            },
            "category": {
                "type": "string",
                "description": (
                    "Asset category. "
                    "Images: 'characters', 'faces', 'pictures', 'tilesets', 'battlebacks', 'parallaxes'. "
                    "Sounds: 'bgm', 'bgs', 'me', 'se'."
                ),
            },
            "filename": {
                "type": "string",
                "description": "Asset filename (with or without extension).",
            },
            "sourcePath": {
                "type": "string",
                "description": "Source file path for import operations.",
            },
            "targetPath": {
                "type": "string",
                "description": "Target file path for export operations.",
            },
            "backupPath": {
                "type": "string",
                "description": "Path for backup/restore operations. Optional for backup.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker GameState Tool Schema
# ============================================================
rpgmaker_gamestate_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
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
                    "resetGameState",
                ],
                "description": "Game state operation to perform.",
            },
            "gameStateData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Game state data for setting.",
            },
            "playerData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Player data for updating.",
            },
            "partyData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Party data for updating.",
            },
            "inventoryData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Inventory data for updating.",
            },
            "itemId": {
                "type": "string",
                "description": "Item ID for inventory operations.",
            },
            "quantity": {
                "type": "integer",
                "description": "Quantity for inventory operations.",
            },
            "flagType": {
                "type": "string",
                "enum": ["switches", "variables"],
                "description": "Flag type for progress flag operations.",
            },
            "flagId": {
                "type": "string",
                "description": "Flag ID for progress flag operations.",
            },
            "value": {
                "oneOf": [
                    {"type": "string"},
                    {"type": "number"},
                    {"type": "boolean"},
                    {"type": "integer"},
                ],
                "description": "Value for flag operations.",
            },
            "mapId": {
                "type": "string",
                "description": "Map ID for map/teleport operations.",
            },
            "x": {
                "type": "integer",
                "description": "X coordinate for teleport.",
            },
            "y": {
                "type": "integer",
                "description": "Y coordinate for teleport.",
            },
            "direction": {
                "type": "integer",
                "description": "Direction for teleport (2=down, 4=left, 6=right, 8=up).",
            },
        },
    },
    ["operation"],
)


# ============================================================
# RPGMaker Audio Tool Schema
# ============================================================
rpgmaker_audio_schema = _schema_with_required(
    {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": [
                    # Audio list operations
                    "listAudioFiles",
                    "getAudioFileById",
                    "getAudioList",
                    # Playback operations
                    "playBgm",
                    "stopBgm",
                    "playBgs",
                    "stopBgs",
                    "playMe",
                    "playSe",
                    "stopAllAudio",
                    # Volume operations
                    "setBgmVolume",
                    "setBgsVolume",
                    "setMeVolume",
                    "setSeVolume",
                    # Settings and file management
                    "getAudioSettings",
                    "updateAudioSettings",
                    "importAudioFile",
                    "exportAudioFile",
                    "deleteAudioFile",
                    "getAudioInfo",
                ],
                "description": (
                    "Audio operation. "
                    "Recommended: 'listAudioFiles' (lightweight file list) + 'getAudioFileById' (full data by filename). "
                    "Deprecated: 'getAudioList' - use listAudioFiles + getAudioFileById instead."
                ),
            },
            "category": {
                "type": "string",
                "enum": ["bgm", "bgs", "me", "se"],
                "description": "Audio category: 'bgm' (background music), 'bgs' (background sound), 'me' (music effect), 'se' (sound effect).",
            },
            "filename": {
                "type": "string",
                "description": "Audio filename (with or without extension).",
            },
            "volume": {
                "type": "number",
                "minimum": 0,
                "maximum": 1,
                "default": 0.8,
                "description": "Volume level (0.0 to 1.0). Default: 0.8.",
            },
            "pitch": {
                "type": "number",
                "minimum": 0.5,
                "maximum": 2.0,
                "default": 1.0,
                "description": "Pitch level (0.5 to 2.0). Default: 1.0.",
            },
            "pan": {
                "type": "number",
                "minimum": -1,
                "maximum": 1,
                "default": 0,
                "description": "Pan level (-1.0=left to 1.0=right). Default: 0 (center).",
            },
            "settingsData": {
                "type": "object",
                "additionalProperties": True,
                "description": "Audio settings for updating.",
            },
            "sourcePath": {
                "type": "string",
                "description": "Source file path for import operations.",
            },
            "targetPath": {
                "type": "string",
                "description": "Target file path for export operations.",
            },
            **PAGINATION_PROPERTIES,
        },
    },
    ["operation"],
)


# ============================================================
# Tool Definitions
# ============================================================
RPGMAKER_TOOL_DEFINITIONS: list[types.Tool] = [
    types.Tool(
        name="rpgmaker_database",
        description=(
            "Manage RPGMaker Unite database items including characters, items, and animations. "
            "Supports CRUD operations for game database entries."
        ),
        inputSchema=rpgmaker_database_schema,
    ),
    types.Tool(
        name="rpgmaker_map",
        description=(
            "Manage RPGMaker Unite maps and map events. "
            "Create, edit, and delete maps, configure tilesets, and manage map events."
        ),
        inputSchema=rpgmaker_map_schema,
    ),
    types.Tool(
        name="rpgmaker_event",
        description=(
            "Manage RPGMaker Unite common events and event commands. "
            "Create and edit event pages, commands, and triggers."
        ),
        inputSchema=rpgmaker_event_schema,
    ),
    types.Tool(
        name="rpgmaker_battle",
        description=(
            "Manage RPGMaker Unite battle system. "
            "Configure enemies, troops, skills, and battle animations."
        ),
        inputSchema=rpgmaker_battle_schema,
    ),
    types.Tool(
        name="rpgmaker_system",
        description=(
            "Manage RPGMaker Unite system settings. "
            "Control game variables, switches, system settings, and save data."
        ),
        inputSchema=rpgmaker_system_schema,
    ),
    types.Tool(
        name="rpgmaker_assets",
        description=(
            "Manage RPGMaker Unite assets including images and sounds. "
            "Import, export, organize, and validate game assets."
        ),
        inputSchema=rpgmaker_assets_schema,
    ),
    types.Tool(
        name="rpgmaker_gamestate",
        description=(
            "Manage RPGMaker Unite game state. "
            "Control player data, party, inventory, progress flags, and map position."
        ),
        inputSchema=rpgmaker_gamestate_schema,
    ),
    types.Tool(
        name="rpgmaker_audio",
        description=(
            "Manage RPGMaker Unite audio system. "
            "Play/stop audio, control volume, and manage audio files (BGM, BGS, ME, SE)."
        ),
        inputSchema=rpgmaker_audio_schema,
    ),
]


# Tool name to Unity bridge tool name mapping
RPGMAKER_TOOL_MAP: dict[str, str] = {
    "rpgmaker_database": "rpgMakerDatabase",
    "rpgmaker_map": "rpgMakerMap",
    "rpgmaker_event": "rpgMakerEvent",
    "rpgmaker_battle": "rpgMakerBattle",
    "rpgmaker_system": "rpgMakerSystem",
    "rpgmaker_assets": "rpgMakerAssets",
    "rpgmaker_gamestate": "rpgMakerGameState",
    "rpgmaker_audio": "rpgMakerAudio",
}
