# ExtendedEditorWindow

`ExtendedEditorWindow` — базовый IMGUI `EditorWindow` публичного SDK с небольшими helpers
для toolbar, контекстных меню, вкладок, двухколоночного layout и редактирования
`SerializedProperty`. Класс находится в [`../ExtendedEditorWindow.cs`](../ExtendedEditorWindow.cs)
и компилируется только в Editor assembly.

База не управляет жизненным циклом `SerializedObject`, Undo или сохранением assets. Окно-наследник
само вызывает `Update()` перед отрисовкой и `ApplyModifiedProperties()` после изменений.

## Минимальное окно

```csharp
using UnityEditor;
using UnityEngine;

public sealed class ExampleEditorWindow : ExtendedEditorWindow
{
    [MenuItem("PRUnitySDK/Tools/Example")]
    private static void Open()
    {
        GetWindow<ExampleEditorWindow>("Example");
    }

    private void OnGUI()
    {
        CreateHorizontalToolBar(() =>
        {
            ToolbarMenu("File", () =>
            {
                MenuItem("Refresh", Repaint);
                MenuSeparator();
                MenuItem("Close", Close);
            });
        });

        Tabs(
            ("General", DrawGeneral),
            ("Advanced", DrawAdvanced));
    }

    private void DrawGeneral() => EditorGUILayout.LabelField("General settings");
    private void DrawAdvanced() => EditorGUILayout.LabelField("Advanced settings");
}
```

## Toolbar и меню

| API | Назначение |
| --- | --- |
| `CreateHorizontalToolBar(Action)` | рисует горизонтальную панель в стиле Unity toolbar и добавляет свободное место справа |
| `ToolbarMenu(name, build, width)` | показывает popup-кнопку и синхронно собирает `GenericMenu` при нажатии |
| `MenuItem(path, action, enabled)` | добавляет активный или disabled пункт в текущее меню |
| `MenuSeparator(path)` | добавляет разделитель в текущее меню |

`MenuItem` и `MenuSeparator` нужно вызывать внутри `build` callback метода `ToolbarMenu`.
Вне этого callback активного `GenericMenu` нет, поэтому вызов намеренно ничего не делает.

## Вкладки

`Tabs(...)` сразу рисует header и содержимое выбранной вкладки. Перегрузка
`Tabs(compact: true, ...)` заменяет горизонтальный toolbar на dropdown, что подходит для
узких docked-окон.

`DrawTabsDropdown(...)` рисует выбор вкладки списком независимо от ширины окна — это
вариант для окон с большим числом разделов, где ряд кнопок неизбежно оказывается
нечитаемым. Ширина списка подбирается по самой длинной подписи и ограничена сверху,
чтобы остальное место тулбара осталось свободным.

В обычном режиме (`compact: false`) вкладки-кнопки переносятся на следующую строку, когда
ряд не помещается по ширине. `GUILayout.Toolbar` укладывается только в один ряд и при
нехватке места обрезает подписи, поэтому ряды набираются вручную по фактической ширине
каждой кнопки.

| Вариант | Когда подходит |
| --- | --- |
| `Tabs(...)` / `DrawTabsHeader(false, ...)` | До пяти-шести вкладок: переключение в один клик |
| `DrawTabsDropdown(...)` | Десяток и больше разделов |
| `Tabs(compact: true, ...)` | Узкие docked-окна |

Для окна со своим scroll/layout можно разделить этапы:

```csharp
var tabs = new (string name, System.Action draw)[]
{
    ("Overview", DrawOverview),
    ("Details", DrawDetails)
};

DrawTabsHeader(position.width < 500f, tabs);

scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
DrawSelectedTab(tabs);
EditorGUILayout.EndScrollView();
```

Текущий индекс доступен наследнику через `SelectedTabIndex`. Он хранится только в экземпляре
окна и не сериализуется, поэтому после пересоздания окна или domain reload может вернуться к
первой вкладке. Передавать пустой набор вкладок не следует.

## Layout helpers

| API | Назначение |
| --- | --- |
| `SplitRow(leftWidth, left, right)` | рисует две вертикальные области с фиксированной шириной слева и отступом между ними |
| `DrawRowSpliter(padding, color, lineSize)` | рисует горизонтальный цветной разделитель с вертикальными отступами |
| `DrawLayoutHorizontalBox(Action)` | legacy-wrapper, который сейчас только вызывает переданный callback и не создаёт визуальный box |

Имя `DrawRowSpliter` сохранено в существующем написании ради совместимости публичного API.

## SerializedProperty helpers

| API | Ожидаемое поле | Поведение |
| --- | --- | --- |
| `DrawSprite(property, height, width)` | object reference типа `Sprite` | компактный `ObjectField` без выбора scene object |
| `DrawColor(property, showLable)` | поле, поддерживаемое стандартным `PropertyField` | рисует label либо только само значение |
| `DrawGuidField(property)` | строка | показывает selectable GUID, позволяет скопировать или заменить его через `Guid.NewGuid()` |

Helpers меняют только переданный `SerializedProperty`. Владелец `SerializedObject` отвечает за
применение изменений, Undo/dirty-state и дополнительную валидацию. Кнопка `Refresh` у GUID
заменяет значение сразу и не показывает подтверждение.

## Использования в SDK

- `PRDebugEditor` использует toolbar и раздельную отрисовку адаптивных вкладок;
- `LocalizationWindow` использует готовый `Tabs(...)`;
- окна проектного слоя используют `SplitRow` и property helpers.

Новые универсальные Editor helpers можно добавлять в эту базу, если они не зависят от
конкретного модуля или private SDK. Специализированное поведение лучше оставлять в самом окне.
