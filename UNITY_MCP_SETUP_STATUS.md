# Unity MCP セットアップ状況

## 現在の状況

### ✅ 完了済み
1. **manifest.jsonにUniMCP4CCを追加済み**
   - `Packages/manifest.json`に`com.dsgarage.unimcp4cc`を追加
   - `packages-lock.json`でインストール確認済み

2. **MCPサーバーが起動中**
   - `.unity-mcp-runtime.json`が存在
   - WebSocketポート: 5050
   - HTTPポート: 5051
   - プロセスID: 54580

### ⚠️ 確認が必要
1. **UnityエディタでのAuto-Setup未実行**
   - `Window > Unity MCP > Setup Claude Code`からAuto-Setupを実行する必要があります

2. **Cursor IDE側の設定未追加**
   - `.cursor/mcp.json`にUniMCP4CCの設定がまだ追加されていません

## 次のステップ

### 方法1: UnityエディタでAuto-Setupを実行（推奨）

1. **Unityエディタを起動**
   - `unity_prototype/ukagaka`プロジェクトを開きます

2. **MCP設定ウィンドウを開く**
   - メニューから `Window > Unity MCP > Setup Claude Code` を選択
   - または `Window > MCP for Unity` を選択

3. **Auto-Setupを実行**
   - 表示されたウィンドウで「Setup Claude Code」または「Auto-Setup」ボタンをクリック
   - これにより、`.cursor/mcp.json`が自動的に更新されます

4. **Cursor IDEを再起動**
   - Auto-Setup完了後、Cursor IDEを再起動して設定を反映

### 方法2: 手動でCursor IDE設定を追加

Auto-Setupが動作しない場合、以下の設定を`.cursor/mcp.json`に手動で追加できます：

```json
{
  "mcpServers": {
    "browser-tools": { ... },
    "sequential thinking": { ... },
    "ai-game-developer": { ... },
    "unity-mcp": {
      "type": "stdio",
      "command": "node",
      "args": [
        "-e",
        "const WebSocket = require('ws'); const ws = new WebSocket('ws://localhost:5050'); process.stdin.pipe(ws); ws.pipe(process.stdout);"
      ]
    }
  }
}
```

**注意**: 上記は例です。UniMCP4CCの実際の接続方法は、パッケージのドキュメントを確認する必要があります。

## トラブルシューティング

### MCPサーバーが起動しない場合
- Unityエディタを再起動してください
- `.unity-mcp-runtime.json`が削除されている場合は、Unityエディタを開き直すと再生成されます

### Auto-Setupが動作しない場合
- Unityエディタのコンソールでエラーメッセージを確認
- UniMCP4CCパッケージが正しくインストールされているか確認（`Window > Package Manager`）

### Cursor IDEでMCPが認識されない場合
- Cursor IDEを再起動
- `.cursor/mcp.json`の構文を確認（JSON形式が正しいか）
- Unityエディタが起動していることを確認

## 参考情報

- **現在のMCPサーバー情報**: `.unity-mcp-runtime.json`
  - WebSocketポート: 5050
  - HTTPポート: 5051
  - プロセスID: 54580

- **既存のMCP設定**: `.cursor/mcp.json`
  - `ai-game-developer`（別のUnity MCPサーバー）が既に設定済み
  - ポート競合に注意（50899 vs 5050/5051）
