# PRUnitySDK Extensions

Набор extension-методов общего назначения для Unity и типов PRUnitySDK. Классы находятся в глобальном пространстве имён, поэтому дополнительный `using` не требуется.

## Состав

| Файл | Назначение |
| --- | --- |
| `ClassExtension.cs` | Unity-aware проверка на `null`, проверка отключённых методов |
| `ComponentExtension.cs` | Получение Unity-компонента через `IComponent` |
| `EntityExtensions.cs` | Получение сущности из `Collision`, `Collider` и `GameObject` |
| `GameObjectExtensions.cs` | Поиск компонентов, работа с иерархией и обновление UI layout |
| `ItemExtensions.cs` | Получение локализованного имени предмета |
| `ListExtensions.cs` | Циклическая навигация и добавление с заменой |
| `QualityExtension.cs` | Сравнение и локализация качества |
| `ReflectionExtension.cs` | Вызов методов по SDK-атрибутам и поиск реализаций типов |
| `SDKExtensions.cs` | Получение Unity-компонентов через `IEntity` |
| `SerializedPropertyUtility.cs` | Формирование имени backing field автосвойства |
| `TransformExtension.cs` | Получение фактического `Transform` объекта или сущности |
| `TweenExtension.cs` | Анимации масштаба на DOTween |
| `VectorExtensions.cs` | Случайные значения диапазона и ограничение `Vector3` |

## Примеры

```csharp
var rigidbody = gameObject.GetOrAddComponent<Rigidbody>();

if (collider.TryGetEntity<PlayerEntity>(out var player))
    player.GetComponent<Animator>();

var value = values.GetNext(ref currentIndex);
var clamped = position.Clamp(minPosition, maxPosition);
```

Методы с `InvokePartialAttribute` можно вызвать с объединением результатов:

```csharp
IEnumerable<Modifier> modifiers = this.CollectPartialResult<Modifier>(context);
```

Подходят методы, возвращающие `T`, `T[]` или `IEnumerable<T>`. Параметры должны быть совместимы с аргументами вызова. `null` разрешён для ссылочных типов и `Nullable<T>`. Методы вызываются по `Order`; результат `null` пропускается с предупреждением.

## Особенности

- `GetComponentsInSelfOrChildren` включает компоненты корневого объекта без повторов.
- Методы циклической навигации меняют переданный через `ref` индекс и выбрасывают `InvalidOperationException` для пустого списка.
- `DoScaleUpDown` создаёт бесконечный Tween; владелец должен вызвать `Kill`, когда анимация больше не нужна.
- `RunMethodHooks`, `RunStaticMethodHooks` и `TryOverrideProperty` кешируют результат сканирования типа, поэтому повторные вызовы не перебирают методы заново. Остальные reflection-методы (`CollectPartialResult`, `FindClassesImplementingInterface`) кеша не имеют — их не стоит вызывать каждый кадр.
- Методы `TryGetEntity` ищут `EntityLinkBase<T>` на том же `GameObject`, а не в родительской иерархии.
