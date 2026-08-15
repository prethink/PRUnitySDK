# Окна PRSDKDatabase и PRSDKSettings

Assets `PRSDKDatabase` и `PRSDKSettings` редактируются в отдельных растягиваемых `EditorWindow`. Обычный Unity
Inspector при этом не переопределяется. Окна автоматически показывают поля, добавленные к singleton-классам через
partial, поэтому при подключении нового модуля центральный Editor изменять не требуется.

Оба окна поддерживают:

- поиск секции по названию;
- сворачивание и разворачивание секций;
- сохранение изменённых assets;
- растягивание вложенных списков и настроек на доступную ширину Inspector.

Публичный SDK использует только стандартный Unity Editor API и не зависит от инспекторов или атрибутов private SDK.

Окна открываются через меню `PRUnitySDK/Windows/Database` и `PRUnitySDK/Windows/Settings`. Кнопка `Asset` выбирает
редактируемый asset в обычном Unity Inspector.

## PRSDKDatabase

Каждая секция имеет собственный foldout и раскрывается независимо от остальных. Развёрнутый каталог использует
ширину окна и получает высоту относительно текущего размера окна. Если `Database<T>` находится внутри другого
сериализуемого объекта, редактор также распознаёт его и показывает отдельной вложенной секцией.

Для каждой секции, наследующейся от `Database<T>`, где `T` является Unity asset, автоматически появляются
дополнительные операции:

- `Добавить все` — находит в `Assets` все совместимые definitions и добавляет только отсутствующие ссылки;
- `Убрать null` — удаляет повреждённые пустые элементы;
- `Очистить` — очищает секцию после подтверждения, не удаляя сами asset-файлы.

`Database<T>.Validate()` по умолчанию проверяет пустые элементы, дубликаты элементов, пустые и повторяющиеся
стабильные `Id`. Конкретная база может дополнить правила:

```csharp
[Serializable]
public sealed class ExampleDatabase : Database<ExampleDefinition>
{
    public override IEnumerable<DatabaseValidationIssue> Validate()
    {
        foreach (DatabaseValidationIssue issue in base.Validate())
            yield return issue;

        foreach (ExampleDefinition definition in Data)
        {
            if (definition != null && definition.Icon == null)
            {
                yield return new DatabaseValidationIssue(
                    "missing-icon",
                    $"У '{definition.name}' отсутствует иконка.",
                    DatabaseValidationSeverity.Warning);
            }
        }
    }
}
```

`BrainrotDatabase` и `PetDatabase` уже дополняют общую проверку: проверяют имя, иконку, переводы и prefab.
Brainrot также проверяет отрицательные `Income` и `Cost`.

### Видимость действий

Кнопки и результаты проверки управляются атрибутом на классе базы:

```csharp
[DatabaseEditorOptions(
    ShowAddAll = true,
    ShowRemoveNull = true,
    ShowClear = false,
    ShowValidation = true)]
[Serializable]
public sealed class ExampleDatabase : Database<ExampleDefinition>
{
}
```

Если атрибут отсутствует, все поддерживаемые действия включены. `Добавить все` и `Убрать null` появляются только
для коллекций Unity assets; для обычных сериализуемых значений остаются список и базовая валидация.

### Представление ItemDefinition

Базы наследников `ItemDefinitionBase` в режиме `Auto` отображаются как каталог:

- слева находятся карточки с `IIconProvider.Icon` и `INameProvider.Name`;
- выбранная карточка подсвечивается цветом `IQualityProvider.Quality`, если качество доступно;
- справа стандартный встроенный редактор показывает все сериализованные поля выбранного asset без подключения
  стороннего custom inspector;
- границу между каталогом и свойствами можно перетаскивать, а обе панели имеют независимую прокрутку и занимают
  всю доступную высоту окна;
- карточки можно сортировать по качеству, имени или дате добавления, фильтровать по качеству и оставлять только
  элементы с проблемами валидации;
- отдельный definition можно добавить через ObjectField над сеткой или удалить кнопкой в правой панели.

Сортировка по дате добавления использует позицию элемента в сериализованном списке базы. Это сохраняет стабильный
результат после переноса проекта и работы через систему контроля версий, где файловая дата создания asset может
измениться. Кнопка со стрелкой переключает направление сортировки. Сортировка влияет только на карточки в Editor
и не переставляет элементы в самой базе.

Фильтр `Только с ошибками` использует `DatabaseValidationIssue.Index`. Поэтому конкретный валидатор должен указывать
индекс проблемного элемента; ошибки с индексом `-1`, относящиеся ко всей базе, не соответствуют отдельной карточке.

Для `SerializedCollections` встроенная правая панель создаёт существующий `SerializedDictionaryInstanceDrawer`,
но хранит его в экземпляре Editor конкретного asset. Поэтому словарь выглядит так же, как в обычном Inspector,
а drawer не сохраняет ссылку на `SerializedProperty` уже уничтоженного Editor. Файлы стороннего плагина не изменяются.

Для остальных полей используется обычный `EditorGUILayout.PropertyField`, поэтому Unity автоматически сохраняет
существующие `PropertyDrawer`: например `SpritePreviewDrawer`, `PrefabPreviewDrawer`, drawer ссылок `Enumeration`
и стандартные отображения массивов, объектов, enum и ссылок. Результат соответствует обычному Inspector, при этом
глобальный private `NaughtyInspector` не становится зависимостью публичного окна.

Представление можно установить явно:

```csharp
[DatabaseEditorOptions(Presentation = DatabaseEditorPresentation.Grid)]
public sealed class ItemDatabase : Database<MyItemDefinition>
{
}
```

`DatabaseEditorPresentation.Default` возвращает обычный список, а `Auto` выбирает сетку для любого наследника
`ItemDefinitionBase`. `BrainrotDatabase` и `PetDatabase` явно используют `Grid`.

Список по-прежнему можно редактировать вручную. Поэтому конкретный проект может держать в базе только нужные
definitions и не включать остальные ссылки в конфигурацию перед сборкой.

Очистка ссылки в базе сама по себе не удаляет asset и не гарантирует его исключение из билда: Unity всё равно
включит definition, если на него ссылается сцена, prefab, другой включённый asset либо он находится в `Resources`.

## PRSDKSettings

Каждое сериализованное partial-поле показывается отдельной секцией. Например, `Advertising`, `Rating`, `Quality`
или настройки нового модуля автоматически появятся в инспекторе после добавления поля в `PRSDKSettings`.
