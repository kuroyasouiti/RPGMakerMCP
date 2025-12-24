# RPGMaker Unite MCP Documentation Index

**RPGMaker Unite MCP のドキュメント索引**

---

## Quick Navigation

### 初めての方

| ドキュメント | 目的 |
|:---|:---|
| [README](README.md) | プロジェクト概要 |
| [Getting Started](GETTING_STARTED.md) | セットアップガイド |
| [MCP Server](MCPServer/README.md) | サーバー詳細 |

---

## Documentation Structure

```
Documentation/
├── README.md ─────────────── プロジェクト概要（英語）
├── README_ja.md ──────────── プロジェクト概要（日本語）
├── GETTING_STARTED.md ────── セットアップガイド
├── INDEX.md ──────────────── このファイル
├── CLAUDE.md ─────────────── Claude AI連携ガイド
├── CHANGELOG.md ──────────── 変更履歴
├── CONTRIBUTING.md ───────── 貢献ガイド
└── MCPServer/
    └── README.md ─────────── MCPサーバー詳細
```

---

## Available Tools (10 tools)

### Utility Tools

| Tool | Description |
|------|-------------|
| `unity_ping` | 接続確認 |
| `unity_compilation_await` | コンパイル完了待機 |

### RPGMaker Tools (8 tools)

| Tool | Description |
|------|-------------|
| `rpgmaker_database` | キャラクター、アイテム、アニメーションの管理 |
| `rpgmaker_map` | マップとマップイベントの管理 |
| `rpgmaker_event` | コモンイベントとイベントコマンドの管理 |
| `rpgmaker_battle` | 敵、トループ、スキル、バトル設定の管理 |
| `rpgmaker_system` | 変数、スイッチ、セーブデータの管理 |
| `rpgmaker_assets` | 画像・音声アセットの管理 |
| `rpgmaker_gamestate` | プレイヤー、パーティ、インベントリの管理 |
| `rpgmaker_audio` | BGM/BGS/ME/SE再生とオーディオ設定 |

---

## RPGMaker Tools Reference

### rpgmaker_database
キャラクター、アイテム、アニメーションの管理

**Operations:**
- `getDatabaseInfo` - データベース情報取得
- **Character**: `listCharacters` / `getCharacterById` / `getCharacters` / `createCharacter` / `updateCharacter` / `deleteCharacter`
- **Item**: `listItems` / `getItemById` / `getItems` / `createItem` / `updateItem` / `deleteItem`
- **Animation**: `listAnimations` / `getAnimationById` / `getAnimations` / `createAnimation` / `updateAnimation` / `deleteAnimation`
- **System**: `getSystemSettings` / `updateSystemSettings`
- **Utility**: `exportDatabase` / `importDatabase` / `backupDatabase` / `restoreDatabase`

> **Recommended**: `list*` (軽量UUIDリスト) + `get*ById` (UUIDで詳細取得) を使用。`get*` (全件取得) は大規模データでは非推奨。

### rpgmaker_map
マップとマップイベントの管理

**Operations:**
- **Map**: `listMaps` / `getMapById` / `getMaps` / `createMap` / `updateMap` / `deleteMap` / `getMapData` / `setMapData`
- **Event**: `listMapEvents` / `getMapEventById` / `getMapEvents` / `createMapEvent` / `updateMapEvent` / `deleteMapEvent`
- **Tileset**: `listTilesets` / `getTilesetById` / `getTilesets` / `setTileset`
- **Settings**: `getMapSettings` / `updateMapSettings`
- **Utility**: `copyMap` / `exportMap` / `importMap`

### rpgmaker_event
コモンイベントとイベントコマンドの管理

**Operations:**
- **Common Event**: `listCommonEvents` / `getCommonEventById` / `getCommonEvents` / `createCommonEvent` / `updateCommonEvent` / `deleteCommonEvent`
- **Commands**: `getEventCommands` / `createEventCommand` / `updateEventCommand` / `deleteEventCommand`
- **Pages**: `getEventPages` / `createEventPage` / `updateEventPage` / `deleteEventPage`
- **Utility**: `copyEvent` / `moveEvent` / `validateEvent`

