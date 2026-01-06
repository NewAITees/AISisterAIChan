# フェーズ3: VRM Viewer 詳細設計（受信側プロトタイプ）

## 目的
SSPゴーストからの表情・セリフ指示をUnity側で受信し、VRMモデルの表情に反映する最小構成を作る。

## スコープ（プロトタイプ）
- UnityでVRMモデル1体を表示
- HTTPで指示を受信して表情を切り替え
- リップシンク・モーション・UIは後回し

## 前提
- Unity 6000系（最新）を使用
- UniVRM 0.125.0+ を使用
- 通信はローカルHTTPのみ（将来切替可能な設計）

## 通信仕様
- 受信URL: http://127.0.0.1:31555/ghost/command
- メソッド: POST
- Content-Type: application/json
- 文字コード: UTF-8
- タイムアウト: SSP側 500-1000ms想定

### JSONフォーマット（暫定）
```
{
  "type": "talk",
  "characterId": 0,
  "expression": "笑顔",
  "text": "セリフ"
}
```

- type: "talk" 固定（今は1種類のみ）
- characterId: 0（将来2体表示に拡張）
- expression: 日本語カテゴリ名（SSP側SurfaceCategoryに対応）
- text: リップシンク用に保持（本プロトタイプでは未使用）

## Unity側構成（最小）

### コンポーネント
- GhostAPIServer
  - HttpListenerで受信
  - JSONをパースし、メインスレッド実行キューへ積む
- MainThreadQueue
  - ConcurrentQueue<Action> をUpdateで実行
- VRMController
  - ExpressionPresetへマッピングし表情を適用

### スレッド設計
- HttpListenerは別スレッド
- Unity操作は必ずメインスレッドで実行

## 表情マッピング（暫定）
SSP SurfaceCategory -> VRM ExpressionPreset
- 普通 -> Neutral
- 笑顔 -> Happy
- 驚き -> Surprised
- 怒り -> Angry
- 悲しい -> Sad
- 恥ずかしい -> Relaxed

未定義の表情はNeutralへフォールバックする。

## エラーハンドリング
- 受信時に例外が出てもサーバーは継続
- JSONパース失敗は無視し、200 OKは返す
- VRMインスタンスが未初期化なら表情変更はスキップ

## セキュリティ/運用
- 127.0.0.1のみで待ち受ける
- ポートは固定 31555（衝突しにくい）

## 将来拡張
- typeに "motion" / "expression" / "speak" を追加
- 通信方式をNamedPipe/UDPへ差し替え
- モデル別の表情マッピングJSON導入

## 成果物（プロトタイプ）
- Unityプロジェクトに以下のスクリプトを追加
  - GhostAPIServer.cs
  - MainThreadQueue.cs
  - VRMController.cs
- サンプル送信で表情が変わることを確認
