using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputController : MonoBehaviour
{
    private CameraFollow camFollow;
    
    [Header("Sensitivity Settings")]
    public float dragSensitivity = 0.5f;
    public float pinchSensitivity = 0.01f;
    public float tapThreshold = 10f; // Pixels move threshold for a tap

    private Vector2 lastTouchPos;
    private float lastPinchDist;

    private void Start()
    {
        camFollow = GetComponent<CameraFollow>();
        if (camFollow == null) camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (camFollow == null) return;

        // Reset virtual inputs that should be momentary
        camFollow.virtualOrbitInput = Vector2.zero;
        camFollow.virtualZoomInput = 0;
        camFollow.isVirtualOrbiting = false;

        if (Input.touchCount == 0) return;

        // Handle Over UI check (usually the first touch)
        if (EventSystem.current != null && Input.touchCount > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                // If touching UI, don't process gestures (let the UI handle it)
                return;
            }
        }

        if (Input.touchCount == 1)
        {
            HandleOrbitDrag();
        }
        else if (Input.touchCount == 2)
        {
            HandlePinchZoom();
        }
    }

    private void HandleOrbitDrag()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            camFollow.isVirtualOrbiting = true;
            camFollow.virtualOrbitInput = new Vector2(
                touch.deltaPosition.x * dragSensitivity,
                touch.deltaPosition.y * dragSensitivity
            );
        }
    }

    private void HandlePinchZoom()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            float currentDist = Vector2.Distance(touch0.position, touch1.position);
            float prevDist = Vector2.Distance(touch0.position - touch0.deltaPosition, touch1.position - touch1.deltaPosition);
            
            float deltaDist = currentDist - prevDist;
            camFollow.virtualZoomInput = deltaDist * pinchSensitivity;
        }
    }
}
