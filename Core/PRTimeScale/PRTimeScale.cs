using System;
using System.Collections.Generic;
using System.Linq;

public class PRTimeScale : SingletonProviderBase<PRTimeScale>, ISingletonInitializer
{
    private Dictionary<Enumeration, float> layers = new Dictionary<Enumeration, float>();

    public const float DefaultTimeScale = 1f;
    private HashSet<Enumeration> activeTaskTimeScaleTemporaly = new();
    private TimeScaleCombineMode? combineMode;
    private bool isInitialize;

    public int InitializeOrder => -1;

    /// <summary>
    /// Возвращает true, если хотя бы для одного слоя действует временное изменение.
    /// </summary>
    public bool HasActiveTemporaryTimeScales => activeTaskTimeScaleTemporaly.Count > 0;

    /// <summary>
    /// Возвращает true, если для указанного слоя действует временное изменение.
    /// </summary>
    public bool IsTimeScaleTemporaryActive(Enumeration layer)
    {
        return layer != null && activeTaskTimeScaleTemporaly.Contains(layer);
    }

    public float GetTimeScale(Enumeration layer = null)
    {
        if (layer == null || !layers.TryGetValue(layer, out var value))
            return layers[PRTimeScaleEnumerationProvider.Global];

        return value;
    }

    public float GetGlobalTimeScale()
    {
        if(!isInitialize)
            return DefaultTimeScale;

        return layers[PRTimeScaleEnumerationProvider.Global];
    }

    public void SetGlobalTimeScale(float value)
    {
        layers[PRTimeScaleEnumerationProvider.Global] = value;
        PRTimeScaleEvents.RaiseTimeScaleChange(PRTimeScaleEnumerationProvider.Global, value);
    }

    public void SetTimeScale(Enumeration layer, float value)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        layers[layer] = value;
        PRTimeScaleEvents.RaiseTimeScaleChange(layer, value);
    }

    public void SetGlobalTimeScaleTemporarily(float value, float callBackTime)
    {
        SetTimeScaleTemporarily(PRTimeScaleEnumerationProvider.Global, value, callBackTime);
    }

    public void SetTimeScaleTemporarily(Enumeration layer, float value, float callBackTime)
    {
        if (activeTaskTimeScaleTemporaly.Contains(layer))
            return;

        var previousValue = GetTimeScale(layer);
        this.ExecuteActionWithCallback(
            () => 
            { 
                SetTimeScale(layer, value);
                activeTaskTimeScaleTemporaly.Add(layer);
            }, 
            callBackTime, 
            () => 
            { 
                SetTimeScale(layer, previousValue);
                activeTaskTimeScaleTemporaly.Remove(layer);
            });
    }

    public void Reset()
    {
        combineMode = null;
        foreach (var key in layers.Keys.ToList())
        {
            layers[key] = DefaultTimeScale;
            PRTimeScaleEvents.RaiseTimeScaleChange(key, DefaultTimeScale);
        }
    }

    public float Resolve(Enumeration layer = null)
    {
        if (!isInitialize)
            return DefaultTimeScale;

        var globalLayer = PRTimeScaleEnumerationProvider.Global;
        var global = layers[globalLayer];

        // Global is the root scale, not a child layer. Combining it with itself
        // would square the value in Multiply mode (0.5 -> 0.25) and make global
        // animations run at a different speed than PRTime and physics.
        if (layer == null || layer == globalLayer)
            return global;

        var value = layers[layer];

        var currentSettings = combineMode != null 
            ? combineMode 
            : PRUnitySDK.Settings.Project.TimeScaleCombineMode;

        return currentSettings switch
        {
            TimeScaleCombineMode.Multiply => global * value,
            TimeScaleCombineMode.Max => Math.Max(global, value),
            TimeScaleCombineMode.Min => Math.Min(global, value),
            TimeScaleCombineMode.OverrideGlobal => value,
            _ => global * value
        };
    }

    public void SingletonInitialize()
    {
        var options = new PRTimeScaleEnumerationProvider().GetOptions();
        foreach (var item in options)
            layers.Add(item, DefaultTimeScale);

        isInitialize = true;
    }
}
