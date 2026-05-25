using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Unified Scientific Data Panel that handles typing effects, scrollable content,
/// and dynamic lists for moons and satellites.
/// </summary>
public class ScientificDetailsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI detailsText;
    public CanvasGroup canvasGroup;
    public RectTransform scanLine;
    public ScrollRect scrollRect;

    [Header("Lists")]
    public GameObject moonListContainer;
    public GameObject satelliteListContainer;
    public GameObject listButtonPrefab;

    [Header("Settings")]
    public float fadeInSpeed = 4f;
    public float typeSpeed = 0.005f;
    public float scanSpeed = 250f;
    public float scanRange = 180f;

    private bool isVisible = false;
    private CelestialBody currentBody;
    private Coroutine typingCoroutine;
    private readonly List<GameObject> activeListButtons = new List<GameObject>();
    private SelectionManager selectionManager;
    private CanvasGroup mobileControlsGroup;
    private AdaptiveWebGLLayout adaptiveLayout;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0;
        selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        adaptiveLayout = Object.FindFirstObjectByType<AdaptiveWebGLLayout>();
        
        // Find the mobile controls overlay
        GameObject overlay = GameObject.Find("MobileControlsOverlay");
        if (overlay != null)
        {
            mobileControlsGroup = overlay.GetComponent<CanvasGroup>();
            if (mobileControlsGroup == null) mobileControlsGroup = overlay.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Prepares the data and populates lists, but keeps the panel invisible.
    /// </summary>
    public void Prepare(CelestialBody body)
    {
        if (body == null || body.info == null) return;
        currentBody = body;

        isVisible = false;
        if (canvasGroup != null) canvasGroup.alpha = 0;
        
        // Hide mobile controls while panel is preparing/transitioning
        UpdateMobileControlsVisibility(false);

        PopulateLists(body);

        if (headerText != null) headerText.text = string.Empty;
        if (detailsText != null) detailsText.text = string.Empty;
    }

    /// <summary>
    /// Triggers the fade-in and starts the typing animation.
    /// </summary>
    public void StartDisplay()
    {
        if (currentBody == null) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        isVisible = true;
        UpdateMobileControlsVisibility(false);
        typingCoroutine = StartCoroutine(TypeScientificData(currentBody));
    }

    public void Hide()
    {
        isVisible = false;
        currentBody = null;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        ClearLists();
        if (canvasGroup != null) canvasGroup.alpha = 0;
        UpdateMobileControlsVisibility(true);
    }

    private void UpdateMobileControlsVisibility(bool show)
    {
        if (mobileControlsGroup != null)
        {
            mobileControlsGroup.alpha = show ? 1f : 0f;
            mobileControlsGroup.interactable = show;
            mobileControlsGroup.blocksRaycasts = show;
        }
    }

    private void PopulateLists(CelestialBody body)
    {
        ClearLists();
        if (listButtonPrefab == null) return;

        if (moonListContainer != null)
        {
            bool hasMoons = body.childMoons != null && body.childMoons.Length > 0;
            if (moonListContainer.transform.parent != null)
                moonListContainer.transform.parent.gameObject.SetActive(hasMoons);

            if (hasMoons)
            {
                foreach (CelestialBody moon in body.childMoons) CreateListButton(moon, moonListContainer.transform);
            }
        }

        if (satelliteListContainer != null)
        {
            bool hasSats = body.childSatellites != null && body.childSatellites.Length > 0;
            if (satelliteListContainer.transform.parent != null)
                satelliteListContainer.transform.parent.gameObject.SetActive(hasSats);

            if (hasSats)
            {
                foreach (CelestialBody sat in body.childSatellites) CreateListButton(sat, satelliteListContainer.transform);
            }
        }
    }

    private void CreateListButton(CelestialBody target, Transform parent)
    {
        GameObject btnObj = Instantiate(listButtonPrefab, parent);
        activeListButtons.Add(btnObj);
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = target.name.ToUpper();

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                SelectionManager sm = ResolveSelectionManager();
                if (sm != null) sm.SelectBody(target);
            });
        }
    }

    public void SetSelectionManager(SelectionManager manager)
    {
        selectionManager = manager;
    }

    private void ClearLists()
    {
        foreach (GameObject btn in activeListButtons) if (btn != null) Destroy(btn);
        activeListButtons.Clear();
    }

    private IEnumerator TypeScientificData(CelestialBody body)
    {
        CelestialBodyInfo info = body.info;
        if (headerText == null || detailsText == null) yield break;

        headerText.text = string.Empty;
        detailsText.text = string.Empty;

        string fullHeader = $"{info.name.ToUpper()} SCIENTIFIC DATA";
        for (int i = 0; i <= fullHeader.Length; i++)
        {
            headerText.text = fullHeader.Substring(0, i);
            yield return new WaitForSeconds(typeSpeed);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=#00FFFF><b>[ ORBITAL MECHANICS ]</b></color>");
        sb.AppendLine($"Orbital Velocity: {FormatValue(info.orbital_velocity_km_s, "km/s")}");
        sb.AppendLine($"Orbital Period: {FormatValue(info.orbital_period_days, "days")}");
        sb.AppendLine($"Distance from Sun: {FormatValue(info.distance_from_sun_10e6_km, "10^6 km")}");
        sb.AppendLine($"Eccentricity: {FormatValue(info.eccentricity, string.Empty)}");
        sb.AppendLine($"Inclination: {FormatValue(info.inclination_deg, "deg")}");
        sb.AppendLine(string.Empty);
        sb.AppendLine("<color=#00FFFF><b>[ PHYSICAL PROPERTIES ]</b></color>");
        sb.AppendLine($"Mass: {info.mass_10e24_kg}e24 kg");
        sb.AppendLine($"Diameter: {info.diameter_km} km");
        sb.AppendLine($"Gravity: {FormatValue(info.gravity_m_s2, "m/s^2")}");
        sb.AppendLine($"Mean Temp: {info.mean_temp_c} C");
        sb.AppendLine($"Escape Velocity: {FormatValue(info.escape_velocity_km_s, "km/s")}");
        sb.AppendLine($"Mean Density: {FormatValue(info.density_kg_m3, "kg/m^3")}");
        sb.AppendLine($"Axial Tilt: {FormatValue(info.axial_tilt_deg, "deg")}");
        sb.AppendLine(string.Empty);
        sb.AppendLine("<color=#00FFFF><b>[ ENVIRONMENT ]</b></color>");
        sb.AppendLine($"Composition: {info.composition}");
        sb.AppendLine($"Atmos. Pressure: {info.atmospheric_pressure}");
        sb.AppendLine($"Magnetic Field: {(!string.IsNullOrEmpty(info.magnetic_field) ? info.magnetic_field : "N/A")}");

        if (!string.IsNullOrEmpty(info.discovered_by))
        {
            sb.AppendLine(string.Empty);
            sb.AppendLine("<color=#00FFFF><b>[ DISCOVERY ]</b></color>");
            sb.AppendLine($"Discovered By: {info.discovered_by}");
            sb.AppendLine($"Discovery Year: {info.discovery_year}");
        }

        string fullContent = sb.ToString();
        int step = 12;
        for (int i = 0; i <= fullContent.Length; i += step)
        {
            detailsText.text = fullContent.Substring(0, Mathf.Min(i, fullContent.Length));

            if (scrollRect != null && scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            yield return new WaitForSeconds(typeSpeed);
        }
        detailsText.text = fullContent;

        if (scrollRect != null && scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }

    private string FormatValue(float val, string unit) => (val == 0) ? "N/A" : $"{val.ToString("N2")} {unit}";

    private SelectionManager ResolveSelectionManager()
    {
        if (selectionManager == null)
        {
            selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        }

        return selectionManager;
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeInSpeed);

        canvasGroup.interactable = canvasGroup.blocksRaycasts = (canvasGroup.alpha > 0.7f);

        if (scanLine != null)
        {
            float y = Mathf.PingPong(Time.time * scanSpeed, scanRange) - (scanRange / 2f);
            scanLine.anchoredPosition = new Vector2(0, y);
        }
    }
}
