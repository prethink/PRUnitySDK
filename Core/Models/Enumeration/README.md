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

## Значение по умолчанию

Незаполненная ссылка раньше отдавала `null`, хотя выпадающий список показывал первый
пункт: в инспекторе значение выглядело выбранным, а в коде его не было — и расхождение
никак себя не проявляло, пока что-нибудь не переставало считаться.

Значение по умолчанию объявляет сам набор, и объявляет обязательно — свойство
абстрактное, как и `IncludeInherited`. Чем заменить пустое значение, знает только набор,
и общего правила тут нет: у одних это первый пункт, у других осмысленного умолчания
не существует вовсе, и честный ответ — `null`.

Обычный случай — первое объявленное значение, для него есть `FirstOption`:

```csharp
public partial class GizmoEnumerations : EnumerationProviderBase
{
    public override Enumeration Default => FirstOption;
    public override bool IncludeInherited => true;
}
```

Порядок значений задаётся атрибутом:

```csharp
public partial class LevelObjectGroups : ObjectStateGroupEnumerations
{
    [EnumerationOrder(-10)] public static readonly Enumeration Crystals = new(nameof(Crystals));
    [EnumerationOrder(10)]  public static readonly Enumeration Doors = new(nameof(Doors));
}
```

Чем меньше число, тем раньше значение в списке. Значения без атрибута считаются нулевыми
и идут между отрицательными и положительными, сохраняя между собой порядок объявления —
поэтому одно значение можно поднять наверх, не расставляя номера всем остальным.

Атрибут работает и между уровнями иерархии: наследник может поставить своё значение
впереди базовых.

Без атрибута порядок прежний — как объявлено в коде: сначала базовый набор, внутри типа
поля сортируются по `MetadataToken`, который растёт в порядке объявления. Само по себе
это работает, но у `partial`-набора части лежат в разных файлах, и порядок между ними
определяется тем, в каком порядке компилятор получил файлы, то есть их именами. Там,
где порядок важен, его лучше задать атрибутом.

Порядок влияет и на выпадающий список в инспекторе, и на `FirstOption`, а значит
и на значение по умолчанию тех наборов, которые берут его оттуда.

Когда умолчание не совпадает с первым пунктом, его называют явно:

```csharp
public partial class ObjectStateGroupEnumerations : EnumerationProviderBase
{
    public static readonly Enumeration Common = new(nameof(Common));

    public override Enumeration Default => Common;
    public override bool IncludeInherited => true;
}
```

`EnumerationReference<T>.ToEnumeration()` при пустом значении возвращает `Default`,
а инспектор показывает именно его, а не первый пункт списка.

`Value` не бывает `null`: без выбранного значения отдаётся значение по умолчанию,
а если набор его не объявил — пустая строка. Строку можно сравнивать и выводить,
не проверяя каждый раз.

Саму ссылку тоже стоит инициализировать в поле:

```csharp
[SerializeField] private EnumerationReference<ObjectStateGroupEnumerations> group = new();
```

Уже выбранное значение это не перезатрёт: инициализатор отрабатывает при создании
объекта, а Unity накладывает сериализованные данные поверх. Смысл в другом — до первой
сериализации, у объекта, созданного кодом, или сразу после `AddComponent`, поле бывает
пустым, и обращение к нему давало бы `null` мимо значения по умолчанию.

Там, где ссылка может не существовать вовсе, есть статические формы — они разбираются
и с пустым значением, и с отсутствующей ссылкой:

```csharp
Enumeration group = EnumerationReference<ObjectStateGroupEnumerations>.ToEnumeration(field);
string name = EnumerationReference<ObjectStateGroupEnumerations>.ToValue(field);
```

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
