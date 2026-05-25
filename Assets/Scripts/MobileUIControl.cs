using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileUIControl : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public enum ControlType { Joystick, AltitudeUp, AltitudeDown, Boost }
    public ControlType controlType;
    
    [Header("Joystick Settings")]
    public RectTransform joystickKnob;
    public float joystickRange = 50f;

    private CameraFollow camFollow;
    private Vector2 joystickInput;

    private void Start()
    {
        camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (camFollow == null) return;

        switch (controlType)
        {
            case ControlType.AltitudeUp:
                camFollow.virtualAltitudeInput = 1f;
                break;
            case ControlType.AltitudeDown:
                camFollow.virtualAltitudeInput = -1f;
                break;
            case ControlType.Boost:
                camFollow.virtualBoost = true;
                break;
            case ControlType.Joystick:
                OnDrag(eventData);
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (camFollow == null) return;

        switch (controlType)
        {
            case ControlType.AltitudeUp:
            case ControlType.AltitudeDown:
                camFollow.virtualAltitudeInput = 0f;
                break;
            case ControlType.Boost:
                camFollow.virtualBoost = false;
                break;
            case ControlType.Joystick:
                ResetJoystick();
                break;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controlType != ControlType.Joystick || joystickKnob == null) return;

        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos = Vector2.ClampMagnitude(pos, joystickRange);
            joystickKnob.anchoredPosition = pos;
            
            joystickInput = pos / joystickRange;
            if (camFollow != null) camFollow.virtualMoveInput = joystickInput;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controlType == ControlType.Joystick) ResetJoystick();
    }

    private void ResetJoystick()
    {
        if (joystickKnob != null) joystickKnob.anchoredPosition = Vector2.zero;
        joystickInput = Vector2.zero;
        if (camFollow != null) camFollow.virtualMoveInput = Vector2.zero;
    }
}
