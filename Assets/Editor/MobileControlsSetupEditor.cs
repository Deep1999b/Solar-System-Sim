using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MobileControlsSetupEditor : EditorWindow
{
    [MenuItem("Solar System/Generate Mobile Controls")]
    public static void SetupMobileUI()
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

        // 2. Clear existing mobile controls
        Transform existing = canvas.transform.Find("MobileControlsOverlay");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // 3. Create Overlay Root
        GameObject overlayObj = new GameObject("MobileControlsOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        RectTransform overlayRt = overlayObj.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        // 4. Create Joystick
        GameObject joystickBg = new GameObject("Joystick_BG");
        joystickBg.transform.SetParent(overlayObj.transform, false);
        RectTransform bgRt = joystickBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0);
        bgRt.anchorMax = new Vector2(0, 0);
        bgRt.pivot = new Vector2(0, 0);
        bgRt.anchoredPosition = new Vector2(100, 100);
        bgRt.sizeDelta = new Vector2(200, 200);
        
        Image bgImg = joystickBg.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.1f);
        // Try to find a circle sprite or just use default square for now
        
        GameObject joystickHandle = new GameObject("Handle");
        joystickHandle.transform.SetParent(joystickBg.transform, false);
        RectTransform handleRt = joystickHandle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(80, 80);
        Image handleImg = joystickHandle.AddComponent<Image>();
        handleImg.color = new Color(0, 1, 1, 0.5f);

        MobileUIControl joystickCtrl = joystickBg.AddComponent<MobileUIControl>();
        joystickCtrl.controlType = MobileUIControl.ControlType.Joystick;
        joystickCtrl.joystickKnob = handleRt;
        joystickCtrl.joystickRange = 100f;

        // 5. Create Altitude Buttons Container
        GameObject altContainer = new GameObject("AltitudeButtons");
        altContainer.transform.SetParent(overlayObj.transform, false);
        RectTransform altRt = altContainer.AddComponent<RectTransform>();
        altRt.anchorMin = new Vector2(1, 0);
        altRt.anchorMax = new Vector2(1, 0);
        altRt.pivot = new Vector2(1, 0);
        altRt.anchoredPosition = new Vector2(-100, 100);
        altRt.sizeDelta = new Vector2(100, 250);

        // Altitude Up
        GameObject upBtn = CreateMobileButton(altContainer.transform, "Button_Up", "UP", new Vector2(0, 75));
        upBtn.GetComponent<MobileUIControl>().controlType = MobileUIControl.ControlType.AltitudeUp;

        // Altitude Down
        GameObject downBtn = CreateMobileButton(altContainer.transform, "Button_Down", "DOWN", new Vector2(0, -75));
        downBtn.GetComponent<MobileUIControl>().controlType = MobileUIControl.ControlType.AltitudeDown;

        // 6. Create Boost Button
        GameObject boostBtn = CreateMobileButton(overlayObj.transform, "Button_Boost", "BOOST", new Vector2(-100, 400));
        RectTransform boostRt = boostBtn.GetComponent<RectTransform>();
        boostRt.anchorMin = new Vector2(1, 0);
        boostRt.anchorMax = new Vector2(1, 0);
        boostRt.pivot = new Vector2(1, 0);
        boostBtn.GetComponent<MobileUIControl>().controlType = MobileUIControl.ControlType.Boost;

        // 7. Add MobileInputController to Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<MobileInputController>() == null)
            {
                mainCam.gameObject.AddComponent<MobileInputController>();
            }
        }

        Undo.RegisterCreatedObjectUndo(overlayObj, "Generate Mobile Controls");
        Debug.Log("<b>[Mobile Controls]</b> Generated and added to UI Canvas.");
    }

    private static GameObject CreateMobileButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);
        rt.anchoredPosition = anchoredPos;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0, 1, 1, 0.2f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.sizeDelta = Vector2.zero;

        btnObj.AddComponent<MobileUIControl>();

        return btnObj;
    }
}
