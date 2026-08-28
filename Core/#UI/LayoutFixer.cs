using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LayoutFixer : PRMonoBehaviour
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        StartCoroutine(FixLayout());
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PRUnitySDK.LanguageManager.OnChangeLangEvent += Translate_OnChangeLangEvent;
    }

    private void Translate_OnChangeLangEvent(string obj)
    {
        StartCoroutine(FixLayout());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        PRUnitySDK.LanguageManager.OnChangeLangEvent -= Translate_OnChangeLangEvent;
    }

    public IEnumerator FixLayout()
    {
        yield return new WaitForEndOfFrame();
        var layout = GetComponent<RectTransform>();
        if (layout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
    }

    public static void FixLayout(GameObject root)
    {
        if (root == null)
            return;

        root.RefreshLayoutGroupsImmediateAndRecursive();
    }

    public static IEnumerator FixLayoutCorutina(GameObject root)
    {
        yield return new WaitForEndOfFrame();

        if (root != null && root.gameObject.activeSelf)
            root.RefreshLayoutGroupsImmediateAndRecursive();
    }
}
