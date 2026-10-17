using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Щёлкает, когда игрок нажимает элемент интерфейса: кнопку, переключатель, карточку окна.
/// </summary>
/// <remarks>
/// Один на игру и смотрит на ввод сам, а не ждёт вызова от кнопок: окна собираются кодом,
/// и общей фабрики, через которую прошла бы каждая кнопка, нет.
/// <para>
/// Нажатием считается то же, что и у <see cref="EventSystem"/>: элемент принимает клик
/// (<see cref="IPointerClickHandler"/>), палец отпущен на нём же и почти не сдвинулся. Поэтому
/// джойстик и кнопки бега, которым нужны нажатие и отпускание, а не клик, молчат, как и
/// прокрутка списка. Неактивная кнопка тоже молчит.
/// </para>
/// <para>
/// Решение принимается раньше, чем кнопка отработает нажатие (<see cref="DefaultExecutionOrderAttribute"/>):
/// крестик закрывает окно, и после этого спросить, что было под пальцем, уже не у кого.
/// </para>
/// </remarks>
[DefaultExecutionOrder(-1000)]
public sealed class UIClickSoundPlayer : MonoBehaviour
{
    private const int MouseId = -1;

    private readonly Dictionary<int, Press> presses = new();
    private readonly List<RaycastResult> hits = new();

    private PointerEventData pointer;
    private EventSystem pointerEventSystem;

    private readonly struct Press
    {
        public Press(GameObject target, Vector2 position)
        {
            Target = target;
            Position = position;
        }

        public GameObject Target { get; }

        public Vector2 Position { get; }
    }

    private void Update()
    {
        // При касании Unity дублирует первый палец мышью: пока пальцы на экране, мышь не слушаем.
        if (Input.touchCount > 0)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnDown(touch.fingerId, touch.position);
                        break;
                    case TouchPhase.Ended:
                        OnUp(touch.fingerId, touch.position);
                        break;
                    case TouchPhase.Canceled:
                        presses.Remove(touch.fingerId);
                        break;
                }
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
            OnDown(MouseId, Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            OnUp(MouseId, Input.mousePosition);
    }

    private void OnDown(int id, Vector2 position)
    {
        GameObject target = FindTarget(position);

        if (target != null)
            presses[id] = new Press(target, position);
        else
            presses.Remove(id);
    }

    private void OnUp(int id, Vector2 position)
    {
        if (!presses.Remove(id, out Press press))
            return;

        // Сдвинутый палец — прокрутка или перетаскивание, а не нажатие.
        float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
        if ((position - press.Position).sqrMagnitude > threshold * threshold)
            return;

        if (FindTarget(position) == press.Target)
            Play();
    }

    /// <summary>
    /// Элемент, который примет клик в этой точке экрана, либо <c>null</c>.
    /// </summary>
    private GameObject FindTarget(Vector2 position)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return null;

        // Своя система событий у каждой сцены: данные указателя привязаны к ней.
        if (pointer == null || pointerEventSystem != eventSystem)
        {
            pointer = new PointerEventData(eventSystem);
            pointerEventSystem = eventSystem;
        }

        pointer.position = position;
        hits.Clear();
        eventSystem.RaycastAll(pointer, hits);

        if (hits.Count == 0)
            return null;

        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);

        if (handler == null)
            return null;

        if (handler.TryGetComponent(out Selectable selectable) && !selectable.IsInteractable())
            return null;

        return handler.GetComponentInParent<UIClickSoundMute>(true) != null ? null : handler;
    }

    private static void Play()
    {
        UIClickSoundSettings settings = PRUnitySDK.Settings != null ? PRUnitySDK.Settings.UIClickSound : null;

        if (settings == null || !settings.Enabled || settings.Clip == null)
            return;

        SoundManager sound = PRUnitySDK.Managers != null ? PRUnitySDK.Managers.Sound : null;

        if (sound != null)
            sound.PlaySoundUIOneShot(settings.Clip, settings.Volume);
    }
}
