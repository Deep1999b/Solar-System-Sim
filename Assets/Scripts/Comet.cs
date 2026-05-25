using UnityEngine;

public class Comet : MonoBehaviour
{
    [Header("Orbital Elements")]
    public float semiMajorAxis = 2000f; // millions of km
    public float eccentricity = 0.967f; // Halley's eccentricity
    public float orbitalPeriod = 76f; // Years
    public float inclination = 0f;

    private TrailRenderer tr;
    private float startTime;
    private float orbitProgress; // 0 to 2PI
    private Transform sunTransform;

    void Start()
    {
        sunTransform = SolarSystemRegistry.FindBodyTransform("Sun");
        if (sunTransform == null)
        {
            GameObject sun = GameObject.Find("Sun");
            if (sun != null) sunTransform = sun.transform;
        }

        tr = GetComponent<TrailRenderer>();
        // Random start position in the orbit (True Anomaly)
        orbitProgress = Random.Range(0f, 2f * Mathf.PI);
        
        // Initial positioning
        UpdatePosition(0f);
    }

    void Update()
    {
        // Calculate current angular speed based on orbital period
        // 1 Sim Sec = 1 Day
        float periodInDays = orbitalPeriod * 365.25f;
        float angularSpeed = (2f * Mathf.PI) / periodInDays;
        
        // Use the global simulation time step for perfect synchronization
        float dt = Time.deltaTime;
        if (SimulationManager.Instance != null) dt *= SimulationManager.Instance.simulationTimeStep;

        // Advance progress
        orbitProgress += angularSpeed * dt;
        
        UpdatePosition(orbitProgress);
    }

    private void UpdatePosition(float angle)
    {
        // 1. Keplerian ellipse: r = a(1-e^2) / (1 + e cos(theta))
        float r = (semiMajorAxis * (1 - eccentricity * eccentricity)) / (1 + eccentricity * Mathf.Cos(angle));
        
        // 2. Position in the orbital plane
        Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
        
        // 3. Apply inclination and rotation relative to Sun
        // If Sun moves (Barycenter), we must add its position
        if (sunTransform != null)
            transform.localPosition = sunTransform.localPosition + pos;
        else
            transform.localPosition = pos;

        // 4. Dynamic Tail Effect
        if (tr != null)
        {
            // Halley's perihelion is ~88M km (0.58 AU). 
            // The tail should be extremely long when close to the sun.
            float perihelion = semiMajorAxis * (1 - eccentricity);
            float currentDist = r;
            
            // Tail length scales with inverse square of distance
            float intensity = Mathf.Clamp01(perihelion / currentDist);
            tr.time = Mathf.Lerp(1f, 100f, intensity * intensity);
            tr.startWidth = Mathf.Lerp(0.05f, 0.5f, intensity);
        }
    }
}
