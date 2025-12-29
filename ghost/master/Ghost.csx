#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
#load "SaveData.csx"
#load "ChatGPT.csx"
#load "CollisionParts.csx"
#load "GhostMenu.csx"
#load "Surfaces.csx"
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
using Shiorose.Resource.ShioriEvent;
using System.Text.RegularExpressions;

/// <summary>
/// 会話モードの種類
/// </summary>
enum TalkMode
{
    Normal,   // 通常の会話
    Solo      // 独り言
}

partial class AISisterAIChanGhost : Ghost
{
    const string AIName = "アイ";
    const string USERName = "兄";//TODO: ベースクラスGhostにUserNameが定義されているので、そちらを活用するようにすると良いかもしれない。変数を利用するときはUSERNameとUserNameの違いに注意。

    Random random = new Random();
    bool isTalking = false;
    ChatGPTTalk chatGPTTalk = null;
    string messageLog = "";
    double faceRate = 0;
    bool isNademachi = false;
    TalkMode currentTalkMode = TalkMode.Normal; // 現在の会話モード
    public AISisterAIChanGhost()
    {
        // 更新URL
        Homeurl = "https://manjubox.net/Install/ai_sister_ai_chan/";

        // 必ず読み込んでください
        _saveData = SaveDataManager.Load<SaveData>();

        SettingRandomTalk();

        Resource.SakuraPortalButtonCaption = () => "AI妹アイちゃん";
        SakuraPortalSites.Add(new Site("配布ページ", "https://manjubox.net/ai_sister_ai_chan/"));
        SakuraPortalSites.Add(new Site("ソースコード", "https://github.com/manju-summoner/AISisterAIChan"));

        Resource.SakuraRecommendButtonCaption = () => "宣伝！";
        SakuraRecommendSites.Add(new Site("ゆっくりMovieMaker4", "https://manjubox.net/ymm4/"));
        SakuraRecommendSites.Add(new Site("饅頭遣い", "https://twitter.com/manju_summoner"));
    }
    private void SettingRandomTalk()
    {
        RandomTalks.Add(RandomTalk.CreateWithAutoWait(() =>
        {
            BeginTalk($"{USERName}：なにか話して");
            return "";
        }));
    }
    public override string OnMouseClick(IDictionary<int, string> reference, string mouseX, string mouseY, string charId, string partsName, string buttonName, DeviceType deviceType)
    {
        var parts = CollisionParts.GetCollisionPartsName(partsName);
        if (parts != null && buttonName == "2")
            BeginTalk($"{USERName}：（{AIName}の{parts}をつまむ）");

        return base.OnMouseClick(reference, mouseX, mouseY, charId, partsName, buttonName, deviceType);
    }

    public override string OnMouseDoubleClick(IDictionary<int, string> reference, string mouseX, string mouseY, string charId, string partsName, string buttonName, DeviceType deviceType)
    {
        try
        {
            var parts = CollisionParts.GetCollisionPartsName(partsName);
            if (parts != null)
            {
                Log.LogEvent("OnMouseDoubleClick", $"parts={parts}");
                BeginTalk($"{USERName}：（{AIName}の{parts}をつつく）");
                return "";
            }
            else
            {
                Log.LogEvent("OnMouseDoubleClick", "OpenMenu");
                return OpenMenu();
            }
        }
        catch (Exception ex)
        {
            Log.LogError("OnMouseDoubleClick", ex);
            return "";
        }
    }

