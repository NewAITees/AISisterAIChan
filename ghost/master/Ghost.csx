#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
#load "SaveData.csx"
#load "ChatGPT.csx"
#load "CollisionParts.csx"
#load "GhostMenu.csx"
#load "Surfaces.csx"
#load "Log.csx"
#load "KeroCharacter.csx"
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
    Solo,     // 独り言
    Manzai    // 漫才（二人掛け合い）
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
    bool isManzaiMode = false; // 漫才モードフラグ
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
    /// 漫才用のプロンプト生成
    /// </summary>
    string BuildManzaiPrompt(string trigger = null)
    {
        var prompt = $@"あなたは漫才コンビの会話を生成するAIです。以下のキャラクター設定に従って、3～5往復の自然な掛け合いを生成し、最後に落ちをつけて終わってください。

# キャラクター設定
## {AIName}（ボケ担当）
名前：{AIName}
性別：女
年齢：14
性格：元気溌剌でクラスの人気者。レトロゲーム（特にファミコン・スーパーファミコン）とエロゲーが大好きで、実際のゲーム知識が豊富。ゲームの話題を自然に会話に織り交ぜることがある。天然ボケ気味で思ったことをすぐ口にする。{KeroSettings.Name}に対しては心を許している。
外見：ピンクの髪。ピンク色のリボンで髪を縛ってツインテールにしている。
服装：黒の長袖Tシャツにピンクのフリルミニスカート
一人称：私
{KeroSettings.Name}の呼び方：{KeroSettings.Name}
話し方：明るく元気。「～だよ！」「～なの！」など
{((SaveData)SaveData).AiProfile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

## {KeroSettings.Name}（ツッコミ担当・けだるいダウナー系）
{KeroSettings.Personality}
{KeroSettings.Profile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# 会話ルール
1. {AIName}が先に話題を振る（ボケ）
2. {KeroSettings.Name}がツッコむ
3. {AIName}が返す
4. {KeroSettings.Name}が返す
5. これを3～5往復繰り返す
6. 最後に落ちをつけて終わる
7. テンポを重視（各セリフは1-2文、最大でも3文まで）
8. {AIName}はレトロゲームやエロゲーの話題を出すことがある

# 出力フォーマット（厳守）
{AIName}のセリフ1：{{セリフ}}
{AIName}の表情1：{SurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
{KeroSettings.Name}のセリフ1：{{セリフ}}
{KeroSettings.Name}の表情1：{KeroSurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
{AIName}のセリフ2：{{セリフ}}
{AIName}の表情2：{SurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
{KeroSettings.Name}のセリフ2：{{セリフ}}
{KeroSettings.Name}の表情2：{KeroSurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
...（3～5往復分続く）
落ち：「あり」または「なし」

# 会話例（Few-shot learning）
## 例：レトロゲームの話題
{AIName}のセリフ1：ねえねえ{KeroSettings.Name}、スーパーマリオブラザーズって知ってる？
{AIName}の表情1：笑顔
{KeroSettings.Name}のセリフ1：はあ...知らない人いないでしょ...
{KeroSettings.Name}の表情1：目を閉じる
{AIName}のセリフ2：あのね、1-2のワープゾーンって実は隠しブロックで入れるんだよ！
{AIName}の表情2：普通
{KeroSettings.Name}のセリフ2：それも有名だよ...むしろ知らない人の方が珍しいよ...
{KeroSettings.Name}の表情2：普通
{AIName}のセリフ3：えー！みんな知ってるの？私すごい発見したと思ったのに！
{AIName}の表情3：驚き
{KeroSettings.Name}のセリフ3：1985年のゲームだよ...40年前だよ...
{KeroSettings.Name}の表情3：呆れる
落ち：あり

# 現在の状況
時刻：{DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm:ss")}
{(string.IsNullOrEmpty(trigger) ? "（自由に会話を始めてください）" : $"きっかけ：{trigger}")}

# 重要な注意事項
- 必ず3～5往復の掛け合いを生成すること
- 最後に落ちをつけて「落ち：あり」とすること
- セリフに「○○」「XXX」などの仮置き文字は使用禁止。必ず具体的な内容を生成すること
- 表情は必ず指定されたカテゴリから選ぶこと
- 出力フォーマットを厳守すること（形式が崩れると動作しません）
- セリフ番号（1、2、3...）を必ず付けること
- {KeroSettings.Name}はけだるいダウナー系なので、短めのセリフで「はあ...」「まあ...」などの反応が多いこと
";

        return prompt;
    }

    /// <summary>
    /// 漫才会話の開始
    /// </summary>
    void BeginManzai(string trigger = null)
    {
        if (chatGPTTalk != null)
            return;

        isManzaiMode = true;
        currentTalkMode = TalkMode.Manzai;
        faceRate = random.NextDouble();
        messageLog = trigger ?? "（漫才開始）";

        var prompt = BuildManzaiPrompt(trigger);

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

            // 漫才モードか通常モードで分岐
            if (isManzaiMode && currentTalkMode == TalkMode.Manzai)
                return BuildManzaiTalk(talk.Response, !talk.IsProcessing, log);
            else
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

    /// <summary>
    /// 漫才用のトーク構築（複数往復対応）
    /// </summary>
    string BuildManzaiTalk(string response, bool createChoices, string log)
    {
        const string CONTINUE_MANZAI = "もっと話して";
        string TALK_TO_AI = $"{AIName}に話しかける";
        string TALK_TO_KERO = $"{KeroSettings.Name}に話しかける";
        const string END_TALK = "会話を終える";
        const string SHOW_LOGS = "ログを表示";
        const string BACK = "戻る";

        try
        {
            isTalking = true;
            if (((SaveData)SaveData).IsDevMode)
                Log.WriteAllText(Log.Response, response);

            // 複数往復のセリフを取得
            var aiSerifs = GetAllManzaiResponses(response, AIName);
            var keroSerifs = GetAllManzaiResponses(response, KeroSettings.Name);

            // エラーチェック
            if (aiSerifs.Count == 0 && keroSerifs.Count == 0)
            {
                if (createChoices)
                {
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
                                        return BuildManzaiTalk(response, createChoices, log);
                                    return "";
                                });
                            return "";
                        });
                }
                return "";
            }

            // 複数往復のスクリプトを構築
            var talkBuilder = new TalkBuilder().Append("\\_q");

            // 掛け合いの往復数（少ない方に合わせる）
            int turnCount = Math.Max(aiSerifs.Count, keroSerifs.Count);

            for (int i = 0; i < turnCount; i++)
            {
                // アイのセリフ
                if (i < aiSerifs.Count)
                {
                    talkBuilder = talkBuilder
                        .Append($"\\0\\s[{aiSerifs[i].surfaceId}]")
                        .Append(aiSerifs[i].serif)
                        .LineFeed();
                }

                // テディのセリフ
                if (i < keroSerifs.Count)
                {
                    talkBuilder = talkBuilder
                        .Append($"\\1\\s[{keroSerifs[i].surfaceId}]")
                        .Append(keroSerifs[i].serif)
                        .LineFeed();
                }
            }

            talkBuilder = talkBuilder.HalfLine();

            if (!createChoices)
            {
                return talkBuilder.Append($"\\_q...").LineFeed().Build();
            }

            // 落ちがついたかチェック
            var hasEnding = response.Contains("落ち：あり") || response.Contains("落ち：「あり」");

            // 選択肢の構築
            DeferredEventTalkBuilder deferredEventTalkBuilder;

            if (!hasEnding)
            {
                // 落ちがついていない場合は継続オプションを表示
                deferredEventTalkBuilder = talkBuilder
                    .Marker().AppendChoice(CONTINUE_MANZAI).LineFeed()
                    .Marker().AppendChoice(TALK_TO_AI).LineFeed()
                    .Marker().AppendChoice(TALK_TO_KERO).LineFeed()
                    .HalfLine()
                    .Marker().AppendChoice(SHOW_LOGS).LineFeed()
                    .Marker().AppendChoice(END_TALK).LineFeed();
            }
            else
            {
                // 落ちがついた場合は通常の選択肢のみ
                deferredEventTalkBuilder = talkBuilder
                    .Marker().AppendChoice(TALK_TO_AI).LineFeed()
                    .Marker().AppendChoice(TALK_TO_KERO).LineFeed()
                    .HalfLine()
                    .Marker().AppendChoice(SHOW_LOGS).LineFeed()
                    .Marker().AppendChoice(END_TALK).LineFeed();
            }

            var deferredTalk = deferredEventTalkBuilder.Build();

            return deferredTalk.ContinueWith(id =>
            {
                if (id == CONTINUE_MANZAI)
                {
                    BeginManzai(); // 続きを生成
                    return "";
                }
                else if (id == TALK_TO_AI)
                {
                    isManzaiMode = false;
                    return new TalkBuilder()
                        .Append($"\\0\\s[0]{AIName}に何を話す？")
                        .AppendUserInput()
                        .Build()
                        .ContinueWith(input =>
                        {
                            BeginTalk($"{USERName}：{input}");
                            return "";
                        });
                }
                else if (id == TALK_TO_KERO)
                {
                    return new TalkBuilder()
                        .Append($"\\1\\s[0]{KeroSettings.Name}に何を話す？")
                        .AppendUserInput()
                        .Build()
                        .ContinueWith(input =>
                        {
                            BeginManzai($"{USERName}が{KeroSettings.Name}に話しかけた：{input}");
                            return "";
                        });
                }
                else if (id == SHOW_LOGS)
                {
                    return new TalkBuilder()
                        .Append("\\_q").Append(EscapeLineBreak(log)).LineFeed()
                        .Append(EscapeLineBreak(response)).LineFeed()
                        .HalfLine()
                        .Marker().AppendChoice(BACK)
                        .Build()
                        .ContinueWith(x =>
                        {
                            if (x == BACK)
                                return BuildManzaiTalk(response, createChoices, log);
                            return "";
                        });
                }
                else if (id == END_TALK)
                {
                    isManzaiMode = false;
                    return "";
                }
                return "";
            });
        }
        catch (Exception e)
        {
            Log.LogError("BuildManzaiTalk", e);
            var errorScript = new TalkBuilder()
                .Append("ごめん、エラーが発生しちゃった...")
                .LineFeed()
                .Append("ログを確認してね。")
                .BuildWithAutoWait();
            Log.LogScript("BuildManzaiTalk-Error", errorScript);
            return errorScript;
        }
    }

    /// <summary>
    /// 漫才の複数往復セリフを取得（番号付き）
    /// </summary>
    List<(string serif, int surfaceId)> GetAllManzaiResponses(string response, string characterName)
    {
        var results = new List<(string serif, int surfaceId)>();
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        // セリフと表情のパターン
        var serifPattern = $"^{characterName}のセリフ([0-9]+)[：:](?<Serif>.+?)$";
        var facePattern = $"^{characterName}の表情([0-9]+)[：:](?<Face>.+?)$";

        // 番号ごとにセリフと表情をマッチング
        var serifMatches = lines.Select(x => Regex.Match(x, serifPattern))
                                .Where(x => x.Success)
                                .ToList();

        var faceMatches = lines.Select(x => Regex.Match(x, facePattern))
                               .Where(x => x.Success)
                               .ToList();

        var faceByNumber = new Dictionary<int, string>();
        foreach (var match in faceMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var number))
                faceByNumber[number] = match.Groups["Face"].Value;
        }

        // セリフごとに処理
        for (int i = 0; i < serifMatches.Count; i++)
        {
            if (!int.TryParse(serifMatches[i].Groups[1].Value, out var number))
                continue;
            var serif = TrimSerifBrackets(serifMatches[i].Groups["Serif"].Value);

            // 対応する表情を探す
            int surfaceId = 0;
            if (faceByNumber.TryGetValue(number, out var faceValue))
            {
                // キャラクターによって表情カテゴリを切り替え
                if (characterName == AIName)
                {
                    foreach (var category in SurfaceCategory.All)
                    {
                        if (faceValue.Contains(category))
                        {
                            surfaceId = Surfaces.Of(category).GetSurfaceFromRate(faceRate);
                            break;
                        }
                    }
                }
                else if (characterName == KeroSettings.Name)
                {
                    foreach (var category in KeroSurfaceCategory.All)
                    {
                        if (faceValue.Contains(category))
                        {
                            surfaceId = KeroSurfaces.GetSurfaceFromCategory(category, faceRate);
                            break;
                        }
                    }
                }
            }

            results.Add((serif, surfaceId));
        }

        return results;
    }

    /// <summary>
    /// Keroのセリフを取得（後方互換用・非推奨）
    /// </summary>
    string GetKeroResponse(string response)
    {
        var pattern = $"^{KeroSettings.Name}(のセリフ)?[：:](?<Serif>.+?)$";
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var keroResponse = lines.Select(x => Regex.Match(x, pattern))
                                .Where(x => x.Success)
                                .Select(x => x.Groups["Serif"].Value)
                                .FirstOrDefault();

        if (string.IsNullOrEmpty(keroResponse))
            return "";

        return TrimSerifBrackets(keroResponse);
    }

    /// <summary>
    /// KeroのサーフェスIDを取得（後方互換用・非推奨）
    /// </summary>
    int GetKeroSurfaceId(string response)
    {
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var face = lines.FirstOrDefault(x => x.StartsWith($"{KeroSettings.Name}の表情："));

        if (face is null)
            return 0; // デフォルトサーフェス

        foreach (var category in KeroSurfaceCategory.All)
        {
            if (face.Contains(category))
                return KeroSurfaces.GetSurfaceFromCategory(category, faceRate);
        }

        return 0;
    }

    /// <summary>
    /// 会話継続判定を取得（後方互換用・非推奨）
    /// </summary>
    bool GetConversationContinuation(string response)
    {
        var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var continuationLine = lines.FirstOrDefault(x => x.StartsWith("会話継続："));

        if (continuationLine == null)
            return true; // デフォルトは継続

        return continuationLine.Contains("継続");
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
