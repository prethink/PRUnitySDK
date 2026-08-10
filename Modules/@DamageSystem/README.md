# DamageSystem

Модуль описывает создание, модификацию и применение урона к сущностям PRUnitySDK. Основной компонент состояния — `HealthComponent`, а данные об атаке передаются через интерфейс `IDamageProvider`.

## Поток обработки

1. Оружие или другой источник формирует `IDamageProvider`.
2. `IDamageable.TakeDamage()` принимает атакующего, оружие и провайдер урона.
3. `DamageHookEvent` позволяет изменить провайдер либо отметить результат как `Miss` или `Blocked`.
4. `HealthComponent` получает итоговый `DamageData` и изменяет здоровье.
5. Локальные события компонента и глобальные `CombatEvents` уведомляют остальные системы.

При использовании хитбокса вызов перенаправляется в `HealthComponent` сущности, указанной в `EntityLink`. Перед передачей вызывается `GetHandledDamage()`, поэтому специализированный хитбокс может добавить собственный модификатор урона.

## Основные типы

| Тип | Назначение |
| --- | --- |
| `DamageData` | Значение, тип, источник, отдача, болевой шок и применённые модификаторы |
| `IDamageProvider` | Источник `DamageData` |
| `DamageBase` | Базовая реализация провайдера |
| `CommonDamage` | Универсальный простой урон |
| `IDamageable` | Контракт объекта, принимающего урон |
| `HealthComponent` | Здоровье, смерть, лечение и возрождение сущности |
| `EntityHitBoxBase` | Перенаправление попадания от коллайдера к сущности |
| `UnitHitBox` | Зона тела и множитель урона |
| `DamageResistanceComponent` | Сопротивления сущности различным типам урона |
| `DamageHookEvent` | Изменение или блокировка урона до применения |
| `CombatEvents` | Глобальные события попадания и убийства |

## Настройка сущности

Добавьте `HealthComponent` на тот же `GameObject`, где расположен наследник `EntityBase`. Начальное здоровье при `Start()` устанавливается равным `MaxHealth`.

Для вынесенного на дочерний объект коллайдера добавьте:

- `EntityLink` — автоматически найдёт `EntityBase` в родительских объектах;
- наследника `EntityHitBoxBase`, например `UnitHitBox`;
- ссылку на используемый `Collider` в Inspector.

Полная настройка и пример вызова из raycast-оружия приведены в [`HitBox/README.md`](../HitBox/README.md).

У `UnitHitBox` настройте `HitGroup`, `DamageMultiplier` и при необходимости `IsCritical`. Попадание в голову автоматически получает флаг `Critical`.

Пример множителей в стиле классического шутера:

| Зона | Пример множителя |
| --- | ---: |
| Голова | 4.0 |
| Грудь, руки | 1.0 |
| Живот | 1.25 |
| Ноги | 0.75 |

Это только стартовые значения: фактический баланс задаётся отдельно на каждом хитбоксе в Inspector.

## Нанесение урона

```csharp
IDamageProvider damage = new CommonDamage(
    damage: 25f,
    knockBackPower: 4f);

DamageResult result = target.TakeDamage(
    attacker,
    weapon,
    damage);
```

Если известна точка или конкретный коллайдер попадания, используйте соответствующую перегрузку `TakeDamage()`. Она дополнительно вызовет `OnHitVector` или `OnHitCollider`.

Возможные результаты:

- `NotHandled` — подходящий обработчик не найден;
- `Miss` — попадание отменено или сущность недоступна для урона;
- `Blocked` — урон заблокирован;
- `Damaged` — здоровье уменьшено;
- `Killed` — урон привёл к смерти.

## Данные урона

Для ручного создания данных можно использовать фабричный метод:

```csharp
DamageData data = DamageData.Create(damage: 15f, painShock: 20f);
data.DamageType = DamageType.Fire | DamageType.AreaOfEffect;
data.DamageSource = attacker;

IDamageProvider damage = new CommonDamage(data);
```

`DamageType` является набором флагов. Несколько попаданий одной атаки можно объединить через `CombineDamageData()`, передав общий `DamageId`.

Дополнительные поля результата:

- `RawDamage` — исходное значение до зональных множителей и защиты;
- `AbsorbedDamage` — сколько урона поглотили сопротивления;
- `HitGroup` — зона попадания.

## Модификаторы

`MultiplyDamageDecorator` создаёт копию исходных данных, умножает урон и по умолчанию добавляет флаг `Critical`:

```csharp
IDamageProvider baseDamage = new CommonDamage(20f);
IDamageProvider criticalDamage = new MultiplyDamageDecorator(
    baseDamage,
    multiply: 2f,
    addCriticalFlag: true);

target.TakeDamage(attacker, weapon, criticalDamage);
```

