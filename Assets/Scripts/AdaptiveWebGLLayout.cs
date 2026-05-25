using UnityEngine;
using UnityEngine.UI;

public class AdaptiveWebGLLayout : MonoBehaviour
{
    [Header("Desktop Embed Scaling")]
    [SerializeField] private Vector2 desktopReferenceResolution = new Vector2(1600f, 900f);
    [SerializeField] private float desktopMinimumScale = 0.68f;
    [SerializeField] private float sidebarScaleBias = 0.12f;

    private RectTransform scientificSidebar;
    private RectTransform minimapPanel;
    private RectTransform flightHud;
    private CanvasGroup mobileControlsGroup;

    private Vector3 scientificSidebarBaseScale = Vector3.one;
    private Vector3 minimapPanelBaseScale = Vector3.one;
    private Vector3 flightHudBaseScale = Vector3.one;

    private int lastWidth = -1;
    private int lastHeight = -1;

    private void Awake()
    {
        CacheReferences();
        ApplyLayout();
    }

    private void Update()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
        {
            return;
        }

        ApplyLayout();
    }

    private void CacheReferences()
    {
        scientificSidebar = GameObject.Find("ScientificSidebar")?.GetComponent<RectTransform>();
        minimapPanel = GameObject.Find("MinimapPanel")?.GetComponent<RectTransform>();
        flightHud = GameObject.Find("FlightHUD")?.GetComponent<RectTransform>();
        mobileControlsGroup = GameObject.Find("MobileControlsOverlay")?.GetComponent<CanvasGroup>();

        if (scientificSidebar != null)
        {
            scientificSidebarBaseScale = scientificSidebar.localScale;
        }

        if (minimapPanel != null)
        {
            minimapPanelBaseScale = minimapPanel.localScale;
        }

        if (flightHud != null)
        {
            flightHudBaseScale = flightHud.localScale;
        }
    }

    public bool ShouldUseMobileControls()
    {
        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
    }

    public void ApplyLayout()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float widthRatio = Screen.width / desktopReferenceResolution.x;
        float heightRatio = Screen.height / desktopReferenceResolution.y;
        float desktopScale = Mathf.Clamp(Mathf.Min(widthRatio, heightRatio), desktopMinimumScale, 1f);

        if (scientificSidebar != null)
        {
            float sidebarScale = Mathf.Clamp(desktopScale + sidebarScaleBias, desktopMinimumScale, 1f);
            scientificSidebar.localScale = scientificSidebarBaseScale * sidebarScale;
        }

        if (minimapPanel != null)
        {
            minimapPanel.localScale = minimapPanelBaseScale * desktopScale;
        }

        if (flightHud != null)
        {
            flightHud.localScale = flightHudBaseScale * desktopScale;
        }

        // Removed platform detection - visibility is now managed by ScientificDetailsUI
    }
}
