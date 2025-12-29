#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
#load "SaveData.csx"
#load "Log.csx"
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
        try
        {
            Log.LogEvent("OnBoot", $"shellName={shellName}, isHalt={isHalt}, haltGhostName={haltGhostName}");

            var generatedScript = new TalkBuilder()
            .AppendLine("おかえり、おにいちゃん。")
            .BuildWithAutoWait();

            Log.LogScript("OnBoot", generatedScript);

            // デバッグ用：起動スクリプトをJSONログとして保存
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
                File.WriteAllText("log/boot_script_log.json", json, encoding);
            }
            catch (Exception ex)
            {
                Log.LogError("OnBoot JSON logging", ex);
            }

            return generatedScript;
        }
        catch (Exception ex)
        {
            Log.LogError("OnBoot", ex);
            return "";
        }
    }

    public override string OnFirstBoot(IDictionary<int, string> reference, int vanishCount = 0)
    {
        try
        {
            Log.LogEvent("OnFirstBoot", $"vanishCount={vanishCount}");

            var generatedScript = new TalkBuilder()
            .AppendLine("おかえり、おにいちゃん。")
            .BuildWithAutoWait();

            Log.LogScript("OnFirstBoot", generatedScript);
            return generatedScript;
        }
        catch (Exception ex)
        {
            Log.LogError("OnFirstBoot", ex);
            return "";
        }
    }

    public override string OnClose(IDictionary<int, string> reference, string reason = "")
    {
        try
        {
            Log.LogEvent("OnClose", $"reason={reason}");

            var generatedScript = new TalkBuilder()
            .Append("また話そうね、おにいちゃん。")
            .EmbedValue("\\-")
            .BuildWithAutoWait();

            Log.LogScript("OnClose", generatedScript);

            // デバッグ用：終了スクリプトをJSONログとして保存
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
                File.WriteAllText("log/close_script_log.json", json, encoding);
            }
            catch (Exception ex)
            {
                Log.LogError("OnClose JSON logging", ex);
            }

            return generatedScript;
        }
        catch (Exception ex)
        {
            Log.LogError("OnClose", ex);
            return "";
        }
    }
}