using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinimapGenerator : EditorWindow
{
    [MenuItem("Solar System/Generate Enhanced Minimap")]
    public static void Generate()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Minimap Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }
        
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj == null)
        {
            esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
        SetupInputModule(esObj);

        Transform oldMinimap = canvas.transform.Find("MinimapPanel");
        if (oldMinimap != null) Undo.DestroyObjectImmediate(oldMinimap.gameObject);

        // 1. Root Panel
        GameObject panelObj = new GameObject("MinimapPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.zero;
        panelRt.pivot = Vector2.zero;
        panelRt.anchoredPosition = new Vector2(30, 30);
        panelRt.sizeDelta = new Vector2(350, 350);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.01f, 0.03f, 0.08f, 0.98f);
        panelImg.raycastTarget = true; 
        panelObj.AddComponent<Outline>().effectColor = new Color(0, 1, 1, 0.4f);

        MinimapController controller = panelObj.AddComponent<MinimapController>();

        // 2. Map Window (Masked area)
        GameObject maskObj = new GameObject("MapMask");
        maskObj.transform.SetParent(panelObj.transform, false);
        RectTransform maskRt = maskObj.AddComponent<RectTransform>();
        maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
        maskRt.offsetMin = new Vector2(5, 75); // Higher footer room
        maskRt.offsetMax = new Vector2(-5, -35); 
        maskObj.AddComponent<Image>().raycastTarget = false;
        maskObj.AddComponent<Mask>().showMaskGraphic = false;

        // 3. Container & Grid
        GameObject gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(maskObj.transform, false);
        RectTransform gRt = gridObj.AddComponent<RectTransform>();
        gRt.anchorMin = Vector2.zero; gRt.anchorMax = Vector2.one; gRt.sizeDelta = Vector2.zero;
        RawImage gridImg = gridObj.AddComponent<RawImage>();
        gridImg.color = new Color(0, 1, 1, 0.03f); gridImg.raycastTarget = false;
        controller.gridBackground = gridImg;

        GameObject containerObj = new GameObject("MapContainer");
        containerObj.transform.SetParent(maskObj.transform, false);
        controller.mapContainer = containerObj.AddComponent<RectTransform>();
        controller.mapContainer.anchorMin = controller.mapContainer.anchorMax = controller.mapContainer.pivot = new Vector2(0.5f, 0.5f);

        // 4. FOOTER AREA (Dedicated space for Toggles & Scale)
        GameObject footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(panelObj.transform, false);
        RectTransform footerRt = footerObj.AddComponent<RectTransform>();
        footerRt.anchorMin = Vector2.zero; footerRt.anchorMax = new Vector2(1, 0);
        footerRt.pivot = new Vector2(0.5f, 0);
        footerRt.anchoredPosition = new Vector2(0, 5);
        footerRt.sizeDelta = new Vector2(-10, 65);
        Image footerImg = footerObj.AddComponent<Image>();
        footerImg.color = new Color(0, 1, 1, 0.05f);

        // 5. Scale Bar (Inside Footer)
        GameObject scaleRoot = new GameObject("ScaleBar");
        scaleRoot.transform.SetParent(footerObj.transform, false);
        RectTransform scRt = scaleRoot.AddComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0, 0); scRt.anchorMax = new Vector2(0, 0);
        scRt.pivot = new Vector2(0, 0);
        scRt.anchoredPosition = new Vector2(10, 10);
        scRt.sizeDelta = new Vector2(100, 30);
        
        GameObject line = new GameObject("Line");
        line.transform.SetParent(scaleRoot.transform, false);
        RectTransform lRt = line.AddComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = new Vector2(1, 0);
        lRt.anchoredPosition = new Vector2(0, 2); lRt.sizeDelta = new Vector2(0, 2);
        line.AddComponent<Image>().color = Color.white;
        controller.scaleBarLine = lRt;

        // Try to find a default TMP font asset
        TMP_FontAsset defaultFont = null;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset LiberationSans");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(scaleRoot.transform, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) t.font = defaultFont;
        t.fontSize = 11; t.color = Color.white; t.raycastTarget = false;
        txt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
        controller.scaleBarText = t;

        // 6. Toggles (Inside Footer)
        GameObject toggles = new GameObject("TogglesRoot");
        toggles.transform.SetParent(footerObj.transform, false);
        RectTransform tRt = toggles.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(1, 0); tRt.anchorMax = new Vector2(1, 0);
        tRt.pivot = new Vector2(1, 0);
        tRt.anchoredPosition = new Vector2(-10, 10);
        tRt.sizeDelta = new Vector2(180, 40);
        
        HorizontalLayoutGroup hlg = toggles.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5; hlg.childControlWidth = true; hlg.childForceExpandWidth = false;

        CreateTogglePersistent(toggles.transform, "MOONS", controller, "ToggleMoons", defaultFont);
        CreateTogglePersistent(toggles.transform, "SATS", controller, "ToggleSatellites", defaultFont);

        // 7. Tooltip & Player Marker
        GameObject tooltipObj = new GameObject("Tooltip");
        tooltipObj.transform.SetParent(panelObj.transform, false);
        RectTransform ttRt = tooltipObj.AddComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0.5f, 1); ttRt.anchorMax = new Vector2(0.5f, 1);
        ttRt.pivot = new Vector2(0.5f, 1); ttRt.anchoredPosition = new Vector2(0, -8);
        ttRt.sizeDelta = new Vector2(250, 25);
        TextMeshProUGUI tt = tooltipObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tt.font = defaultFont;
        tt.fontSize = 16; tt.fontStyle = FontStyles.Bold;
        tt.color = Color.cyan; tt.alignment = TextAlignmentOptions.Center;
        tooltipObj.SetActive(false);
        controller.tooltipText = tt;

        GameObject pObj = new GameObject("PlayerMarker");
        pObj.transform.SetParent(containerObj.transform, false);
        RectTransform pRt = pObj.AddComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(24, 24);
        Image pImg = pObj.AddComponent<Image>();
        pImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        pImg.color = Color.green; pImg.raycastTarget = false;
        controller.playerMarker = pRt;

        controller.circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        controller.orbitRingSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        SelectionManager sm = Object.FindAnyObjectByType<SelectionManager>();
        if (sm != null)
        {
            sm.minimap = controller;
            controller.SetSelectionManager(sm);
            EditorUtility.SetDirty(sm);
        }

        Undo.RegisterCreatedObjectUndo(panelObj, "Create Fixed Minimap");
        Debug.Log("<b>[Minimap]</b> Footer layout fixed to prevent overlapping.");
    }

    private static void SetupInputModule(GameObject esObj)
    {
        System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            if (esObj.GetComponent(inputModuleType) == null)
            {
                var old = esObj.GetComponent<StandaloneInputModule>();
                if (old != null) Undo.DestroyObjectImmediate(old);
                esObj.AddComponent(inputModuleType);
            }
        }
        else
        {
            if (esObj.GetComponent<StandaloneInputModule>() == null) esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private static void CreateTogglePersistent(Transform parent, string label, MinimapController controller, string methodName, TMP_FontAsset font)
    {
        GameObject toggleObj = new GameObject("Toggle_" + label);
        toggleObj.transform.SetParent(parent, false);
        RectTransform rt = toggleObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(85, 30);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(toggleObj.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0, 0.5f);
        bgRt.sizeDelta = new Vector2(18, 18);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 1, 1, 0.2f);

        GameObject check = new GameObject("Check");
        check.transform.SetParent(bg.transform, false);
        Image cImg = check.AddComponent<Image>();
        cImg.color = Color.cyan;
        cImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        check.GetComponent<RectTransform>().sizeDelta = new Vector2(14, 14);
        
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(toggleObj.transform, false);
        TextMeshProUGUI l = lbl.AddComponent<TextMeshProUGUI>();
        if (font != null) l.font = font;
        l.text = label; l.fontSize = 11; l.color = Color.white;
        l.alignment = TextAlignmentOptions.Left; l.raycastTarget = false;
        lbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(22, 0);

        toggle.targetGraphic = bgImg;
        toggle.graphic = cImg;
        toggle.isOn = true;

        UnityEventTools.AddPersistentListener(toggle.onValueChanged, (UnityEngine.Events.UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<bool>), controller, methodName));
        EditorUtility.SetDirty(toggle);
    }
}
