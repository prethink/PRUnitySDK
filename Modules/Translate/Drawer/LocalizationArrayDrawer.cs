using UnityEditor;

[CustomPropertyDrawer(typeof(LocalizationArray))]
public class LocalizationArrayDrawer : LocalizationLangDrawerBase
{
    protected override SerializedProperty GetValues(SerializedProperty property)
    {
        return property.FindPropertyRelative("values");
    }
}