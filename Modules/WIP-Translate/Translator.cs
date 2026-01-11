using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Translator : ITranslateble
{
    [SerializeField] protected TranslateDictionary translates = new();

    [SerializeField] protected string key { get; set; }

    public string Key => key;

    public virtual string GetTranslateByLang(string lang)
    {
        return Translator.GetTranslateByLang(translates.Dictionary, lang, key);
    }

    public virtual string GetTranslate()
    {
        return GetTranslateByLang(PRUnitySDK.CurrentLang);
    }

    public virtual string GetTranslateWithArgs(params string[] args)
    {
        return Translator.GetTranslateWithArgs(this, args);
    }

    public static string GetTranslateByLang(IReadOnlyDictionary<LangType, string> translates, string lang, string key)
    {
        var convertLang = LocalizationUtils.GetLanguageEnum(lang);

        if (translates.Count == 0)
            return "";

        if (translates.TryGetValue(convertLang, out var translate))
            return translate;

        return $"{key}_for_lang_{lang}";
    }

    public static string GetTranslateByLangWithParams(IReadOnlyDictionary<LangType, string> translates, string lang, string key, params object[] args)
    {
        var convertLang = LocalizationUtils.GetLanguageEnum(lang);

        if (translates.TryGetValue(convertLang, out var translate))
            return string.Format(translate, args);

        return $"{key}_for_lang_{lang}";
    }

    public static string GetTranslate(IReadOnlyDictionary<LangType, string> translates, string key)
    {
        return GetTranslateByLang(translates, PRUnitySDK.CurrentLang, key);
    }

    public static string GetTranslate(IReadOnlyDictionary<LangType, string> translates)
    {
        return GetTranslateByLang(translates, PRUnitySDK.CurrentLang, "NoKey");
    }

    public static string GetTranslateWithArgs(IReadOnlyDictionary<LangType, string> translates, string key, params string[] args)
    {
        return GetTranslateByLangWithParams(translates, PRUnitySDK.CurrentLang, key, args);
    }

    public static string GetTranslateWithArgs(ITranslateble translateble, params string[] args)
    {
        return GetTranslateByLangWithParams(translateble.GetTranslateDictionary(), PRUnitySDK.CurrentLang, translateble.Key, args);
    }

    public void SetKey(string key)
    {
        this.key = key;
    }

    public IReadOnlyDictionary<LangType, string> GetTranslateDictionary()
    {
        return translates.Dictionary;
    }

    public bool HasTranslate()
    {
        return translates.Dictionary.Count > 0;
    }

    public Translator(string key, Dictionary<LangType, string> translates)
    {
        this.key = key;

        foreach (var item in translates)
            this.translates[item.Key] = item.Value;
    }

    public Translator[] Split(int maxLength)
    {
        return Split(Key, maxLength, translates.Dictionary);
    }

    public static Translator[] Split(string key, int maxLength, IReadOnlyDictionary<LangType, string> translates)
    {
        Dictionary<LangType, List<string>> chunksByLang = new();
        int maxChunks = 0;

        foreach (var pair in translates)
        {
            var chunks = TextUtils.ChunkText(pair.Value, maxLength);
            chunksByLang[pair.Key] = chunks;
            if (chunks.Count > maxChunks)
                maxChunks = chunks.Count;
        }

        Translator[] result = new Translator[maxChunks];

        for (int i = 0; i < maxChunks; i++)
        {
            var part = new Dictionary<LangType, string>();

            foreach (var lang in chunksByLang.Keys)
            {
                var list = chunksByLang[lang];
                part[lang] = i < list.Count ? list[i] : "";
            }

            result[i] = new Translator($"{key}_{i}", part);
        }

        return result;
    }

    private List<string> SplitByLength(string input, int maxLength)
    {
        List<string> chunks = new();
        for (int i = 0; i < input.Length; i += maxLength)
        {
            int length = Math.Min(maxLength, input.Length - i);
            chunks.Add(input.Substring(i, length));
        }
        return chunks;
    }
}
