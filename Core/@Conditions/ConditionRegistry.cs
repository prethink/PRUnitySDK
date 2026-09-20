using System;
using System.Collections.Generic;

/// <summary>
/// Правила, которые игра объявляет кодом и выбирает по имени.
/// </summary>
/// <remarks>
/// Второй способ описать условие, рядом с классами <see cref="ICondition"/>. Класс нужен
/// там, где у правила есть настройки: порог, ресурс, сравнение. Реестр — там, где
/// настраивать нечего и весь ответ лежит в коде игры: пройден ли туториал, идёт ли бой
/// с боссом. Такие правила классом были бы файлом с нулём полей.
/// <para>
/// В инспекторе такое правило выбирает <see cref="RegisteredCondition"/>.
/// </para>
/// </remarks>
public class ConditionRegistry : SingletonProviderBase<ConditionRegistry>
{
    private readonly Dictionary<Enumeration, Func<bool>> rules = new();

    /// <summary>
    /// Имена, о которых уже пожаловались.
    /// </summary>
    /// <remarks>
    /// Условие спрашивают на каждый удар и на каждый кадр, поэтому жалоба на незнакомое
    /// имя без этого набора залила бы лог тысячами одинаковых строк за секунду.
    /// </remarks>
    private readonly HashSet<Enumeration> reported = new();

    /// <summary>
    /// Объявляет правило.
    /// </summary>
    /// <remarks>
    /// Зовут на готовности SDK — раньше игра ещё не собрана, а ассеты уже могут
    /// спрашивать. Повторная регистрация заменяет прежнее правило: так его переопределяет
    /// сцена, которой нужно своё.
    /// <para>
    /// Правило спрашивают часто, поэтому оно обязано быть дешёвым и ничего не менять.
    /// </para>
    /// </remarks>
    /// <param name="key">Имя правила.</param>
    /// <param name="rule">Ответ правила.</param>
    public void Register(Enumeration key, Func<bool> rule)
    {
        if (key == null || rule == null)
            return;

        rules[key] = rule;
        reported.Remove(key);
    }

    /// <summary>
    /// Убирает правило.
    /// </summary>
    /// <remarks>
    /// Нужен тому, кто объявил правило от имени сцены: со сменой сцены замыкание держало бы
    /// её объекты, а ответ считался бы по тому, чего уже нет.
    /// </remarks>
    public void Unregister(Enumeration key)
    {
        if (key != null)
            rules.Remove(key);
    }

    /// <summary>
    /// Объявлено ли правило с таким именем.
    /// </summary>
    public bool IsRegistered(Enumeration key)
    {
        return key != null && rules.ContainsKey(key);
    }

    /// <summary>
    /// Выполнено ли правило.
    /// </summary>
    /// <remarks>
    /// Незнакомое имя — единственное место во всей системе условий, где о недонастроенном
    /// говорят вслух. В остальных случаях пустая настройка означает «правила нет»,
    /// а здесь имя выбрано, но правила за ним не оказалось: это опечатка или
    /// несостоявшаяся регистрация, и молчать о такой разницей вреднее.
    /// <para>
    /// Возвращается при этом <c>true</c> — как и везде в системе: недонастроенное
    /// не запирает. Ошибка в логе есть, игра не встала.
    /// </para>
    /// </remarks>
    /// <param name="key">Имя правила.</param>
    /// <returns>Ответ правила; <c>true</c>, если правила с таким именем нет.</returns>
    public bool Evaluate(Enumeration key)
    {
        if (key == null)
            return true;

        if (rules.TryGetValue(key, out Func<bool> rule))
            return rule.Invoke();

        if (reported.Add(key))
            PRLog.WriteError(this, $"Правило [{key}] не объявлено: условие считается выполненным. " +
                                   "Проверьте регистрацию в ConditionRegistry.");

        return true;
    }

    /// <summary>
    /// Убирает все правила.
    /// </summary>
    public void Clear()
    {
        rules.Clear();
        reported.Clear();
    }
}
