# Unity MCP セットアップガイド

## 概要
このドキュメントでは、UnityプロジェクトにUniMCP4CCを導入し、Cursor IDE（Claude Code）と連携する手順を説明します。

## 前提条件
- Unity 6000.3.2f1以上
- Unityプロジェクト: `unity_prototype/ukagaka`
- Cursor IDE（またはClaude Code）

## インストール手順

### 1. Unity Package Managerでのパッケージ追加（完了済み）
`Packages/manifest.json`に以下の依存関係が追加されています：
```json
"com.dsgarage.unimcp4cc": "https://github.com/dsgarage/UniMCP4CC.git"
```

Unityエディタを開くと、自動的にパッケージがダウンロード・インストールされます。

### 2. Unityエディタでの設定

1. **Unityエディタを起動**
   - `unity_prototype/ukagaka`プロジェクトを開きます

2. **MCP設定ウィンドウを開く**
   - メニューから `Window > Unity MCP > Setup Claude Code` を選択します
   - または `Window > MCP for Unity` を選択します

3. **Auto-Setupの実行**
   - 表示されたウィンドウで「Setup Claude Code」または「Auto-Setup」ボタンをクリックします
   - これにより、Cursor IDE（Claude Code）の設定ファイルが自動的に更新されます

### 3. Cursor IDE側の設定確認

Auto-Setupが正常に完了すると、Cursor IDE側のMCP設定が自動的に更新されます。

**手動確認が必要な場合：**
- Cursor IDEの設定ファイル（通常は `%APPDATA%\Cursor\User\settings.json` または `.cursor/mcp.json`）を確認
- Unity MCPサーバーが正しく登録されているか確認

### 4. 接続テスト

1. **Unityエディタを起動した状態で、Cursor IDEを起動**
2. **Cursor IDEでMCP接続を確認**
   - `/mcp` コマンドで `UnityMCP` が接続されていることを確認
3. **Unityプロジェクトに関する質問や指示を実行**
   - 例：「現在のシーンにGameObjectを追加して」
   - 例：「VRMControllerスクリプトを表示して」

## トラブルシューティング

### パッケージがインストールされない場合
- Unityエディタを再起動してください
- `Packages/manifest.json`の構文を確認してください
- インターネット接続を確認してください

### Auto-Setupが動作しない場合
- Unityエディタのバージョンが6000.3.2f1以上であることを確認
- Unityエディタのコンソールでエラーメッセージを確認
- 手動でCursor IDEのMCP設定ファイルを編集する必要がある場合があります

### Cursor IDEでMCPが認識されない場合
- Cursor IDEを再起動してください
- MCP設定ファイルのパスと内容を確認してください
- Unityエディタが起動していることを確認してください

## 参考リンク
- [UniMCP4CC GitHub](https://github.com/dsgarage/UniMCP4CC)
- [MCP for Unity ドキュメント](https://glama.ai/mcp/servers/%40dsgarage/UniMCP4CC)

## 次のステップ
セットアップ完了後、以下の機能が使用可能になります：
- Unityシーンの操作
- コンポーネントの追加・編集
- アセット管理
- C#スクリプトの生成・編集
