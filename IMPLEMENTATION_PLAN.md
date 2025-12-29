# AI妹アイちゃん 機能拡張実装計画書

**作成日:** 2025年12月29日
**対象プロジェクト:** AI妹アイちゃん (Ollama版)
**計画期間:** 約5ヶ月（20週間）

---

## 📋 目次

1. [概要](#概要)
2. [フェーズ1: 単体会話モード](#フェーズ1-単体会話モード)
3. [フェーズ2: コンビ漫才モード](#フェーズ2-コンビ漫才モード)
4. [フェーズ3: Live2D/3D対応](#フェーズ3-live2d3d対応)
5. [総合スケジュール](#総合スケジュール)
6. [技術参考資料](#技術参考資料)

---

## 概要

### 実装する3つの主要機能

| 機能 | 優先度 | 実装期間 | 技術的難易度 |
|------|--------|----------|--------------|
| 💬 単体会話モード | 最高 | 4-6日 | 低 |
| 🎭 コンビ漫才モード | 中 | 13-17日 | 中 |
| 🎨 Live2D/3D対応 | 低 | 5-7週間 | 高 |

### 実装順序の根拠

```
単体会話モード (基盤)
    ↓
    ├─→ コンビ漫才モード (ロジック拡張)
    │       ↓
    └─────→ Live2D/3D対応 (ビジュアル拡張)
```

- **単体会話モード**は他機能の基礎となる会話履歴管理を確立
- **コンビ漫才モード**は単体会話の仕組みを2キャラクターに拡張
- **Live2D/3D対応**はロジックが完成してからビジュアルを強化

---

## フェーズ1: 単体会話モード

### 🎯 目標

ユーザー不在でもキャラクターが自発的に会話を続ける「環境音モード」の実装

### 📊 実装期間

**4-6日**

### 🔍 技術調査結果

#### Ollama会話履歴管理のベストプラクティス

**会話履歴の形式:**
```javascript
[
  {role: "user", content: "こんにちは"},
  {role: "assistant", content: "こんにちは。何かお手伝いできることはありますか？"}
]
```

**コンテキストウィンドウ設定:**
- `num_ctx`パラメータで32,000トークン以上推奨
- メモリ使用量はモデルサイズの約1.5倍

**ストリーミング機能:**
- 既存実装（ChatGPT.csx）でストリーミング対応済み
- `stream=true`により逐次応答受信可能

**参考資料:**
- [PowerShell × Ollama：会話履歴管理の実装](https://qiita.com/Tadataka_Takahashi/items/4318182e2e35fb7b4737)
- [Ollamaストリーミング応答の活用方法](https://apidog.com/jp/blog/ollama-streaming-responses-and-tool-calling-jp/)
- [ブラウザ上でローカルLLMと対話：対話履歴の追加](https://kobayashinote.com/browser-localllm4-dialogue/)

### 📝 実装タスク

#### タスク1.1: 会話履歴管理システム（2-3日）

**新規ファイル:** `ghost/master/ConversationHistory.csx`

**実装内容:**
```csharp
#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ConversationHistory
{
    private List<ChatGPTMessage> messages = new List<ChatGPTMessage>();
    private const int MAX_HISTORY = 20; // メモリ節約のため上限設定
    private const string HISTORY_FILE = "conversation_history.json";

    /// <summary>
    /// ユーザーメッセージを追加
    /// </summary>
    public void AddUserMessage(string content)
    {
        messages.Add(new ChatGPTMessage
        {
            role = "user",
            content = content
        });
        TrimHistory();
    }

    /// <summary>
    /// アシスタントメッセージを追加
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        messages.Add(new ChatGPTMessage
        {
            role = "assistant",
            content = content
        });
        TrimHistory();
    }

    /// <summary>
    /// 最近のN件のメッセージを取得
    /// </summary>
    public ChatGPTMessage[] GetRecentMessages(int count = 10)
    {
        return messages.Skip(Math.Max(0, messages.Count - count)).ToArray();
    }

    /// <summary>
    /// 全メッセージを取得（Ollama API用）
    /// </summary>
    public ChatGPTMessage[] GetAllMessages()
    {
        return messages.ToArray();
    }

    /// <summary>
    /// 履歴をクリア
    /// </summary>
    public void Clear()
    {
        messages.Clear();
    }

    /// <summary>
    /// 履歴をファイルに保存
    /// </summary>
    public void SaveToFile()
    {
        var json = JsonConvert.SerializeObject(messages);
        File.WriteAllText(HISTORY_FILE, json);
    }

    /// <summary>
    /// 履歴をファイルから読み込み
    /// </summary>
    public void LoadFromFile()
    {
        if (File.Exists(HISTORY_FILE))
        {
            var json = File.ReadAllText(HISTORY_FILE);
            messages = JsonConvert.DeserializeObject<List<ChatGPTMessage>>(json);
        }
    }

    /// <summary>
    /// 履歴を上限数に制限
    /// </summary>
    private void TrimHistory()
    {
        if (messages.Count > MAX_HISTORY)
        {
            messages.RemoveRange(0, messages.Count - MAX_HISTORY);
        }
    }

    /// <summary>
    /// 会話履歴を要約形式で取得（プロンプト用）
    /// </summary>
    public string GetHistorySummary()
    {
        return string.Join("\r\n", messages.Select(m =>
            $"{(m.role == "user" ? "兄" : "アイ")}：{m.content}"
        ));
    }
}
```

**変更箇所:**

1. **SaveData.csx** - 会話履歴有効化フラグ追加:
```csharp
[DataMember]
public bool IsConversationHistoryEnabled { get; set; } = true;
```

2. **Ghost.csx** - ConversationHistoryの統合:
```csharp
#load "ConversationHistory.csx"

partial class AISisterAIChanGhost : Ghost
{
    ConversationHistory conversationHistory = new ConversationHistory();

    public AISisterAIChanGhost()
    {
        // 既存コンストラクタ内容...

        // 会話履歴の読み込み
        if (((SaveData)SaveData).IsConversationHistoryEnabled)
        {
            conversationHistory.LoadFromFile();
        }
    }

    void BeginTalk(string message)
    {
        if (chatGPTTalk != null)
            return;

        faceRate = random.NextDouble();

        // 会話履歴に追加
        if (((SaveData)SaveData).IsConversationHistoryEnabled)
        {
            conversationHistory.AddUserMessage(message);
        }

        messageLog = message + "\r\n";

        var prompt = BuildPromptWithHistory(message);

        // 既存のOllama APIリクエスト処理...
        var request = new ChatGPTRequest()
        {
            stream = true,
            model = "gpt-oss:20b",
            messages = ((SaveData)SaveData).IsConversationHistoryEnabled
                ? conversationHistory.GetAllMessages()
                : new ChatGPTMessage[]
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

    string BuildPromptWithHistory(string currentMessage)
    {
        // 既存のプロンプトベース
        var basePrompt = $@"{AIName}と{USERName}が会話をしています。...";

        if (((SaveData)SaveData).IsConversationHistoryEnabled &&
            conversationHistory.GetAllMessages().Length > 0)
        {
            basePrompt += $@"

# これまでの会話履歴
{conversationHistory.GetHistorySummary()}

# 現在の発言
{currentMessage}";
        }

        return basePrompt;
    }
}
```

#### タスク1.2: 自動会話開始ロジック（1-2日）

**新規ファイル:** `ghost/master/AutoTalk.csx`

**実装内容:**
```csharp
#r "Rosalind.dll"
using Shiorose;
using System;

partial class AISisterAIChanGhost : Ghost
{
    private int autoTalkCounter = 0;
    private bool isAutoTalkMode = false;
    private const int AUTO_TALK_CHECK_INTERVAL = 60; // 秒

    /// <summary>
    /// 自動会話モードの有効化/無効化
    /// </summary>
    public void SetAutoTalkMode(bool enabled)
    {
        isAutoTalkMode = enabled;
        autoTalkCounter = 0;

        if (enabled)
        {
            // 会話履歴がない場合は初期トピックで開始
            if (conversationHistory.GetAllMessages().Length == 0)
            {
                BeginAutoTalk("最近どう？");
            }
        }
    }

    /// <summary>
    /// 自動会話の開始
    /// </summary>
    private void BeginAutoTalk(string initialTopic = null)
    {
        if (isTalking || chatGPTTalk != null)
            return;

        string prompt;

        if (initialTopic != null)
        {
            prompt = $"{USERName}：{initialTopic}";
        }
        else
        {
            // 前回の会話の続き
            prompt = GenerateContinuationPrompt();
        }

        BeginTalk(prompt);
    }

    /// <summary>
    /// 会話継続用のプロンプト生成
    /// </summary>
    private string GenerateContinuationPrompt()
    {
        var topics = new string[]
        {
            "（無言で見つめる）",
            "なにか面白いことあった？",
            "今日の調子はどう？",
            "最近気になることある？",
            "ねえ、ちょっと聞いて",
        };

        // ランダムに話題を選択
        var topic = topics[random.Next(topics.Length)];
        return $"{USERName}：{topic}";
    }
}
```

**Ghost.csxの変更 - OnSecondChange/OnMinuteChangeでの呼び出し:**
```csharp
public override string OnSecondChange(IDictionary<int, string> reference, string uptime, bool isOffScreen, bool isOverlap, bool canTalk, string leftSecond)
{
    // 既存の会話生成処理...
    if (canTalk && chatGPTTalk != null)
    {
        // 既存コード
    }

    // 自動会話モードのチェック
    if (isAutoTalkMode && canTalk && !isTalking && chatGPTTalk == null)
    {
        autoTalkCounter++;
        if (autoTalkCounter >= AUTO_TALK_CHECK_INTERVAL)
        {
            autoTalkCounter = 0;
            BeginAutoTalk();
        }
    }

    return base.OnSecondChange(reference, uptime, isOffScreen, isOverlap, canTalk, leftSecond);
}
```

#### タスク1.3: 会話終了判定の改善（1日）

**Ghost.csxのBuildTalk()修正:**
```csharp
string BuildTalk(string response, bool createChoices, string log)
{
    // 既存コード...

    var aiResponse = GetAIResponse(response);
    var surfaceId = GetSurfaceId(response);
    var onichanResponse = GetOnichanRenponse(response);

    // 会話継続判定を取得
    var shouldContinue = GetConversationContinuation(response);

    // 会話履歴に追加
    if (((SaveData)SaveData).IsConversationHistoryEnabled && !string.IsNullOrEmpty(aiResponse))
    {
        conversationHistory.AddAssistantMessage(aiResponse);
        conversationHistory.SaveToFile();
    }

    // 会話継続が「終了」の場合、自動トークモードを一時停止
    if (!shouldContinue && isAutoTalkMode)
    {
        isAutoTalkMode = false;
        // X分後に再開（タイマー設定）
        // TODO: 再開タイマーの実装
    }

    // 既存のトークビルド処理...
}

/// <summary>
/// 会話継続判定を取得
/// </summary>
bool GetConversationContinuation(string response)
{
    var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    var continuationLine = lines.FirstOrDefault(x => x.StartsWith("会話継続："));

    if (continuationLine == null)
        return true; // デフォルトは継続

    return continuationLine.Contains("継続");
}
```

#### タスク1.4: メニューUI追加（0.5日）

**GhostMenu.csxの変更:**
```csharp
private string SettingsTalk()
{
    const string CHANGE_RANDOMTALK_INTERVAL = "ランダムトークの頻度を変更する";
    const string CHANGE_CHOICE_COUNT = "選択肢の数を変更する";
    const string TOGGLE_AUTO_TALK = "自動会話モードを変更する（現在：" + (isAutoTalkMode ? "有効" : "無効") + "）";
    const string TOGGLE_HISTORY = "会話履歴を変更する（現在：" + (((SaveData)SaveData).IsConversationHistoryEnabled ? "有効" : "無効") + "）";
    string CHANGE_RANDOM_IDLING_SURFACE = "定期的に身じろぎする（現在：" + (((SaveData)SaveData).IsRandomIdlingSurfaceEnabled ? "有効" : "無効") + "）";
    string CHANGE_DEVMODE = "開発者モードを変更する（現在：" + (((SaveData)SaveData).IsDevMode ? "有効" : "無効") + "）";
    const string CLEAR_HISTORY = "会話履歴をクリア";
    const string BACK = "戻る";

    return new TalkBuilder()
        .Append("設定を変更するね。")
        .LineFeed()
        .HalfLine()
        .Marker().AppendChoice(TOGGLE_AUTO_TALK).LineFeed()
        .Marker().AppendChoice(TOGGLE_HISTORY).LineFeed()
        .Marker().AppendChoice(CLEAR_HISTORY).LineFeed()
        .HalfLine()
        .Marker().AppendChoice(CHANGE_RANDOMTALK_INTERVAL).LineFeed()
        .Marker().AppendChoice(CHANGE_CHOICE_COUNT).LineFeed()
        .Marker().AppendChoice(CHANGE_RANDOM_IDLING_SURFACE).LineFeed()
        .HalfLine()
        .Marker().AppendChoice(CHANGE_DEVMODE).LineFeed()
        .HalfLine()
        .Marker().AppendChoice(BACK)
        .BuildWithAutoWait()
        .ContinueWith(id =>
        {
            if (id == TOGGLE_AUTO_TALK)
            {
                SetAutoTalkMode(!isAutoTalkMode);
                return SettingsTalk();
            }
            else if (id == TOGGLE_HISTORY)
            {
                ((SaveData)SaveData).IsConversationHistoryEnabled = !((SaveData)SaveData).IsConversationHistoryEnabled;
                return SettingsTalk();
            }
            else if (id == CLEAR_HISTORY)
            {
                conversationHistory.Clear();
                conversationHistory.SaveToFile();
                return new TalkBuilder().Append("会話履歴をクリアしたよ！").BuildWithAutoWait();
            }
            // 既存の処理...
            else if (id == CHANGE_RANDOMTALK_INTERVAL)
                return ChangeRandomTalkIntervalTalk();
            // ... 他の選択肢
            else
                return OpenMenu();
        });
}
```

### ✅ 成果物

- [x] 会話履歴の永続化（JSON形式で保存）
- [x] 自動会話モードのON/OFF切り替え
- [x] 会話が自然に継続する仕組み
- [x] 会話終了判定による適切な区切り
- [x] メニューからの履歴クリア機能

### 🧪 テスト項目

1. 会話履歴が正しく保存・読み込みされるか
2. 自動会話モードで定期的に会話が開始されるか
3. 会話終了判定が正しく動作するか
4. メモリ使用量が上限（20件）で制限されるか
5. SSP再起動後も履歴が保持されるか

---

## フェーズ2: コンビ漫才モード

### 🎯 目標

二人組キャラクター（アイ & テディ）による自然な掛け合い漫才の実装

### 📊 実装期間

**13-17日**

### 🔍 技術調査結果

#### LLMキャラクター付与の最新研究（2025年）

**プロンプトエンジニアリング vs ファインチューニング:**
- プロンプトは手軽だが複雑な指示では一貫性が難しい
- ファインチューニングは500件以上の学習データで効果的
- 2キャラクター同時生成には構造化プロンプトが有効

**伺かでのAI実装成功事例:**
- ChatGPTによる掛け合い再現の成功例あり
- テンプレートゴーストで誰でもAI化可能

**参考資料:**
- [LLMキャラ付けファインチューニング：プロンプトエンジニアリングとの比較](https://note.com/sharp_engineer/n/ne9c667a01d0e)
- [LLM を用いた発話生成のキャラクター性付与（NLP2025論文）](https://www.anlp.jp/proceedings/annual_meeting/2025/pdf_dir/Q5-22.pdf)
- [伺かのかけあいをChatGPTで再現してみるテスト](https://zenn.dev/kinzal/articles/87735f72278c63)

### 📝 実装タスク

#### タスク2.1: Kero（相方）キャラクター設計（3-4日）

**新規ファイル:** `ghost/master/KeroCharacter.csx`

**実装内容:**
```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Kero（相方キャラクター）の設定
/// </summary>
public static class KeroSettings
{
    public const string Name = "テディ";

    public const string Personality = @"
名前：テディ
性別：不明（ぬいぐるみ）
年齢：不詳
外見：茶色いクマのぬいぐるみ
性格：
- ドライでシニカルなユーモアを持つ
- アイの暴走を冷静にツッコミで制止
- 意外と優しい一面もある
- 理屈っぽいが的確な指摘をする
- アイに振り回されることが多い

一人称：俺
アイの呼び方：アイ、お前
話し方：「～だろ」「～じゃねえか」など、やや乱暴だが親しみやすい";

    public const string Role = "ツッコミ担当";

    /// <summary>
    /// Keroのプロフィール（カスタマイズ可能）
    /// </summary>
    public static Dictionary<string, string> Profile = new Dictionary<string, string>()
    {
        ["好きなもの"] = "静かな時間、読書",
        ["苦手なもの"] = "騒がしい場所、アイの無茶振り",
        ["特技"] = "的確なツッコミ、冷静な分析",
    };
}

/// <summary>
/// Kero用サーフェス（表情）カテゴリ
/// </summary>
public static class KeroSurfaceCategory
{
    public const string Normal = "普通";
    public const string Annoyed = "呆れる";
    public const string Surprised = "驚き";
    public const string Angry = "怒り";
    public const string Smile = "笑顔";
    public const string Tired = "疲れた";

    public static string[] All = new string[]
    {
        Normal,
        Annoyed,
        Surprised,
        Angry,
        Smile,
        Tired,
    };
}

/// <summary>
/// Kero用サーフェスマッピング
/// </summary>
public class KeroSurfaces
{
    static Random random = new Random();
    static Dictionary<string, KeroSurfaces> SurfaceList = new Dictionary<string, KeroSurfaces>()
    {
        [KeroSurfaceCategory.Normal] = new KeroSurfaces(10, 11, 12),
        [KeroSurfaceCategory.Annoyed] = new KeroSurfaces(20, 21),
        [KeroSurfaceCategory.Surprised] = new KeroSurfaces(30, 31),
        [KeroSurfaceCategory.Angry] = new KeroSurfaces(40, 41),
        [KeroSurfaceCategory.Smile] = new KeroSurfaces(50, 51),
        [KeroSurfaceCategory.Tired] = new KeroSurfaces(60, 61),
    };

    public static KeroSurfaces Of(string category) => SurfaceList[category];

    int[] surfaces;
    public KeroSurfaces(params int[] surfaces)
    {
        this.surfaces = surfaces;
    }

    public int GetRandomSurface()
    {
        return surfaces[random.Next(surfaces.Length)];
    }

    public int GetSurfaceFromRate(double rate)
    {
        var index = Math.Min((int)(surfaces.Length * rate), surfaces.Length - 1);
        return surfaces[index];
    }
}
```

**shell/master/ への追加:**
- Kero用PNG画像を追加（または既存shellの流用）
- `surfaces.txt`にKeroサーフェス定義を追加

#### タスク2.2: 2キャラクター掛け合いプロンプト設計（5-7日）

**Ghost.csxの拡張:**

```csharp
#load "KeroCharacter.csx"

partial class AISisterAIChanGhost : Ghost
{
    private bool isManzaiMode = false; // 漫才モードフラグ

    /// <summary>
    /// 漫才用のプロンプト生成（研究に基づく構造化プロンプト）
    /// </summary>
    string BuildManzaiPrompt(string trigger = null)
    {
        var conversationLog = ((SaveData)SaveData).IsConversationHistoryEnabled
            ? conversationHistory.GetHistorySummary()
            : "";

        var prompt = $@"あなたは漫才コンビの会話を生成するAIです。以下のキャラクター設定に従って、自然な掛け合いを生成してください。

# キャラクター設定
## {AIName}（ボケ担当）
名前：{AIName}
性別：女
年齢：14
性格：元気溌剌でクラスの人気者。天然ボケ気味で思ったことをすぐ口にする。{KeroSettings.Name}に対しては心を許している。
外見：ピンクの髪。ピンク色のリボンで髪を縛ってツインテールにしている。
服装：黒の長袖Tシャツにピンクのフリルミニスカート
一人称：私
{KeroSettings.Name}の呼び方：{KeroSettings.Name}
話し方：明るく元気。「～だよ！」「～なの！」など
{((SaveData)SaveData).AiProfile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

## {KeroSettings.Name}（ツッコミ担当）
{KeroSettings.Personality}
{KeroSettings.Profile.Select(x => x.Key + "：" + x.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + "\r\n" + b)}

# 会話ルール
1. {AIName}が先に話題を振る、または前回の続きを話す（ボケ）
2. {KeroSettings.Name}が反応・ツッコミを入れる
3. 1往復または2往復で完結させる
4. テンポを重視（各セリフは1-2文、最大でも3文まで）
5. 自然な会話の流れを意識
6. 必要に応じて話題を変えても良い

# 出力フォーマット（厳守）
{AIName}のセリフ：{{セリフ}}
{AIName}の表情：{SurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
{KeroSettings.Name}のセリフ：{{セリフ}}
{KeroSettings.Name}の表情：{KeroSurfaceCategory.All.Select(x => $"「{x}」").Aggregate((a, b) => a + "、" + b)}のいずれか
会話継続：「継続」または「終了」

# 会話例（Few-shot learning）
## 例1
{AIName}のセリフ：ねえねえ{KeroSettings.Name}、今日は何の日か知ってる？
{AIName}の表情：普通
{KeroSettings.Name}のセリフ：...お前の誕生日でも何でもない、ただの平日だろ。
{KeroSettings.Name}の表情：呆れる
会話継続：継続

## 例2
{AIName}のセリフ：あのね、今日学校でね、すっごく面白いことがあったの！
{AIName}の表情：笑顔
{KeroSettings.Name}のセリフ：おう、聞いてやるから落ち着け。
{KeroSettings.Name}の表情：普通
会話継続：継続

## 例3
{AIName}のセリフ：{KeroSettings.Name}って、本当は優しいよね。
{AIName}の表情：笑顔
{KeroSettings.Name}のセリフ：...急にどうした。気持ち悪いぞ。
{KeroSettings.Name}の表情：驚き
会話継続：終了

# 現在の状況
時刻：{DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm:ss")}
{(string.IsNullOrEmpty(conversationLog) ? "" : $"前回までの会話：\r\n{conversationLog}\r\n")}
{(string.IsNullOrEmpty(trigger) ? "（自由に会話を始めてください）" : $"きっかけ：{trigger}")}

# 重要な注意事項
- セリフに「○○」「XXX」などの仮置き文字は使用禁止。必ず具体的な内容を生成すること
- 表情は必ず指定されたカテゴリから選ぶこと
- 出力フォーマットを厳守すること（形式が崩れると動作しません）
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
}
```

#### タスク2.3: レスポンスパーサーの拡張（3-4日）

**Ghost.csxにKero用パーサー追加:**

```csharp
/// <summary>
/// Keroのセリフを取得
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
/// KeroのサーフェスIDを取得
/// </summary>
int GetKeroSurfaceId(string response)
{
    var lines = response.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    var face = lines.FirstOrDefault(x => x.StartsWith($"{KeroSettings.Name}の表情："));

    if (face is null)
        return 10; // デフォルトサーフェス

    foreach (var category in KeroSurfaceCategory.All)
    {
        if (face.Contains(category))
            return KeroSurfaces.Of(category).GetSurfaceFromRate(faceRate);
    }

    return 10;
}

/// <summary>
/// 漫才用のトーク構築
/// </summary>
string BuildManzaiTalk(string response, bool createChoices, string log)
{
    try
    {
        isTalking = true;
        if (((SaveData)SaveData).IsDevMode)
            Log.WriteAllText(Log.Response, response);

        var aiSerif = GetAIResponse(response);
        var aiSurface = GetSurfaceId(response);
        var keroSerif = GetKeroResponse(response);
        var keroSurface = GetKeroSurfaceId(response);
        var shouldContinue = GetConversationContinuation(response);

        // 会話履歴に追加
        if (((SaveData)SaveData).IsConversationHistoryEnabled)
        {
            if (!string.IsNullOrEmpty(aiSerif))
                conversationHistory.AddAssistantMessage($"{AIName}：{aiSerif}");
            if (!string.IsNullOrEmpty(keroSerif))
                conversationHistory.AddAssistantMessage($"{KeroSettings.Name}：{keroSerif}");
            conversationHistory.SaveToFile();
        }

        var talkBuilder = new TalkBuilder()
            .Append($"\\_q\\0\\s[{aiSurface}]")
            .Append(aiSerif)
            .LineFeed();

        if (!string.IsNullOrEmpty(keroSerif))
        {
            talkBuilder = talkBuilder
                .Append($"\\1\\s[{keroSurface}]")
                .Append(keroSerif)
                .LineFeed();
        }

        talkBuilder = talkBuilder.HalfLine();

        if (!createChoices)
        {
            return talkBuilder.Append($"\\_q...").LineFeed().Build();
        }

        // 会話継続の場合の選択肢
        const string CONTINUE_MANZAI = "もっと話して";
        const string TALK_TO_AI = $"{AIName}に話しかける";
        const string TALK_TO_KERO = $"{KeroSettings.Name}に話しかける";
        const string END_TALK = "会話を終える";

        return talkBuilder
            .Marker().AppendChoice(CONTINUE_MANZAI).LineFeed()
            .Marker().AppendChoice(TALK_TO_AI).LineFeed()
            .Marker().AppendChoice(TALK_TO_KERO).LineFeed()
            .HalfLine()
            .Marker().AppendChoice(END_TALK).LineFeed()
            .Build()
            .ContinueWith(id =>
            {
                if (id == CONTINUE_MANZAI)
                {
                    BeginManzai(); // 続きを生成
                    return "";
                }
                else if (id == TALK_TO_AI)
                {
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
                        .Append($"\\1\\s[10]{KeroSettings.Name}に何を話す？")
                        .AppendUserInput()
                        .Build()
                        .ContinueWith(input =>
                        {
                            BeginManzai($"{USERName}が{KeroSettings.Name}に話しかけた：{input}");
                            return "";
                        });
                }
                return "";
            });
    }
    catch (Exception e)
    {
        return e.ToString();
    }
}
```

**OnSecondChangeの修正:**

```csharp
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
        if (isManzaiMode)
            return BuildManzaiTalk(talk.Response, !talk.IsProcessing, log);
        else
            return BuildTalk(talk.Response, !talk.IsProcessing, log);
    }

    // 既存の自動会話チェック...
    return base.OnSecondChange(reference, uptime, isOffScreen, isOverlap, canTalk, leftSecond);
}
```

#### タスク2.4: モード切り替えUI（2日）

**GhostMenu.csxの変更:**

```csharp
private string OpenMenu()
{
    const string RAND = "なにか話して";
    const string COMMUNICATE = "話しかける";
    const string MANZAI_MODE = "漫才モード";
    const string CHANGEPROFILE = "プロフィールを変更する";
    const string SETTINGS = "設定を変えたい";
    const string CANCEL = "なんでもない";

    return new TalkBuilder()
        .Append("どうしたの？").LineFeed()
        .HalfLine()
        .Marker().AppendChoice(RAND).LineFeed()
        .Marker().AppendChoice(COMMUNICATE).LineFeed()
        .Marker().AppendChoice(MANZAI_MODE).LineFeed()
        .HalfLine()
        .Marker().AppendChoice(CHANGEPROFILE).LineFeed()
        .Marker().AppendChoice(SETTINGS).LineFeed()
        .HalfLine()
        .Marker().AppendChoice(CANCEL)
        .BuildWithAutoWait()
        .ContinueWith((id) =>
        {
            switch (id)
            {
                case RAND:
                    isManzaiMode = false;
                    return OnRandomTalk();
                case COMMUNICATE:
                    isManzaiMode = false;
                    return new TalkBuilder().Append("なになに？").AppendCommunicate().Build();
                case MANZAI_MODE:
                    isManzaiMode = true;
                    BeginManzai();
                    return "";
                case CHANGEPROFILE:
                    return ChangeProfileTalk();
                case SETTINGS:
                    return SettingsTalk();
                default:
                    return new TalkBuilder().Append("そう…？").BuildWithAutoWait();
            }
        });
}
```

### ✅ 成果物

- [x] 2キャラクター（アイ & テディ）の掛け合い漫才
- [x] ボケ・ツッコミの役割分担
- [x] テンポの良い会話生成
- [x] ユーザー参加型の選択肢
- [x] 観客モード（自動継続）

### 🧪 テスト項目

1. 2キャラクターのセリフが正しく生成されるか
2. 表情切り替えが両キャラで動作するか
3. ボケ・ツッコミの役割が機能しているか
4. 会話のテンポが適切か（冗長でないか）
5. 出力フォーマットが崩れずパースできるか

---

## フェーズ3: Live2D/3D対応

### 🎯 目標

2D静止画スプライトからLive2DまたはVRM 3Dモデルによる豊かな表現への移行

### 📊 実装期間

**5-7週間**（プロトタイプ）
**2-3ヶ月**（本格実装、モデル制作含む）

### 🔍 技術調査結果

#### SSPとLive2D統合の現状

**調査結果:**
- **SSP用Live2Dプラグインは存在しない**
- 伺かはSERIKO形式（PNG + surfaces.txt）が標準
- Live2D統合には独自アプローチが必要

#### 代替アプローチ：VRMデスクトップマスコット

**成功事例（2025年）:**
- **uDesktopMascot**（2025年1月リリース）：オープンソースのVRMデスクトップマスコット
- Unity + UniVRM + UniWinApiで自作可能（1ヶ月で実装例あり）
- LLM統合VRMマスコット開発事例（2025年4-5月）

**技術スタック:**
- Unity 2022.3 LTS以降
- UniVRM 0.125.0以降
- UniWinApi（ウィンドウ透過・常駐化）
- C# HTTP APIサーバー

**参考資料:**
- [オープンソースのデスクトップマスコットアプリ「uDesktopMascot」](https://forest.watch.impress.co.jp/docs/news/1654140.html)
- [Unity初心者が1か月でデスクトップマスコットを作ってみた話](https://qiita.com/segfo/items/1f5db4c95b393012fe00)
- [GitHub - UniWinApi: Windows API collection for Unity](https://github.com/kirurobo/UniWinApi)
- [デスクトップマスコット開発 ～天満さくらに憧れて～](https://3up-tec.jp/2024/11/15/1530/)

### 🎯 推奨実装アプローチ

**選択肢A: SSP併用ハイブリッド方式**（推奨）

#### アーキテクチャ概要

```
┌─────────────────┐         ┌──────────────────────┐
│  SSP (既存)      │         │  Unity VRMビューワー  │
│                 │         │  (新規開発)          │
│  ┌──────────┐  │         │  ┌───────────────┐  │
│  │Ghost.csx  │  │◄──HTTP──┤  │ GhostAPIServer │  │
│  └──────────┘  │  API    │  └───────────────┘  │
│       ↓         │         │         ↓            │
│  ┌──────────┐  │         │  ┌───────────────┐  │
│  │ChatGPT.csx│  │         │  │ VRMController  │  │
│  └──────────┘  │         │  └───────────────┘  │
│       ↓         │         │         ↓            │
│  Ollama API     │         │  Live2D/VRMモデル   │
└─────────────────┘         └──────────────────────┘
```

**通信プロトコル:**
- SSPのゴーストスクリプト → HTTP POST → Unity VRMビューワー
- JSON形式で表情・セリフ・モーション指令を送信
- Unity側は`localhost:8080`でリッスン

### 📝 実装タスク

#### タスク3.1: Unity VRMビューワー開発（3-4週間）

**開発環境構築（1-2日）:**

1. Unity 2022.3 LTS インストール
2. UniVRM パッケージ導入
   ```
   Window > Package Manager > Add package from git URL
   https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM10
   ```
3. UniWinApi導入
   ```
   git clone https://github.com/kirurobo/UniWinApi.git
   ```

**プロジェクト構造:**
```
GhostVRMViewer/
├── Assets/
│   ├── Scripts/
│   │   ├── GhostAPIServer.cs      // HTTP API受信
│   │   ├── VRMController.cs       // VRM制御
│   │   ├── ExpressionMapper.cs    // 表情マッピング
│   │   └── WindowController.cs    // ウィンドウ制御
│   ├── VRM/
│   │   ├── Ai.vrm                 // アイちゃんVRMモデル
│   │   └── Teddy.vrm              // テディVRMモデル
│   └── StreamingAssets/
├── Packages/
└── ProjectSettings/
```

**GhostAPIServer.cs実装例:**

```csharp
using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

public class GhostAPIServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;

    public VRMController vrmController;

    void Start()
    {
        StartServer();
    }

    void OnDestroy()
    {
        StopServer();
    }

    void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        isRunning = true;

        listenerThread = new Thread(ListenForRequests);
        listenerThread.Start();

        Debug.Log("Ghost API Server started on http://localhost:8080/");
    }

    void StopServer()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Join();
    }

    void ListenForRequests()
    {
        while (isRunning)
        {
            try
            {
                var context = listener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"API Server Error: {ex.Message}");
            }
        }
    }

    void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod == "POST")
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string json = reader.ReadToEnd();
                var command = JsonConvert.DeserializeObject<GhostCommand>(json);

                // Unityメインスレッドで実行
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ExecuteCommand(command);
                });

                // レスポンス
                string responseString = "{\"status\":\"ok\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        response.Close();
    }

    void ExecuteCommand(GhostCommand command)
    {
        switch (command.type)
        {
            case "expression":
                vrmController.SetExpression(command.characterId, command.expression);
                break;
            case "speak":
                vrmController.Speak(command.characterId, command.text);
                break;
            case "motion":
                vrmController.PlayMotion(command.characterId, command.motionName);
                break;
        }
    }
}

[Serializable]
public class GhostCommand
{
    public string type;           // "expression", "speak", "motion"
    public int characterId;       // 0=Ai, 1=Teddy
    public string expression;     // "smile", "surprised", etc.
    public string text;           // セリフテキスト
    public string motionName;     // モーション名
}
```

**VRMController.cs実装例:**

```csharp
using UnityEngine;
using UniVRM10;
using System.Collections;
using System.Collections.Generic;

public class VRMController : MonoBehaviour
{
    public Vrm10Instance aiVrmInstance;      // アイちゃんVRM
    public Vrm10Instance teddyVrmInstance;   // テディVRM

    private Dictionary<string, ExpressionPreset> expressionMap = new Dictionary<string, ExpressionPreset>()
    {
        {"普通", ExpressionPreset.Neutral},
        {"笑顔", ExpressionPreset.Happy},
        {"驚き", ExpressionPreset.Surprised},
        {"怒り", ExpressionPreset.Angry},
        {"悲しい", ExpressionPreset.Sad},
        {"恥ずかしい", ExpressionPreset.Relaxed},
    };

    void Start()
    {
        LoadVRMModels();
    }

    async void LoadVRMModels()
    {
        // VRMモデルの読み込み
        // （実装詳細は省略 - UniVRMのドキュメント参照）
    }

    public void SetExpression(int characterId, string expressionName)
    {
        var instance = characterId == 0 ? aiVrmInstance : teddyVrmInstance;

        if (instance == null) return;

        // 全表情をリセット
        foreach (var preset in System.Enum.GetValues(typeof(ExpressionPreset)))
        {
            instance.Runtime.Expression.SetWeight(
                ExpressionKey.CreateFromPreset((ExpressionPreset)preset),
                0f
            );
        }

        // 指定表情を適用
        if (expressionMap.ContainsKey(expressionName))
        {
            var preset = expressionMap[expressionName];
            instance.Runtime.Expression.SetWeight(
                ExpressionKey.CreateFromPreset(preset),
                1.0f
            );
        }
    }

    public void Speak(int characterId, string text)
    {
        // リップシンク処理（口パク）
        StartCoroutine(LipSyncCoroutine(characterId, text));
    }

    IEnumerator LipSyncCoroutine(int characterId, string text)
    {
        var instance = characterId == 0 ? aiVrmInstance : teddyVrmInstance;

        // 簡易リップシンク（文字数に応じて口パク）
        float duration = text.Length * 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float weight = Mathf.Sin(elapsed * 10f) * 0.5f + 0.5f;
            instance.Runtime.Expression.SetWeight(
                ExpressionKey.CreateFromPreset(ExpressionPreset.Aa),
                weight
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // リセット
        instance.Runtime.Expression.SetWeight(
            ExpressionKey.CreateFromPreset(ExpressionPreset.Aa),
            0f
        );
    }

    public void PlayMotion(int characterId, string motionName)
    {
        // モーション再生処理
        // （Animator経由でアニメーション再生）
    }
}
```

**WindowController.cs実装例（UniWinApi使用）:**

```csharp
using UnityEngine;
using Kirurobo;

public class WindowController : MonoBehaviour
{
    private UniWindowController uniwinController;

    void Start()
    {
        uniwinController = GetComponent<UniWindowController>();

        // ウィンドウ設定
        uniwinController.isTransparent = true;      // 透過
        uniwinController.isTopmost = true;          // 最前面
        uniwinController.isClickThrough = false;    // クリック貫通OFF
    }
}
```

#### タスク3.2: SSP連携実装（1週間）

**新規ファイル:** `ghost/master/VRMBridge.csx`

```csharp
#r "Rosalind.dll"
#r "Newtonsoft.Json.dll"
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class VRMBridge
{
    private const string VRM_API_ENDPOINT = "http://localhost:8080/ghost/command";
    private static HttpClient client = new HttpClient();

    /// <summary>
    /// 表情を送信
    /// </summary>
    public static async void SendExpression(int characterId, string expression)
    {
        var command = new
        {
            type = "expression",
            characterId = characterId,
            expression = expression
        };

        await SendCommand(command);
    }

    /// <summary>
    /// セリフを送信（リップシンク用）
    /// </summary>
    public static async void SendSpeak(int characterId, string text)
    {
        var command = new
        {
            type = "speak",
            characterId = characterId,
            text = text
        };

        await SendCommand(command);
    }

    /// <summary>
    /// モーションを送信
    /// </summary>
    public static async void SendMotion(int characterId, string motionName)
    {
        var command = new
        {
            type = "motion",
            characterId = characterId,
            motionName = motionName
        };

        await SendCommand(command);
    }

    /// <summary>
    /// コマンドをVRMビューワーに送信
    /// </summary>
    private static async Task SendCommand(object command)
    {
        try
        {
            var json = JsonConvert.SerializeObject(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(VRM_API_ENDPOINT, content);

            if (!response.IsSuccessStatusCode)
            {
                // エラーログ
                Log.WriteAllText("vrm_error.log", $"VRM API Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // 接続失敗時（VRMビューワー未起動など）
            Log.WriteAllText("vrm_error.log", $"VRM Bridge Exception: {ex.Message}");
        }
    }
}

/// <summary>
/// 表情名をVRM用に変換
/// </summary>
public static class SurfaceCategoryExtensions
{
    public static string ToVRMExpression(string surfaceCategory)
    {
        // SSPのサーフェスカテゴリ → VRM表情名マッピング
        var mapping = new Dictionary<string, string>()
        {
            [SurfaceCategory.Normal] = "普通",
            [SurfaceCategory.Smile] = "笑顔",
            [SurfaceCategory.Surprise] = "驚き",
            [SurfaceCategory.Angry] = "怒り",
            [SurfaceCategory.Sad] = "悲しい",
            [SurfaceCategory.Embarrassed] = "恥ずかしい",
        };

        return mapping.ContainsKey(surfaceCategory) ? mapping[surfaceCategory] : "普通";
    }
}
```

**Ghost.csxの変更:**

```csharp
#load "VRMBridge.csx"

partial class AISisterAIChanGhost : Ghost
{
    string BuildTalk(string response, bool createChoices, string log)
    {
        // 既存コード...

        var aiResponse = GetAIResponse(response);
        var surfaceId = GetSurfaceId(response);
        var surfaceCategory = GetSurfaceCategory(surfaceId);

        // VRMモードが有効な場合
        if (((SaveData)SaveData).IsVRMModeEnabled)
        {
            // 表情を送信
            VRMBridge.SendExpression(0, SurfaceCategoryExtensions.ToVRMExpression(surfaceCategory));

            // セリフを送信（リップシンク）
            VRMBridge.SendSpeak(0, aiResponse);
        }

        // 既存のSSP用トーク構築...
    }

    string BuildManzaiTalk(string response, bool createChoices, string log)
    {
        // 既存コード...

        var aiSerif = GetAIResponse(response);
        var aiSurface = GetSurfaceId(response);
        var keroSerif = GetKeroResponse(response);
        var keroSurface = GetKeroSurfaceId(response);

        // VRMモード
        if (((SaveData)SaveData).IsVRMModeEnabled)
        {
            // アイの表情・セリフ
            VRMBridge.SendExpression(0, SurfaceCategoryExtensions.ToVRMExpression(GetSurfaceCategory(aiSurface)));
            VRMBridge.SendSpeak(0, aiSerif);

            // テディの表情・セリフ
            VRMBridge.SendExpression(1, GetKeroSurfaceCategory(keroSurface));
            VRMBridge.SendSpeak(1, keroSerif);
        }

        // 既存のSSP用トーク構築...
    }
}
```

**SaveData.csxの変更:**

```csharp
[DataMember]
public bool IsVRMModeEnabled { get; set; } = false;
```

**GhostMenu.csxにVRMモード切り替え追加:**

```csharp
const string TOGGLE_VRM_MODE = "VRMモードを変更する（現在：" + (((SaveData)SaveData).IsVRMModeEnabled ? "有効" : "無効") + "）";

// SettingsTalk()内
.Marker().AppendChoice(TOGGLE_VRM_MODE).LineFeed()

// ContinueWith内
else if (id == TOGGLE_VRM_MODE)
{
    ((SaveData)SaveData).IsVRMModeEnabled = !((SaveData)SaveData).IsVRMModeEnabled;
    return SettingsTalk();
}
```

#### タスク3.3: VRMモデル準備（外注または自作）

**オプションA: VRoid Studioで作成（無料・初心者向け）**

1. VRoid Studio ダウンロード: https://vroid.com/studio
2. アイちゃんのデザインに基づいてモデル作成
   - 髪型：ピンクのツインテール
   - 衣装：黒の長袖Tシャツ + ピンクのフリルスカート
   - 表情プリセット設定
3. VRM 1.0形式でエクスポート

**オプションB: 既存VRMモデルの改変**

- BOOTH等でベースモデル購入
- Blender + VRM Add-onで編集
- テクスチャ差し替え

**オプションC: プロに外注**

- SKIMAやココナラでVRMモデル制作依頼
- 予算：2-10万円程度

**必要なモデル:**
- アイちゃん（sakura/\0）
- テディ（kero/\1）

**表情プリセット:**
- Neutral（普通）
- Happy（笑顔）
- Surprised（驚き）
- Angry（怒り）
- Sad（悲しい）
- Relaxed（恥ずかしい）

#### タスク3.4: テスト・調整（1週間）

**テスト項目:**

1. **SSP ↔ Unity連携テスト**
   - HTTP通信が正常に動作するか
   - コマンドが正しく受信・実行されるか

2. **表情同期テスト**
   - SSPのサーフェス変更がVRMの表情に反映されるか
   - 遅延は許容範囲か（100ms以下推奨）

3. **リップシンクテスト**
   - セリフに合わせて口が動くか
   - タイミングは自然か

4. **パフォーマンステスト**
   - CPU/メモリ使用量
   - フレームレート（60fps維持推奨）

5. **安定性テスト**
   - 長時間動作でクラッシュしないか
   - Unity側が落ちた場合SSPは正常動作するか

**最適化項目:**

- VRMモデルのポリゴン数削減
- テクスチャ圧縮
- 不要なBlendShapeの削除
- ガベージコレクション最適化

### ✅ 成果物

- [x] Unity製VRMビューワー
- [x] SSPとのHTTP API連携
- [x] アイちゃん・テディのVRMモデル
- [x] 表情・リップシンク機能
- [x] ウィンドウ透過・常駐機能

### 🧪 テスト項目

1. VRMビューワーが正常起動するか
2. SSPからの指令で表情が変わるか
3. リップシンクが機能するか
4. ウィンドウ透過が正しく動作するか
5. 2キャラクター同時表示できるか

### 📊 実装スケジュール（フェーズ3詳細）

**Week 1-2: 環境構築・プロトタイプ**
- Unity環境セットアップ
- 既存VRMモデルで動作確認
- HTTP API基本実装

**Week 3-4: VRM制御実装**
- 表情制御
- リップシンク
- モーション再生

**Week 5: SSP連携**
- VRMBridge実装
- Ghost.csx統合
- 通信テスト

**Week 6-7: VRMモデル準備**
- アイちゃんモデル作成
- テディモデル作成
- 表情プリセット設定

**Week 8-11: 統合・最適化**
- 全機能統合
- パフォーマンス最適化
- バグ修正
- ドキュメント作成

---

## 総合スケジュール

### ガントチャート形式

| フェーズ | 期間 | Week 1-2 | Week 3-5 | Week 6-11 | Week 12-20 |
|---------|------|----------|----------|-----------|------------|
| 💬 単体会話モード | 4-6日 | ████ | | | |
| 🎭 コンビ漫才モード | 13-17日 | | ███████ | | |
| 🎨 Live2D/3D対応 | 5-7週間 | | | ████████████ | ████████████ |

### マイルストーン

| Week | マイルストーン | 成果物 |
|------|---------------|--------|
| Week 2 | 自動会話デモ完成 | 会話履歴管理、自動トーク機能 |
| Week 5 | 漫才掛け合いデモ完成 | 2キャラ漫才、プロンプト最適化 |
| Week 11 | VRM連携プロトタイプ完成 | Unity VRMビューワー、SSP連携 |
| Week 20 | 全機能統合版リリース | VRMモデル統合、パフォーマンス最適化 |

### リソース配分

**開発時間配分:**
- フェーズ1（単体会話）: 5%
- フェーズ2（コンビ漫才）: 15%
- フェーズ3（Live2D/3D）: 80%

**スキル要件:**
- C#プログラミング: 必須
- Unity開発: フェーズ3で必要
- プロンプトエンジニアリング: フェーズ1-2で重要
- 3Dモデリング: 外注可能

---

## 技術参考資料

### フェーズ1関連

- [PowerShell × Ollama：会話履歴管理の実装](https://qiita.com/Tadataka_Takahashi/items/4318182e2e35fb7b4737)
- [Ollamaストリーミング応答の活用方法](https://apidog.com/jp/blog/ollama-streaming-responses-and-tool-calling-jp/)
- [ブラウザ上でローカルLLMと対話：対話履歴の追加](https://kobayashinote.com/browser-localllm4-dialogue/)
- [Ollama完全ガイド：ローカルLLMをゼロからマスターする](https://apidog.com/jp/blog/how-to-use-ollama-jp/)

### フェーズ2関連

- [LLMキャラ付けファインチューニング：プロンプトエンジニアリングとの比較](https://note.com/sharp_engineer/n/ne9c667a01d0e)
- [LLM を用いた発話生成のキャラクター性付与（NLP2025論文）](https://www.anlp.jp/proceedings/annual_meeting/2025/pdf_dir/Q5-22.pdf)
- [伺かのかけあいをChatGPTで再現してみるテスト](https://zenn.dev/kinzal/articles/87735f72278c63)
- [【伺か】誰でもうちの子をAI化できるテンプレートを作った](https://ambergonslibrary.com/ukagaka/8991/)
- [LLMのキャラ付け完全ガイド](https://media.a-x.inc/llm-character/)

### フェーズ3関連

- [オープンソースのデスクトップマスコットアプリ「uDesktopMascot」](https://forest.watch.impress.co.jp/docs/news/1654140.html)
- [Unity初心者が1か月でデスクトップマスコットを作ってみた話](https://qiita.com/segfo/items/1f5db4c95b393012fe00)
- [GitHub - UniWinApi: Windows API collection for Unity](https://github.com/kirurobo/UniWinApi)
- [デスクトップマスコット開発 ～天満さくらに憧れて～](https://3up-tec.jp/2024/11/15/1530/)
- [【Unity】VRMモデルをインポートする方法](https://geeos.blog/2025/03/24/unity-vrm-import/)
- [UniVRM公式ドキュメント](https://site-builder.wiki/posts/23532)

### 伺か関連

- [伺かから外部 API を叩く～GPT の言葉を喋らせる最小構成～](https://note.com/allegromoltov/n/n354c0e237c13)
- [独立伺か研究施設 ばぐとら研究所](https://ssp.shillest.net/)
- [伺か情報ブログ「ghost-log」](https://ghost-log.net/)

---

## 補足事項

### リスク管理

**技術的リスク:**

| リスク | 影響度 | 対策 |
|--------|--------|------|
| Ollamaモデルの応答品質 | 中 | プロンプト改善、モデル変更 |
| VRM連携の遅延 | 高 | 非同期処理、キャッシング |
| Unity開発の技術習得 | 高 | 段階的学習、既存プロジェクト参考 |

**スケジュールリスク:**

| リスク | 影響度 | 対策 |
|--------|--------|------|
| フェーズ3の長期化 | 高 | プロトタイプで早期検証 |
| モデル制作の遅延 | 中 | 既存VRMで先行開発 |

### バージョン管理

- Git使用推奨
- 各フェーズごとにタグ作成
  - `v2.0-auto-talk`（フェーズ1完了）
  - `v2.1-manzai`（フェーズ2完了）
  - `v3.0-vrm`（フェーズ3完了）

### ドキュメント

各フェーズ完了時に以下を更新：
- README.md
- CLAUDE.md
- CHANGELOG.md
- 本実装計画書

---

**作成者:** Claude Code
**最終更新:** 2025年12月29日
