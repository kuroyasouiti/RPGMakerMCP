# RPGMaker Unite MCP

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)](https://unity.com/)
[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

AI-powered RPGMaker Unite development toolkit using Model Context Protocol (MCP).

AIアシスタント（Claude、Cursor等）からRPGMaker Uniteのゲームデータを直接操作できるツールキットです。

## Features

- **8 RPGMaker Tools** - Database, Map, Event, Battle, System, Assets, GameState, Audio
- **Real-time Integration** - Bidirectional WebSocket communication with Unity Editor
- **AI-Driven Development** - Manage game data using natural language

## Installation

### Unity Package Manager (Recommended)

1. Open Unity Editor
2. Go to **Window > Package Manager**
3. Click **+** button and select **Add package from git URL...**
4. Enter the following URL:

```
https://github.com/your-username/RPGMakerMCP.git?path=Assets/RPGMakerMCP
```

> Replace `your-username` with the actual repository owner.

### Manual Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/your-username/RPGMakerMCP.git
   ```

2. Copy `Assets/RPGMakerMCP` folder to your Unity project's `Assets` folder.

## Quick Start

### 1. Start Unity Bridge

1. Open Unity Editor with your RPGMaker Unite project
2. Go to **Tools > MCP Assistant**
3. Click **Start Bridge**
4. Verify status shows "Connected"

### 2. Start MCP Server

```bash
cd Assets/RPGMakerMCP/MCPServer

# Install dependencies
uv sync

# Start server
python start_server.py
```

### 3. Test Connection

Ask your AI assistant:
```
Test the RPGMaker Unite MCP connection
```

## Available Tools

| Tool | Description |
|------|-------------|
| `rpgmaker_database` | Characters, items, animations |
| `rpgmaker_map` | Maps and map events |
| `rpgmaker_event` | Common events and commands |
| `rpgmaker_battle` | Enemies, troops, skills |
| `rpgmaker_system` | Variables, switches, save data |
| `rpgmaker_assets` | Images and sounds |
| `rpgmaker_gamestate` | Player, party, inventory |
| `rpgmaker_audio` | BGM/BGS/ME/SE playback |

## Usage Examples

### Get Characters

```python
rpgmaker_database(operation='listCharacters')
```

### Create Character

```python
rpgmaker_database(
    operation='createCharacter',
    filename='hero_001',
    characterData={
        'name': 'Hero',
        'class': 'Warrior',
        'level': 1
    }
)
```

### Play BGM

```python
rpgmaker_audio(operation='playBgm', filename='battle_theme', volume=0.8)
```

## Requirements

- **Unity**: 2022.3 LTS or later (Unity 6 recommended)
- **Python**: 3.10+
- **uv**: Python package manager (recommended)
- **RPGMaker Unite**: Installed in Unity project

### Installing uv

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

## Documentation

- [Getting Started](Assets/RPGMakerMCP/Documentation/GETTING_STARTED.md)
- [Documentation Index](Assets/RPGMakerMCP/Documentation/INDEX.md)
- [MCP Server Details](Assets/RPGMakerMCP/Documentation/MCPServer/README.md)
- [Contributing](Assets/RPGMakerMCP/Documentation/CONTRIBUTING.md)
- [Changelog](Assets/RPGMakerMCP/Documentation/CHANGELOG.md)

## Network Configuration

| Service | Port | URI |
|---------|------|-----|
| Unity Bridge | 7070 | `ws://localhost:7070/bridge` |
| MCP Server | stdio | (MCP default) |

## License

MIT License

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](Assets/RPGMakerMCP/Documentation/CONTRIBUTING.md) for guidelines.

---

**Power up your RPGMaker Unite development with AI!**
