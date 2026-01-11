public class CustomLanguageTranslator : LanguageTranslator
{
    public override string KeyWord { get; protected set; } = string.Empty;
    public override string[] GetKeys()
    {
        return new string[] { TranslateKeysBase.EMPTY };
    }
} 