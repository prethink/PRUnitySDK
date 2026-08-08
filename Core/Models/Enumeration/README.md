# Enumeration

`Enumeration` — расширяемый строковый идентификатор для случаев, когда обычный C# `enum` нельзя расширить между модулями или необходимо хранить значение как стабильную строку.

## Объявление значений

Значения группируются в provider:

```csharp
public sealed class ResourceEnumerationProvider : EnumerationProviderBase
{
    public static readonly Enumeration Coin = new(nameof(Coin));
    public static readonly Enumeration Crystal = new(nameof(Crystal));

    public override bool IncludeInherited => true;
}
```

Поля должны быть публичными и статическими. Рекомендуется также использовать `readonly`, чтобы идентификатор нельзя было заменить во время выполнения.

`Enumeration<T>` указывает тип связанного значения:

```csharp
public static readonly Enumeration<float> Sensitivity = new(nameof(Sensitivity));
public static readonly Enumeration<bool> InvertVertical = new(nameof(InvertVertical));
```

Тип `T` используется хранилищами данных и не является самим значением идентификатора.

## Unity-сериализация

Для поля с dropdown в Inspector используйте `EnumerationReference<TProvider>`:

```csharp
[SerializeField]
private EnumerationReference<ResourceEnumerationProvider> currencyType;
```

В asset сохраняется строка. Runtime-значение можно получить через:

```csharp
Enumeration currency = currencyType.ToEnumeration();
```

Если сохранённая строка больше не существует в provider, Inspector показывает `Missing: OldValue` и сохраняет её до явного выбора нового значения. Пустой provider отображается как обычное строковое поле и не вызывает ошибку Inspector.

## Получение значений

```csharp
IReadOnlyList<Enumeration> options =
    typeof(ResourceEnumerationProvider).GetEnumerations(includeInherited: true);
```

Reflection-результаты и экземпляры providers кэшируются. Кэши очищаются при `SubsystemRegistration`, в том числе для Play Mode без domain reload.

`Enumeration.GetOrCreate(value)` возвращает общий runtime-экземпляр для строки. Пустая строка возвращает `null`; конструктор `Enumeration` не принимает `null`, пустые строки и пробелы.

## Равенство и область имён

В текущей версии идентичность глобальна и определяется только строкой с ordinal-сравнением:

```csharp
new Enumeration("IsGround") == new Enumeration("IsGround") // true
```

Provider не входит в равенство. Поэтому одинаковые строки из разных providers считаются одним значением. Если значения не должны пересекаться, используйте уникальные имена:

```csharp
new Enumeration("Player.Flags.IsGround");
new Enumeration("Player.State.IsGround");
```

Изменение equality с добавлением provider scope потребует отдельной миграции сохранений, словарей и публичных API.

## Рекомендации

- Используйте `nameof(Field)` для стабильного объявления.
- Не переименовывайте сохранённые значения без миграции данных.
- Объявляйте поля как `public static readonly`.
- Не создавайте идентификаторы из неограниченного пользовательского ввода.
- Проверяйте `ToEnumeration()` на `null`, если reference может быть не заполнен.

`EnumerationUtility` и `IEnumerationOptionsProvider` оставлены только для обратной совместимости и помечены как устаревшие. Для нового кода используйте `EnumerationExtensions` и `IEnumerationProvider`.
