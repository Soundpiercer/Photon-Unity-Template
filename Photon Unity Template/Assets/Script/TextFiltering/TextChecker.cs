using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// Nickname Check needs extra check (RegEx check)
public enum TextCheckMode
{
    Chat,
    Nickname,
}

// Result status that derived from the checker.
public enum TextCheckResult
{
    Valid,

    /// <summary>
    /// Contains censored words in the input.
    /// </summary>
    Invalid_Censored,

    /// <summary>
    /// Input has inappropriate length.
    /// </summary>
    Invalid_Length,

    /// <summary>
    /// Contains inappropriate characters (blank, emoji, etc.)
    /// </summary>
    Invalid_RegEx,
}

// TextChecker Main Class.
public class TextChecker
{
    /// <summary>
    /// Data that extracted from the CensoredWord CSV.
    /// </summary>
    private static string censoredWordsCSVDataString;

    /// <summary>
    /// Censored words container.
    /// </summary>
    private static Trie censoredWordsTrie;
    private static Trie CensoredWordsTrie
    {
        get
        {
            if (censoredWordsTrie == null)
            {
                BuildTrieFromCSV();
            }

            return censoredWordsTrie;
        }
    }

    public static void Init(TextAsset textAsset)
    {
        censoredWordsCSVDataString = textAsset.text;
        BuildTrieFromCSV();
    }

    private static void BuildTrieFromCSV()
    {
        // 1. Trie Init
        censoredWordsTrie = new Trie();

        // 2. Parse CSVs.
        List<CensoredWord> censoredWordList = new List<CensoredWord>
        { new CensoredWord("fuck"), new CensoredWord("shit") };

        // 3. Add to Trie
        foreach (CensoredWord word in censoredWordList)
        {
            censoredWordsTrie.Add(word.Key);
        }

        // 4. Build Tries
        censoredWordsTrie.Build();
    }

    /// <summary>
    /// Checks if the input string has error. (using return values)
    /// </summary>
    /// <param name="text">input string</param>
    /// <param name="mode">Chat or Nickname</param>
    /// <returns>isError</returns>
    public static bool Check(string text, TextCheckMode mode)
    {
        string loweredText = text.ToLower(); // is not case-specific

        if (mode == TextCheckMode.Chat)
        {
            if (hasCensoredWords(loweredText, mode))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else // TextCheckMode.Nickname
        {
            if (string.IsNullOrEmpty(loweredText))
            {
                return true;
            }
            else if (hasCensoredWords(loweredText, mode))
            {
                return true;
            }
            else if (IsRegExError(loweredText))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Checks if the input string has error and executes the callback. (using callback)
    /// </summary>
    /// <param name="text">Input string</param>
    /// <param name="mode">Check Mode</param>
    /// <param name="callBack">Action after the callback</param>
    public static void CheckWithCallback(string text, TextCheckMode mode, Action<TextCheckResult> callBack = null)
    {
        string loweredText = text.ToLower(); // is not case-specific

        if (mode == TextCheckMode.Chat)
        {
            if (hasCensoredWords(loweredText, mode))
            {
                callBack(TextCheckResult.Invalid_Censored);
            }
            else
            {
                callBack(TextCheckResult.Valid);
            }
        }
        else // TextCheckMode.Nickname
        {
            if (string.IsNullOrEmpty(loweredText))
            {
                callBack(TextCheckResult.Invalid_Length);
            }
            else if (hasCensoredWords(loweredText, mode))
            {
                callBack(TextCheckResult.Invalid_Censored);
            }
            else if (IsRegExError(loweredText))
            {
                callBack(TextCheckResult.Invalid_RegEx);
            }
            else
            {
                callBack(TextCheckResult.Valid);
            }
        }
    }

    /// <summary>
    /// Censored Word Check.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static bool hasCensoredWords(string text, TextCheckMode mode)
    {
        bool flag = false;

        // The result becomes true at any single occurrences.
        foreach (string word in CensoredWordsTrie.Find(text))
        {
            flag = true;
        }

        return flag;
    }

    /// <summary>
    /// RegEx Check.
    /// </summary>
    /// <param name="text">Input string.</param>
    /// <returns></returns>
    private static bool IsRegExError(string text)
    {
        // Filters all normal characters we want to use.
        string filtered = Regex.Replace(text, @"[ ^0-9a-zA-Z가-힣ㄱ-ㅎㅏ-ㅣ!?,.\s ]{1,12}", "", RegexOptions.Singleline);

        // If no character remains, we can assume that there exists an inappropriate character.
        if (!string.IsNullOrEmpty(filtered))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}