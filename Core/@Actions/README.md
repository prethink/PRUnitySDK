# Actions

Система Actions описывает переиспользуемые действия с единым контрактом проверки и
выполнения. Действие можно хранить как `ScriptableObject`, размещать компонентом на
GameObject или передавать через `IActionProvider`.

## Основные типы

| Тип | Назначение |
| --- | --- |
| `IAction` | Контракт с методами `CanExecute()` и `Execute()` |
| `IActionProvider` | Предоставляет действие через свойство `Action` |
| `ActionBase` | Основа ScriptableObject-действий |
| `ActionMonoBehaviourBase` | Основа действий-компонентов |
| `IconActionBase` | ScriptableObject-действие с иконкой |
| `OpenURLAction` | Открывает URL через `Application.OpenURL` |
| `LangAction` | Переключает язык через LanguageManager SDK |

## Жизненный цикл выполнения

`Execute()` сначала вызывает `CanExecute()`. Базовые реализации запрещают выполнение,
пока `PRUnitySDK.IsInitialized` равен `false`, и записывают предупреждение через `PRLog`.
Если проверка пройдена, вызывается защищённый метод `Action()`.

```text
Execute()
└── CanExecute()
    ├── false → действие не выполняется, результат false
    └── true  → Action(), результат true
```

## Создание ScriptableObject-действия

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actions/Load level")]
public class LoadLevelAction : ActionBase
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

После компиляции создайте asset через `Create → Game → Actions → Load level` и
передавайте его потребителям как `IAction` или `ActionBase`.

## Создание действия-компонента

```csharp
public class DisableObjectAction : ActionMonoBehaviourBase
{
    protected override void Action()
    {
        gameObject.SetActive(false);
    }
}
```

## Использование через provider

```csharp
public class ActionButton : MonoBehaviour, IActionProvider
{
    [field: SerializeField] public ActionBase ActionAsset { get; private set; }

    public IAction Action => ActionAsset;

    public void Click()
    {
        Action?.Execute();
    }
}
```

## Рекомендации

- Проверки доступности размещайте в `CanExecute()`, а эффект — в `Action()`.
- Не вызывайте `Action()` напрямую: так будет пропущена проверка готовности SDK.
- Для данных, переиспользуемых между сценами, выбирайте `ActionBase`.
- Для действий, зависящих от конкретного GameObject, выбирайте `ActionMonoBehaviourBase`.
- Не храните изменяемое runtime-состояние в общем ScriptableObject-действии, если один
  asset используется несколькими объектами.

