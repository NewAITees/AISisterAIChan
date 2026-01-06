# Unity MCP Bridge セットアップ完了

## 実施した作業

### 1. ✅ Node.jsの確認
- Node.js v22.14.0 がインストール済み
- npm 11.1.0 が利用可能

### 2. ✅ WebSocket-stdioブリッジスクリプトの作成
- `unity_prototype/ukagaka/mcp-bridge.js` を作成
- Unity MCPサーバー（WebSocketポート5050）とCursor IDE（stdio）を接続

### 3. ✅ 必要なパッケージのインストール
- `ws` パッケージをインストール（WebSocket通信用）

### 4. ✅ Cursor IDEのMCP設定を更新
- `.cursor/mcp.json` に `unity-mcp` サーバーを追加
- ブリッジスクリプトを実行するように設定

## 設定内容

### Cursor IDE側の設定（`.cursor/mcp.json`）

```json
"unity-mcp": {
  "type": "stdio",
  "command": "node",
  "args": [
    "C:/Users/perso/Downloads/ssp/ghost/ollama_ghost_new/unity_prototype/ukagaka/mcp-bridge.js"
  ],
  "env": {}
}
```

### Unity MCPサーバー側
- WebSocketポート: 5050（`.unity-mcp-runtime.json`から自動検出）
- HTTPポート: 5051
- プロセスID: 41908（現在実行中）

## 次のステップ

### 1. Cursor IDEを再起動
設定を反映するために、Cursor IDEを完全に終了して再起動してください。

### 2. 接続テスト
Cursor IDEを再起動後、以下を確認：

1. **MCPサーバーの状態確認**
   - Cursor IDEのMCP設定で `unity-mcp` が接続されているか確認
   - エラーメッセージがないか確認

2. **Unityエディタの確認**
   - Unityエディタが起動していることを確認
   - `.unity-mcp-runtime.json` が存在することを確認

3. **動作テスト**
   - Cursor IDEでUnityプロジェクトに関する質問をしてみる
   - 例：「現在のUnityシーンにGameObjectを追加して」

## トラブルシューティング

### ブリッジスクリプトが動作しない場合

1. **Node.jsのパス確認**
   ```powershell
   node --version
   ```

2. **ブリッジスクリプトの直接実行テスト**
   ```powershell
   cd "C:\Users\perso\Downloads\ssp\ghost\ollama_ghost_new\unity_prototype\ukagaka"
   node mcp-bridge.js
   ```
   - エラーメッセージを確認

3. **WebSocket接続の確認**
   - Unityエディタが起動していることを確認
   - `.unity-mcp-runtime.json` のポート番号が正しいか確認

### Unity MCPサーバーが起動していない場合

1. **Unityエディタを起動**
   - `unity_prototype/ukagaka` プロジェクトを開く

2. **MCPサーバーの状態確認**
   - `.unity-mcp-runtime.json` が存在するか確認
   - 存在しない場合、UnityエディタでMCPパッケージが正しくインストールされているか確認

### ポート競合の場合

- 現在のポート: WebSocket 5050, HTTP 5051
- 他のプロセスが同じポートを使用している場合、`.unity-mcp-runtime.json` のポート番号を確認

## 参考情報

- **ブリッジスクリプト**: `unity_prototype/ukagaka/mcp-bridge.js`
- **Unity MCP設定**: `unity_prototype/ukagaka/.unity-mcp-runtime.json`
- **Cursor IDE設定**: `C:\Users\perso\.cursor\mcp.json`
- **Node.jsパッケージ**: `unity_prototype/ukagaka/package.json`
