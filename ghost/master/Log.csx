using System.IO;
using System;
using System.Text;

static class Log
{
    // 既存のログファイル（開発モード時のLLM通信ログ）
    public static string Prompt = "prompt.txt";
    public static string Response = "response.txt";

    // 新しいログファイル（常時出力）
    public static string Event = "shiori_events.log";
    public static string Script = "generated_scripts.log";
    public static string Error = "error.log";

    static string path = "./log/";

    static Log()
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public static void WriteAllText(string fileName, string value)
    {
        File.WriteAllText(path + fileName, value, Encoding.UTF8);
    }

    /// <summary>
    /// ログファイルに追記する（タイムスタンプ付き）
    /// </summary>
    public static void AppendLine(string fileName, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}\n";
        File.AppendAllText(path + fileName, logLine, Encoding.UTF8);
    }

    /// <summary>
    /// SHIORIイベントをログに記録
    /// </summary>
    public static void LogEvent(string eventName, string detail = "")
    {
        var message = string.IsNullOrEmpty(detail)
            ? $"Event: {eventName}"
            : $"Event: {eventName} | {detail}";
        AppendLine(Event, message);
    }

    /// <summary>
    /// 生成されたスクリプトをログに記録
    /// </summary>
    public static void LogScript(string eventName, string script)
    {
        var escapedScript = script.Replace("\n", "\\n").Replace("\r", "");
        AppendLine(Script, $"{eventName} => {escapedScript}");
    }

    /// <summary>
    /// エラーをログに記録
    /// </summary>
    public static void LogError(string location, Exception ex)
    {
        var message = $"ERROR in {location}\n" +
                     $"  Type: {ex.GetType().FullName}\n" +
                     $"  Message: {ex.Message}\n" +
                     $"  StackTrace: {ex.StackTrace ?? "(none)"}\n" +
                     $"  InnerException: {ex.InnerException?.ToString() ?? "(none)"}";
        AppendLine(Error, message);
    }

    /// <summary>
    /// エラーをログに記録（文字列メッセージ版）
    /// </summary>
    public static void LogError(string location, string errorMessage)
    {
        AppendLine(Error, $"ERROR in {location}: {errorMessage}");
    }
}