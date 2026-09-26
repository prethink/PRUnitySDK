#if PRSDK_TESTS
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Разбор флагов проекта: одни правила для любой площадки.
/// </summary>
public class RemoteFlagsTests
{
    /// <summary>
    /// Флаги из словаря — площадка для теста.
    /// </summary>
    private sealed class DictionaryFlags : IRemoteFlags
    {
        private readonly Dictionary<string, string> values;

        public DictionaryFlags(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public bool TryGetString(string name, out string value) => values.TryGetValue(name, out value);
    }

    private enum Mode
    {
        Easy,
        Hard
    }

    private IRemoteFlags flags;

    [SetUp]
    public void SetUp()
    {
        flags = new DictionaryFlags(new Dictionary<string, string>
        {
            { "int", " 7 " },
            { "negative", "-3" },
            { "float_dot", "0.5" },
            { "float_comma", "0,5" },
            { "bool_word", "True" },
            { "bool_digit", "0" },
            { "bool_yes", "yes" },
            { "text", "hello" },
            { "mode", "hard" },
            { "not_int", "1.5" },
        });
    }

    [Test]
    public void Int_TrimmedAndSigned()
    {
        Assert.AreEqual(7, flags.GetInt("int"));
        Assert.AreEqual(-3, flags.GetInt("negative"));
    }

    [Test]
    public void Int_FractionIsNotInt_Fallback()
    {
        Assert.IsFalse(flags.TryGetInt("not_int", out _));
        Assert.AreEqual(42, flags.GetInt("not_int", 42));
    }

    [Test]
    public void Float_DotAndComma()
    {
        Assert.AreEqual(0.5f, flags.GetFloat("float_dot"));
        Assert.AreEqual(0.5f, flags.GetFloat("float_comma"));
    }

    [Test]
    public void Bool_WordsAndDigits()
    {
        Assert.IsTrue(flags.GetBool("bool_word"));
        Assert.IsTrue(flags.TryGetBool("bool_digit", out bool digit));
        Assert.IsFalse(digit);
        Assert.IsTrue(flags.GetBool("bool_yes"));
        Assert.IsFalse(flags.TryGetBool("text", out _));
    }

    [Test]
    public void Enum_CaseInsensitive()
    {
        Assert.AreEqual(Mode.Hard, flags.GetEnum("mode", Mode.Easy));
        Assert.AreEqual(Mode.Easy, flags.GetEnum("text", Mode.Easy));
    }

    [Test]
    public void Missing_ReturnsFallback()
    {
        Assert.AreEqual("none", flags.GetString("missing", "none"));
        Assert.AreEqual(5, flags.GetInt("missing", 5));
        Assert.IsTrue(flags.GetBool("missing", true));
    }
}
#endif
