using System;
using System.Net;
using UnityEngine;

/// <summary>
/// ネットワークエラーを捕捉し、詳細な情報を表示する
/// Unityのログシステムと連携してエラーを記録
/// </summary>
public class NetworkErrorHandler : MonoBehaviour
{
    [Header("設定")]
    [SerializeField]
    private bool enableDiagnostics = true;

    [SerializeField]
    private bool showDetailedErrors = true;

    private void OnEnable()
    {
        // アプリケーションレベルの未処理例外を捕捉
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
            return;

        // SocketException または WebException を検出
        if (logString.Contains("SocketException") || logString.Contains("WebException") || logString.Contains("ConnectFailure"))
        {
            // TaskCanceledException は接続タイムアウトの場合が多いので、警告レベルに下げる
            if (logString.Contains("TaskCanceledException"))
            {
                if (enableDiagnostics)
                {
                    Debug.LogWarning("=== ネットワーク接続タイムアウトが検出されました ===");
                    Debug.LogWarning("これは通常、接続先のサーバーが起動していない場合に発生します。");
                    Debug.LogWarning("AI-Game-DeveloperまたはUnity MCPサーバーを使用しない場合は、");
                    Debug.LogWarning("AI-Game-Developer-Config.json の keepConnected を false に設定してください。");
                }
                return; // エラーとして扱わない
            }

            if (enableDiagnostics)
            {
                Debug.LogWarning("=== ネットワーク接続エラーが検出されました ===");
                Debug.LogWarning($"エラーメッセージ: {logString}");
                
                if (showDetailedErrors && !string.IsNullOrEmpty(stackTrace))
                {
                    Debug.LogWarning($"スタックトレース:\n{stackTrace}");
                }

                // 診断情報を表示
                ShowDiagnosticInfo();
            }
        }
    }

    private void ShowDiagnosticInfo()
    {
        Debug.LogWarning("=== ネットワーク接続エラーの対処法 ===");
        Debug.LogWarning("1. 接続先のサーバーが起動しているか確認してください");
        Debug.LogWarning("2. ファイアウォールがポートをブロックしていないか確認してください");
        Debug.LogWarning("3. ポートが既に使用されていないか確認してください");
        Debug.LogWarning("4. Unity MCPサーバーが起動しているか確認してください");
        Debug.LogWarning("5. AI-Game-Developerサーバーが起動しているか確認してください");
        Debug.LogWarning("");
        Debug.LogWarning("診断を実行するには、NetworkConnectionHelper.DiagnoseNetworkConnections() を呼び出してください");
    }

    /// <summary>
    /// 手動で診断を実行
    /// </summary>
    [ContextMenu("ネットワーク診断を実行")]
    public void RunDiagnostics()
    {
        NetworkConnectionHelper.DiagnoseNetworkConnections();
    }
}
