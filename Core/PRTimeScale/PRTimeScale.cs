using System;
using System.Collections.Generic;
using System.Diagnostics;

public class PRTimeScale : SingletonProviderBase<PRTimeScale>, ISingletonInitializer
{
    private Dictionary<Enumeration, float> layers = new Dictionary<Enumeration, float>();

    public const float DefaultTimeScale = 1f;
    private HashSet<Enumeration> activeTaskTimeScaleTemporaly = new();

    public int InitializeOrder => -1;

    public float GetTimeScale(Enumeration layer = null)
    {
        if (layer == null || !layers.TryGetValue(layer, out var value))
            return layers[PRTimeScaleEnumerationProvider.Global];

        return value;
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
        PRLog.WriteDebug(this, $"SetTimeScaleTemporarily: layer={layer}, value={value}, callBackTime={callBackTime}");
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

    public float Resolve(Enumeration layer = null)
    {
        if (layer == null)
            return layers[PRTimeScaleEnumerationProvider.Global];

        var global = layers[PRTimeScaleEnumerationProvider.Global];
        var value = layers[layer];

        return PRUnitySDK.Settings.Project.TimeScaleCombineMode switch
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
    }
}