Применённый модификатор записывается в `DamageData.AppliedModifiers`, поэтому один тип модификатора не применяется к одним данным повторно.

## Хуки

Перед проверкой блокировки `HealthComponent` публикует `DamageHookEvent`. Слушатель может:

- заменить урон через `ModifyDamage()`;
- изменить результат через `ModifyResult()`;
- полностью заблокировать попадание через `BlockDamage()`;
- отметить промах через `MissDamage()`.

Хуки следует использовать для брони, иммунитетов, командных правил и других эффектов, которые должны сработать до изменения здоровья.

## Сопротивления

Добавьте `DamageResistanceComponent` на объект с `EntityBase` и заполните список `Resistances`. Каждое правило содержит маску типов и множитель:

- `0` — полный иммунитет;
- `0.5` — половина урона;
- `1` — без изменения;
- значение больше `1` — уязвимость.

Если в маске правила указано несколько флагов, оно срабатывает при совпадении любого из них. Подходящие правила применяются последовательно, поэтому их множители перемножаются.

Пример настроек:

| Типы | Множитель | Результат |
| --- | ---: | --- |
| `Bullet` | 0.8 | Защита от пуль 20% |
| `Fire` | 0 | Иммунитет к огню |
| `Poison, Radiation` | 1.5 | Уязвимость к яду и радиации |

## События

Локальные события `HealthComponent`:

- `OnHealthChange`;
- `OnEntityDead`;
- `OnRevive`;
- `OnHitVector`;
- `OnHitCollider`;
- `OnSpawn`;
- `OnScaleChanged`.

`OnDamageProcessed` передаёт `DamageOutcome` после любой попытки нанесения урона. Он содержит результат, здоровье до и после, фактически нанесённый и поглощённый урон. Последний результат также доступен через `LastDamageOutcome`.

Глобальные события:

- `IOnTakeDamageEvents.OnTakeDamage()` — применённый урон, включая смертельный;
- `IEntityKillEvent.OnKill()` — убийство сущности уроном;
- `IDamageProcessedEvents.OnDamageProcessed()` — любая завершённая попытка, включая `Miss`, `Blocked` и `NotHandled`.

`TakeDamageEvent` содержит `Outcome`, `Result`, `AppliedDamage` и снимок итогового `DamageData`. `EntityKillEventArgs` также содержит тот же `Outcome`, поэтому обработчику убийства доступны тип урона, зона попадания и фактически снятое здоровье.

Пример глобального слушателя:

```csharp
public sealed class CombatLog : PRMonoBehaviour, IDamageProcessedEvents
{
    public void OnDamageProcessed(DamageProcessedEvent args)
    {
        PRLog.WriteDebug(
            this,
            $"{args.Attacker} -> {args.Victim}: " +
            $"{args.Outcome.Result}, damage={args.Outcome.AppliedDamage}");
    }
}
```

Порядок событий при обычном попадании:

1. здоровье изменяется;
2. вызывается локальный `OnHealthChange`;
3. сохраняется `LastDamageOutcome` и вызывается локальный `OnDamageProcessed`;
4. публикуется глобальный `OnTakeDamage`;
5. публикуется глобальный `OnDamageProcessed`.

При смертельном попадании сущность переводится в мёртвое состояние до глобальных событий. Между `OnTakeDamage` и завершающим `OnDamageProcessed` дополнительно публикуется `OnKill`.

## Лечение и возрождение

`AddHealth()` лечит только живую сущность и не превышает `MaxHealth`. При успешном лечении вызывается `OnHealthChange`.

Методы `Revive()` восстанавливают сущность, при необходимости меняют позицию и поворот, включают скрытый при смерти объект и вызывают `OnRevive`.

## Текущие ограничения

- `DamageOverTimeDecorator` пока является заготовкой и не наносит периодический урон.
- `TickDamageBrain` поддерживает интервал тиков, но ещё не содержит цели и непосредственного вызова `TakeDamage()`.
- `KnockBackPower`, `PainShock` и `IActiveDamageModifier` хранят данные для внешних обработчиков; `HealthComponent` самостоятельно их не применяет.
- Отрицательное значение `Damage` фактически увеличит здоровье. Для лечения следует использовать `AddHealth()`, а источники урона должны выдавать неотрицательные значения.

## Рекомендации

- Создавайте новый `DamageId` для каждой независимой атаки.
- Указывайте `DamageSource`, если источник важен для эффектов или аналитики.
- Не изменяйте общий `DamageData` из нескольких систем одновременно; используйте `Clone()` или декораторы.
- Проверяйте возвращаемый `DamageResult`, если после попадания требуется отдельная реакция.
