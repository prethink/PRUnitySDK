# Actions

Система `Actions` описывает переиспользуемые действия с единым контрактом проверки и выполнения. Действие можно хранить как `ScriptableObject`, размещать компонентом на `GameObject` или предоставлять через `IActionProvider`.

## Основные типы

| Тип | Назначение |
| --- | --- |
| `IAction` | Контракт с методами `CanExecute()` и `Execute()` |
| `IActionProvider` | Предоставляет действие через свойство `Action` |
| `ActionExecuter` | Общая механика проверки и выполнения для разных базовых Unity-типов |
| `ActionBase` | Основа ScriptableObject-действий |
| `ActionMonoBehaviourBase` | Основа действий-компонентов |
| `IconActionBase` | ScriptableObject-действие с иконкой |
| `OpenURLAction` | Открывает проверенный HTTP/HTTPS URL |
| `LangAction` | Переключает язык через LanguageManager SDK |
| `AddBoolValueAction` | Устанавливает bool-свойство в `ProjectPropertiesManager` |
| `AddLongValueAction` | Прибавляет значение к long-свойству в `ProjectPropertiesManager` |
| `AddFloatValueAction` | Прибавляет значение к float-свойству в `ProjectPropertiesManager` |
| `AddStringValueAction` | Устанавливает string-свойство в `ProjectPropertiesManager` |
| `AddDateTimeValueAction` | Устанавливает DateTime-свойство из ISO-8601 строки |

`ActionExecuter` намеренно используется через композицию: `ScriptableObject` и `MonoBehaviour` не могут наследоваться от одного общего класса действий, но должны выполнять одинаковую проверку.

## Выполнение

`Execute()` передаёт в executor виртуальный метод `CanExecute()` владельца. Поэтому переопределённые проверки всегда применяются и при прямом вызове `Execute()`.

```text
Execute()
└── CanExecute()
    ├── false → действие не вызывается, результат false
    └── true  → Action(), результат true
```

Базовая проверка требует завершённой инициализации `PRUnitySDK`. Наследник расширяет её через `base.CanExecute()`:

```csharp
public override bool CanExecute()
{
    return base.CanExecute() && amount > 0;
}
```

`true` означает, что внутреннее действие было вызвано. Исключения из действия не преобразуются в `false`: они передаются вызывающему коду.

## ScriptableObject-действие

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actions/Load level")]
public sealed class LoadLevelAction : ActionBase
{
    [SerializeField] private int sceneIndex;

    public override bool CanExecute()
    {
        return base.CanExecute() && sceneIndex >= 0;
    }

    protected override void Action()
    {
        SceneChanger.Instance.SceneChange(sceneIndex);
    }
}
```

После компиляции создайте asset через `Create → Game → Actions → Load level`.

ScriptableObject-действия подходят для конфигурации, которая переиспользуется между сценами. Не храните изменяемое состояние конкретного вызова в общем asset: один экземпляр может одновременно использоваться несколькими объектами.

## Действие-компонент

```csharp
public sealed class DisableObjectAction : ActionMonoBehaviourBase
{
    public override bool CanExecute()
    {
        return base.CanExecute() && gameObject.activeSelf;
    }

    protected override void Action()
    {
        gameObject.SetActive(false);
    }
}
```

`ActionMonoBehaviourBase` подходит для действий, зависящих от состояния конкретного `GameObject`.

## Использование через provider

```csharp
public sealed class ActionProvider : MonoBehaviour, IActionProvider
{
    [SerializeField] private ActionBase action;

    public IAction Action => action;
}
```

Потребитель работает только с интерфейсом:

```csharp
if (provider.Action?.Execute() == true)
{
    OnActionExecuted();
}
```

Нет необходимости отдельно вызывать `CanExecute()` перед `Execute()`: `Execute()` повторно выполняет полную виртуальную проверку. Отдельный вызов полезен для отображения доступности кнопки, подсказки или интерактивного объекта.

## OpenURLAction

`OpenURLAction` выполняется только для абсолютных URL со схемой `http` или `https`. Пустые строки, относительные адреса и другие схемы отклоняются через `CanExecute()`.

## Действия ProjectProperties

Набор покрывает все типы `ProjectPropertiesManager`:

- `AddBoolValueAction` устанавливает bool (историческое имя сохранено для совместимости);
- `AddLongValueAction` и `AddFloatValueAction` прибавляют `count`;
- `AddStringValueAction` устанавливает строку, включая пустую;
- `AddDateTimeValueAction` устанавливает дату из ISO-8601 строки, например `2026-12-31T23:59:59Z`.

Все действия требуют непустой `propertyName`; DateTime-действие дополнительно проверяет формат даты в `CanExecute()`.

`AddLongValueAction` и `AddFloatValueAction` выполняют изменение с `save: false`. Это позволяет составному reward/purchase-процессу выполнить одну общую запись после нескольких изменений. Остальные действия сохраняют данные сразу через стандартные `Set*`-методы.

## Рекомендации

- Условия доступности размещайте в `CanExecute()`.
- Эффект размещайте в защищённом `Action()`.
- В переопределённом `CanExecute()` обычно вызывайте `base.CanExecute()`.
- Не вызывайте `Action()` напрямую: это обходит проверки.
- Учитывайте результат `Execute()` в потребителях.
- Проверяйте сериализованные ссылки на действие на `null`.
- Для асинхронных операций и подробного результата потребуется отдельный async/result-контракт; текущий `IAction` является синхронным.
