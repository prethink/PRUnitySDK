# Proxies

Proxy-компоненты принимают Unity callback на одном GameObject и перенаправляют его другим
компонентам. Это полезно, когда Collider, Rigidbody или UI-графика находятся на дочернем
объекте, а игровая логика — на корневом.

## Основные типы

| Тип | Назначение |
| --- | --- |
| `PRMonoBehaviourProxy` | База для делегирования в `PRMonoBehaviour` |
| `TriggerProxy` | Перенаправляет `OnTriggerEnter`, `Stay` и `Exit` |
| `CollisionProxy` | Перенаправляет `OnCollisionEnter`, `Stay` и `Exit` |
| `PointerProxy` | Перенаправляет UI pointer down, up и exit |

## PRMonoBehaviourProxy

Базовый proxy содержит два направления делегирования:

1. `refObject` — основной объект-получатель, назначаемый в Inspector.
2. `registeredLink` — дополнительные runtime-получатели, добавленные через `Subscribe()`.

Сначала вызывается соответствующий `UnityEvent`, затем `refObject`, затем все runtime
подписчики.

```csharp
TriggerProxy proxy = GetComponent<TriggerProxy>();
proxy.Subscribe(receiver);

// Позже, например в OnDisable:
proxy.Unsubscribe(receiver);
```

Получение компонента с основного объекта:

```csharp
if (proxy.TryComponentFromProxy<HealthComponent>(out var health))
{
    health.ApplyDamage(10);
}
```

## TriggerProxy

Добавьте `TriggerProxy` на GameObject с trigger Collider. В Inspector доступны события:

- `OnTriggerEnterEvent`;
- `OnTriggerStay`;
- `OnTriggerExit`.

```text
Unity OnTriggerEnter
├── UnityEvent<Collider>
├── refObject.InvokeOnTriggerEnter(...)
└── registeredLink.InvokeOnTriggerEnter(...)
```

Получатели должны наследоваться от `PRMonoBehaviour`. Делегирование проходит через
публичные invoke-методы базового класса, поэтому сохраняются его проверки паузы и
`DisableMethodsAttribute`.

## CollisionProxy

Работает аналогично TriggerProxy, но принимает `Collision`:

- `OnCollisionEnterEvent`;
- `OnCollisionStayEvent`;
- `OnCollisionEnterExitEvent` — событие выхода из столкновения.

Последнее имя исторически содержит лишнее `Enter`; при переименовании следует использовать
`FormerlySerializedAs`, чтобы сохранить UnityEvent-ссылки в prefab'ах и сценах.

## PointerProxy

Перенаправляет интерфейсы Unity EventSystem:

```csharp
IPointerDownHandler
IPointerUpHandler
IPointerExitHandler
```

В `InitializationComponents()` proxy получает реализации этих интерфейсов с `refObject`.
Сам объект с `PointerProxy` должен находиться в UI raycast-цепочке и иметь подходящий
`Graphic`/raycast target или другой источник pointer-событий.

## Пример структуры prefab

```text
PlayerRoot
├── PlayerLogic              # PRMonoBehaviour-получатель
└── InteractionTrigger       # Collider (Is Trigger) + TriggerProxy
```

На `InteractionTrigger.TriggerProxy.refObject` назначается `PlayerLogic`. Физическая
геометрия остаётся на дочернем объекте, а обработка события выполняется корневой логикой.

## Ограничения

- Unity по умолчанию не сериализует `HashSet<T>`. Поле `registeredLink`, несмотря на
  `[SerializeField]`, предназначено фактически для runtime-регистрации и не должно
  настраиваться через Inspector без custom serialization.
- `PointerProxy` не проверяет `refObject` на `null` перед `GetComponent`; отсутствующая
  ссылка приведёт к `NullReferenceException` при инициализации.
- `PointerProxy` кэширует handlers один раз. Изменение `refObject` в runtime не обновит их.
- Подписчик должен вызвать `Unsubscribe`, иначе proxy продолжит хранить ссылку до своего
  уничтожения.
- Добавление или удаление подписчиков во время обхода `registeredLink` может привести к
  ошибке изменения коллекции.
- Proxy не заменяет корректную настройку Rigidbody/Collider по правилам Unity Physics.

## Рекомендации

- Подписывайтесь в `OnEnable`, отписывайтесь в `OnDisable`.
- Проверяйте обязательные ссылки в `OnValidate` или добавьте custom inspector.
- Для Inspector-получателей используйте `refObject`, для динамических — `Subscribe()`.
- Не объявляйте собственные Unity physics callbacks в наследниках proxy; переопределяйте
  методы `PROnTrigger...` и `PROnCollision...`.

