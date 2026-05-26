using UnityEngine;

[RequireComponent(typeof(CustomLanguageTranslator))]
public class LocalLanguageTranslator : PRMonoBehaviour
{
    [SerializeField] Translator translator; 

    private CustomLanguageTranslator customLanguageTranslator;

    protected override void Awake()
    {
        base.Awake();
        customLanguageTranslator.SetTranslatable(translator);
    }

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        customLanguageTranslator ??= GetComponent<CustomLanguageTranslator>();
    }
}
