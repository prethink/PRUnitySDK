using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
public class SpritePreviewDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (SpritePreviewAttribute)attribute;

        Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect previewRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 4, position.width, attr.Height);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (property.objectReferenceValue == null)
            return;

        var sprite = property.objectReferenceValue as Sprite;
        if (sprite == null)
            return;

        // Получаем текстуру превью
        Texture2D texture = AssetPreview.GetAssetPreview(sprite)
                            ?? AssetPreview.GetMiniThumbnail(sprite);

        if (texture != null)
        {
            GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.HelpBox(previewRect, "No Preview", MessageType.Info);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (SpritePreviewAttribute)attribute;
        return EditorGUIUtility.singleLineHeight + 4 + attr.Height;
    }
}