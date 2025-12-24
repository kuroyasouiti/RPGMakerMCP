# RPGMaker Unite MCP Server

**AI-powered RPGMaker Unite development toolkit - Model Context Protocol integration**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)](https://www.python.org/)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)](https://unity.com/)
[![MCP](https://img.shields.io/badge/MCP-0.9.0%2B-green)](https://modelcontextprotocol.io/)
[![Version](https://img.shields.io/badge/Version-1.0.0-brightgreen)]()

## 🎯 概要

RPGMaker Unite MCP Serverは、AIアシスタント（Claude、Cursor等）がRPGMaker Uniteのゲームデータを直接操作できるようにするMCPサーバーです。

## ✨ 主な機能

- **8つのRPGMaker専用ツール** - ゲームデータの完全な管理
- **リアルタイムBridge** - WebSocketによる双方向通信
- **自動コンパイル待機** - スクリプトコンパイルの検出と待機

## 🚀 クイックスタート

### 1. 依存関係のインストール

```bash
cd Assets/RPGMakerMCP/MCPServer
uv sync
```

### 2. サーバーの起動

```bash
python start_server.py
```

または直接実行：

```bash
uv run python src/server.py
```

### 3. Unity Bridgeの起動

1. Unity Editorで **Tools > MCP Assistant** を開く
2. **Start Bridge** をクリック
3. "Connected" ステータスを確認

### 4. 接続テスト

AIアシスタントで以下を試してください：

```
RPGMaker Unite MCPの接続をテストしてください
```

## 🛠️ 利用可能なツール

### ユーティリティツール

| ツール | 説明 |
|------|------|
| `unity_ping` | 接続確認とUnityバージョン情報 |
| `unity_compilation_await` | コンパイル完了待機 |

### RPGMakerツール

| ツール | 説明 | 主な操作 |
|------|------|---------|
| `rpgmaker_database` | データベース管理 | getCharacters, createCharacter, getItems, createItem 等 |
| `rpgmaker_map` | マップ管理 | getMaps, createMap, getMapEvents, createMapEvent 等 |
| `rpgmaker_event` | イベント管理 | getCommonEvents, createCommonEvent, getEventCommands 等 |
| `rpgmaker_battle` | バトル管理 | getEnemies, createEnemy, getSkills, createSkill 等 |
| `rpgmaker_system` | システム管理 | getGameVariables, setGameVariable, getSwitches 等 |
| `rpgmaker_assets` | アセット管理 | getImages, importImage, getSounds, importSound 等 |
| `rpgmaker_gamestate` | ゲーム状態管理 | getPlayerData, updatePlayerData, getInventory 等 |
| `rpgmaker_audio` | オーディオ管理 | playBgm, stopBgm, playSe, getAudioSettings 等 |

## 🏗️ アーキテクチャ

```
AIクライアント (Claude/Cursor) <--(MCP)--> Python MCPサーバー <--(WebSocket)--> Unity Bridge
                                            (MCPServer/src/)      (Editor/MCPBridge/)
```

### ディレクトリ構造

```
MCPServer/
├── src/
│   ├── server.py           # サーバーエントリポイント
│   ├── config.py           # 設定
│   ├── version.py          # バージョン情報
│   ├── bridge/             # Unity Bridge通信
│   │   └── bridge_manager.py
│   ├── server/             # MCPサーバー作成
│   │   └── create_mcp_server.py
│   ├── tools/              # ツール定義
│   │   ├── register_tools.py
│   │   └── rpgmaker_tools.py
│   └── resources/          # リソース定義
│       └── register_resources.py
├── start_server.py         # 起動スクリプト
├── pyproject.toml          # Python依存関係
└── README.md               # このファイル
```

## ⚙️ 設定

### 環境変数

| 変数 | 説明 | デフォルト |
|-----|------|----------|
| `MCP_SERVER_TRANSPORT` | トランスポートモード: `stdio` または `websocket` | `stdio` |
| `MCP_SERVER_HOST` | WebSocketサーバーホスト | `127.0.0.1` |
| `MCP_SERVER_PORT` | WebSocketサーバーポート | `7070` |
| `MCP_LOG_LEVEL` | ログレベル: `trace`, `debug`, `info`, `warn`, `error` | `info` |

### WebSocket設定

Unity Bridgeの設定（`config/env.py`）：

| 設定 | 値 |
|------|-----|
| URI | `ws://localhost:7070/bridge` |
| Ping間隔 | 接続時に設定 |
| 最大メッセージサイズ | 10MB |
| コマンドタイムアウト | 45秒 |

## 💻 開発

### 開発依存関係のインストール

```bash
uv sync --extra dev
```

### コード品質

```bash
# フォーマット
uv run black src/

# リント
uv run ruff check src/

# 型チェック
uv run mypy src/
```

### テスト

```bash
pytest
```

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

### マップイベントの作成

```python
rpgmaker_map(
    operation='createMapEvent',
    mapId='map_001',
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

### BGMの再生

```python
rpgmaker_audio(operation='playBgm', filename='battle_theme', volume=0.8)
```

### ゲーム変数の設定

```python
rpgmaker_system(operation='setGameVariable', variableId=1, value=100)
```

## 🔧 トラブルシューティング

### 接続エラー

1. Unity Editorが起動しているか確認
2. **Tools > MCP Assistant** でBridgeが起動しているか確認
3. ポート7070が使用可能か確認
4. ファイアウォールがブロックしていないか確認

### タイムアウト

1. Unity Consoleでエラーログを確認
2. 大きなデータセットの場合は操作を分割
3. コマンドタイムアウト設定を確認（デフォルト60秒）

### データが見つからない

1. `Assets/RPGMaker/Storage/` フォルダを確認
2. RPGMaker Uniteでデータを作成済みか確認
3. ファイルパスが正しいか確認

## 📄 ライセンス

MIT License

---

**RPGMaker Unite開発をAIでパワーアップ！**
