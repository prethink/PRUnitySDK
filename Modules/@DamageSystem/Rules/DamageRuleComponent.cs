using UnityEngine;

/// <summary>
/// Правило урона этой сцены: действует, пока компонент включён.
/// </summary>
/// <remarks>
/// Обёртка над <see cref="DamageRule"/>: при включении кладёт правило в сервис
/// <see cref="DamageRules"/>, при выключении снимает. Правило, нужное на всех сценах,
/// кладут не сюда, а в настройки проекта (<see cref="DamageRulesSettings"/>).
/// </remarks>
public class DamageRuleComponent : PRMonoBehaviour
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Правило урона сцены.")]
    private DamageRule rule = new MultiplyDamageRule();

    /// <summary>
    /// Правило этого компонента.
    /// </summary>
    public DamageRule Rule => rule;

    protected override void OnEnable()
    {
        base.OnEnable();
        DamageRules.Instance.Add(rule, this);
    }

    protected override void OnDisable()
    {
        DamageRules.Instance.Remove(this);
        base.OnDisable();
    }
}