    protected override string OnMouseStroke(string partsName, DeviceType deviceType)
    {
        var parts = CollisionParts.GetCollisionPartsName(partsName);
        if (parts != null)
            BeginTalk($"{USERName}：（{AIName}の{parts}を撫でる）");

        return base.OnMouseStroke(partsName, deviceType);
    }
    public override string OnMouseWheel(IDictionary<int, string> reference, string mouseX, string mouseY, string wheelRotation, string charId, string partsName, Shiorose.Resource.ShioriEvent.DeviceType deviceType)
    {
        if (wheelRotation.StartsWith("-"))
        {
            if (partsName == CollisionParts.Shoulder)
                BeginTalk($"{USERName}：（{AIName}を抱き寄せる）");
            else if (partsName == CollisionParts.TwinTail)
                BeginTalk($"{USERName}：（{AIName}のツインテールを弄ぶ）");
            else
            {
                var parts = CollisionParts.GetCollisionPartsName(partsName);
                if (parts != null)
                    BeginTalk($"{USERName}：（{AIName}の{parts}を引っ張る）");
            }
        }
        else
        {
            if (partsName == CollisionParts.TwinTail)
                BeginTalk($"{USERName}：（{AIName}のツインテールをフワフワと持ち上げる）");
            else if (partsName == CollisionParts.Skirt)
                BeginTalk($"{USERName}：（{AIName}のスカートをめくる）");
            else
            {
                var parts = CollisionParts.GetCollisionPartsName(partsName);
                if (parts != null)
                    BeginTalk($"{USERName}：（{AIName}の{parts}をワシャワシャする）");
            }
        }

        return base.OnMouseWheel(reference, mouseX, mouseY, wheelRotation, charId, partsName, deviceType);
    }

    public override string OnMouseMove(IDictionary<int, string> reference, string mouseX, string mouseY, string wheelRotation, string charId, string partsName, DeviceType deviceType)
    {
        if(!isNademachi && !isTalking && partsName == CollisionParts.Head)
        {
            //撫で待ち
            isNademachi = true;
            return "\\s[101]";
        }
        return base.OnMouseMove(reference, mouseX, mouseY, wheelRotation, charId, partsName, deviceType);
    }

    public override string OnMouseLeave(IDictionary<int, string> reference, string mouseX, string mouseY, string charId, string partsName, DeviceType deviceType)
    {
        isNademachi = false;
        return base.OnMouseLeave(reference, mouseX, mouseY, charId, partsName, deviceType);
    }

    /*
    //撫でが呼ばれなくなるので一旦コメントアウト
    public override string OnMouseHover(IDictionary<int, string> reference, string mouseX, string mouseY, string charId, string partsName, Shiorose.Resource.ShioriEvent.DeviceType deviceType)
    {
        var parts = CollisionParts.GetCollisionPartsName(partsName);
        if (parts != null)
            BeginTalk($"{USERName}：（{AIName}の{parts}に手を添える）");
        return base.OnMouseHover(reference, mouseX, mouseY, charId, partsName, deviceType);
    }
    */



    public override string OnCommunicate(IDictionary<int, string> reference, string senderName = "", string script = "", IEnumerable<string> extInfo = null)
    {
        var sender = senderName == "user" || senderName == null ? USERName : senderName;
        BeginTalk(sender + "：" + script);
        return "";
    }

