using UnityEngine;
using UnityEngine.EventSystems;

public class PointerProxy : PRMonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] protected GameObject refObject;

    private IPointerDownHandler pointerDownHandler;
    private IPointerUpHandler pointerUpHandler;
    private IPointerExitHandler pointerExitHandler;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        pointerDownHandler = refObject.GetComponent<IPointerDownHandler>();
        pointerExitHandler = refObject.GetComponent<IPointerExitHandler>();
        pointerUpHandler = refObject.GetComponent<IPointerUpHandler>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownHandler?.OnPointerDown(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerExitHandler?.OnPointerExit(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerUpHandler?.OnPointerUp(eventData);
    }
}
