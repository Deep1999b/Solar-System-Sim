using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class DynamicOrbitLine : MonoBehaviour
{
    [Header("Width Settings")]
    public float baseWidthMultiplier = 1.0f;
    [Tooltip("How much the trail thickens as you move away.")]
    public float distanceWidthFactor = 0.005f; 
    public float minWidth = 0.01f;
    public float maxWidth = 100f;

    private TrailRenderer tr;
    private Camera mainCam;
    private float originalStartWidth;

    void Start()
    {
        tr = GetComponent<TrailRenderer>();
        mainCam = Camera.main;
        
        // Capture the width set by the SolarSystemGenerator as the baseline
        originalStartWidth = tr.startWidth;
    }

    void LateUpdate()
    {
        if (mainCam == null || tr == null) return;

        float distance = Vector3.Distance(mainCam.transform.position, transform.position);
        
        // Calculate new width: Base + (Distance scaled by factor)
        float dynamicWidth = (originalStartWidth * baseWidthMultiplier) + (distance * distanceWidthFactor);
        float finalWidth = Mathf.Clamp(dynamicWidth, minWidth, maxWidth);

        tr.startWidth = finalWidth;
        // Keep the trail tapering to 0 for a "comet/path" look
        tr.endWidth = 0; 
    }
}
