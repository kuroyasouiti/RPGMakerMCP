# RPGMaker Unite MCP - AI駆動型ゲーム開発ツールキット

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-1.0.0-brightgreen)]()

RPGMaker Unite MCPは、AIアシスタント（Claude、Cursor等）からRPGMaker Uniteのゲームデータを直接操作できるツールキットです。Model Context Protocol（MCP）による双方向通信で、自然言語でキャラクター、マップ、イベント、バトルシステムを管理できます。

## ✨ 主な機能

- **8つのRPGMaker専用ツール** - データベース、マップ、イベント、バトル、システム、アセット、ゲーム状態、オーディオ
- **リアルタイム連携** - Unity EditorとWebSocket双方向通信
- **AI駆動型開発** - 自然言語でゲームデータを操作

## 🎮 利用可能なツール

### ユーティリティツール

| ツール | 説明 |
|------|------|
| `unity_ping` | 接続確認 |
| `unity_compilation_await` | コンパイル完了待機 |

### RPGMakerツール

| ツール | 説明 |
|------|------|
| `rpgmaker_database` | キャラクター、アイテム、アニメーションの管理 |
| `rpgmaker_map` | マップとマップイベントの管理 |
| `rpgmaker_event` | コモンイベントとイベントコマンドの管理 |
| `rpgmaker_battle` | 敵、トループ、スキル、バトル設定の管理 |
| `rpgmaker_system` | 変数、スイッチ、セーブデータの管理 |
| `rpgmaker_assets` | 画像・音声アセットの管理 |
| `rpgmaker_gamestate` | プレイヤー、パーティ、インベントリの管理 |
| `rpgmaker_audio` | BGM/BGS/ME/SE再生とオーディオ設定 |

## 🚀 クイックスタート

### 1. Unity Bridgeの起動

1. Unity EditorでRPGMaker Uniteプロジェクトを開く
2. **Tools > MCP Assistant** を選択
3. **Start Bridge** をクリック
4. "Connected" ステータスを確認

### 2. MCPサーバーの起動

```bash
cd Assets/RPGMakerMCP/MCPServer
python start_server.py
```

### 3. 接続テスト

AIアシスタントで以下を試してください：

```
RPGMaker Unite MCPの接続をテストしてください
```

## 📁 データ保存場所

| データ種別 | 場所 |
|-----------|------|
| キャラクター | `Assets/RPGMaker/Storage/Character/JSON/` |
| アイテム | `Assets/RPGMaker/Storage/Item/JSON/` |
| アニメーション | `Assets/RPGMaker/Storage/Animation/JSON/` |
| システム | `Assets/RPGMaker/Storage/System/` |
| マップ | `Assets/RPGMaker/Storage/Map/` |
| イベント | `Assets/RPGMaker/Storage/Event/` |
| サウンド | `Assets/RPGMaker/Storage/Sounds/` |

## 📖 使用例

### キャラクターの取得

```python
rpgmaker_database(operation='getCharacters')
```

### 新しいキャラクターの作成

```python
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
```

### マップの取得

```python
rpgmaker_map(operation='getMaps')
```

### BGMの再生

```python
rpgmaker_audio(operation='playBgm', filename='battle_theme', volume=0.8)
```

### 変数の設定

```python
rpgmaker_system(operation='setGameVariable', variableId=1, value=100)
```

### インベントリにアイテム追加

```python
rpgmaker_gamestate(
    operation='addItemToInventory',
    itemId='potion_001',
    quantity=5
)
```

## 🏗️ アーキテクチャ

```
AIクライアント (Claude/Cursor) <--(MCP)--> Python MCPサーバー <--(WebSocket)--> Unity Bridge
                                            (MCPServer/src/)      (Editor/MCPBridge/)
```

### コンポーネント

| コンポーネント | 場所 | 説明 |
|-----------|----------|-------------|
| Unity Bridge | `Editor/MCPBridge/` | WebSocketサーバー（Unity Editor内） |
| Python MCPサーバー | `MCPServer/src/` | MCPプロトコル実装 |
| RPGMakerハンドラー | `Editor/MCPBridge/Handlers/RPGMaker/` | RPGMakerデータ操作 |

## 🌐 ネットワーク設定

| サービス | ポート | URI |
|---------|------|-----|
| Unity Bridge | 7070 | `ws://localhost:7070/bridge` |
| MCPサーバー | stdio | (MCP default) |

## 🔧 トラブルシューティング

### 接続エラー

1. Unity Editorで **Tools > MCP Assistant** を起動
2. **Start Bridge** をクリック
3. ポート7070が使用可能か確認

### データが見つからない

1. RPGMaker Uniteのデータが `Assets/RPGMaker/Storage/` に存在するか確認
2. ファイルパスが正しいか確認

### タイムアウト

1. Unity Consoleでエラーログを確認
2. 大きなデータセットの場合は操作を分割

## 📚 ドキュメント

- [Getting Started](GETTING_STARTED.md) - セットアップガイド
- [INDEX](INDEX.md) - ドキュメント索引
- [MCPサーバー](MCPServer/README.md) - MCPサーバー詳細

## 📄 ライセンス

MIT License

---

**RPGMaker Unite開発をAIでパワーアップ！**
