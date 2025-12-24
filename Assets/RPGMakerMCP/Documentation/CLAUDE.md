# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**RPGMaker Unite MCP** is a Model Context Protocol (MCP) server that enables AI assistants to interact with RPGMaker Unite game data in real-time. It provides 10 tools (8 RPGMaker-specific + 2 utility) for managing characters, maps, events, battles, and more.

**Unity Version:** 2022.3 LTS or later (Unity 6 recommended)
**Python Version:** 3.10+

### Architecture

```
AI Client (Claude/Cursor) <--(MCP)--> Python MCP Server <--(WebSocket)--> Unity Bridge
                                      (MCPServer/src/)      (Editor/MCPBridge/)
```

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **McpBridgeService.cs** | `Editor/MCPBridge/` | WebSocket server in Unity Editor |
| **RPGMaker*Handler.cs** | `Editor/MCPBridge/Handlers/RPGMaker/` | RPGMaker data operations |
| **bridge_manager.py** | `MCPServer/src/bridge/` | Python WebSocket client |
| **rpgmaker_tools.py** | `MCPServer/src/tools/` | RPGMaker tool schemas |

---

## Available Tools (10 tools)

### Utility Tools

| Tool | Description |
|------|-------------|
| `unity_ping` | Test connection to Unity bridge |
| `unity_compilation_await` | Wait for Unity compilation to complete |

### RPGMaker Tools (8 tools)

| Tool | Description |
|------|-------------|
| `rpgmaker_database` | Characters, items, animations management |
| `rpgmaker_map` | Maps and map events management |
| `rpgmaker_event` | Common events and commands management |
| `rpgmaker_battle` | Enemies, troops, skills, battle settings |
| `rpgmaker_system` | Variables, switches, save data |
| `rpgmaker_assets` | Images and sounds management |
| `rpgmaker_gamestate` | Player, party, inventory, progress |
| `rpgmaker_audio` | BGM/BGS/ME/SE playback and settings |

---

## Quick Command Reference

### Database Operations

```python
# List characters (lightweight UUID list)
rpgmaker_database(operation='listCharacters', offset=0, limit=10)

# Get character by UUID (full data)
rpgmaker_database(operation='getCharacterById', uuId='abc-123-def')

# Create new character
rpgmaker_database(
    operation='createCharacter',
    filename='hero_001',
    characterData={
        'name': 'Hero',
        'class': 'Warrior',
        'level': 1,
        'stats': {'hp': 100, 'mp': 50, 'attack': 15, 'defense': 10}
    }
)

# Update character
rpgmaker_database(
    operation='updateCharacter',
    uuId='abc-123-def',
    characterData={'level': 5}
)

# Delete character
rpgmaker_database(operation='deleteCharacter', uuId='abc-123-def')
```

### Map Operations

```python
# List maps
rpgmaker_map(operation='listMaps')

# Get map by UUID
rpgmaker_map(operation='getMapById', uuId='map-uuid')

# Create map event
rpgmaker_map(
    operation='createMapEvent',
    uuId='map-uuid',
    eventData={
        'name': 'Treasure Chest',
        'x': 10,
        'y': 15,
        'pages': [
            {
                'trigger': 'action',
                'commands': [
                    {'type': 'showText', 'text': 'You found a treasure!'},
                    {'type': 'addItem', 'itemId': 'gold', 'quantity': 100}
                ]
            }
        ]
    }
)
```

### Audio Operations

```python
# Play BGM
rpgmaker_audio(operation='playBgm', filename='battle_theme', volume=0.8)

# Stop BGM
rpgmaker_audio(operation='stopBgm')

# Play sound effect
rpgmaker_audio(operation='playSe', filename='cursor', volume=1.0)
```

### System Operations

```python
# Get game variables
rpgmaker_system(operation='getGameVariables')

# Set game variable
rpgmaker_system(operation='setGameVariable', variableId='1', value=100)

# Get switches
rpgmaker_system(operation='getSwitches')

# Set switch
rpgmaker_system(operation='setSwitch', switchId='1', value=True)
```

### Game State Operations

