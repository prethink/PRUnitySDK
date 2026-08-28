# Фабрики MonoBehaviour

Фабрики SDK создают prefab из `Resources` и централизуют правила singleton, родителя, Canvas и `DontDestroyOnLoad`.

`ResourcePath` указывается относительно любой папки `Resources`, без `Resources/` и расширения `.prefab`:

```text
Assets/<любая папка>/Resources/PRUnitySDK/Prefabs/Windows/MonoWindows/SomeWindow.prefab
                                      ↓
PRUnitySDK/Prefabs/Windows/MonoWindows/SomeWindow
```

Generic-тип фабрики всегда должен совпадать с компонентом на корневом объекте prefab.

## `MonoBehaviourFactoryBase<T>`

Основная фабрика для обычных компонентов. Позволяет настроить все параметры создания:

```csharp
public sealed class ProjectileFactory : MonoBehaviourFactoryBase<Projectile>
{
    public override string ResourcePath => "Game/Prefabs/Projectile";
    public override bool IsSingleton => false;
    public override bool WorldPositionStays => false;
    public override bool DonDestroyOnLoad => false;
}

Projectile projectile = new ProjectileFactory().Create(projectilesRoot);
```

- `IsSingleton` возвращает ранее созданный экземпляр при повторном `Create`;
- `WorldPositionStays` передаётся в `Transform.SetParent`;
- `DonDestroyOnLoad` сохраняет созданный объект между сценами;
- `Create(parent)` принимает необязательного родителя.

## `SingletonMonoBehaviourFactoryBase<T>`

Сокращённая фабрика для глобального компонента. По умолчанию:

- `IsSingleton = true`;
- `WorldPositionStays = false`;
- `DonDestroyOnLoad = true`.

```csharp
public sealed class AdMessageFactory : SingletonMonoBehaviourFactoryBase<AdMessage>
{
    public override string ResourcePath =>
        $"{PRUnitySDK.ResourcePaths.PrefabsPath}/Advertising/AdMessage";
}

AdMessage message = new AdMessageFactory().Create();
```

Используйте этот вариант для менеджеров и других объектов, которые должны существовать в одном экземпляре между сценами.

## `MonoWindowFactoryBase<T>`

Фабрика окон, наследуемых от `MonoWindowBase`. Она помещает окно в контейнер `PRUnitySDK.Windows`.

```csharp
public sealed class InventoryWindowFactory : MonoWindowFactoryBase<InventoryWindow>
{
    public override string ResourcePath =>
        $"{PRUnitySDK.ResourcePaths.MonoWindowsPaths}/InventoryWindow";

    public override bool UseSharedCanvas => true;
    public override bool WorldPositionStays => false;
    public override bool IsSingleton => true;
}

InventoryWindow window = new InventoryWindowFactory().CreateMonoWindow();
```

- `UseSharedCanvas = true` размещает окно на общем Canvas;
- `false` использует основной контейнер окон;
- контейнеры окон должны быть инициализированы до `CreateMonoWindow()`.

## `NotifierFactoryBase<T>`

Фабрика UI-уведомлений. Созданный объект автоматически становится дочерним объектом `PRUnitySDK.Windows.Notifiers`.

```csharp
public sealed class LootNotifierFactory : NotifierFactoryBase<LootNotifier>
{
    public override string ResourcePath =>
        $"{PRUnitySDK.ResourcePaths.NotifiersPath}/LootNotifier";

    public override bool IsSingleton => true;
}

LootNotifier notifier = new LootNotifierFactory().Create();
```

## Как выбрать фабрику

| Задача | Базовый класс | Метод создания |
| --- | --- | --- |
| Обычный prefab, пули, контейнеры | `MonoBehaviourFactoryBase<T>` | `Create(parent)` |
| Единственный глобальный компонент | `SingletonMonoBehaviourFactoryBase<T>` | `Create(parent)` |
| Окно `MonoWindowBase` | `MonoWindowFactoryBase<T>` | `CreateMonoWindow()` |
| UI-уведомление | `NotifierFactoryBase<T>` | `Create()` |

## Ограничения

- Prefab обязательно должен находиться внутри папки `Resources`.
- Путь чувствителен к структуре папок и не содержит расширения.
- Обычная фабрика сейчас не проверяет результат `Resources.Load` перед `Instantiate`; неверный путь приведёт к ошибке создания.
- Singleton кэшируется отдельно для каждого generic-типа `T`.
- `MonoWindowFactoryBase` и `NotifierFactoryBase` требуют заранее инициализированные UI-контейнеры SDK.
