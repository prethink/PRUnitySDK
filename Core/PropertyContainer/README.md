# Property modifiers

`PropertyContainer` рассчитывает изменяемые характеристики объекта во время игры. Он подходит для скорости, силы и количества прыжков, гравитации, множителей урона и других числовых параметров.

Контейнер не хранит постоянный прогресс и не заменяет систему сохранений. Обычно каждый игрок или другая сущность имеет собственный экземпляр контейнера.

## Порядок расчёта

Персональные модификаторы применяются в следующем порядке:

```text
(baseValue + сумма Add) × произведение Multiply → Override
```

После этого `EntityStatsUtils` применяет глобальные `GameRules`:

```text
EntityStatsBase → StatModifierCollector → GameRules → итоговое значение
```

Таким образом, глобальные минимальные и максимальные ограничения накладываются поверх экипировки, эффектов и бонусов игрока.

## Типы контейнеров

- `FloatPropertyContainer` — скорость, ускорение, сила прыжка и другие дробные значения.
- `IntPropertyContainer` — количество прыжков и другие небольшие целые значения.
- `LongPropertyContainer` — большие целочисленные характеристики.

Для характеристик игрока основная цепочка сейчас использует `FloatPropertyContainer`, потому что `EntityStatsBase`, `StatModifier` и `GameRules` хранят значения в `float`. `EntityStatsUtils.GetStatInt()` округляет итог только после применения всех модификаторов и правил.

## Типы модификаторов

### Add

Прибавляет значение к базовой характеристике:

```csharp
properties.Add(
    CharacterEnumeration.WalkSpeed,
    boots,
    2f,
    ModifierTypes.Add);
```

При базовой скорости `5` результат до остальных модификаторов будет `7`.

### Multiply

Умножает значение после сложения всех `Add`:

```csharp
properties.Add(
    CharacterEnumeration.SprintSpeed,
    sprintEffect,
    1.5f,
    ModifierTypes.Multiply);
```

### Override

Полностью заменяет рассчитанное значение:

```csharp
properties.Add(
    CharacterEnumeration.WalkSpeed,
    freezeEffect,
    0f,
    ModifierTypes.Override,
    priority: 10);
```

У `Override` большее число означает более высокий приоритет. Если приоритет одинаковый, выигрывает последний добавленный модификатор. Это сохраняет прежнее поведение `StatModifierCollector`.

## Источник модификатора

Параметр `source` обозначает объект, который добавил эффект. Это может быть экипировка, питомец, усилитель или компонент состояния:

```csharp
properties.Add(stat, source, value, ModifierTypes.Add);
properties.Remove(stat, source);
properties.ClearSource(source);
```

`Remove()` удаляет модификаторы источника только для одной характеристики. `ClearSource()` удаляет их сразу из всех характеристик.

Если источником выступает уничтоженный `UnityEngine.Object`, контейнер удалит его модификаторы при следующем чтении значения.

## Использование напрямую

### Скорость типа float

```csharp
var properties = new FloatPropertyContainer();

properties.Add(
    CharacterEnumeration.WalkSpeed,
    speedBoost,
    1.25f,
    ModifierTypes.Multiply);

float modifiedSpeed = properties.Get(
    CharacterEnumeration.WalkSpeed,
    baseSpeed);

float finalSpeed = properties.GetWithRules(
    CharacterEnumeration.WalkSpeed,
    baseSpeed);
```

`Get()` применяет только персональные модификаторы. `GetWithRules()` дополнительно применяет `GameRules`.

### Количество прыжков типа int

```csharp
var properties = new IntPropertyContainer();

properties.Add(
    CharacterEnumeration.JumpCount,
    doubleJumpItem,
    1,
    ModifierTypes.Add);

int jumpCount = properties.GetWithRules(
    CharacterEnumeration.JumpCount,
    baseJumpCount);
```

## Интеграция с игроком

Для обычных характеристик игрока вручную создавать контейнер не требуется. `StatModifierCollector` находится на `PlayerController.prefab`, собирает модификаторы дочерних объектов и используется через `EntityStatsUtils`:

```csharp
float speed = EntityStatsUtils.GetStat(
    CharacterEnumeration.WalkSpeed,
    Core.Stats,
    statCollector);

int jumpCount = EntityStatsUtils.GetStatInt(
    CharacterEnumeration.JumpCount,
    Core.Stats,
    statCollector,
    1);
```

Сборщик поддерживает два интерфейса:

- `IStatModifiersProvider` возвращает несколько модификаторов;
- `IStatModifierProvider` возвращает один модификатор.

При старте игрока выполняется первоначальный сбор. После добавления или удаления экипировки необходимо вызвать:

```csharp
EntityEvents.EquipmentChanged(player);
```

Сборщик перестроит контейнер и отправит `EntityEvents.RefreshStats(player)`.

## Важные замечания

- Не используйте один контейнер одновременно для нескольких игроков.
- Не передавайте `null` как ключ или источник модификатора.
- Не применяйте `GameRules` повторно после `GetWithRules()`.
- Для времени, скорости и силы используйте `float` без промежуточного округления.
- Для количества прыжков обычно достаточно `int`.
- `long` формально поддерживается контейнером, но существующая цепочка `EntityStatsBase` и `GameRules` основана на `float` и не сохраняет полную точность больших `long`.