```python
# Get player data
rpgmaker_gamestate(operation='getPlayerData')

# Add item to inventory
rpgmaker_gamestate(
    operation='addItemToInventory',
    itemId='potion_001',
    quantity=5
)

# Teleport player
rpgmaker_gamestate(
    operation='teleportPlayer',
    mapId='map_002',
    x=10,
    y=20,
    direction=2  # 2=down, 4=left, 6=right, 8=up
)
```

---

## Best Practices

### 1. Use list* + get*ById Pattern

For large datasets, use the lightweight list operation followed by specific getById calls:

```python
# Good: Get lightweight list first
characters = rpgmaker_database(operation='listCharacters', limit=100)

# Then get specific character details
for char in characters['items']:
    detail = rpgmaker_database(operation='getCharacterById', uuId=char['uuId'])
```

### 2. Use Pagination for Large Lists

```python
# Get first page
page1 = rpgmaker_database(operation='listCharacters', offset=0, limit=50)

# Get second page
page2 = rpgmaker_database(operation='listCharacters', offset=50, limit=50)
```

### 3. Check Connection First

```python
# Verify bridge connection
result = unity_ping()
if result.get('connected'):
    # Proceed with operations
    pass
```

### 4. NEVER Edit .meta Files

Unity manages `.meta` files automatically. Manual editing can break asset references.

---

## Data Storage Locations

| Data Type | Path |
|-----------|------|
| Characters | `Assets/RPGMaker/Storage/Character/JSON/` |
| Items | `Assets/RPGMaker/Storage/Item/JSON/` |
| Animations | `Assets/RPGMaker/Storage/Animation/JSON/` |
| System | `Assets/RPGMaker/Storage/System/` |
| Maps | `Assets/RPGMaker/Storage/Map/` |
| Events | `Assets/RPGMaker/Storage/Event/` |
| Sounds | `Assets/RPGMaker/Storage/Sounds/` |

---

## Network Configuration

| Service | Port | URI |
|---------|------|-----|
| Unity Bridge | 7070 | `ws://localhost:7070/bridge` |
| MCP Server | stdio | (MCP default) |

---

## Development Commands

### Running the MCP Server

```bash
cd Assets/RPGMakerMCP/MCPServer

# Using uv (recommended)
python start_server.py

# Or run directly
uv run python src/server.py
```

### Testing in Unity

1. Open Unity Editor
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Connection status appears in the window

---

## Troubleshooting

### "Unity bridge is not connected"

1. Check Unity Editor is running
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Verify port 7070 is available

### "Data not found"

1. Verify data exists in `Assets/RPGMaker/Storage/`
2. Check file paths are correct
3. Ensure RPGMaker Unite has created the data

### Timeout Errors

1. Check Unity Console for errors
2. For large datasets, use pagination
3. Default timeout is 45 seconds

---

## File Structure

```
Assets/RPGMakerMCP/
├── Editor/
│   └── MCPBridge/
│       ├── McpBridgeService.cs      # WebSocket server
│       ├── McpCommandProcessor.cs   # Command dispatcher
│       └── Handlers/
│           └── RPGMaker/            # RPGMaker handlers
│               ├── RPGMakerDatabaseHandler.cs
│               ├── RPGMakerMapHandler.cs
│               ├── RPGMakerEventHandler.cs
│               ├── RPGMakerBattleHandler.cs
│               ├── RPGMakerSystemHandler.cs
│               ├── RPGMakerAssetsHandler.cs
│               ├── RPGMakerGameStateHandler.cs
│               └── RPGMakerAudioHandler.cs
├── MCPServer/
│   ├── src/
│   │   ├── server.py               # Entry point
│   │   ├── version.py              # Version info
│   │   ├── bridge/                 # WebSocket client
│   │   ├── tools/                  # Tool definitions
│   │   │   ├── register_tools.py
│   │   │   └── rpgmaker_tools.py
│   │   └── config/                 # Configuration
│   ├── start_server.py             # Launch script
│   └── pyproject.toml              # Dependencies
└── Documentation/
    ├── README.md
    ├── README_ja.md
    ├── GETTING_STARTED.md
    ├── INDEX.md
    ├── CLAUDE.md                   # This file
    ├── CHANGELOG.md
    ├── CONTRIBUTING.md
    └── MCPServer/
        └── README.md
```

---

## Version

**RPGMaker Unite MCP v1.0.0**
