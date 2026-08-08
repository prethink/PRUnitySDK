# PRMonoBehaviour

`PRMonoBehaviour` — базовый класс игровых компонентов PRUnitySDK. Он дополняет Unity
lifecycle хуками, которые учитывают логическую паузу, автоматически регистрирует объект
в `EventBus` и трекере сохранений, а также унифицирует обработку физики.

## Lifecycle

| Unity callback | PR hook | Пауза логики |
| --- | --- | --- |
| `Awake` | `InitializationComponents()` | Не проверяется |
| `Start` | Запуск optional coroutine-хуков | Не проверяется |
| `Update` | `PRPreUpdate → PRUpdate → PRPostUpdate` | Выполнение пропускается |
| `LateUpdate` | `PRLateUpdate()` | Выполнение пропускается |
| `FixedUpdate` | `PRFixedUpdate()` | Выполнение пропускается |
| End of frame | `PREndOfFrame()` | Через PR coroutine |
| After physics | `PRLateFixedUpdate()` | Через PR coroutine |

## Типичный компонент

```csharp
public class MovingPlatform : PRMonoBehaviour
{
    [SerializeField] private float speed = 2f;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        // Получение компонентов и начальная регистрация.
    }

    protected override void PRUpdate()
    {
        transform.position += Vector3.forward * speed * PRTime.Instance.GameDeltaTime;
    }
}
```

При переопределении стандартных методов `Awake`, `Start`, `OnEnable`, `OnDisable`,
`OnValidate` и `InitializationComponents` вызывайте базовую реализацию. Иначе часть
инфраструктуры SDK может не выполниться.

## Фазы Update

```csharp
protected override bool PRPreUpdate()
{
    return isReady;
}

protected override void PRUpdate()
{
    // Основная логика кадра.
}

protected override void PRPostUpdate()
{
    // Логика после основного обновления.
}
```

Если `PRPreUpdate()` возвращает `false`, обе следующие фазы пропускаются.

## Физические callback'и

Вместо объявления Unity-методов используйте PR-хуки:

```csharp
protected override void PROnTriggerEnter(Collider other)
{
    Debug.Log($"Trigger: {other.name}");
}

protected override void PROnCollisionEnter(Collision collision)
{
    Debug.Log($"Collision: {collision.gameObject.name}");
}
```

Доступны варианты trigger callback с `Collider`, а также с `Collider` и его
`attachedRigidbody`. Если Rigidbody присутствует, базовый класс вызывает оба подходящих
хука. Для `Stay` можно переопределить интервалы:

```csharp
protected override float PROnTriggerStayTimeout() => 0.1f;
protected override float PROnCollisionStayTimeout() => 0.1f;
```

Есть также `PROnTriggerEnter2D`, `PROnTriggerStay2D` и `PROnTriggerExit2D`.

## Автоматическая регистрация

`InitializationComponents()` вызывает `RegisterEventsOnCreated()`:

- объект подписывается в `EventBus` на реализованные интерфейсы;
- объект добавляется в `PRUnitySDK.Trackers.Saveables`.

При уничтожении `UnRegisterEventsOnDestroy()` выполняет обратные операции. Регистрация
не привязана к `OnEnable/OnDisable`, поэтому выключенный объект остаётся подписанным до
уничтожения.

## Дополнительные возможности

### PRDestroy

```csharp
PRDestroy(gameObject);
PRDestroy(gameObject, timeout: 2f);
```

Задержка использует `PRTimeType.GameTime`. Отрицательный timeout игнорируется.

### LateFixedUpdate и EndOfFrame

```csharp
protected override bool UseCoroutineLateFixedUpdate() => true;
protected override void PRLateFixedUpdate() { }

protected override bool UseCoroutineWaitForEndOfFrame() => true;
protected override void PREndOfFrame() { }
```

Эти хуки реализованы бесконечными корутинами, запускаемыми в `Start`.

### Сохранение

`TrySaveData()` является точкой расширения `ISaveable` и по умолчанию возвращает
успешный результат без записи данных.

## PRMonoBehaviourHost

Глобальный host:

- запускает корутины без локального владельца;
- обслуживает зарегистрированные `IPRUpdate`, `IPRFixedUpdate` и `IPRTickable`;
- выполняет ручной `Physics.Simulate`, когда simulation mode установлен в `Script`;
- использует интервал тика из настроек проекта.

Коллекции host нельзя безопасно изменять во время их обхода без дополнительной защиты.
Регистрируйте и снимайте объекты на границах lifecycle, а не внутри callback того же цикла.

## Ограничения

- Unity lifecycle-методы являются виртуальными; забытый вызов `base` может отключить
  обязательную инфраструктуру.
- `IsMethodDisabled()` использует reflection в физических callback'ах.
- Наследник, объявивший собственный Unity `OnTrigger...` или `LateUpdate`, может обойти
  PR-обработку. Используйте методы с префиксом `PR`/`PROn`.

