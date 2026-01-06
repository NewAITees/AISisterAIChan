# Unity MCP メニューが見つからない場合のトラブルシューティング

## 問題
Unityエディタで `Window > Unity MCP > Setup Claude Code` メニューが見つからない

## 確認手順

### 1. Unity Package Managerでパッケージを確認

1. Unityエディタで `Window > Package Manager` を開く
2. 左上のドロップダウンで「In Project」または「All」を選択
3. リスト内で `UniMCP4CC` または `com.dsgarage.unimcp4cc` を探す
4. パッケージが表示されていない場合 → インストールされていない
5. パッケージが表示されているがエラーがある場合 → エラーメッセージを確認

### 2. Unityエディタの再起動

パッケージをインストールした後、Unityエディタを完全に終了して再起動してください。
- エディタスクリプトが再コンパイルされ、メニューが表示される可能性があります

### 3. コンソールでエラーを確認

1. Unityエディタで `Window > General > Console` を開く
2. エラーメッセージ（赤いアイコン）がないか確認
3. 警告メッセージ（黄色いアイコン）も確認
4. エラーがある場合、その内容を確認して対処

### 4. パッケージの再インストール

#### 方法A: manifest.jsonから削除して再追加

1. `Packages/manifest.json` から以下の行を一時的に削除：
   ```json
   "com.dsgarage.unimcp4cc": "https://github.com/dsgarage/UniMCP4CC.git",
   ```
2. Unityエディタを再起動（パッケージが削除される）
3. 再度 `manifest.json` に追加
4. Unityエディタを再起動（パッケージが再インストールされる）

#### 方法B: Unity Package Managerから直接インストール

1. Unityエディタで `Window > Package Manager` を開く
2. 左上の「+」ボタンをクリック
3. 「Add package from git URL...」を選択
4. 以下のURLを入力：
   ```
   https://github.com/dsgarage/UniMCP4CC.git
   ```
5. 「Add」をクリック

### 5. 別のMCPパッケージを試す

UniMCP4CCが動作しない場合、以下の代替パッケージを試すことができます：

#### CoplayDev/unity-mcp

1. Unity Package Managerで以下を追加：
   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity
   ```
2. メニュー: `Window > MCP For Unity > Setup Window`

#### UnityMCP (isuzu-shiranui)

1. Unity Package Managerで以下を追加：
   ```
   https://github.com/isuzu-shiranui/UnityMCP.git?path=jp.shiranui-isuzu.unity-mcp
   ```
2. 設定: `Edit > Preferences > Unity MCP`

## 現在の状況

- ✅ `manifest.json`にUniMCP4CCが追加済み
- ✅ `packages-lock.json`でインストール確認済み
- ✅ `.unity-mcp-runtime.json`が存在（MCPサーバーが起動中）
- ❓ Unityエディタのメニューに表示されない

## 推奨される次のステップ

1. **Unity Package Managerでパッケージの状態を確認**
   - パッケージが表示されているか
   - エラーがないか

2. **Unityエディタを再起動**
   - 完全に終了してから再起動

3. **コンソールでエラーを確認**
   - エラーメッセージがあれば、その内容を確認

4. **パッケージの再インストールを試す**
   - 上記の方法AまたはBを実行

5. **別のMCPパッケージを試す**
   - UniMCP4CCが動作しない場合、CoplayDev/unity-mcpを試す
