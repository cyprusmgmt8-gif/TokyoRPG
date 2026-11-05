using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How Japanese is shown. Furigana is listed for the settings screen, but it is not available yet.
/// </summary>
public enum JapaneseScriptDisplay
{
    JapaneseAndRomaji = 0,
    JapaneseOnly = 1,
    JapaneseAndFurigana = 2
}

/// <summary>
/// One Japanese phrase for any future screen: dialogue, menus, quests, subtitles, quizzes.
/// Fill Japanese and, when the Japanese uses kanji, the kana Reading.
/// Fill Romaji yourself when the phrase contains the particles は or へ,
/// because those kana are not always said ha and he.
/// Leave Romaji empty for plain kana such as つくえ or ベッド. The game can build that Romaji.
/// </summary>
[Serializable]
public class JapanesePhrase
{
    [TextArea(1, 3)]
    public string japanese;

    [Tooltip("Hiragana or katakana reading. Required when Japanese contains kanji.")]
    [TextArea(1, 3)]
    public string reading;

    [Tooltip("Hepburn romaji. Required for sentences with は or へ. Example: Kore wa beddo desu.")]
    [TextArea(1, 3)]
    public string romaji;

    [TextArea(1, 3)]
    public string english;

    public string ResolvedRomaji()
    {
        return JapaneseReading.Resolve(japanese, reading, romaji);
    }
}

/// <summary>
/// The one place that decides which Romaji to show.
/// Study cards, the dictionary, grammar, revision and the city map all use this.
/// </summary>
public static class JapaneseReading
{
    static readonly HashSet<string> warned = new HashSet<string>();

    public static bool FuriganaAvailable => false;

    public static JapaneseScriptDisplay Mode
    {
        get
        {
            PlayerProfile.EnsureLoaded();
            if (PlayerProfile.Data != null && PlayerProfile.Data.readingDisplay == (int)JapaneseScriptDisplay.JapaneseOnly)
            {
                return JapaneseScriptDisplay.JapaneseOnly;
            }

            return JapaneseScriptDisplay.JapaneseAndRomaji;
        }
    }

    public static bool ShowRomaji => Mode == JapaneseScriptDisplay.JapaneseAndRomaji;

    public static string Resolve(string japanese, string kanaReading, string authoredRomaji)
    {
        if (!TryResolve(japanese, kanaReading, authoredRomaji, out string romaji))
        {
            return "";
        }

        return romaji;
    }

    /// <summary>
    /// Uses the authored Romaji when it is present.
    /// Otherwise builds Romaji from kana. Kanji without a kana reading returns false and is logged once.
    /// </summary>
    public static bool TryResolve(string japanese, string kanaReading, string authoredRomaji, out string romaji)
    {
        romaji = "";
        if (!string.IsNullOrWhiteSpace(authoredRomaji))
        {
            romaji = authoredRomaji.Trim();
            return true;
        }

        string source = string.IsNullOrWhiteSpace(kanaReading) ? japanese : kanaReading;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        if (KanaRomaji.ContainsKanji(source))
        {
            WarnMissing(japanese, source);
            return false;
        }

        romaji = KanaRomaji.ToRomaji(source);
        return !string.IsNullOrWhiteSpace(romaji);
    }

    public static string ShownRomaji(string japanese, string kanaReading, string authoredRomaji)
    {
        if (!ShowRomaji)
        {
            return "";
        }

        return Resolve(japanese, kanaReading, authoredRomaji);
    }

    static void WarnMissing(string japanese, string source)
    {
        string key = japanese + "|" + source;
        if (!warned.Add(key))
        {
            return;
        }

        Debug.LogWarning("Tokyo RPG reading needed: \"" + japanese + "\" has kanji and no kana reading, so Romaji was left blank.");
    }
}
