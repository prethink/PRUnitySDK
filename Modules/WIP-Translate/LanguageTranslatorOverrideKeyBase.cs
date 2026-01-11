using UnityEngine;

public abstract class LanguageTranslatorOverrideKeyBase : MonoBehaviour, IOverrideTranslateKey
{
    public abstract string GetKey();

    public abstract string SetKey(string key);
}
