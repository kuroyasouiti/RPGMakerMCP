# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RPGMaker Unite is a Unity-based RPG game development framework with an integrated Model Context Protocol (MCP) server for AI-assisted development. The project bridges traditional game design tools with AI capabilities through a Python MCP server and C# Unity bridge.

**Unity Version:** 6000.2.6f2 (Unity 2022.3 LTS compatible)
**Python Version:** 3.10+ required for MCP server

## Architecture

### Three-Layer System

1. **Python MCP Server** (`Assets/RPGMakerMCP/MCPServer/`)
   - MCP-based server exposing RPGMaker tools to AI assistants
   - Communicates with Unity via WebSocket on `ws://localhost:6400/rpgmaker`
   - 8 tool categories: Database, Map, Event, Battle, System, Assets, GameState, Audio

2. **C# Unity Bridge** (`Assets/RPGMakerMCP/Editor/MCPBridge/`)
   - Editor-only scripts that execute commands from MCP server
   - Tool handlers in `Handlers/RPGMaker/`: RPGMakerDatabaseHandler, RPGMakerMapHandler, etc.
   - Entry point: `McpBridgeService.cs` (WebSocket server on 6400)
   - Commands executed on Unity main thread for thread safety

3. **RPGMaker Core** (`Assets/RPGMaker/Codebase/`)
   - `CoreSystem/` - Shared helpers, models, services
   - `Editor/` - Unity Editor tools and inspectors
   - `Runtime/` - Game systems (Battle, Event, Map, GameState, Title, GameOver)
   - `Add-ons/` - Extension modules

### Legacy Code

`Assets/RPGMakerMcp_Legacy/` - Previous MCP implementation (deprecated, kept for reference)

### Data Storage (`Assets/RPGMaker/Storage/`)

All game data stored as JSON with ScriptableObject assets:
- `Animation/` - Sprite and Effekseer animations
- `Character/` - Playable characters and NPCs
- `Item/` - Items, weapons, armor
- `Map/` - World maps with tilesets
- `Event/` - Map and common events
- `Flags/` - Variables and switches
- `Sounds/` - BGM, BGS, ME, SE organized by type
- `System/` - Core game configuration

## Development Commands

### MCP Server (Python with uv)

```bash
cd Assets/RPGMakerMCP/MCPServer

# Start server with uv (recommended)
python start_server.py

# Or run directly with uv
uv run python src/server.py

# Sync dependencies
uv sync

# Development setup with dev tools
uv sync --extra dev

# Code quality
uv run black src/            # Format
uv run ruff check src/       # Lint
uv run mypy src/             # Type check
```

**Installing uv:**
```bash
# Windows
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### Unity

Open project in Unity Editor. The MCP Bridge activates via `Tools > MCP Assistant` menu.

### Testing

```bash
# Python tests
cd Assets/RPGMakerMCP/MCPServer
pytest

# Unity tests run via Unity Test Runner (Window > General > Test Runner)
```

## Network Configuration

| Service | Protocol | URI |
|---------|----------|-----|
| Unity Bridge | WebSocket | `ws://localhost:6400/rpgmaker` |
| MCP Server | stdio | (MCP default) |

**WebSocket Settings:**
- Ping interval: 20 seconds
- Ping timeout: 10 seconds
- Max message size: 32MB
- Command timeout: 60 seconds

Config in `Assets/RPGMakerMCP/MCPServer/src/config.py`.

## MCP Tools

### RPGMaker-specific Tools (8 tools)

| Tool Name | Description |
|-----------|-------------|
| `rpgmaker_database` | Manage characters, items, animations, system settings |
| `rpgmaker_map` | Manage maps, map events, tilesets |
| `rpgmaker_event` | Manage common events, event commands, pages |
| `rpgmaker_battle` | Manage enemies, troops, skills, battle settings |
| `rpgmaker_system` | Manage variables, switches, save data |
| `rpgmaker_assets` | Manage images and sounds |
| `rpgmaker_gamestate` | Manage player, party, inventory, progress flags |
| `rpgmaker_audio` | Manage BGM/BGS/ME/SE playback and settings |

### Unity General Tools

Additional tools for Unity scene, GameObject, component, asset, prefab management are also available.

## Key Dependencies

**Unity Packages:**
- Universal Render Pipeline (17.2.0)
- Input System (1.14.2)
- Addressables (2.7.6)
- 2D Tilemap Extras (5.0.2)
- Newtonsoft JSON (3.2.2)

**Python:**
- mcp (>=0.9.0)
- websockets (>=12.0)
- uvicorn (>=0.27.0)
- starlette (>=0.36.0)

## Important Conventions

- Never edit `.meta` files - Unity manages these automatically
- Game data modifications should go through MCP tools or RPGMaker Editor
- Python MCP tools follow pattern: `rpgmaker_[category](operation="...", **params)`
- All MCP responses return `{success: bool, result/error: ...}`