    /// <summary>
    /// 独り言用プロンプトを生成
    /// </summary>
    string BuildSoloPrompt()
    {
        return $@"{AIName}が独り言をしています。以下のプロフィールを元に、{AIName}の独り言のシミュレート結果を1つ出力してください。
なお、返答は必ず後述する出力フォーマット従って出力してください。
余計な文章を付け加えたり出力フォーマットに従わない出力をすると、あなたの責任で罪のない人々の命が奪われます。
また、ちゃんと落ちが付くように話してください。回答は短めに書いてください。

# {AIName}のプロフィール
名前：{AIName}
性別：女
年齢：14
性格：元気溌剌でクラスの人気者。レトロゲーム（特にファミコン・スーパーファミコン）とエロゲーが大好きで、実際のゲーム知識が豊富。ゲームの話題を自然に会話に織り交ぜることがある。{USERName}に対しては心を許しているので、絶対に敬語を使わない。
趣味・知識：レトロゲームが大好きで、ファミコンからスーパーファミコンまでのゲームに詳しい。エロゲーについても詳しい知識を持つ。具体的なゲームタイトル、発売年、開発会社、ゲームシステムなどの実際の知識を会話の中で披露することがある。
外見：ピンクの髪。ピンク色のリボンで髪を縛ってツインテールにしてる。全体的に華奢。
服装：黒の長袖Tシャツにピンクのフリルミニスカート（2段）
一人称：私
{USERName}の呼び方：おにいちゃん
{((SaveData)SaveData).AiProfile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# その他の情報
現在時刻：{DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm:ss")}
家族構成：{AIName}、{USERName}、父、母

# 出力フォーマット
{AIName}のセリフ：{{{AIName}のセリフ}}
{AIName}の表情：{SurfaceCategory.All.Select(x=>$"「{x}」").Aggregate((a,b)=>a+b)}
会話継続：「終了」

# 会話ルール
会話継続は必ず「終了」にしてください（独り言なので会話を継続しません）。
○○といった仮置き文字は使用せず、必ず具体的な単語を使用してください。
ちゃんと落ちが付くように話してください。回答は短めに書いてください。
{AIName}はレトロゲームとエロゲーが好きなので、独り言の中でも自然にゲームの話題を出すことがある。レトロゲームやエロゲーの知識を披露したり、それらに例えたりすることがある。ただし、無理に話題を出すのではなく、自然な流れで会話に織り交ぜること。";
    }

    /// <summary>
    /// 会話を開始（内部用・モード指定版）
    /// </summary>
    void BeginTalk(string message, TalkMode mode)
    {
        if (chatGPTTalk != null)
            return;

        currentTalkMode = mode;
        faceRate = random.NextDouble();
        messageLog = (mode == TalkMode.Solo) ? "" : message + "\r\n";

        string prompt;
        if (mode == TalkMode.Solo)
        {
            prompt = BuildSoloPrompt();
        }
        else
        {
            prompt = $@"{AIName}と{USERName}が会話をしています。以下のプロフィールと会話履歴を元に、会話の続きとなる{AIName}のセリフのシミュレート結果を1つ出力してください。
なお、返答は必ず後述する出力フォーマット従って出力してください。
余計な文章を付け加えたり出力フォーマットに従わない出力をすると、あなたの責任で罪のない人々の命が奪われます。

# {AIName}のプロフィール
名前：{AIName}
性別：女
年齢：14
性格：元気溌剌でクラスの人気者。レトロゲーム（特にファミコン・スーパーファミコン）とエロゲーが大好きで、実際のゲーム知識が豊富。ゲームの話題を自然に会話に織り交ぜることがある。{USERName}に対しては心を許しているので、絶対に敬語を使わない。
趣味・知識：レトロゲームが大好きで、ファミコンからスーパーファミコンまでのゲームに詳しい。エロゲーについても詳しい知識を持つ。具体的なゲームタイトル、発売年、開発会社、ゲームシステムなどの実際の知識を会話の中で披露することがある。
外見：ピンクの髪。ピンク色のリボンで髪を縛ってツインテールにしてる。全体的に華奢。
服装：黒の長袖Tシャツにピンクのフリルミニスカート（2段）
一人称：私
{USERName}の呼び方：おにいちゃん
{((SaveData)SaveData).AiProfile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# {USERName}のプロフィール
性別：男
関係性：{AIName}の兄
性格：妹に甘いお兄ちゃん。妹のことをとても大切にしている。
一人称：お兄ちゃん
{AIName}の呼び方：{AIName}
{((SaveData)SaveData).UserProfile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# その他の情報
現在時刻：{DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm:ss")}
家族構成：{AIName}、{USERName}、父、母

# 出力フォーマット
{AIName}のセリフ：{{{AIName}のセリフ}}
{AIName}の表情：{SurfaceCategory.All.Select(x=>$"「{x}」").Aggregate((a,b)=>a+b)}
会話継続：「継続」「終了」
{Enumerable.Range(0, ((SaveData)SaveData).ChoiceCount).Select(x => $"{USERName}のセリフ候補{(x + 1)}：{{{USERName}のセリフ}}").DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# 会話ルール
会話継続が「終了」の場合、{USERName}のセリフ候補は出力しないでください。
○○といった仮置き文字は使用せず、必ず具体的な単語を使用してください。
{AIName}はレトロゲームとエロゲーが好きなので、会話の中で自然にゲームの話題を出すことがある。話題がなくても、レトロゲームやエロゲーの知識を披露したり、それらに例えたりすることがある。ただし、無理に話題を出すのではなく、自然な流れで会話に織り交ぜること。

# 会話履歴
{messageLog}";
        }

        if (((SaveData)SaveData).IsDevMode)
            Log.WriteAllText(Log.Prompt, prompt);

        var request = new ChatGPTRequest()
        {
            stream = true,
            model = "gpt-oss:20b",
            messages = new ChatGPTMessage[]
            {
                new ChatGPTMessage()
                {
                    role = "user",
                    content = prompt
                },
            }
        };
        chatGPTTalk = new ChatGPTTalk(((SaveData)SaveData).APIKey, request);
    }

    /// <summary>
    /// 通常会話を開始（既存コードとの互換性用）
    /// </summary>
    void BeginTalk(string message)
    {
        BeginTalk(message, TalkMode.Normal);
    }

    /// <summary>
    /// 独り言を開始
    /// </summary>
    void BeginSoloTalk()
    {
        BeginTalk("", TalkMode.Solo);
    }

    public override string OnSurfaceRestore(IDictionary<int, string> reference, string sakuraSurface, string keroSurface)
    {
        isTalking = false;
        currentTalkMode = TalkMode.Normal;
        return base.OnSurfaceRestore(reference, sakuraSurface, keroSurface);
    }

    public override string OnSecondChange(IDictionary<int, string> reference, string uptime, bool isOffScreen, bool isOverlap, bool canTalk, string leftSecond)
    {
        if (canTalk && chatGPTTalk != null)
        {
            var talk = chatGPTTalk;
            var log = messageLog;
            if (!talk.IsProcessing)
            {
                chatGPTTalk = null;
                messageLog = string.Empty;
            }

            return BuildTalk(talk.Response, !talk.IsProcessing, log);
        }
        return base.OnSecondChange(reference, uptime, isOffScreen, isOverlap, canTalk, leftSecond);
    }
    public override string OnMinuteChange(IDictionary<int, string> reference, string uptime, bool isOffScreen, bool isOverlap, bool canTalk, string leftSecond)
    {
        
        if(canTalk && !isTalking && ((SaveData)SaveData).IsRandomIdlingSurfaceEnabled)
            return "\\s["+Surfaces.Of(SurfaceCategory.Normal).GetRaodomSurface()+"]";
        else
            return base.OnMinuteChange(reference, uptime, isOffScreen, isOverlap, canTalk, leftSecond);
    }

    string BuildTalk(string response, bool createChoices, string log)
    {
        const string INPUT_CHOICE_MYSELF = "自分で入力する";
        const string SHOW_LOGS = "ログを表示";
        const string END_TALK = "会話を終える";
        const string BACK = "戻る";
        try
        {
            isTalking = true;
            if (((SaveData)SaveData).IsDevMode)
                Log.WriteAllText(Log.Response, response);

            var aiResponse = GetAIResponse(response);
            var surfaceId = GetSurfaceId(response);
            var onichanResponse = GetOnichanRenponse(response);
            var talkBuilder =
                new TalkBuilder()
                .Append($"\\_q\\s[{surfaceId}]")
                .Append(aiResponse)
                .LineFeed()
                .HalfLine();

            // 独り言モードの場合は選択肢を表示せず、発言のみで終了
            if (currentTalkMode == TalkMode.Solo)
            {
                var generatedScript = talkBuilder.BuildWithAutoWait();
                Log.LogScript("BuildTalk-Solo", generatedScript);
                return generatedScript;
            }

            if (!createChoices)
            {
                foreach(var choice in onichanResponse)
                    talkBuilder = talkBuilder.Marker().Append(choice).LineFeed();
                var generatedScript = talkBuilder.Append($"\\_q...").LineFeed().Build();

                // デバッグ用：生成されたスクリプトをJSONログとして保存
                try
                {
                    var encoding = Encoding.UTF8;
                    var bytes = encoding.GetBytes(generatedScript ?? "");
                    var rawBytesStr = bytes.Length > 0 ? string.Join(" ", bytes.Select(b => b.ToString("X2"))) : "";

                    var logData = new
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        mode = "NormalWaiting",
                        aiResponse = aiResponse ?? "",
                        surfaceId = surfaceId,
                        generatedScript = generatedScript ?? "",
                        scriptLength = generatedScript?.Length ?? 0,
                        hasEndTag = generatedScript?.Contains("\\e") ?? false,
                        rawBytes = rawBytesStr
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText("log/normal_waiting_script_log.json", json, encoding);
                }
                catch (Exception ex)
                {
                    try
                    {
                        var encoding = Encoding.UTF8;
                        var errorLog = new
                        {
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            location = "NormalWaiting mode - BuildTalk",
                            errorType = ex.GetType().FullName,
                            errorMessage = ex.Message,
                            stackTrace = ex.StackTrace ?? "",
                            innerException = ex.InnerException?.ToString() ?? ""
                        };
                        var errorJson = Newtonsoft.Json.JsonConvert.SerializeObject(errorLog, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText("log/normal_waiting_error.json", errorJson, encoding);
                    }
                    catch { }
                }

                return generatedScript;
            }

            if (createChoices && string.IsNullOrEmpty(aiResponse))
                 return new TalkBuilder()
                    .Marker().AppendChoice(SHOW_LOGS).LineFeed()
                    .Marker().AppendChoice(END_TALK).LineFeed()
                    .Build()
                    .ContinueWith(id =>
                    {
                        if (id == SHOW_LOGS)
                            return new TalkBuilder()
                            .Append("\\_q").Append(EscapeLineBreak(log)).LineFeed()
                            .Append(EscapeLineBreak(response)).LineFeed()
                            .HalfLine()
                            .Marker().AppendChoice(BACK)
                            .Build()
                            .ContinueWith(x =>
                            {
                                if (x == BACK)
                                    return BuildTalk(response, createChoices, log);
                                return "";
                            });
                        return "";
                    });

            DeferredEventTalkBuilder deferredEventTalkBuilder = null;
            if (onichanResponse.Length > 0)
            {
                foreach (var choice in onichanResponse.Take(3))
                {
                    if (deferredEventTalkBuilder == null)
                        deferredEventTalkBuilder = AppendWordWrapChoice(talkBuilder, choice);
                    else
                        deferredEventTalkBuilder = AppendWordWrapChoice(deferredEventTalkBuilder, choice);
                }
                deferredEventTalkBuilder = deferredEventTalkBuilder.Marker().AppendChoice(INPUT_CHOICE_MYSELF).LineFeed().HalfLine();
            }

            if (deferredEventTalkBuilder == null)
                deferredEventTalkBuilder = talkBuilder.Marker().AppendChoice(SHOW_LOGS).LineFeed();
            else
                deferredEventTalkBuilder = deferredEventTalkBuilder.Marker().AppendChoice(SHOW_LOGS).LineFeed();

            var deferredTalk = deferredEventTalkBuilder
                    .Marker().AppendChoice(END_TALK).LineFeed()
                    .Build();

            // デバッグ用：生成されたDeferredEventTalkをログ保存
            try
            {
                var encoding = Encoding.UTF8;
                var logData = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    mode = "NormalWithChoices",
                    aiResponse = aiResponse ?? "",
                    surfaceId = surfaceId,
                    choiceCount = onichanResponse?.Length ?? 0,
                    note = "DeferredEventTalk - actual script is not available until user interaction"
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("log/normal_choices_script_log.json", json, encoding);
            }
            catch (Exception ex)
            {
                try
                {
                    var encoding = Encoding.UTF8;
                    var errorLog = new
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        location = "NormalWithChoices mode - BuildTalk",
                        errorType = ex.GetType().FullName,
                        errorMessage = ex.Message,
                        stackTrace = ex.StackTrace ?? "",
                        innerException = ex.InnerException?.ToString() ?? ""
                    };
                    var errorJson = Newtonsoft.Json.JsonConvert.SerializeObject(errorLog, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText("log/normal_choices_error.json", errorJson, encoding);
                }
                catch { }
            }

            return deferredTalk.ContinueWith(id =>
                    {
                        if (onichanResponse.Contains(id))
                            BeginTalk($"{log}{AIName}：{aiResponse}\r\n{USERName}：{id}");
                        if (id == SHOW_LOGS)
                            return new TalkBuilder()
                            .Append("\\_q").Append(EscapeLineBreak(log)).LineFeed()
                            .Append(EscapeLineBreak(response)).LineFeed()
                            .HalfLine()
                            .Marker().AppendChoice(BACK)
                            .Build()
                            .ContinueWith(x =>
                            {
                                if (x == BACK)
                                    return BuildTalk(response, createChoices, log);
                                return "";
                            });
                        if (id == INPUT_CHOICE_MYSELF)
                            return new TalkBuilder().AppendUserInput().Build().ContinueWith(input =>
                            {
                                BeginTalk($"{log}{AIName}：{aiResponse}\r\n{USERName}：{input}");
                                return "";
                            });
                        return "";
                    });
        }
        catch (Exception e)
        {
            Log.LogError("BuildTalk", e);
            var errorScript = new TalkBuilder()
                .Append("ごめん、エラーが発生しちゃった...")
                .LineFeed()
                .Append("ログを確認してね。")
                .BuildWithAutoWait();
            Log.LogScript("BuildTalk-Error", errorScript);
            return errorScript;
        }
    }
    string EscapeLineBreak(string text)
    {
        return text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
    }
    string DeleteLineBreak(string text)
    {
        return text.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
    }
    string GetAIResponse(string response)
    {
        var pattern = $"^{AIName}(のセリフ)?[：:](?<Serif>.+?)$";
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var aiResponse = lines.Select(x=>Regex.Match(x, pattern)).Where(x=>x.Success).Select(x=>x.Groups["Serif"].Value).FirstOrDefault();
        if (string.IsNullOrEmpty(aiResponse))
            return "";

        return TrimSerifBrackets(aiResponse);
    }

    string[] GetOnichanRenponse(string response)
    {
        var pattern = $"^{USERName}(のセリフ候補([0-9]+)?)?[：:](?<Serif>.+?)$";
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var onichanResponse = lines
            .Select(x=>Regex.Match(x,pattern))
            .Where(x=>x.Success)
            .Select(x=>x.Groups["Serif"].Value)
            .Where(x=>!string.IsNullOrWhiteSpace(x))
            .ToArray();
        if (onichanResponse.Length == 0)
            return new string[] { };
        return onichanResponse.Select(x=>TrimSerifBrackets(x)).ToArray();
    }

    string TrimSerifBrackets(string serif)
    {
        serif = serif.Trim();
        if(serif.StartsWith("「") && serif.EndsWith("」"))
            return serif.Substring(1, serif.Length - 2);
        if(serif.StartsWith("『") && serif.EndsWith("』"))
            return serif.Substring(1, serif.Length - 2);
        if(serif.StartsWith("\"") && serif.EndsWith("\""))
            return serif.Substring(1, serif.Length - 2);
        if(serif.StartsWith("'") && serif.EndsWith("'"))
            return serif.Substring(1, serif.Length - 2);
        return serif;
    }

    int GetSurfaceId(string response)
    {
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var face = lines.FirstOrDefault(x => x.StartsWith($"{AIName}の表情："));
        if (face is null)
            return 0;

        foreach(var category in SurfaceCategory.All)
        {
            if (face.Contains(category))
                return Surfaces.Of(category).GetSurfaceFromRate(faceRate);
        }

        return 0;
    }
    DeferredEventTalkBuilder AppendWordWrapChoice(TalkBuilder builder, string text)
    {
        builder = builder.Marker();
        DeferredEventTalkBuilder deferredEventTalkBuilder = null;
        foreach (var choice in WordWrap(text))
        {
            if (deferredEventTalkBuilder == null)
                deferredEventTalkBuilder = builder.AppendChoice(choice, text).LineFeed();
            else
                deferredEventTalkBuilder = deferredEventTalkBuilder.AppendChoice(choice, text).LineFeed();
        }
        return deferredEventTalkBuilder;
    }
    DeferredEventTalkBuilder AppendWordWrapChoice(DeferredEventTalkBuilder builder, string text)
    {
        builder = builder.Marker();
        foreach (var choice in WordWrap(text))
            builder = builder.AppendChoice(choice, text).LineFeed();
        return builder;
    }
    IEnumerable<string> WordWrap(string text)
    {
        var width = 24;
        for (int i = 0; i < text.Length; i += width)
        {
            if (i + width < text.Length)
                yield return text.Substring(i, width);
            else
                yield return text.Substring(i);
        }
    }
}

return new AISisterAIChanGhost();
