# HitBox

Модуль связывает физические коллайдеры Unity с `DamageSystem`. Хитбокс принимает вызов `IDamageable.TakeDamage()`, преобразует данные попадания и перенаправляет их в `HealthComponent` связанной сущности.

## Состав модуля

| Компонент | Назначение |
| --- | --- |
| `EntityHitBoxBase` | Общая маршрутизация попадания в связанную сущность |
| `UnitHitBox` | Зона тела, множитель урона и критическое попадание |
| `ItemHitBox` | Хитбокс предмета без дополнительного преобразования урона |
| `EntityLink` | Связь дочернего объекта с родительским `EntityBase` |

## Как проходит попадание

```text
Collider
  -> EntityHitBoxBase.TakeDamage()
  -> GetHandledDamage()
  -> EntityLink.Entity
  -> HealthComponent.TakeDamage()
```

`GetHandledDamage()` вызывается ровно один раз для каждого попадания. Это точка расширения для зон тела, брони на отдельном коллайдере и других локальных модификаторов.

## Настройка сущности

На корневом объекте должны находиться:

- наследник `EntityBase`;
- `HealthComponent`.

На каждом дочернем объекте зоны попадания:

- `Collider`;
- `EntityLink`;
- `UnitHitBox` либо `ItemHitBox`.

`EntityHitBoxBase` автоматически находит `EntityLink` и `Collider` на том же объекте. `EntityLink` автоматически ищет `EntityBase` на текущем или родительском объекте.

Свойство `IsConfigured` показывает, найдены ли ссылка на сущность и коллайдер. Если связанная сущность не имеет `HealthComponent`, `TakeDamage()` возвращает `DamageResult.NotHandled`.

## UnitHitBox

Настройки Inspector:

- `Hit Group` — зона попадания;
- `Damage Multiplier` — множитель урона, не может быть отрицательным;
- `Is Critical` — добавить флаг `DamageType.Critical`.

Зона `Head` считается критической автоматически, даже если `Is Critical` выключен.

Пример стартовых множителей:

| Зона | Множитель |
| --- | ---: |
| `Head` | 4.0 |
| `Chest` | 1.0 |
| `Stomach` | 1.25 |
| `LeftArm`, `RightArm` | 1.0 |
| `LeftLeg`, `RightLeg` | 0.75 |

Значения не зашиты в код и задаются отдельно для каждого компонента.

## ItemHitBox

`ItemHitBox` передаёт исходный `IDamageProvider` без изменений. Используйте его для разрушаемых предметов и объектов, которым не нужны зоны тела.

Если предмету требуется собственный множитель или тип урона, создайте наследника `EntityHitBoxBase` и переопределите `GetHandledDamage()`.

## Вызов из raycast-оружия

Система оружия должна искать `IDamageable` именно на объекте коллайдера, в который попал луч:

```csharp
if (Physics.Raycast(ray, out RaycastHit hit, distance, hitMask) &&
    hit.collider.TryGetComponent<IDamageable>(out var damageable))
{
    DamageResult result = damageable.TakeDamage(
        attacker,
        weapon,
        weapon,
        hit.point);
}
```

В примере `weapon` реализует `IDamageProvider`. Можно передать отдельный `CommonDamage` или цепочку декораторов.

Если хитбоксы размещены не на самом объекте коллайдера, используйте поиск в родителе осознанно:

```csharp
IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
```

Предпочтительно держать `Collider` и `EntityHitBoxBase` вместе: тогда разные зоны тела не будут случайно сведены к одному родительскому обработчику.

## Перегрузки TakeDamage

- без дополнительного аргумента — обычное попадание;
- с `Vector3 point` — передаёт мировую точку и вызывает `HealthComponent.OnHitVector`;
- с `Collider collider` — передаёт коллайдер и вызывает `HealthComponent.OnHitCollider`.

Если в последнюю перегрузку передан `null`, хитбокс использует свой сохранённый `Collider`.

## Создание специализированного хитбокса

```csharp
public sealed class ArmoredHitBox : EntityHitBoxBase
{
    [SerializeField, Range(0f, 1f)]
    private float receivedDamage = 0.5f;

    public override IDamageProvider GetHandledDamage(IDamageProvider damage)
    {
        return new MultiplyDamageDecorator(
            damage,
            receivedDamage,
            addCriticalFlag: false);
    }
}
```

## Проверка префаба

Перед использованием убедитесь, что:

- `EntityLink.Entity` указывает на ожидаемую сущность;
- на сущности существует `HealthComponent`;
- `Collider` и хитбокс находятся на одном объекте;
- raycast layer mask включает слой хитбокса;
- оружие вызывает `TakeDamage()`, а не только обрабатывает физическое столкновение;
- триггерные коллайдеры разрешены настройками конкретного physics query.

## Ограничения

- Хитбокс сам не выполняет raycast и не инициирует атаку.
- Отброс, эффекты поверхности и декали должны обрабатываться отдельными системами через данные попадания и события.
- Несколько хитбоксов одной сущности используют одно состояние `HealthComponent`.
