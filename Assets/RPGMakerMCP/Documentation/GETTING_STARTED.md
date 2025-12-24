# Getting Started with RPGMaker Unite MCP

<div align="center">

**🚀 RPGMaker Unite MCPへようこそ！**

このガイドでは、RPGMaker Unite MCPを使ったゲーム開発の始め方を
ステップバイステップで解説します。

</div>

---

## 📋 Table of Contents

1. [セットアップ](#-setup)
2. [Hello World - 最初の操作](#-hello-world)
3. [RPGMaker データを操作](#-rpgmaker-data)
4. [MCP で AI 連携](#-mcp-integration)
5. [次のステップ](#-next-steps)

---

## 🔧 Setup

### 1. 必要要件

- **Unity**: 2022.3 LTS以降（Unity 6推奨）
- **Python**: 3.10以上
- **uv**: Pythonパッケージマネージャー（推奨）
- **RPGMaker Unite**: インストール済み

### 2. uvのインストール

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### 3. MCPサーバーのセットアップ

```bash
cd Assets/RPGMakerMCP/MCPServer
uv sync
```

### 4. 動作確認

Unity Editor を開いたら、以下を確認：

- [ ] **Tools > MCP Assistant** メニューが表示される
- [ ] **Assets/RPGMakerMCP** フォルダが存在する
- [ ] **Assets/RPGMaker/Storage** にゲームデータがある
- [ ] Console にエラーがない

✅ すべて OK なら、次に進みましょう！

---

## 👋 Hello World

最初の操作を行い、MCP接続を確認しましょう。

### Step 1: Unity Bridgeを起動

1. Unity Editorで **Tools > MCP Assistant** を開く
2. **Start Bridge** ボタンをクリック
3. ステータスが "Connected" になるのを確認

### Step 2: MCPサーバーを起動

```bash
cd Assets/RPGMakerMCP/MCPServer
python start_server.py
```

### Step 3: 接続テスト

AIアシスタント（Claude Desktop、Cursor等）で以下を試してください：

```
RPGMaker Unite MCPの接続をテストしてください
```

AIが `unity_ping()` を呼び出し、Unityバージョン情報を表示するはずです。

🎉 **おめでとうございます！** MCP接続が完了しました！

---

## 🎮 RPGMaker Data

### Example 1: キャラクターを取得

AIアシスタントに以下のように指示：

```
RPGMaker Uniteのキャラクター一覧を取得してください
```

AIが自動的に：
```python
rpgmaker_database(operation='getCharacters')
```
を呼び出し、キャラクターデータを表示します。

### Example 2: 新しいアイテムを作成

```
「回復薬」という名前で、HPを50回復するアイテムを作成してください
```

AIが自動的に：
```python
rpgmaker_database(
    operation='createItem',
    filename='healing_potion',
    itemData={
        'name': '回復薬',
        'description': 'HPを50回復する',
        'effect': {'type': 'heal', 'value': 50}
    }
)
```
を実行します。

### Example 3: マップ情報を確認

```
利用可能なマップの一覧を表示してください
```

AIが自動的に：
```python
rpgmaker_map(operation='getMaps')
```
を呼び出します。

### Example 4: BGMを再生

```
バトルBGMを再生してください
```

AIが自動的に：
```python
rpgmaker_audio(operation='playBgm', filename='battle_theme', volume=0.8)
```
を実行します。

---

## 🤖 MCP Integration

### Claude Desktop を設定

`claude_desktop_config.json` に追加：

```json
{
  "mcpServers": {
    "rpgmaker-unite-mcp": {
      "command": "uv",
      "args": ["--directory", "path/to/Assets/RPGMakerMCP/MCPServer", "run", "python", "src/server.py"],
      "env": {
        "MCP_SERVER_TRANSPORT": "stdio",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

`path/to/Assets/RPGMakerMCP/MCPServer` を実際のパスに置き換えてください。

### Cursor を設定

`.cursorrules` に追加：

```
RPGMaker Unite MCP server is available.
Use rpgmaker_* tools to manage game data.
```

### AI でゲームデータを操作

Claude や Cursor で以下のように指示：

```
RPGMaker Uniteで以下のキャラクターを作成してください：
- 名前：勇者アレックス
- クラス：剣士
- レベル：5
- HP：150、MP：30
- 攻撃力：25、防御力：15
```

AIが自動的に:
- キャラクターデータを作成
- JSONファイルに保存
- 確認メッセージを表示

してくれます！🎉

---

## 📚 Next Steps

おめでとうございます！RPGMaker Unite MCPの基本をマスターしました。

### 利用可能なツール

| ツール | 説明 |
|:---|:---|
| `rpgmaker_database` | キャラクター、アイテム、アニメーション管理 |
| `rpgmaker_map` | マップとマップイベント管理 |
| `rpgmaker_event` | コモンイベント、イベントコマンド管理 |
| `rpgmaker_battle` | 敵、トループ、スキル、バトル設定 |
| `rpgmaker_system` | 変数、スイッチ、セーブデータ |
| `rpgmaker_assets` | 画像・音声アセット管理 |
| `rpgmaker_gamestate` | プレイヤー、パーティ、インベントリ |
| `rpgmaker_audio` | BGM/BGS/ME/SE再生とオーディオ設定 |

### プロジェクトアイデア

#### 初級

- [ ] **クエストシステム** - 変数とスイッチで進行管理
- [ ] **ショップシステム** - アイテムとインベントリ操作
- [ ] **セーブ/ロード** - ゲーム状態の保存と読み込み

#### 中級

- [ ] **複雑なイベント** - コモンイベントとページ管理
- [ ] **バトルバランス** - 敵とスキルの調整
- [ ] **マップ連携** - 複数マップ間の移動と状態管理

#### 上級

- [ ] **動的コンテンツ** - AIでイベントを自動生成
- [ ] **データ分析** - ゲームバランスの最適化
- [ ] **カスタムシステム** - 独自のゲームシステム構築

---

## 🆘 Troubleshooting

### よくある問題

#### "Unity Bridgeに接続できない"

**チェックリスト:**
1. Unity Editorが起動している
2. **Tools > MCP Assistant** で Bridge が起動している
3. ポート7070が使用可能
4. ファイアウォールがブロックしていない

#### "データが見つからない"

**解決策:**
1. `Assets/RPGMaker/Storage/` フォルダを確認
2. RPGMaker Uniteでデータを作成済みか確認
3. ファイルパスが正しいか確認

#### "MCPサーバーが起動しない"

**チェックリスト:**
1. Python 3.10+ がインストールされている
2. `uv` がインストールされている
3. `uv sync` を実行済み
4. 正しいディレクトリで実行している

---

## 📖 More Resources

- [📑 Documentation Index](INDEX.md)
- [🔧 MCPサーバー詳細](MCPServer/README.md)
- [📝 Changelog](CHANGELOG.md)

---

<div align="center">

**Happy Game Development! 🎮✨**

[⬅️ Back to Main README](README.md) | [📑 Documentation Index](INDEX.md)

</div>
