using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Controls the 2D Minimap UI with high compatibility and diagnostic logging.
/// Enhanced with exponential smooth zoom, tactical locking, and ultra-wide scale.
/// </summary>
public class MinimapController : MonoBehaviour, IScrollHandler, IDragHandler
{
    public RectTransform mapContainer;
    public RectTransform playerMarker;
    
    [Header("Exponential Zoom Settings")]
    public float zoomSpeed = 0.15f;
    public float zoomSmoothing = 8f;
    [Tooltip("Allows seeing the entire solar system out to Neptune and beyond.")]
    public float minZoom = 0.0000001f; 
    public float maxZoom = 100f;
    
    [Header("Icon & LOD Settings")]
    public float iconScaleMultiplier = 10f;
    public float minIconSize = 15f;
    public float minSunSize = 35f;
    public Sprite circleSprite;
    [Tooltip("Zoom level at which small bodies (moons) start to fade out.")]
    public float lodThreshold = 0.0005f;
    
    [Header("Tactical Overlays")]
    public Sprite orbitRingSprite;
    public RawImage gridBackground;
    public float gridTileMultiplier = 1.0f;
    
    [Header("Scale Bar")]
    public RectTransform scaleBarLine;
    public TextMeshProUGUI scaleBarText;
    public float scaleBarTargetPixels = 100f;

    [Header("Tooltip")]
    public TextMeshProUGUI tooltipText;

    [Header("Visibility Toggles")]
    public bool showMoons = true;
    public bool showSatellites = true;

    // Internal State
    private float currentZoom = 0.001f; 
    private float targetZoom = 0.001f;
    private Transform targetToLock;
    
    private Dictionary<CelestialBody, RectTransform> markers = new Dictionary<CelestialBody, RectTransform>();
    private Dictionary<CelestialBody, RectTransform> orbitRings = new Dictionary<CelestialBody, RectTransform>();
    private HashSet<CelestialBody> ignoredBodies = new HashSet<CelestialBody>();
    private CameraFollow camFollow;
    private SelectionManager selectionManager;
    
    private float nextSyncTime = 0f;
    private const float SYNC_INTERVAL = 1.0f;

    void Start()
    {
        if (Camera.main != null) camFollow = Camera.main.GetComponent<CameraFollow>();
        selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        if (mapContainer != null) mapContainer.anchoredPosition = Vector2.zero;
        
        // Initial setup
        RefreshAll();
        AutoCenterZoom();
    }

    public void RefreshAll()
    {
        foreach (var kvp in markers) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        foreach (var kvp in orbitRings) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        markers.Clear();
        orbitRings.Clear();
        ignoredBodies.Clear();
        SyncMarkers();
    }

    void Update()
    {
        if (mapContainer == null) return;

        // 0. Shortcut for Home/Reset
        if (Input.GetKeyDown(KeyCode.H))
        {
            AutoCenterZoom();
        }

        // 1. Exponential Smooth Zoom
        currentZoom = Mathf.Exp(Mathf.Lerp(Mathf.Log(currentZoom), Mathf.Log(targetZoom), Time.deltaTime * zoomSmoothing));

        if (Time.time >= nextSyncTime)
        {
            SyncMarkers();
            nextSyncTime = Time.time + SYNC_INTERVAL;
        }

        // 2. Target Locking Logic
        if (targetToLock != null)
        {
            Vector3 pos = targetToLock.position;
            // Snap the position instantly to the current zoom level to keep the target pinned to center
            mapContainer.anchoredPosition = -new Vector2(pos.x, pos.z) * currentZoom;
        }

        UpdateMarkers();
        UpdateOrbitRings();
        UpdateGrid();
        UpdateScaleBar();
        UpdatePlayerMarker();
    }

