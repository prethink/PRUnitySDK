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

### AnimatorPauseMonitor

Находит `Animator` на текущем объекте и в детях, сохраняет их скорость, устанавливает
`speed = 0` при логической паузе и восстанавливает сохранённое значение после неё.
Дополнительный Animator можно зарегистрировать через `RegisterAnimator()`.

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
