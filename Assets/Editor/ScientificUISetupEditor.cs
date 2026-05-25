using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Editor script to generate the unified Scientific Data Panel with ScrollView and dynamic lists.
/// </summary>
public class ScientificUISetupEditor : EditorWindow
{
    [MenuItem("Solar System/Generate Scientific Data Panel")]
    public static void SetupUI()
    {
        // 1. Find or Create Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Try to find the default TMP font asset
        TMP_FontAsset defaultFont = null;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset LiberationSans");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        // 2. Clear existing sidebar
        Transform existing = canvas.transform.Find("ScientificSidebar");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // 3. Create Sidebar Panel
        GameObject sidebarObj = new GameObject("ScientificSidebar");
        sidebarObj.transform.SetParent(canvas.transform, false);
        
        RectTransform sidebarRt = sidebarObj.AddComponent<RectTransform>();
        sidebarRt.anchorMin = new Vector2(0, 0); 
        sidebarRt.anchorMax = new Vector2(0.3f, 1f); 
        sidebarRt.pivot = new Vector2(0, 1);
        sidebarRt.anchoredPosition = new Vector2(25, -25);
        sidebarRt.offsetMin = new Vector2(25, 25);
        sidebarRt.offsetMax = new Vector2(0, -25);

        Image sidebarImg = sidebarObj.AddComponent<Image>();
        sidebarImg.color = new Color(0.01f, 0.04f, 0.08f, 0.96f);
        sidebarObj.AddComponent<Outline>().effectColor = new Color(0, 1, 1, 0.4f);

        CanvasGroup cg = sidebarObj.AddComponent<CanvasGroup>();
        ScientificDetailsUI detailsUI = sidebarObj.AddComponent<ScientificDetailsUI>();
        detailsUI.canvasGroup = cg;

        // 4. Header (Fixed at top)
        GameObject headerObj = new GameObject("HeaderPanel");
        headerObj.transform.SetParent(sidebarObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1); headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, 0);
        headerRt.sizeDelta = new Vector2(0, 65);
        
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) headerText.font = defaultFont;
        headerText.fontSize = 22;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.cyan;
        headerText.text = "SCIENTIFIC DATA";
        headerText.alignment = TextAlignmentOptions.Center;
        detailsUI.headerText = headerText;

        // 5. Scan Line
        GameObject scanObj = new GameObject("ScanLine");
        scanObj.transform.SetParent(sidebarObj.transform, false);
        Image scanImg = scanObj.AddComponent<Image>();
        scanImg.color = new Color(0, 1, 1, 0.2f);
        RectTransform scanRT = scanObj.GetComponent<RectTransform>();
        scanRT.anchorMin = new Vector2(0, 0.5f); scanRT.anchorMax = new Vector2(1, 0.5f);
        scanRT.sizeDelta = new Vector2(-15, 2);
        detailsUI.scanLine = scanRT;

        // 6. SCROLL VIEW
        GameObject scrollObj = new GameObject("Scroll View");
        scrollObj.transform.SetParent(sidebarObj.transform, false);
        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(15, 15);
        scrollRt.offsetMax = new Vector2(-10, -75);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        detailsUI.scrollRect = scrollRect;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<Image>().color = new Color(0,0,0,0);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRt;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 1000);
        
        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 20, 10, 10);
        vlg.spacing = 20;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;

        // 7. Details Text
        GameObject detailsObj = new GameObject("DetailsText");
        detailsObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI detailsContent = detailsObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) detailsContent.font = defaultFont;
        detailsContent.fontSize = 16;
        detailsContent.color = Color.white;
        detailsContent.alignment = TextAlignmentOptions.TopLeft;
        detailsContent.textWrappingMode = TextWrappingModes.Normal;
        detailsUI.detailsText = detailsContent;

        // 8. Lists Container
        GameObject listsObj = new GameObject("ListsContainer");
        listsObj.transform.SetParent(contentObj.transform, false);
        VerticalLayoutGroup listVlg = listsObj.AddComponent<VerticalLayoutGroup>();
        listVlg.spacing = 20;
        listVlg.childControlHeight = true;
        listVlg.childForceExpandHeight = false;

        // Moon List
        GameObject moonRoot = new GameObject("MoonSection");
        moonRoot.transform.SetParent(listsObj.transform, false);
        moonRoot.AddComponent<VerticalLayoutGroup>().spacing = 8;
        
        TextMeshProUGUI ml = CreateTMP(moonRoot.transform, "Label", 14, Color.cyan, defaultFont);
        ml.text = "SATELLITES (NATURAL)";
        
        GameObject moonList = new GameObject("List");
        moonList.transform.SetParent(moonRoot.transform, false);
        moonList.AddComponent<VerticalLayoutGroup>().spacing = 4;
        detailsUI.moonListContainer = moonList;

        // Satellite List
        GameObject satRoot = new GameObject("SatSection");
        satRoot.transform.SetParent(listsObj.transform, false);
        satRoot.AddComponent<VerticalLayoutGroup>().spacing = 8;

        TextMeshProUGUI sl = CreateTMP(satRoot.transform, "Label", 14, Color.cyan, defaultFont);
        sl.text = "SATELLITES (ARTIFICIAL)";

        GameObject satList = new GameObject("List");
        satList.transform.SetParent(satRoot.transform, false);
        satList.AddComponent<VerticalLayoutGroup>().spacing = 4;
        detailsUI.satelliteListContainer = satList;

        // 9. Button Template
        GameObject btnTemplate = new GameObject("ListButtonTemplate");
        btnTemplate.transform.SetParent(canvas.transform, false);
        RectTransform btRt = btnTemplate.AddComponent<RectTransform>();
        btRt.sizeDelta = new Vector2(240, 30);
        btnTemplate.AddComponent<Image>().color = new Color(0, 1, 1, 0.12f);
        Button b = btnTemplate.AddComponent<Button>();
        var colors = b.colors;
        colors.highlightedColor = new Color(0, 1, 1, 0.35f);
        b.colors = colors;
        
        GameObject btTxtObj = new GameObject("Text");
        btTxtObj.transform.SetParent(btnTemplate.transform, false);
        TextMeshProUGUI btTxt = btTxtObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) btTxt.font = defaultFont;
        btTxt.text = "BODY NAME"; btTxt.fontSize = 13; btTxt.color = Color.white;
        btTxt.alignment = TextAlignmentOptions.Center;
        btTxt.rectTransform.anchorMin = Vector2.zero; btTxt.rectTransform.anchorMax = Vector2.one;
        btTxt.rectTransform.sizeDelta = Vector2.zero;
        
        btnTemplate.SetActive(false);
        detailsUI.listButtonPrefab = btnTemplate;

        // 10. Link to Selection Manager
        SelectionManager selManager = Object.FindAnyObjectByType<SelectionManager>();
        if (selManager != null)
        {
            selManager.detailsUI = detailsUI;
            detailsUI.SetSelectionManager(selManager);
            EditorUtility.SetDirty(selManager);
        }

        Undo.RegisterCreatedObjectUndo(sidebarObj, "Create Unified Scientific UI");
        Debug.Log("<b>[Scientific UI]</b> Re-generated with correct menu path and layout rebuilders.");
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, int size, Color color, TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size; t.color = color;
        return t;
    }
}
