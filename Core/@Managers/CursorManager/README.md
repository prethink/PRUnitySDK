# CursorManager

`CursorManager` управляет системным курсором Unity через запросы, привязанные к источнику. Это позволяет нескольким окнам и gameplay-системам независимо вызывать `Show`/`Hide`, а при закрытии снимать только собственный запрос через `Release`.

Менеджер не входит в `PRUnitySDK.Managers` и доступен как обычный singleton:

```csharp
CursorManager cursor = CursorManager.Instance;
```

## Модель запросов

Каждый `source` имеет не более одного активного запроса. Повторный `Show(source)` или `Hide(source)` заменяет прежнее состояние и перемещает запрос в конец списка. Фактическое состояние курсора всегда определяется последним обновлённым активным запросом.

```csharp
public sealed class InventoryWindow : MonoBehaviour
{
    private void OnEnable()
    {
        CursorManager.Instance.Show(this);
    }

    private void OnDisable()
    {
        CursorManager.Instance.Release(this);
    }
}
```

Если A вызвал `Show`, затем B вызвал `Hide`, победит B. После `Release(B)` снова применяется запрос A. Это не логика «любой Show сильнее любого Hide», а порядок последнего обращения.

| Метод | Назначение |
| --- | --- |
| `Show(source)` | записать для источника `CursorLockMode.None` и `Visible = true` |
| `Hide(source)` | записать `CursorLockMode.Locked` и `Visible = false` |
| `Release(source)` | удалить запрос и применить последний из оставшихся |
| `HasRequest(source)` | проверить наличие запроса источника |
| `LoadCursorState(source, defaultState)` | после готовности `GameManager` создать запрос из сохранённого bool или переданного fallback |
| `SetCursorSprite(sprite)` | немедленно передать texture спрайта в `Cursor.SetCursor` |

Сравнение источников выполняется через `Equals`. Обычно безопаснее передавать `this`, чтобы разные системы не получили одинаковый логический ключ случайно.

## Загрузка пользовательского состояния

`LoadCursorState` подписывается на `GameManager.ReadySignal` и читает `CursorStatePropertyName` из `ProjectPropertiesManager`. Если ключ отсутствует, используется переданный `CursorState`.

```csharp
var fallback = new CursorManager.CursorState(
    CursorLockMode.Locked,
    visible: false);

CursorManager.Instance.LoadCursorState(this, fallback);
```

Сохранение выполняется отдельно тем же типизированным ключом:

```csharp
PRUnitySDK.Managers.ProjectProperties.SetValue(
    CursorManager.CursorStatePropertyName,
    isCursorVisible);
```

Загруженное значение является только `bool`. Текущий код создаёт из него состояние `CursorLockMode.Locked` + сохранённая видимость; режим блокировки отдельно не сохраняется.

## Сброс между Play Mode

`ResetOnLoad()` с `RuntimeInitializeLoadType.SubsystemRegistration` вызывает `Override(null)`. Поэтому singleton и список запросов пересоздаются даже при отключённом Domain Reload.

## Текущие ограничения

- `LoadCursorState` выполняется только один раз за жизненный цикл singleton: повторные вызовы игнорируются после установки `isLoadingState`.
- Загруженное/fallback-состояние добавляется как обычный активный запрос `source`. Поле `defaultState` текущей реализацией не назначается.
- Если после снятия всех запросов `defaultState` не задан, применяется emergency fallback: `Locked` и `Visible = false`.
- `SetCursorSprite` не входит в `CursorState`; `Release` не восстанавливает предыдущую texture.
- `Release` неизвестного источника безопасен, но не сообщает об ошибке.
