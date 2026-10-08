# HitBox

Модуль связывает физические коллайдеры Unity с `DamageSystem`. Хитбокс принимает вызов `IDamageable.TakeDamage()`, преобразует данные попадания и перенаправляет их в `HealthComponent` связанной сущности.

## Состав модуля

| Компонент | Назначение |
| --- | --- |
| `EntityHitBoxBase` | Общая маршрутизация попадания в связанную сущность |
| `UnitHitBox` | Зона тела, множитель урона и критическое попадание |
| `ItemHitBox` | Хитбокс предмета без дополнительного преобразования урона |
| `EntityLink` | Связь дочернего объекта с родительским `EntityBase` |
| `HitBoxExtensions` | Получение цели урона, хитбокса и здоровья из коллайдера, столкновения или объекта |

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

Свойство `IsConfigured` показывает, найдены ли ссылка на сущность и коллайдер. Связанная сущность доступна как `OwnerEntity`. Если у неё нет `HealthComponent`, `TakeDamage()` возвращает результат `NotHandled` с заполненными участниками удара: атакующим, целью, оружием и провайдером урона.

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
    DamageOutcome outcome = damageable.TakeDamage(
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

Держите `Collider` и `EntityHitBoxBase` на одном объекте: иначе разные зоны тела могут случайно свестись к одному родительскому обработчику.

## Получение цели: HitBoxExtensions

Прямой `TryGetComponent<HealthComponent>` на коллайдере работает только у простой сущности, где здоровье и коллайдер на одном объекте. У персонажа такая проверка молча возвращает `false`: попадание есть, урона нет. Для этого и нужны расширения.

Обычно нужен один метод — «кого бить»:

```csharp
if (other.TryGetDamageTarget(out IDamageable target))
{
    DamageOutcome outcome = target.TakeDamage(attacker, damage);
}
```

Он пробует хитбокс, а без него берёт здоровье. **Бить лучше в хитбокс**: он умножает урон на зону и помечает крит. Прямой удар по здоровью эти правила обходит, и выстрел в голову засчитается обычным.

Порядок здесь ещё и обязателен. Две проверки подряд писать **нельзя**:

```csharp
// НЕВЕРНО: сработают обе ветки, урон уйдёт дважды
if (other.TryGetHitBox(out var box)) box.TakeDamage(...);
if (other.TryGetHealth(out var health)) health.TakeDamage(...);
```

`TryGetHealth` находит здоровье **через** хитбокс, поэтому у персонажа истинны оба
условия. `TryGetDamageTarget` делает такую ошибку невозможной.

| Метод | Что находит |
| --- | --- |
| `TryGetDamageTarget(out …)` | кому наносить урон: хитбокс, без него здоровье |
| `TryGetHitBox(out …)` | только со своего объекта коллайдера |
| `TryGetHitBox<T>(out …)` | то же, но с проверкой типа (`UnitHitBox`, `ItemHitBox`) |
| `TryGetHealth(out …)` | со своего объекта, затем через связь с сущностью |

Все есть для `Collider`, `Collision` и `GameObject`. Отдельные `TryGetHitBox` и `TryGetHealth` нужны, если важен именно хитбокс (зона попадания) или именно здоровье (текущее значение, подписка), а не «нанести урон».

Хитбокс берётся **только со своего объекта**, без подъёма к `Rigidbody`: иначе попадание приписалось бы не той зоне. Здоровье ищется на объекте коллайдера, а если его там нет, то на объекте с `Rigidbody`: у составного тела связь с сущностью часто висит именно там.

У `Collision` сначала проверяется задетый коллайдер (`collision.collider`), потом объект столкновения. У составной иерархии объект столкновения — это всё тело, и зону по нему не различить.

Расширения не поднимаются по родителям. Связь задаётся явно через `EntityLink`: подъём по иерархии нашёл бы чужое здоровье у вложенных сущностей, например питомца внутри игрока. Связь ищется базовым типом `EntityLinkBase`, поэтому находятся и `EntityLink`, и `PlayerLink` на теле игрока.

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
