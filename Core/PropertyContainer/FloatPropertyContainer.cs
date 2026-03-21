using System.Collections.Generic;

public class FloatPropertyContainer : PropertyContainerBase<float>
{
    /// <summary>
    /// Получить итоговое значение.
    /// </summary>
    public override float Get(Enumeration key, float defaultValue = 0f)
    {
        if (cache.TryGetValue(key, out var cached))
            return cached;

        if (!modifiers.TryGetValue(key, out var sources))
            return defaultValue;

        float add = 0f;
        float multiply = 1f;
        float? overrideValue = null;
        int highestPriority = int.MaxValue;

        var deadSources = new List<object>();

        foreach (var sourceKvp in sources)
        {
            var source = sourceKvp.Key;

            if (source.IsNull())
            {
                deadSources.Add(source);
                continue;
            }

            var container = sourceKvp.Value;

            // сортировка по приоритету (если нужна строгая логика)
            foreach (var mod in container.modifiers)
            {
                if (mod.type == ModifierTypes.Add)
                {
                    add += mod.value;
                }
                else if (mod.type == ModifierTypes.Multiply)
                {
                    multiply *= mod.value;
                }
                else if (mod.type == ModifierTypes.Override)
                {
                    if (mod.type == ModifierTypes.Override)
                    {
                        if (mod.Priority <= highestPriority)
                        {
                            highestPriority = mod.Priority;
                            overrideValue = mod.value;
                        }
                    }
                }
            }
        }

        // cleanup мёртвых источников
        foreach (var dead in deadSources)
        {
            sources.Remove(dead);
        }

        // если есть override — он имеет приоритет
        if (overrideValue.HasValue)
        {
            cache[key] = overrideValue.Value;
            return overrideValue.Value;
        }

        float result = defaultValue;
        result += add;
        result *= multiply;

        cache[key] = result;

        return result;
    }
}