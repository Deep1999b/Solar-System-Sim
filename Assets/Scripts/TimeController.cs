using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The available time speeds. 0 is paused.")]
    public float[] timeSpeeds = { 0f, 1f, 5f, 10f, 25f, 50f };
    private int currentIndex = 1; // Default to 1x
    
    private float initialFixedDeltaTime;

    void Start()
    {
        initialFixedDeltaTime = Time.fixedDeltaTime;
        if (timeSpeeds == null || timeSpeeds.Length == 0) timeSpeeds = new float[] { 0f, 1f, 5f, 10f, 25f, 50f };
        currentIndex = Mathf.Clamp(currentIndex, 0, timeSpeeds.Length - 1);
        SetTimeScale(currentIndex);
    }

    void Update()
    {
        // Cycle speeds with [ and ]
        if (Input.GetKeyDown(KeyCode.RightBracket)) 
            SetTimeScale(Mathf.Min(currentIndex + 1, timeSpeeds.Length - 1));
        
        if (Input.GetKeyDown(KeyCode.LeftBracket)) 
            SetTimeScale(Mathf.Max(currentIndex - 1, 0));

        // Space to toggle Pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentIndex == 0) SetTimeScale(1); // Resume to 1x
            else SetTimeScale(0); // Pause
        }
    }

    void SetTimeScale(int index)
    {
        if (timeSpeeds == null || index < 0 || index >= timeSpeeds.Length) return;

        currentIndex = index;
        float newScale = timeSpeeds[currentIndex];
        
        Time.timeScale = newScale;
        
        // CRITICAL: Stop changing fixedDeltaTime. 
        // By keeping it at initialFixedDeltaTime (0.02), Unity will 
        // run more physics steps per second when we speed up timeScale,
        // which keeps the orbits perfectly stable at high speeds.
        Time.fixedDeltaTime = initialFixedDeltaTime;

        Debug.Log($"[Time Controller] Time Scale set to: {newScale}x. Stability maintained.");
    }
}
