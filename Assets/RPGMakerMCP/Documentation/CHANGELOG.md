# 変更履歴

RPGMaker Unite MCPのすべての注目すべき変更はこのファイルに記録されます。

このフォーマットは[Keep a Changelog](https://keepachangelog.com/ja/1.0.0/)に基づいており、
このプロジェクトは[Semantic Versioning](https://semver.org/lang/ja/)に準拠しています。

## [未リリース]

（なし）

## [1.0.0] - 2024-12-24

### 追加

- **RPGMaker Unite MCP 初期リリース**
  - 8つのRPGMaker専用ツールを実装
  - 2つのユーティリティツール（ping、compilation_await）

- **rpgmaker_database ツール**
  - キャラクター管理: `listCharacters`, `getCharacterById`, `getCharacters`, `createCharacter`, `updateCharacter`, `deleteCharacter`
  - アイテム管理: `listItems`, `getItemById`, `getItems`, `createItem`, `updateItem`, `deleteItem`
  - アニメーション管理: `listAnimations`, `getAnimationById`, `getAnimations`, `createAnimation`, `updateAnimation`, `deleteAnimation`
  - システム設定: `getSystemSettings`, `updateSystemSettings`
  - バックアップ: `exportDatabase`, `importDatabase`, `backupDatabase`, `restoreDatabase`

- **rpgmaker_map ツール**
  - マップ管理: `listMaps`, `getMapById`, `getMaps`, `createMap`, `updateMap`, `deleteMap`, `getMapData`, `setMapData`
  - マップイベント: `listMapEvents`, `getMapEventById`, `getMapEvents`, `createMapEvent`, `updateMapEvent`, `deleteMapEvent`
  - タイルセット: `listTilesets`, `getTilesetById`, `getTilesets`, `setTileset`
  - 設定・ユーティリティ: `getMapSettings`, `updateMapSettings`, `copyMap`, `exportMap`, `importMap`

- **rpgmaker_event ツール**
  - コモンイベント: `listCommonEvents`, `getCommonEventById`, `getCommonEvents`, `createCommonEvent`, `updateCommonEvent`, `deleteCommonEvent`
  - イベントコマンド: `getEventCommands`, `createEventCommand`, `updateEventCommand`, `deleteEventCommand`
  - イベントページ: `getEventPages`, `createEventPage`, `updateEventPage`, `deleteEventPage`
  - ユーティリティ: `copyEvent`, `moveEvent`, `validateEvent`

- **rpgmaker_battle ツール**
  - バトル設定: `getBattleSettings`, `updateBattleSettings`
  - 敵管理: `listEnemies`, `getEnemyById`, `getEnemies`, `createEnemy`, `updateEnemy`, `deleteEnemy`
  - トループ管理: `listTroops`, `getTroopById`, `getTroops`, `createTroop`, `updateTroop`, `deleteTroop`
  - スキル管理: `listSkills`, `getSkillById`, `getSkills`, `createSkill`, `updateSkill`, `deleteSkill`
  - バトルアニメーション: `getBattleAnimations`, `updateBattleAnimation`

- **rpgmaker_system ツール**
  - システム情報: `getSystemInfo`
  - 変数: `getGameVariables`, `setGameVariable`
  - スイッチ: `getSwitches`, `setSwitch`
  - 設定: `getSystemSettings`, `updateSystemSettings`
  - セーブデータ: `getSaveData`, `createSaveData`, `loadSaveData`, `deleteSaveData`

- **rpgmaker_assets ツール**
  - 画像管理: `listImages`, `getImageById`, `getImages`, `importImage`, `exportImage`, `deleteImage`
  - サウンド管理: `listSounds`, `getSoundById`, `getSounds`, `importSound`, `exportSound`, `deleteSound`
  - アセット管理: `getAssetInfo`, `organizeAssets`, `validateAssets`, `backupAssets`, `restoreAssets`

- **rpgmaker_gamestate ツール**
  - ゲーム状態: `getGameState`, `setGameState`
  - プレイヤー: `getPlayerData`, `updatePlayerData`
  - パーティ: `getPartyData`, `updatePartyData`
  - インベントリ: `getInventory`, `updateInventory`, `addItemToInventory`, `removeItemFromInventory`
  - 進行フラグ: `getProgressFlags`, `setProgressFlag`
  - マップ・移動: `getCurrentMap`, `setCurrentMap`, `teleportPlayer`
  - リセット: `resetGameState`

- **rpgmaker_audio ツール**
  - リスト: `listAudioFiles`, `getAudioFileById`, `getAudioList`
  - 再生: `playBgm`, `stopBgm`, `playBgs`, `stopBgs`, `playMe`, `playSe`, `stopAllAudio`
  - 音量: `setBgmVolume`, `setBgsVolume`, `setMeVolume`, `setSeVolume`
  - 設定: `getAudioSettings`, `updateAudioSettings`
  - ファイル管理: `importAudioFile`, `exportAudioFile`, `deleteAudioFile`, `getAudioInfo`

- **ページネーションサポート**
  - すべてのリスト操作で `offset` と `limit` パラメータをサポート
  - デフォルト: offset=0, limit=100

- **推奨パターン: list* + get*ById**
  - 軽量なUUIDリストを取得してから個別に詳細を取得
  - 大規模データセットでのパフォーマンス向上

### 技術詳細

- **Python MCP サーバー**
  - Python 3.10+ 対応
  - uvパッケージマネージャー推奨
  - WebSocket通信（ポート7070、パス `/bridge`）
  - mcp >= 0.9.0, websockets >= 12.0

- **Unity C# Bridge**
  - Unity 2022.3 LTS以降対応（Unity 6推奨）
  - Editor/MCPBridge/ にWebSocketサーバー
  - Handlers/RPGMaker/ にRPGMaker専用ハンドラー

- **データ保存場所**
  - キャラクター: `Assets/RPGMaker/Storage/Character/JSON/`
  - アイテム: `Assets/RPGMaker/Storage/Item/JSON/`
  - アニメーション: `Assets/RPGMaker/Storage/Animation/JSON/`
  - システム: `Assets/RPGMaker/Storage/System/`
  - マップ: `Assets/RPGMaker/Storage/Map/`
  - イベント: `Assets/RPGMaker/Storage/Event/`
  - サウンド: `Assets/RPGMaker/Storage/Sounds/`

---

[1.0.0]: https://github.com/your-repo/rpgmaker-unite-mcp/releases/tag/v1.0.0
