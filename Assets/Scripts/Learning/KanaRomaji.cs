using System.Collections.Generic;
using System.Text;

/// <summary>
/// Turns hiragana and katakana into Hepburn romaji.
/// は stays ha and へ stays he. Particle readings such as wa and e belong in an authored Romaji field,
/// because the same kana can be a particle or part of a word.
/// を is written o. The long vowel mark ー repeats the vowel, so ラーメン becomes raamen.
/// This does not read kanji. JapaneseReading refuses kanji that has no kana reading.
/// </summary>
public static class KanaRomaji
{
    static readonly Dictionary<string, string> syllables = BuildSyllables();

    public static string ToRomaji(string kana)
    {
        if (string.IsNullOrWhiteSpace(kana))
        {
            return "";
        }

        StringBuilder result = new StringBuilder();
        bool doubleNext = false;
        for (int i = 0; i < kana.Length; i++)
        {
            char character = ToHiragana(kana[i]);
            if (character == 'っ')
            {
                doubleNext = true;
                continue;
            }

            if (character == 'ー')
            {
                Lengthen(result);
                continue;
            }

            string key = character.ToString();
            if (i + 1 < kana.Length && IsSmallVowel(ToHiragana(kana[i + 1])))
            {
                key += ToHiragana(kana[i + 1]);
                i++;
            }

            if (!syllables.TryGetValue(key, out string romaji))
            {
                result.Append(kana[i]);
                doubleNext = false;
                continue;
            }

            if (doubleNext && romaji.Length > 0)
            {
                result.Append(romaji.StartsWith("ch") ? 't' : romaji[0]);
                doubleNext = false;
            }

            result.Append(romaji);
        }

        return result.ToString();
    }

    public static bool ContainsKanji(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character >= '一' && character <= '龯')
            {
                return true;
            }
        }

        return false;
    }

    static void Lengthen(StringBuilder result)
    {
        if (result.Length == 0)
        {
            return;
        }

        char last = result[result.Length - 1];
        if (last == 'a' || last == 'i' || last == 'u' || last == 'e' || last == 'o')
        {
            result.Append(last);
        }
    }

    static char ToHiragana(char character)
    {
        if (character >= 'ァ' && character <= 'ン')
        {
            return (char)(character - 0x60);
        }

        return character;
    }

    static bool IsSmallVowel(char character)
    {
        return character == 'ゃ' || character == 'ゅ' || character == 'ょ'
            || character == 'ぁ' || character == 'ぃ' || character == 'ぅ' || character == 'ぇ' || character == 'ぉ';
    }

    static Dictionary<string, string> BuildSyllables()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        Add(map, "あ", "a");
        Add(map, "い", "i");
        Add(map, "う", "u");
        Add(map, "え", "e");
        Add(map, "お", "o");
        Add(map, "か", "ka");
        Add(map, "き", "ki");
        Add(map, "く", "ku");
        Add(map, "け", "ke");
        Add(map, "こ", "ko");
        Add(map, "さ", "sa");
        Add(map, "し", "shi");
        Add(map, "す", "su");
        Add(map, "せ", "se");
        Add(map, "そ", "so");
        Add(map, "た", "ta");
        Add(map, "ち", "chi");
        Add(map, "つ", "tsu");
        Add(map, "て", "te");
        Add(map, "と", "to");
        Add(map, "な", "na");
        Add(map, "に", "ni");
        Add(map, "ぬ", "nu");
        Add(map, "ね", "ne");
        Add(map, "の", "no");
        Add(map, "は", "ha");
        Add(map, "ひ", "hi");
        Add(map, "ふ", "fu");
        Add(map, "へ", "he");
        Add(map, "ほ", "ho");
        Add(map, "ま", "ma");
        Add(map, "み", "mi");
        Add(map, "む", "mu");
        Add(map, "め", "me");
        Add(map, "も", "mo");
        Add(map, "や", "ya");
        Add(map, "ゆ", "yu");
        Add(map, "よ", "yo");
        Add(map, "ら", "ra");
        Add(map, "り", "ri");
        Add(map, "る", "ru");
        Add(map, "れ", "re");
        Add(map, "ろ", "ro");
        Add(map, "わ", "wa");
        Add(map, "を", "o");
        Add(map, "ん", "n");
        Add(map, "が", "ga");
        Add(map, "ぎ", "gi");
        Add(map, "ぐ", "gu");
        Add(map, "げ", "ge");
        Add(map, "ご", "go");
        Add(map, "ざ", "za");
        Add(map, "じ", "ji");
        Add(map, "ず", "zu");
        Add(map, "ぜ", "ze");
        Add(map, "ぞ", "zo");
        Add(map, "だ", "da");
        Add(map, "ぢ", "ji");
        Add(map, "づ", "zu");
        Add(map, "で", "de");
        Add(map, "ど", "do");
        Add(map, "ば", "ba");
        Add(map, "び", "bi");
        Add(map, "ぶ", "bu");
        Add(map, "べ", "be");
        Add(map, "ぼ", "bo");
        Add(map, "ぱ", "pa");
        Add(map, "ぴ", "pi");
        Add(map, "ぷ", "pu");
        Add(map, "ぺ", "pe");
        Add(map, "ぽ", "po");
        Add(map, "きゃ", "kya");
        Add(map, "きゅ", "kyu");
        Add(map, "きょ", "kyo");
        Add(map, "しゃ", "sha");
        Add(map, "しゅ", "shu");
        Add(map, "しょ", "sho");
        Add(map, "ちゃ", "cha");
        Add(map, "ちゅ", "chu");
        Add(map, "ちょ", "cho");
        Add(map, "にゃ", "nya");
        Add(map, "にゅ", "nyu");
        Add(map, "にょ", "nyo");
        Add(map, "ひゃ", "hya");
        Add(map, "ひゅ", "hyu");
        Add(map, "ひょ", "hyo");
        Add(map, "みゃ", "mya");
        Add(map, "みゅ", "myu");
        Add(map, "みょ", "myo");
        Add(map, "りゃ", "rya");
        Add(map, "りゅ", "ryu");
        Add(map, "りょ", "ryo");
        Add(map, "ぎゃ", "gya");
        Add(map, "ぎゅ", "gyu");
        Add(map, "ぎょ", "gyo");
        Add(map, "じゃ", "ja");
        Add(map, "じゅ", "ju");
        Add(map, "じょ", "jo");
        Add(map, "びゃ", "bya");
        Add(map, "びゅ", "byu");
        Add(map, "びょ", "byo");
        Add(map, "ぴゃ", "pya");
        Add(map, "ぴゅ", "pyu");
        Add(map, "ぴょ", "pyo");
        return map;
    }

    static void Add(Dictionary<string, string> map, string kana, string romaji)
    {
        map[kana] = romaji;
    }
}
