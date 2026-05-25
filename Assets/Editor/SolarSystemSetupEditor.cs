using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SolarSystemSetupEditor : EditorWindow
{
    [MenuItem("Solar System/Complete Auto-Setup")]
    public static void FullSetup()
    {
        SetupDataComponents();
        SetupOrbits();
        SetupAtmospheres();
        SetupSaturnRings();
        CleanupAsteroidsAndComets();
        SetupTimeController();
        SetupSpaceLighting(); 
        ScientificUISetupEditor.SetupUI(); 
        SetupFlightHUD();
        MinimapGenerator.Generate(); 
        
        Debug.Log("<b>[Solar System]</b> Full setup completed with Unified Scientific UI!");
    }

    public static void SetupSpaceLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
    }

    public static void CleanupAsteroidsAndComets()
    {
        GameObject sys = GameObject.Find("SolarSystem");
        if (sys != null)
        {
            Transform belt = sys.transform.Find("AsteroidBelt");
            if (belt != null) Object.DestroyImmediate(belt.gameObject);
            Transform comets = sys.transform.Find("Comets");
            if (comets != null) Object.DestroyImmediate(comets.gameObject);
        }
    }

    public static void SetupFlightHUD()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        Transform existingHUD = canvas.transform.Find("FlightHUD");
        if (existingHUD != null) Object.DestroyImmediate(existingHUD.gameObject);

        GameObject hudObj = new GameObject("FlightHUD");
        hudObj.transform.SetParent(canvas.transform, false);
        RectTransform hudRT = hudObj.AddComponent<RectTransform>();
        hudRT.anchorMin = hudRT.anchorMax = hudRT.pivot = new Vector2(1, 0);
        hudRT.anchoredPosition = new Vector2(-30, 30);
        hudRT.sizeDelta = new Vector2(350, 180);
        hudObj.AddComponent<Image>().color = new Color(0, 0.05f, 0.15f, 0.85f);
        hudObj.AddComponent<Outline>().effectColor = new Color(0, 1, 1, 0.6f);

        FlightHUD hudScript = hudObj.AddComponent<FlightHUD>();
        GameObject container = new GameObject("Content");
        container.transform.SetParent(hudObj.transform, false);
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20); vlg.spacing = 10;

        hudScript.modeText = CreateTMP(container.transform, "Mode", 20, Color.cyan, FontStyles.Bold);
        hudScript.targetText = CreateTMP(container.transform, "Target", 16, Color.white);
        hudScript.etaText = CreateTMP(container.transform, "ETA", 14, Color.yellow);
        hudScript.speedText = CreateTMP(container.transform, "Speed", 16, Color.white);
    }

    public static void SetupOrbits()
    {
        GravityBody[] bodies = Object.FindObjectsByType<GravityBody>(FindObjectsInactive.Include);
        foreach (var body in bodies)
        {
            if (body.gameObject.name == "Sun") continue;
            if (body.GetComponent<DynamicOrbitLine>() == null && body.GetComponent<TrailRenderer>() != null)
                body.gameObject.AddComponent<DynamicOrbitLine>();
        }
    }

    public static void SetupAtmospheres()
    {
        Shader atmosShader = Shader.Find("Custom/PlanetAtmosphere");
        if (atmosShader == null) return;
        GravityBody[] bodies = Object.FindObjectsByType<GravityBody>(FindObjectsInactive.Include);
        foreach (var body in bodies)
        {
            if (body.gameObject.name == "Sun") continue;
            if (body.transform.Find("Atmosphere") == null)
            {
                GameObject atmosObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                atmosObj.name = "Atmosphere";
                atmosObj.transform.SetParent(body.transform, false);
                atmosObj.transform.localScale = Vector3.one * 1.05f;
                Object.DestroyImmediate(atmosObj.GetComponent<SphereCollider>());
                Material mat = new Material(atmosShader);
                atmosObj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }
    }

    public static void SetupSaturnRings()
    {
        GameObject saturn = GameObject.Find("Saturn");
        if (saturn != null && saturn.transform.Find("Rings") == null)
        {
            GameObject rings = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rings.name = "Rings"; rings.transform.SetParent(saturn.transform, false);
            rings.transform.localRotation = Quaternion.Euler(90, 0, 0); rings.transform.localScale = Vector3.one * 5f;
            rings.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Custom/SaturnRings"));
        }
    }

    public static void SetupDataComponents()
    {
        GravityBody[] bodies = Object.FindObjectsByType<GravityBody>(FindObjectsInactive.Include);
        foreach (var body in bodies)
        {
            CelestialBody cb = body.GetComponent<CelestialBody>();
            if (cb == null) cb = body.gameObject.AddComponent<CelestialBody>();
            if (CelestialBodyDataUtility.TryLoadFromAssetDatabase(body.name, out TextAsset dataJson, out CelestialBodyInfo info))
            {
                cb.dataJson = dataJson;
                cb.info = info;
            }
        }
    }

    public static void SetupTimeController()
    {
        SimulationManager simulationManager = Object.FindAnyObjectByType<SimulationManager>();
        GameObject manager = simulationManager != null ? simulationManager.gameObject : GameObject.Find("SimulationManager");
        if (manager == null) manager = new GameObject("SimulationManager");
        if (manager.GetComponent<TimeController>() == null) manager.AddComponent<TimeController>();
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, int size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = color; t.fontStyle = style;
        return t;
    }
}