### rpgmaker_battle
バトルシステムの管理

**Operations:**
- **Settings**: `getBattleSettings` / `updateBattleSettings`
- **Enemy**: `listEnemies` / `getEnemyById` / `getEnemies` / `createEnemy` / `updateEnemy` / `deleteEnemy`
- **Troop**: `listTroops` / `getTroopById` / `getTroops` / `createTroop` / `updateTroop` / `deleteTroop`
- **Skill**: `listSkills` / `getSkillById` / `getSkills` / `createSkill` / `updateSkill` / `deleteSkill`
- **Animation**: `getBattleAnimations` / `updateBattleAnimation`

### rpgmaker_system
システム設定の管理

**Operations:**
- `getSystemInfo`
- `getGameVariables` / `setGameVariable`
- `getSwitches` / `setSwitch`
- `getSystemSettings` / `updateSystemSettings`
- `getSaveData` / `createSaveData` / `loadSaveData` / `deleteSaveData`

### rpgmaker_assets
画像・音声アセットの管理

**Operations:**
- **Image**: `listImages` / `getImageById` / `getImages` / `importImage` / `exportImage` / `deleteImage`
- **Sound**: `listSounds` / `getSoundById` / `getSounds` / `importSound` / `exportSound` / `deleteSound`
- **Management**: `getAssetInfo` / `organizeAssets` / `validateAssets` / `backupAssets` / `restoreAssets`

### rpgmaker_gamestate
ゲーム状態の管理

**Operations:**
- `getGameState` / `setGameState`
- `getPlayerData` / `updatePlayerData`
- `getPartyData` / `updatePartyData`
- `getInventory` / `updateInventory` / `addItemToInventory` / `removeItemFromInventory`
- `getProgressFlags` / `setProgressFlag`
- `getCurrentMap` / `setCurrentMap` / `teleportPlayer`
- `resetGameState`

### rpgmaker_audio
オーディオシステムの管理

**Operations:**
- **List**: `listAudioFiles` / `getAudioFileById` / `getAudioList`
- **Playback**: `playBgm` / `stopBgm` / `playBgs` / `stopBgs` / `playMe` / `playSe` / `stopAllAudio`
- **Volume**: `setBgmVolume` / `setBgsVolume` / `setMeVolume` / `setSeVolume`
- **Settings**: `getAudioSettings` / `updateAudioSettings`
- **Files**: `importAudioFile` / `exportAudioFile` / `deleteAudioFile` / `getAudioInfo`

---

## Pagination Support

リスト操作では `offset` と `limit` パラメータを使用してページネーションが可能です：

```python
# 最初の10件を取得
rpgmaker_database(operation='listCharacters', offset=0, limit=10)

# 次の10件を取得
rpgmaker_database(operation='listCharacters', offset=10, limit=10)
```

**Parameters:**
- `offset`: スキップするアイテム数（デフォルト: 0）
- `limit`: 取得するアイテム数（デフォルト: 100、-1で無制限）

---

## Data Storage Locations

| Category | Path |
|----------|------|
| Characters | `Assets/RPGMaker/Storage/Character/JSON/` |
| Items | `Assets/RPGMaker/Storage/Item/JSON/` |
| Animations | `Assets/RPGMaker/Storage/Animation/JSON/` |
| System | `Assets/RPGMaker/Storage/System/` |
| Maps | `Assets/RPGMaker/Storage/Map/` |
| Events | `Assets/RPGMaker/Storage/Event/` |
| Sounds | `Assets/RPGMaker/Storage/Sounds/` |

---

## Support

- **Issues**: GitHub Issues
- **Contributing**: [CONTRIBUTING.md](CONTRIBUTING.md)

---

**RPGMaker Unite MCP v1.0.0**
