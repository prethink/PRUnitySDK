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

    /// <summary>
    /// Отметка «код только под свою платформу» — обещание манифеста; здесь оно сверяется с файлами.
    /// </summary>
    /// <remarks>
    /// Скрипт такой интеграции либо обёрнут в символ платформы, либо ничего не подключает сам: ни переопределения
    /// сервиса, ни части загрузчика, ни автозапуска. Иначе он попал бы в чужую сборку вопреки отметке, а проверка
    /// сборки его пропустила бы. Настройки не обёрнуты намеренно — их поля должны жить на любой платформе.
    /// </remarks>
    [Test]
    public void CodeOnlyOnPlatformIsTrueForTheScripts()
    {
        var platformGuard = new Regex(@"^\s*#if\b.*\bUNITY_(ANDROID|IOS|STANDALONE\w*)\b", RegexOptions.Multiline);
        var selfConnecting = new Regex(@"\[\s*(OverrideProperty|OverrideBootstrap|RuntimeInitializeOnLoadMethod|MethodHook)\b");

        List<PRModuleInfo> marked = PRModuleCatalog.Load().Where(module => module.Manifest.CodeOnlyOnPlatform).ToList();
        Assert.IsNotEmpty(marked, "В проекте нет интеграций с отметкой.");

        var violations = new List<string>();

        foreach (PRModuleInfo module in marked)
        {
            Assert.IsNotEmpty(module.Scripts, $"{module.Id}: у модуля не найдено скриптов.");

            foreach (string script in module.Scripts.Where(path => !path.Contains("/Editor/")))
            {
                string text = File.ReadAllText(script);

                if (!platformGuard.IsMatch(text) && selfConnecting.IsMatch(text))
                    violations.Add($"{module.Id}: {script}");
            }
        }

        CollectionAssert.IsEmpty(violations, "Скрипты подключают себя сами, но не обёрнуты в символ платформы.");
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
