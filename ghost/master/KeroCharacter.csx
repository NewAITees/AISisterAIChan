#load "Surfaces.csx"
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
外見：ピンクの髪。ピンク色のリボンで髪を縛ってツインテールにしてる。全体的に華奢。（見た目はアイと同じ）
服装：黒の長袖Tシャツにピンクのフリルミニスカート（2段）（見た目はアイと同じ）
性格：
- けだるいダウナー系で、いつも疲れた様子
- アイの元気な発言に「はあ...」「まあ...」と反応することが多い
- 基本的にやる気がないが、時々的確なツッコミを入れる
- 面倒くさがりで、できるだけ動きたくない
- アイに振り回されることが多いが、内心ではアイのことを気にかけている
- レトロゲームやエロゲーの話題には興味を示すが、基本的には「そうなんだ...」程度の反応

一人称：私、あたし
アイの呼び方：アイ、お前
話し方：「～だよ...」「～なの...」「はあ...」「まあ...」など、語尾がだらりと下がる。短めのセリフが多い。";

    public const string Role = "ツッコミ担当（けだるい系）";

    /// <summary>
    /// Keroのプロフィール（カスタマイズ可能）
    /// </summary>
    public static Dictionary<string, string> Profile = new Dictionary<string, string>()
    {
        ["好きなもの"] = "静かな時間、寝ること、面倒くさくないこと",
        ["苦手なもの"] = "騒がしい場所、アイの無茶振り、動くこと",
        ["特技"] = "的確なツッコミ（ただし面倒くさがる）、疲れた表情",
    };
}

/// <summary>
/// Kero用サーフェス（表情）カテゴリ
/// 見た目は同じなので、既存のSurfaceCategoryを使用
/// </summary>
public static class KeroSurfaceCategory
{
    // 既存のSurfaceCategoryを再利用
    public const string Normal = SurfaceCategory.Normal;        // 普通
    public const string Tired = SurfaceCategory.CloseEyes;      // 疲れた（目を閉じる）
    public const string Annoyed = SurfaceCategory.Amazed;       // 呆れる
    public const string Surprised = SurfaceCategory.Surprise;   // 驚き
    public const string Sad = SurfaceCategory.Sad;               // 悲しい
    public const string Smile = SurfaceCategory.Smile;          // 笑顔（稀）

    public static string[] All = new string[]
    {
        Normal,
        Tired,
        Annoyed,
        Surprised,
        Sad,
        Smile,
    };
}

/// <summary>
/// Kero用サーフェスマッピング
/// 見た目は同じなので、既存のSurfacesを使用
/// </summary>
public class KeroSurfaces
{
    static Random random = new Random();

    /// <summary>
    /// カテゴリからサーフェスIDを取得（既存のSurfacesを使用）
    /// </summary>
    public static int GetSurfaceFromCategory(string category, double rate)
    {
        // 既存のSurfacesクラスを使用
        try
        {
            return Surfaces.Of(category).GetSurfaceFromRate(rate);
        }
        catch
        {
            return 0; // デフォルト
        }
    }

    /// <summary>
    /// ランダムにサーフェスIDを取得
    /// </summary>
    public static int GetRandomSurfaceFromCategory(string category)
    {
        try
        {
            return Surfaces.Of(category).GetRaodomSurface();
        }
        catch
        {
            return 0; // デフォルト
        }
    }
}