    private void UpdateMarkers()
    {
        foreach (var kvp in markers)
        {
            CelestialBody body = kvp.Key;
            RectTransform marker = kvp.Value;
            if (body == null || marker == null) continue;

            bool isMoon = (body.info != null && body.info.type == "Moon") || (body.parentBody != null);
            bool isSat = (body.info != null && body.info.type == "Satellite") || (body.GetComponent<LagrangeSatellite>() != null);

            // Check Visibility Toggles FIRST
            if (isMoon && !showMoons) { marker.gameObject.SetActive(false); continue; }
            if (isSat && !showSatellites) { marker.gameObject.SetActive(false); continue; }

            Vector3 pos = body.transform.position;
            marker.anchoredPosition = new Vector2(pos.x, pos.z) * currentZoom;

            float baseSize = body.transform.localScale.x * currentZoom * iconScaleMultiplier;
            float finalSize = Mathf.Max(baseSize, minIconSize);
            if (body.gameObject.name == "Sun") finalSize = Mathf.Max(baseSize, minSunSize);
            marker.sizeDelta = new Vector2(finalSize, finalSize);

            // LOD Fading for small bodies
            if (isMoon || isSat)
            {
                CanvasGroup cg = marker.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    float targetAlpha = Mathf.InverseLerp(lodThreshold * 0.2f, lodThreshold * 5f, currentZoom);
                    cg.alpha = targetAlpha;
                    marker.gameObject.SetActive(targetAlpha > 0.05f); // Turn off if too faded
                }
                else marker.gameObject.SetActive(true);
            }
            else marker.gameObject.SetActive(true);
        }
    }

    private void UpdateOrbitRings()
    {
        foreach (var kvp in orbitRings)
        {
            CelestialBody body = kvp.Key;
            RectTransform ring = kvp.Value;
            if (body == null || ring == null) continue;

            // Hide rings for moons/sats if their markers are hidden
            bool isMoon = (body.info != null && body.info.type == "Moon") || (body.parentBody != null);
            if (isMoon && !showMoons) { ring.gameObject.SetActive(false); continue; }
            else ring.gameObject.SetActive(true);

            float radius = (float)(body.info != null ? (body.info.distance_from_sun_10e6_km * 10.0 / 100.0) : body.transform.position.magnitude);
            float pixelRadius = radius * currentZoom * 2f; 
            ring.sizeDelta = new Vector2(pixelRadius, pixelRadius);
            ring.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateGrid()
    {
        if (gridBackground != null)
        {
            gridBackground.uvRect = new Rect(
                -mapContainer.anchoredPosition.x / (gridBackground.rectTransform.rect.width * 0.1f), 
                -mapContainer.anchoredPosition.y / (gridBackground.rectTransform.rect.height * 0.1f), 
                10f, 10f
            );
        }
    }

    private void UpdateScaleBar()
    {
        if (scaleBarLine != null && scaleBarText != null)
        {
            double kmPerPixel = 100000.0 / currentZoom;
            double totalKm = kmPerPixel * scaleBarTargetPixels;
            string distText = (totalKm >= 149597870.7) ? (totalKm / 149597870.7).ToString("F2") + " AU" : 
                             (totalKm >= 1000000) ? (totalKm / 1000000.0).ToString("F1") + "M km" : totalKm.ToString("N0") + " km";
            scaleBarText.text = distText;
        }
    }

    private void UpdatePlayerMarker()
    {
        if (playerMarker != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            playerMarker.anchoredPosition = new Vector2(camPos.x, camPos.z) * currentZoom;
            playerMarker.localRotation = Quaternion.Euler(0, 0, -Camera.main.transform.eulerAngles.y);
        }
    }

    private void SyncMarkers()
    {
        CelestialBody[] allBodies = SolarSystemRegistry.GetBodiesSnapshot();
        List<CelestialBody> toRemove = new List<CelestialBody>();
        foreach (var key in markers.Keys) if (key == null) toRemove.Add(key);
        foreach (var r in toRemove) {
            if (markers.ContainsKey(r) && markers[r] != null) Destroy(markers[r].gameObject);
            markers.Remove(r);
        }

        foreach (var body in allBodies)
        {
            if (body == null || markers.ContainsKey(body) || ignoredBodies.Contains(body)) continue;
            bool isMoon = (body.info != null && body.info.type == "Moon") || (body.parentBody != null);
            bool isSat = (body.info != null && body.info.type == "Satellite") || (body.GetComponent<LagrangeSatellite>() != null);
            CreateMarker(body);
            if (!isMoon && !isSat && body.name != "Sun") CreateOrbitRing(body);
        }
    }

    private void CreateMarker(CelestialBody body)
    {
        GameObject markerObj = new GameObject("Marker_" + body.name);
        markerObj.transform.SetParent(mapContainer, false);
        if (body.name == "Sun") markerObj.transform.SetAsFirstSibling();
        else markerObj.transform.SetAsLastSibling();

        RectTransform rt = markerObj.AddComponent<RectTransform>();
        Image img = markerObj.AddComponent<Image>();
        img.sprite = circleSprite;
        markerObj.AddComponent<CanvasGroup>();

        Color c = GetBodyColor(body);
        if (body.name == "Sun") c.a = 0.6f;
        img.color = c;

        Button btn = markerObj.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            SetFocus(body.transform);
            if (camFollow != null) camFollow.SetAutopilotTarget(body.transform);
            SelectionManager sel = ResolveSelectionManager();
            if (sel != null) sel.SelectBody(body);
        });

        markers.Add(body, rt);
    }

    private void CreateOrbitRing(CelestialBody body)
    {
        if (orbitRingSprite == null) return;
        GameObject ringObj = new GameObject("OrbitRing_" + body.name);
        ringObj.transform.SetParent(mapContainer, false);
        ringObj.transform.SetAsFirstSibling(); 
        RectTransform rt = ringObj.AddComponent<RectTransform>();
        Image img = ringObj.AddComponent<Image>();
        img.sprite = orbitRingSprite;
        img.raycastTarget = false;
        
        Color c = GetBodyColor(body);
        c.a = 0.15f; 
        img.color = c;
        
        orbitRings.Add(body, rt);
    }

    private Color GetBodyColor(CelestialBody body)
    {
        // 1. Try to get color from Renderer
        Renderer rend = body.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            if (rend.sharedMaterial.HasProperty("_BaseColor")) return rend.sharedMaterial.GetColor("_BaseColor");
            if (rend.sharedMaterial.HasProperty("_Color")) return rend.sharedMaterial.GetColor("_Color");
        }

        // 2. Fallback to predefined colors based on name
        string n = body.name.Replace("(Clone)", "").Trim();
        switch (n)
        {
            case "Sun": return new Color(1f, 0.9f, 0.2f);
            case "Mercury": return Color.gray;
            case "Venus": return new Color(1f, 0.64f, 0.0f);
            case "Earth": return new Color(0.2f, 0.5f, 1.0f);
            case "Moon": return new Color(0.8f, 0.8f, 0.8f);
            case "Mars": return new Color(1f, 0.3f, 0.2f);
            case "Jupiter": return new Color(0.8f, 0.6f, 0.4f);
            case "Saturn": return new Color(0.9f, 0.8f, 0.5f);
            case "Uranus": return new Color(0.6f, 0.9f, 1.0f);
            case "Neptune": return new Color(0.3f, 0.4f, 1.0f);
            case "Pluto": return new Color(0.7f, 0.6f, 0.5f);
            default: return Color.white;
        }
    }

    public void SetSelectionManager(SelectionManager manager) => selectionManager = manager;
    public void SetFocus(Transform target) => targetToLock = target;
    
    public void ToggleMoons(bool value) 
    { 
        showMoons = value; 
        UpdateMarkers(); // Force immediate update
        Debug.Log($"[Minimap] Moons Visibility: {value}"); 
    }
    
    public void ToggleSatellites(bool value) 
    { 
        showSatellites = value; 
        UpdateMarkers(); // Force immediate update
        Debug.Log($"[Minimap] Satellites Visibility: {value}"); 
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    public void AutoCenterZoom()
    {
        CelestialBody[] bodies = SolarSystemRegistry.GetBodiesSnapshot();
        if (bodies.Length == 0) return;

        float maxDist = 10f;
        foreach (var body in bodies)
        {
            if (body == null) continue;
            // Filter to find the most distant planet (Neptune/Pluto)
            float dist = body.transform.position.magnitude;
            if (dist > maxDist) maxDist = dist;
        }
        
        // Calibrate so the furthest planet is within the 350x350 panel
        targetZoom = 150f / maxDist;
        currentZoom = targetZoom;
        if (mapContainer != null) mapContainer.anchoredPosition = Vector2.zero;
        SetFocus(null);
    }

    public void OnScroll(PointerEventData eventData)
    {
        // Exponential zoom: Multiply/Divide to keep relative speed consistent across scales
        float multiplier = 1f + (Mathf.Abs(eventData.scrollDelta.y) * zoomSpeed);
        if (eventData.scrollDelta.y > 0) targetZoom *= multiplier;
        else targetZoom /= multiplier;
        
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mapContainer != null) {
            mapContainer.anchoredPosition += eventData.delta;
            if (eventData.delta.magnitude > 1f) targetToLock = null;
        }
    }

    private SelectionManager ResolveSelectionManager()
    {
        if (selectionManager == null)
        {
            selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        }

        return selectionManager;
    }
}
