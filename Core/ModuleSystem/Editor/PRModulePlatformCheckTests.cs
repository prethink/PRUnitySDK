#if PRSDK_TESTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Интеграция не своей платформы в сборке: веб-площадка в сборке под PC или Android.
/// </summary>
public class PRModulePlatformCheckTests
{
    private const string Web = "WebGL";
    private const string Pc = "StandaloneWindows64, StandaloneWindows, StandaloneLinux64, StandaloneOSX";

    private readonly List<PRModuleManifest> created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (PRModuleManifest manifest in created)
            Object.DestroyImmediate(manifest);

        created.Clear();
    }

    private PRModuleManifest Module(string id, string platform, bool codeOnlyOnPlatform = false)
    {
        var manifest = ScriptableObject.CreateInstance<PRModuleManifest>();
        created.Add(manifest);

        var serialized = new SerializedObject(manifest);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = id;
        serialized.FindProperty("platform").stringValue = platform;
        serialized.FindProperty("codeOnlyOnPlatform").boolValue = codeOnlyOnPlatform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return manifest;
    }

    private static string[] Foreign(BuildTarget target, ICollection<string> disabled, params PRModuleManifest[] manifests)
    {
        return PRModulePlatformCheck.FindForeign(manifests, disabled, target).Select(manifest => manifest.Id).ToArray();
    }

    private static readonly string[] None = new string[0];

    [Test]
    public void WebPlatformInPcBuildIsForeign()
    {
        PRModuleManifest web = Module("WEB", Web);
        PRModuleManifest steam = Module("STEAM", Pc, true);

        CollectionAssert.AreEqual(new[] { "WEB" }, Foreign(BuildTarget.StandaloneWindows64, None, web, steam));
        CollectionAssert.AreEqual(new[] { "WEB" }, Foreign(BuildTarget.Android, None, web, steam));
    }

    [Test]
    public void WebPlatformInWebBuildIsFine()
    {
        CollectionAssert.IsEmpty(Foreign(BuildTarget.WebGL, None, Module("WEB", Web)));
    }

    [Test]
    public void NativeIntegrationInWebBuildIsFine()
    {
        // Её код обёрнут в символ платформы и в чужую сборку не попадает.
        CollectionAssert.IsEmpty(Foreign(BuildTarget.WebGL, None, Module("STEAM", Pc, true), Module("WEB", Web)));
    }

    [Test]
    public void NativeIntegrationWithoutPlatformGuardIsForeign()
    {
        CollectionAssert.AreEqual(new[] { "PC" }, Foreign(BuildTarget.WebGL, None, Module("PC", Pc)));
    }

    [Test]
    public void DisabledIntegrationIsFine()
    {
        CollectionAssert.IsEmpty(Foreign(BuildTarget.StandaloneWindows64, new[] { "WEB" }, Module("WEB", Web)));
    }

    [Test]
    public void ModuleWithoutPlatformIsFine()
    {
        CollectionAssert.IsEmpty(Foreign(BuildTarget.StandaloneWindows64, None, Module("ANY", string.Empty)));
    }

    [Test]
    public void MessageNamesModulePlatformAndTarget()
    {
        string message = PRModulePlatformCheck.Describe(Module("WEB", Web), BuildTarget.StandaloneWindows64);

        StringAssert.Contains("WEB", message);
        StringAssert.Contains(Web, message);
        StringAssert.Contains(BuildTarget.StandaloneWindows64.ToString(), message);
    }

    /// <summary>
    /// Набор отключённых модулей берётся из символов той платформы, которую спросили: у двух платформ он может
    /// различаться (символы правят руками и сливают в git).
    /// </summary>
    [Test]
    public void DisabledModulesComeFromTheSymbolsOfThatPlatform()
    {
        const string webSymbols = "SOME_DEFINE;PRSDK_DISABLE_STEAM; PRSDK_DISABLE_RUSTORE ;PRSDK_TESTS";
        const string pcSymbols = "PRSDK_DISABLE_WEB;PRSDK_TESTS";

        CollectionAssert.AreEquivalent(new[] { "STEAM", "RUSTORE" }, PRModuleDefines.ParseDisabledIds(webSymbols));
        CollectionAssert.AreEquivalent(new[] { "WEB" }, PRModuleDefines.ParseDisabledIds(pcSymbols));
        CollectionAssert.IsEmpty(PRModuleDefines.ParseDisabledIds(null));

        PRModuleManifest web = Module("WEB", Web);

        // Сборка под PC: площадка отключена в символах PC — сборку не останавливает, даже если в символах WebGL включена.
        CollectionAssert.IsEmpty(Foreign(BuildTarget.StandaloneWindows64, PRModuleDefines.ParseDisabledIds(pcSymbols), web));
        CollectionAssert.AreEqual(new[] { "WEB" },
            Foreign(BuildTarget.StandaloneWindows64, PRModuleDefines.ParseDisabledIds(webSymbols), web));
    }

    [Test]
    public void ActivePlatformSymbolsMatchSelectedGroup()
    {
        CollectionAssert.AreEquivalent(PRModuleDefines.ReadDisabledIds(),
            PRModuleDefines.ReadDisabledIds(EditorUserBuildSettings.activeBuildTarget));
    }

    private static readonly Regex PlatformSymbol = new(@"(?<!!\s*)\bUNITY_(ANDROID|IOS|STANDALONE\w*)\b");

    private static readonly Regex SelfConnecting =
        new(@"\[\s*(OverrideProperty|OverrideBootstrap|RuntimeInitializeOnLoadMethod|MethodHook)\b");

    /// <summary>
    /// Строки скрипта, которые подключают его сами (переопределение сервиса, часть загрузчика, автозапуск, хук) и при
    /// этом не лежат внутри условия с символом платформы.
    /// </summary>
    /// <remarks>
    /// Разбирает вложенность <c>#if</c> / <c>#elif</c> / <c>#else</c> / <c>#endif</c>: наличия символа платформы
    /// где-то в файле мало — подключение должно стоять под ним. Ветка <c>#else</c> и условие с отрицанием
    /// (<c>!UNITY_ANDROID</c>) защитой не считаются. Комментарии пропускаются.
    /// </remarks>
    private static List<string> UnguardedConnections(string text)
    {
        var unguarded = new List<string>();
        var guards = new Stack<bool>();
        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();

            if (line.StartsWith("#if"))
                guards.Push(PlatformSymbol.IsMatch(line));
            else if (line.StartsWith("#elif") && guards.Count > 0)
            {
                guards.Pop();
                guards.Push(PlatformSymbol.IsMatch(line));
            }
            else if (line.StartsWith("#else") && guards.Count > 0)
            {
                guards.Pop();
                guards.Push(false);
            }
            else if (line.StartsWith("#endif") && guards.Count > 0)
                guards.Pop();
            else if (!line.StartsWith("//") && SelfConnecting.IsMatch(line) && !guards.Contains(true))
                unguarded.Add($"{index + 1}: {line}");
        }

        return unguarded;
    }

    [Test]
    public void ConnectionOutsidePlatformConditionIsFound()
    {
        const string leaking =
            "#if !PRSDK_DISABLE_X\n" +
            "#if UNITY_ANDROID\n" +
            "public class Guarded { [MethodHook(MethodHookStage.SDK)] void A() { } }\n" +
            "#endif\n" +
            "public partial class PRUnitySDK\n" +
            "{\n" +
            "    [OverrideProperty(typeof(IAdManager), 4)]\n" +
            "    private static void Leak() { }\n" +
            "}\n" +
            "#endif\n";

        List<string> found = UnguardedConnections(leaking);

        Assert.AreEqual(1, found.Count, string.Join("; ", found));
        StringAssert.StartsWith("7:", found[0]);
    }

    [Test]
    public void ConnectionInsidePlatformConditionIsFine()
    {
        const string guarded =
            "#if !PRSDK_DISABLE_X\n" +
            "#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX\n" +
            "public partial class Bootstrap\n" +
            "{\n" +
            "#if UNITY_EDITOR\n" +
            "    [OverrideBootstrap]\n" +
            "#endif\n" +
            "    [MethodHook(MethodHookStage.PostOnEnable)]\n" +
            "    private void OnEnableSteam() { }\n" +
            "}\n" +
            "#endif\n" +
            "#endif\n";

        CollectionAssert.IsEmpty(UnguardedConnections(guarded));
    }

    [TestCase("#if UNITY_ANDROID\n#else\n[OverrideProperty(typeof(IAdManager), 4)]\n#endif\n", TestName = "ElseBranchIsNotAGuard")]
    [TestCase("#if !UNITY_ANDROID\n[OverrideProperty(typeof(IAdManager), 4)]\n#endif\n", TestName = "NegatedSymbolIsNotAGuard")]
    [TestCase("#if UNITY_WEBGL\n[RuntimeInitializeOnLoadMethod]\n#endif\n", TestName = "OtherPlatformIsNotAGuard")]
    [TestCase("#if UNITY_ANDROID\n#endif\n[MethodHook(MethodHookStage.SDK)]\n", TestName = "ClosedConditionIsNotAGuard")]
    [TestCase("#if UNITY_EDITOR\n#elif UNITY_WEBGL\n[OverrideBootstrap]\n#endif\n", TestName = "ElifOtherPlatformIsNotAGuard")]
    public void ConnectionWithoutPlatformGuardIsFound(string text)
    {
        Assert.AreEqual(1, UnguardedConnections(text).Count);
    }

    [Test]
    public void CommentedConnectionIsIgnored()
    {
        CollectionAssert.IsEmpty(UnguardedConnections("// [OverrideProperty(typeof(IAdManager), 4)]\n"));
    }

    /// <summary>
    /// Отметка «код только под свою платформу» — обещание манифеста; здесь оно сверяется с файлами.
    /// </summary>
    /// <remarks>
    /// Каждое подключение в скриптах такой интеграции — переопределение сервиса, часть загрузчика, автозапуск, хук —
    /// должно лежать внутри условия с символом платформы. Иначе оно попало бы в чужую сборку вопреки отметке, а
    /// проверка сборки его пропустила бы. Настройки не обёрнуты намеренно — их поля должны жить на любой платформе,
    /// и подключений в них нет.
    /// </remarks>
    [Test]
    public void CodeOnlyOnPlatformIsTrueForTheScripts()
    {
        List<PRModuleInfo> marked = PRModuleCatalog.Load().Where(module => module.Manifest.CodeOnlyOnPlatform).ToList();
        Assert.IsNotEmpty(marked, "В проекте нет интеграций с отметкой.");

        var violations = new List<string>();
        int checkedConnections = 0;

        foreach (PRModuleInfo module in marked)
        {
            Assert.IsNotEmpty(module.Scripts, $"{module.Id}: у модуля не найдено скриптов.");

            foreach (string script in module.Scripts.Where(path => !path.Contains("/Editor/")))
            {
                string text = File.ReadAllText(script);
                checkedConnections += SelfConnecting.Matches(text).Count;

                foreach (string line in UnguardedConnections(text))
                    violations.Add($"{module.Id}: {script}:{line}");
            }
        }

        Assert.Greater(checkedConnections, 0, "Проверка ничего не нашла: в помеченных интеграциях нет ни одного подключения.");
        CollectionAssert.IsEmpty(violations, "Подключения вне условия с символом платформы.");
    }

    /// <summary>
    /// Настоящие манифесты: веб-площадки в чужой сборке ловятся, нативные интеграции помечены как собирающиеся
    /// только под свою платформу.
    /// </summary>
    [Test]
    public void RealIntegrationsAreMarkedByPlatform()
    {
        List<PRModuleManifest> integrations = PRModuleCatalog.Load()
            .Select(module => module.Manifest)
            .Where(manifest => manifest.Platform.Length > 0)
            .ToList();

        Assert.IsNotEmpty(integrations, "В проекте нет интеграций с платформой.");

        foreach (PRModuleManifest manifest in integrations)
        {
            bool web = manifest.SupportsPlatform(BuildTarget.WebGL);

            if (web)
            {
                Assert.IsFalse(manifest.CodeOnlyOnPlatform, $"{manifest.Id}: веб-площадка не обёрнута в символ платформы.");
                CollectionAssert.Contains(Foreign(BuildTarget.StandaloneWindows64, None, manifest), manifest.Id);
            }
            else
            {
                Assert.IsTrue(manifest.CodeOnlyOnPlatform,
                    $"{manifest.Id}: нативная интеграция без отметки «код только под свою платформу» — сборка WebGL с ней будет остановлена.");
            }
        }
    }
}
#endif
