# RPGMaker Unite MCP Server

Model Context Protocol (MCP) server for RPGMaker Unite - AI-powered RPG development toolkit.

**Version: 1.1.0**

## About

This MCP server enables AI assistants to interact with RPGMaker Unite through the Model Context Protocol, providing 10 tools (8 RPGMaker-specific + 2 utility) for managing game data in real-time.

## Installation

This package is installed automatically via Unity Editor's MCP Server Manager.

For manual installation and detailed documentation, see:
- [Installation Guide](../Documentation/Installation/INSTALL_GUIDE.md)
- [Quick Start Guide](../Documentation/Installation/QUICKSTART.md)
- [Full Documentation](../Documentation/README.md)

## Features

### RPGMaker Tools (8 tools)

| Tool | Description |
|------|-------------|
| `rpgmaker_database` | Characters, items, animations, system settings |
| `rpgmaker_map` | Maps, map events, tilesets |
| `rpgmaker_event` | Common events, event commands, pages |
| `rpgmaker_battle` | Enemies, troops, skills, battle settings |
| `rpgmaker_system` | Variables, switches, save data |
| `rpgmaker_assets` | Images and sounds management |
| `rpgmaker_gamestate` | Player, party, inventory, progress flags |
| `rpgmaker_audio` | BGM/BGS/ME/SE playback and settings |

### Utility Tools

| Tool | Description |
|------|-------------|
| `unity_ping` | Test connection to Unity bridge |
| `unity_compilation_await` | Wait for Unity compilation to complete |

### Key Features

- **UUID-based Operations**: All CRUD operations use UUIDs for reliable data access
- **Pagination Support**: All list operations support `offset` and `limit` parameters
- **Path Validation**: Secure file operations with path traversal protection
- **Token Security**: Secure token handling with log masking
- **Improved Error Handling**: Specific exception types with helpful error messages

## Requirements

- Python 3.10 or higher
- Unity 2021.3 or higher
- MCP SDK 0.9.0 or higher

## Documentation

For complete documentation, visit the [Documentation](../Documentation) folder.

## License

MIT License - See [LICENSE](../../LICENSE) file for details.

## Repository

https://github.com/kuroyasouiti/RPGMakerUnite

