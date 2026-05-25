using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public ScientificDetailsUI detailsUI;
    public MinimapController minimap;
    
    private CameraFollow camFollow;
    private CelestialBody lastSelectedBody;

    private void Start()
    {
        if (Camera.main != null)
        {
            camFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (camFollow != null)
        {
            // Subscribe to arrival event to show the UI
            camFollow.OnAutopilotArrived.AddListener(OnCameraArrived);
        }

        if (detailsUI != null)
        {
            detailsUI.SetSelectionManager(this);
        }

        if (minimap != null)
        {
            minimap.SetSelectionManager(this);
        }

        // Auto-select Earth by default
        StartCoroutine(AutoSelectEarthDelayed());
    }

    private System.Collections.IEnumerator AutoSelectEarthDelayed()
    {
        // Wait a frame to ensure all bodies are registered in the SolarSystemRegistry
        yield return null;

        if (SolarSystemRegistry.TryGetBody("Earth", out CelestialBody earth))
        {
            SelectBody(earth);
        }
        else if (SolarSystemRegistry.TryGetBody("Earth(Clone)", out earth))
        {
            SelectBody(earth);
        }
    }

    private void OnDestroy()
    {
        if (camFollow != null)
        {
            camFollow.OnAutopilotArrived.RemoveListener(OnCameraArrived);
        }
    }

    private void OnCameraArrived()
    {
        if (detailsUI != null)
        {
            // Camera has arrived, trigger the animation/display
            detailsUI.StartDisplay();
        }
    }

    private Vector2 touchStartPos;
    private bool isTouchMoving;

    private void Update()
    {
        // Handle Mouse
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection(Input.mousePosition);
        }

        // Handle Touch (Specifically for distinguishing tap vs drag)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                isTouchMoving = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (Vector2.Distance(touch.position, touchStartPos) > 10f)
                {
                    isTouchMoving = true;
                }
            }
            else if (touch.phase == TouchPhase.Ended && !isTouchMoving)
            {
                HandleSelection(touch.position);
            }
        }
    }

    private void HandleSelection(Vector3 screenPos)
    {
        // Ignore clicks over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // For mobile, we need to check fingerId
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;
            
            // For mouse, standard check is fine
            if (Input.touchCount == 0 && EventSystem.current.IsPointerOverGameObject())
                return;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CelestialBody body = hit.collider.GetComponentInParent<CelestialBody>();
            if (body != null) SelectBody(body);
        }
        else
        {
            // Clicked empty space: Disable flight/UI
            if (camFollow != null) camFollow.SetFreeFlight();
            if (detailsUI != null) detailsUI.Hide();
            if (minimap != null) minimap.SetFocus(null);
            lastSelectedBody = null;
        }
    }

    public void SelectBody(CelestialBody body)
    {
        if (body == null) return;
        lastSelectedBody = body;

        // 1. Move Camera
        if (camFollow != null) camFollow.SetAutopilotTarget(body.transform);

        // 2. Prepare Sidebar (it stays hidden until OnCameraArrived)
        if (detailsUI != null)
        {
            detailsUI.Hide(); // Reset state
            detailsUI.Prepare(body);
        }

        // 3. Selection Visuals
        if (minimap != null) minimap.SetFocus(body.transform);
    }
}
