# ResourcePaths

`ResourcePaths` — централизованный каталог путей к runtime-ресурсам PRUnitySDK.
Доступ к нему выполняется через `PRUnitySDK.ResourcePaths`. Общие пути позволяют
фабрикам, сервисам и singleton-объектам не дублировать строковые литералы и сохранять
единую структуру папок.

## Базовые пути

Все значения задаются относительно любой папки `Resources`: префикс
`Assets/.../Resources` и расширение файла в путь для `Resources.Load` не входят.

| Поле | Значение | Назначение |
| --- | --- | --- |
| `CorePath` | `PRUnitySDK` | Корень runtime-ресурсов SDK |
| `PrefabsPath` | `PRUnitySDK/Prefabs` | Общие prefab'ы SDK |
| `MonoWindowsPaths` | `PRUnitySDK/Prefabs/Windows/MonoWindows` | Prefab'ы `MonoWindow` |
| `NotifiersPath` | `PRUnitySDK/Prefabs/Windows/Notifier` | Prefab'ы notifier'ов |

Для составления дополнительных путей доступны константы сегментов:

- `PRUnitySDKFolderName`;
- `PrefabsFolderName`;
- `WindowFolderName`;
- `MonoWindowFolderName`;
- `NotifierFolderName`.

## Использование

Используйте наиболее конкретное готовое поле и добавляйте только имя ресурса:

```csharp
public override string ResourcePath =>
    $"{PRUnitySDK.ResourcePaths.MonoWindowsPaths}/InventoryWindow";
```

Такой путь соответствует, например, asset'у:

```text
Assets/PRUnitySDK/Resources/PRUnitySDK/Prefabs/Windows/MonoWindows/InventoryWindow.prefab
```

Одинаковый относительный путь не должен существовать одновременно в нескольких папках
`Resources`, иначе результат `Resources.Load` становится неоднозначным.

## Расширение модулем

`ResourcePaths` объявлен как `partial`. Модуль может добавить собственный путь в файле
`ResourcePaths.<ModuleName>.cs`, расположенном рядом с реализацией модуля:

```csharp
public partial class ResourcePaths
{
    public readonly string InventoryPath =
        $"{PRUnitySDKFolderName}/{PrefabsFolderName}/Inventory";
}
```

Файл расширения должен входить в ту же runtime assembly, что и базовый `ResourcePaths`,
и не должен находиться в папке `Editor`.

Переиспользуемые пути размещайте в публичном `PRUnitySDK`. Пути к game-specific
ресурсам держите рядом с соответствующим модулем `PRUnitySDKPrivate`, не добавляя
private-зависимости в публичное ядро.

## Правила добавления путей

- Собирайте пути из существующих констант сегментов.
- Не добавляйте завершающий `/`.
- Используйте `/`, независимо от файловой системы.
- Храните путь к каталогу, если внутри него загружается несколько однотипных ресурсов.
- Не включайте в значение имя папки `Resources` или расширение asset'а.
- Для нового общего каталога сначала добавьте именованную константу сегмента, затем
  вычисляемое `readonly`-поле полного пути.
