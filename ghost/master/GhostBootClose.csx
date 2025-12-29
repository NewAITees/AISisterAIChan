#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
#load "SaveData.csx"
using Shiorose;
using Shiorose.Resource;
using Shiorose.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

partial class AISisterAIChanGhost : Ghost
{
    public override string OnBoot(IDictionary<int, string> references, string shellName = "", bool isHalt = false, string haltGhostName = "")
    {
        var generatedScript = new TalkBuilder()
        .AppendLine("おかえり、おにいちゃん。")
        .BuildWithAutoWait();

        // デバッグ用：起動スクリプトをログ保存
        try
        {
            var encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(generatedScript ?? "");
            var rawBytesStr = bytes.Length > 0 ? string.Join(" ", bytes.Select(b => b.ToString("X2"))) : "";

            var logData = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                mode = "OnBoot",
                generatedScript = generatedScript ?? "",
                scriptLength = generatedScript?.Length ?? 0,
                hasEndTag = generatedScript?.Contains("\\e") ?? false,
                rawBytes = rawBytesStr
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("boot_script_log.json", json, encoding);
        }
        catch { }

        return generatedScript;
    }

    public override string OnFirstBoot(IDictionary<int, string> reference, int vanishCount = 0)
    {
        return new TalkBuilder()
        .AppendLine("おかえり、おにいちゃん。")
        .BuildWithAutoWait();
    }

    public override string OnClose(IDictionary<int, string> reference, string reason = "")
    {
        var generatedScript = new TalkBuilder()
        .Append("また話そうね、おにいちゃん。")
        .EmbedValue("\\-")
        .BuildWithAutoWait();

        // デバッグ用：終了スクリプトをログ保存
        try
        {
            var encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(generatedScript ?? "");
            var rawBytesStr = bytes.Length > 0 ? string.Join(" ", bytes.Select(b => b.ToString("X2"))) : "";

            var logData = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                mode = "OnClose",
                generatedScript = generatedScript ?? "",
                scriptLength = generatedScript?.Length ?? 0,
                hasEndTag = generatedScript?.Contains("\\e") ?? false,
                rawBytes = rawBytesStr
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("close_script_log.json", json, encoding);
        }
        catch { }

        return generatedScript;
    }
}