using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ネットワーク接続の診断とヘルパー機能
/// 接続エラーの原因を特定し、解決方法を提案する
/// </summary>
public static class NetworkConnectionHelper
{
    /// <summary>
    /// 指定されたポートが使用可能か確認する
    /// </summary>
    /// <param name="port">確認するポート番号</param>
    /// <returns>使用可能な場合true</returns>
    public static bool IsPortAvailable(int port)
    {
        TcpListener tcpListener = null;
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            if (tcpListener != null)
            {
                try
                {
                    tcpListener.Stop();
                }
                catch
                {
                    // 既に停止している場合は無視
                }
            }
        }
    }

    /// <summary>
    /// 指定されたホストとポートに接続可能か確認する
    /// </summary>
    /// <param name="host">ホスト名またはIPアドレス</param>
    /// <param name="port">ポート番号</param>
    /// <param name="timeoutMs">タイムアウト（ミリ秒）</param>
    /// <returns>接続可能な場合true</returns>
    public static async Task<bool> CanConnectAsync(string host, int port, int timeoutMs = 5000)
    {
        try
        {
            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning($"接続タイムアウト: {host}:{port}");
                    return false;
                }

                if (tcpClient.Connected)
                {
                    Debug.Log($"接続成功: {host}:{port}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"接続失敗: {host}:{port}");
                    return false;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError($"SocketException: {host}:{port} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"接続エラー: {host}:{port} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// HTTPエンドポイントに接続可能か確認する
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="timeoutMs">タイムアウト（ミリ秒）</param>
    /// <returns>接続可能な場合true</returns>
    public static async Task<bool> CanConnectToHttpAsync(string url, int timeoutMs = 5000)
    {
        try
        {
            Uri uri = new Uri(url);
            int port = uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);
            string host = uri.Host;

            return await CanConnectAsync(host, port, timeoutMs);
        }
        catch (Exception ex)
        {
            Debug.LogError($"URL解析エラー: {url} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ネットワーク接続の診断を実行する
    /// </summary>
    public static async void DiagnoseNetworkConnections()
    {
        Debug.Log("=== ネットワーク接続診断を開始 ===");

        // Ghost API Server のポート確認
        int ghostPort = 31555;
        bool ghostPortAvailable = IsPortAvailable(ghostPort);
        Debug.Log($"Ghost API Server ポート {ghostPort}: {(ghostPortAvailable ? "使用可能" : "使用中または使用不可")}");

        // MCP Server のポート確認
        int mcpWebSocketPort = 5050;
        int mcpHttpPort = 5051;
        bool mcpWsAvailable = IsPortAvailable(mcpWebSocketPort);
        bool mcpHttpAvailable = IsPortAvailable(mcpHttpPort);
        Debug.Log($"MCP WebSocket ポート {mcpWebSocketPort}: {(mcpWsAvailable ? "使用可能" : "使用中")}");
        Debug.Log($"MCP HTTP ポート {mcpHttpPort}: {(mcpHttpAvailable ? "使用可能" : "使用中")}");

        // AI-Game-Developer のホスト確認
        string aiGameDevHost = "http://localhost:54230";
        bool aiGameDevConnected = await CanConnectToHttpAsync(aiGameDevHost, 3000);
        Debug.Log($"AI-Game-Developer ({aiGameDevHost}): {(aiGameDevConnected ? "接続可能" : "接続不可")}");

        Debug.Log("=== ネットワーク接続診断完了 ===");
    }

    /// <summary>
    /// SocketExceptionの詳細なエラーメッセージを取得
    /// </summary>
    /// <param name="ex">SocketException</param>
    /// <returns>エラーメッセージ</returns>
    public static string GetSocketErrorMessage(SocketException ex)
    {
        string message = $"SocketException (Error Code: {ex.SocketErrorCode}): {ex.Message}";

        switch (ex.SocketErrorCode)
        {
            case SocketError.ConnectionRefused:
                message += "\n原因: 接続先のサーバーが起動していないか、ポートが閉じられています。";
                message += "\n対処法: サーバーが起動しているか確認してください。";
                break;
            case SocketError.TimedOut:
                message += "\n原因: 接続タイムアウトが発生しました。";
                message += "\n対処法: ネットワーク接続とファイアウォール設定を確認してください。";
                break;
            case SocketError.AddressAlreadyInUse:
                message += "\n原因: ポートが既に使用されています。";
                message += "\n対処法: 別のポートを使用するか、既存のプロセスを終了してください。";
                break;
            case SocketError.NetworkUnreachable:
                message += "\n原因: ネットワークに到達できません。";
                message += "\n対処法: ネットワーク接続を確認してください。";
                break;
            default:
                message += $"\n詳細: {ex}";
                break;
        }

        return message;
    }
}
