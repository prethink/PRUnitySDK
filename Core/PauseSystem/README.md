# PauseSystem

PauseSystem централизует разные причины паузы и уведомляет заинтересованные системы через
`EventBus`. Пауза не ограничивается изменением `Time.timeScale`: игровая логика SDK
проверяет `PRUnitySDK.PauseManager.IsLogicPaused`.

## Виды паузы

| Свойство | Из чего складывается |
| --- | --- |
| `IsProjectPaused` | Project pause или потеря фокуса |
| `IsMusicPaused` | Project pause, music pause или потеря фокуса |
| `IsLogicPaused` | Project, logic, focus, tutorial или cutscene pause |
| `IsTutorialPaused` | Отдельная пауза туториала |
| `IsCutScenePaused` | Отдельная пауза катсцены |
| `IsFocusPaused` | Пауза при потере фокуса |

## Доступ к менеджеру

```csharp
IPauseManager pause = PRUnitySDK.PauseManager;

pause.SetLogicPaused(true, this, isUserAction: true);
bool isPaused = pause.IsLogicPaused;
pause.SetLogicPaused(false, this, isUserAction: true);
```

Параметр `executer` сохраняется в `PauseStateEventArgs` и позволяет определить источник
изменения. Текущая реализация хранит по одному `bool` на каждый вид паузы: это не стек
запросов. Если две системы установили один вид паузы, снятие паузы одной системой снимет
его для обеих.

## Получение уведомлений

```csharp
public class PauseView : MonoBehaviour, IPauseStateListener
{
    private void OnEnable() => EventBus.Subscribe(this);
    private void OnDisable() => EventBus.Unsubscribe(this);

    public void OnPauseStateChanged(PauseStateEventArgs args)
    {
        gameObject.SetActive(PRUnitySDK.PauseManager.IsLogicPaused);
    }
}
```

`PauseStateEventArgs` сообщает:

- какой вид паузы изменился;
- предыдущее значение;
- новое значение изменённого флага через `CurrentValue`;
- был ли запрос пользовательским;
- кто инициировал изменение;
- является ли уведомление принудительным/custom.

После события проверяйте итоговые свойства `PauseManager`: снятие одного флага не
гарантирует, что логика продолжилась, поскольку активной может оставаться другая причина.

## Мониторы Unity-компонентов

Для анимации есть два способа остановки — со снимком скорости и с ручным тиком. Они
сосуществуют: аниматор, которым управляет `AnimatorTimeScaleDriver`, монитор не трогает.

| | `AnimatorPauseMonitor` | `AnimatorTimeScaleDriver` |
| --- | --- | --- |
| Кто обновляет аниматор | Unity | драйвер вручную |
| Пауза | `speed = 0` + снимок прежней скорости | шаг времени не передаётся |
| Замедление | через `animator.speed` | через размер шага времени |
| Слои `PRTimeScale` | общий множитель скорости | шаг умножается на `Resolve(layer)` |
| Root motion | работает как обычно | требует ручного применения |

### AnimatorPauseMonitor

Находит `Animator` на текущем объекте и в детях, сохраняет их скорость, устанавливает
`speed = 0` при логической паузе и восстанавливает сохранённое значение после неё.
Дополнительный Animator можно зарегистрировать через `RegisterAnimator()`.

**Скорость такого аниматора нельзя менять напрямую.** Пока пауза активна, реальное
значение равно нулю, а исходное лежит в снимке — прямая запись `animator.speed`
потеряется при возобновлении. Используйте `AnimatorPauseMonitor.SetSpeed(animator, speed)`:
он пишет в снимок во время паузы и в аниматор в остальное время.

```csharp
// PlayerAnimationController
private void ApplyTimeScale()
{
    var timeScale = PRTimeScale.Instance.Resolve(entity.GetTimeScaleLayer());
    AnimatorPauseMonitor.SetSpeed(animator, timeScale);
}
```

### AnimatorTimeScaleDriver

Тикает аниматор вручную — так же, как хост тикает физику через
`Physics.Simulate(GameFixedDeltaTime)`. Компонент выводит аниматор из автоматического
обновления (`animator.enabled = false`) и продвигает его в `PRUpdate`:

```csharp
var deltaTime = PRTime.Instance.RealDeltaTime * PRTimeScale.Instance.Resolve(layer);
animator.Update(deltaTime);
```

`PRMonoBehaviour` не вызывает `PRUpdate` во время логической паузы, поэтому анимация
останавливается сама — снимок скорости не нужен, и затирать нечего.

Настройки компонента:

| Поле | Назначение |
| --- | --- |
| `animator` | управляемый аниматор, по умолчанию берётся с этого объекта |
| `timeScaleLayer` | слой масштаба времени; пусто — глобальный |
| `restoreOnDisable` | вернуть автообновление при выключении; нужно для объектов из пула |

Когда выбирать драйвер: нужны разные скорости у разных сущностей (замедлить врага, но не
игрока), детерминированное продвижение анимации или единый источник времени с физикой.

Когда оставить монитор: у объекта root motion, либо достаточно обычной остановки на паузе.

### RigidBodyPauseMonitor

Находит дочерние `Rigidbody`, сохраняет velocity, angular velocity и gravity, затем
обнуляет скорости и отключает gravity. После снятия паузы значения восстанавливаются.

Текущая реализация намеренно не переводит Rigidbody в `isKinematic` и не восстанавливает
это поле: соответствующий код отключён из-за известной проблемы. Поэтому монитор не
гарантирует полной остановки внешней физической симуляции.

## Связь с другими системами

- `PRMonoBehaviour` пропускает PR update-циклы во время логической паузы.
- `PRTime` обнуляет игровые delta time во время логической паузы.
- `WaitPause` удерживает корутину, пока логическая пауза активна.
- `WaitContinueGame` делает обратное: ждёт наступления логической паузы.

## Рекомендации

- Для нескольких независимых владельцев паузы используйте отдельные типы паузы или
  добавьте token/source-based механизм поверх текущего API.
- Не полагайтесь только на `Time.timeScale = 0`: это не является контрактом PauseSystem.
- Подписывайтесь в `OnEnable` и отписывайтесь в `OnDisable`, если объект не должен
  получать события в выключенном состоянии.
