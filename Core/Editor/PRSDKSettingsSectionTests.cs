#if PRSDK_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Помощник окон с превью: пути полей, сравнение со значением по умолчанию и сброс.
/// </summary>
/// <remarks>
/// Работает на временной копии настроек: настоящий ассет проекта тест не трогает.
/// </remarks>
public class PRSDKSettingsSectionTests
{
    private const string ShowDuration = "Custom.ShowDuration";

    private PRSDKSettings settings;
    private PRSDKSettingsSection section;

    [SetUp]
    public void SetUp()
    {
        settings = ScriptableObject.CreateInstance<PRSDKSettings>();
        settings.hideFlags = HideFlags.HideAndDontSave;
        section = new PRSDKSettingsSection("WindowTransition", settings);

        Assert.That(section.Begin(), Is.True, "раздел находится по имени свойства");
    }

    [TearDown]
    public void TearDown()
    {
        section.Dispose();
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void Find_TranslatesPropertyNamesToBackingFields()
    {
        SerializedProperty property = section.Find(ShowDuration);

        Assert.That(property, Is.Not.Null);
        Assert.That(property.propertyPath, Does.EndWith("<ShowDuration>k__BackingField"));
    }

    [Test]
    public void IsDefault_FollowsValueChanges()
    {
        SerializedProperty property = section.Find(ShowDuration);

        Assert.That(section.IsDefault(property), Is.True, "свежие настройки — значения по умолчанию");

        property.floatValue = 1.25f;
        section.End();

        Assert.That(section.IsDefault(property), Is.False);
    }

    [Test]
    public void ResetToDefault_RestoresConstructorValue()
    {
        float standard = new MonoWindowTransition().ShowDuration;
        SerializedProperty property = section.Find(ShowDuration);

        property.floatValue = 1.25f;
        section.End();

        section.ResetToDefault(property);
        section.End();

        Assert.That(settings.WindowTransition.Custom.ShowDuration, Is.EqualTo(standard).Within(1e-5f));
    }

    [Test]
    public void ResetAll_RestoresWholeSection()
    {
        section.Find("Enabled").boolValue = false;
        section.Find(ShowDuration).floatValue = 2f;
        section.End();

        section.ResetAll();
        section.End();

        Assert.That(settings.WindowTransition.Enabled, Is.True);
        Assert.That(section.IsDefault(section.Root), Is.True);
    }
}
#endif
