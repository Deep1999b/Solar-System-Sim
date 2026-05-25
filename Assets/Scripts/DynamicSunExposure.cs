using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DynamicSunExposure : MonoBehaviour
{
    [Header("Emission Settings")]
    public float maxEmission = 50f;
    public float minEmission = 1f;
    
    [Header("Distance Settings (KM)")]
    [Tooltip("Distance in KM where Sun is at minimum brightness (close up).")]
    public float minDistanceKm = 2000000f; // 2 Million km
    
    [Tooltip("Distance in KM where Sun reaches max brightness (deep space).")]
    public float maxDistanceKm = 150000000f; // 150 Million km (Earth Distance)

    private Material sunMaterial;
    private Camera mainCamera;

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) sunMaterial = rend.material;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (sunMaterial == null || mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Calculate distance in Units, then convert to real KM
        float distanceUnits = Vector3.Distance(transform.position, mainCamera.transform.position);
        float distanceKm = SolarSystemScale.UnitsToKm(distanceUnits);
        
        // Use the KM-based lerp for scale independence
        float t = Mathf.InverseLerp(minDistanceKm, maxDistanceKm, distanceKm);
        t = t * t; // Smooth curve
        
        float currentEmission = Mathf.Lerp(minEmission, maxEmission, t);
        
        if (sunMaterial.HasProperty("_EmissionStrength"))
        {
            sunMaterial.SetFloat("_EmissionStrength", currentEmission);
        }
    }
}
