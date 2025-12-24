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

## RPGMaker Tools Reference

### rpgmaker_database
キャラクター、アイテム、アニメーションの管理

**Operations:**
- `getDatabaseInfo` - データベース情報取得
- `getCharacters` / `createCharacter` / `updateCharacter` / `deleteCharacter`
- `getItems` / `createItem` / `updateItem` / `deleteItem`
- `getAnimations` / `createAnimation` / `updateAnimation` / `deleteAnimation`
- `getSystemSettings` / `updateSystemSettings`
- `exportDatabase` / `importDatabase` / `backupDatabase` / `restoreDatabase`

### rpgmaker_map
マップとマップイベントの管理

**Operations:**
- `getMaps` / `createMap` / `updateMap` / `deleteMap`
- `getMapData` / `setMapData`
- `getMapEvents` / `createMapEvent` / `updateMapEvent` / `deleteMapEvent`
- `getTilesets` / `setTileset`
- `getMapSettings` / `updateMapSettings`
- `copyMap` / `exportMap` / `importMap`

### rpgmaker_event
コモンイベントとイベントコマンドの管理

**Operations:**
- `getCommonEvents` / `createCommonEvent` / `updateCommonEvent` / `deleteCommonEvent`
- `getEventCommands` / `createEventCommand` / `updateEventCommand` / `deleteEventCommand`
- `getEventPages` / `createEventPage` / `updateEventPage` / `deleteEventPage`
- `copyEvent` / `moveEvent` / `validateEvent`

### rpgmaker_battle
バトルシステムの管理

**Operations:**
- `getBattleSettings` / `updateBattleSettings`
- `getEnemies` / `createEnemy` / `updateEnemy` / `deleteEnemy`
- `getTroops` / `createTroop` / `updateTroop` / `deleteTroop`
- `getSkills` / `createSkill` / `updateSkill` / `deleteSkill`
- `getBattleAnimations` / `updateBattleAnimation`

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
- `getImages` / `importImage` / `exportImage` / `deleteImage`
- `getSounds` / `importSound` / `exportSound` / `deleteSound`
- `getAssetInfo` / `organizeAssets` / `validateAssets`
- `backupAssets` / `restoreAssets`

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
- `getAudioList`
- `playBgm` / `stopBgm` / `setBgmVolume`
- `playBgs` / `stopBgs` / `setBgsVolume`
- `playMe` / `setMeVolume`
- `playSe` / `setSeVolume`
- `stopAllAudio`
- `getAudioSettings` / `updateAudioSettings`
- `importAudioFile` / `exportAudioFile` / `deleteAudioFile` / `getAudioInfo`

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
