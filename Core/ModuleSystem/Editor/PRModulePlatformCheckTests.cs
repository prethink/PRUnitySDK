#if PRSDK_TESTS
using System.Collections.Generic;
using System.Linq;
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
