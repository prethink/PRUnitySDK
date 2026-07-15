using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TextUtils
{
    public static string GetColoredText(Color color, string text)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    public static float FixWidth(
        TextMeshProUGUI textMesh,
        float baseWidth,
        float percent)
    {
        if (textMesh == null)
            return 0;

        float maxWidth = baseWidth * percent;

        textMesh.ForceMeshUpdate();

        Vector2 preferredSize = textMesh.GetPreferredValues(
            textMesh.text,
            maxWidth,
            Mathf.Infinity);

        float width = Mathf.Min(preferredSize.x, maxWidth);

        textMesh.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width);

        LayoutRebuilder.ForceRebuildLayoutImmediate(textMesh.rectTransform);

        return width;
    }
}
