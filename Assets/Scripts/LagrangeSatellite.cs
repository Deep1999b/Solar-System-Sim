using UnityEngine;

/// <summary>
/// Controls a satellite in a Halo orbit around a Sun-Earth Lagrange point (L1 or L2).
/// Uses high-precision math and the 1 unit = 100,000km scale.
/// </summary>
public class LagrangeSatellite : MonoBehaviour
{
    public enum LPoint { L1, L2 }
    
    [Header("Orbital Parents")]
    public GravityBody sun;
    public GravityBody earth;
    
    [Header("Lagrange Settings")]
    public LPoint targetPoint = LPoint.L1;
    public float amplitudeUnits = 1.0f; // 1 unit = 100,000km
    public float timeOffset = 0f;       // To spread satellites out
    
    private void Start()
    {
        // Auto-link if not assigned
        if (sun == null) {
            SolarSystemRegistry.TryGetGravityBody("Sun", out sun);
            if (sun == null)
            {
                GameObject sunObj = GameObject.Find("Sun");
                if (sunObj) sun = sunObj.GetComponent<GravityBody>();
            }
        }
        if (earth == null) {
            SolarSystemRegistry.TryGetGravityBody("Earth", out earth);
            if (earth == null)
            {
                GameObject earthObj = GameObject.Find("Earth");
                if (earthObj) earth = earthObj.GetComponent<GravityBody>();
            }
        }
    }

    void LateUpdate()
    {
        if (sun == null || earth == null) return;

        // 1. Get Sun-Earth Vector (Double Precision)
        Vector3d pSun = sun.physicsBody.position;
        Vector3d pEarth = earth.physicsBody.position;
        
        Vector3d sunToEarth = pEarth - pSun;
        double dist = sunToEarth.magnitude;
        Vector3d dir = sunToEarth.normalized;

        // 2. Compute the L-Point Center
        // Scale: 1,500,000 km = 15 Units
        double lDist = 15.0; 
        Vector3d lCenter;

        if (targetPoint == LPoint.L1)
            lCenter = pEarth - (dir * lDist); // Toward Sun
        else
            lCenter = pEarth + (dir * lDist); // Away from Sun

        // 3. Compute Halo Orbit Motion (Lissajous-style)
        // 1 second = 1 day. We use simulation time for consistency.
        float t = 0;
        if (SimulationManager.Instance != null) 
            t = SimulationManager.Instance.totalSimTime + timeOffset;
        else
            t = Time.time + timeOffset;

        // Convert day-based time to a reasonable orbital frequency (approx 1 cycle per 180 days)
        float freq = t * (Mathf.PI * 2f / 180f);

        // x = center.x + sin(time) * amplitude
        // y = center.y + cos(time) * amplitude * 0.5 (We map y to Z for orbital plane)
        // z = center.z + sin(time * 0.5) * amplitude * 0.3 (We map z to Y for vertical wobble)
        double offsetX = System.Math.Sin(freq) * amplitudeUnits;
        double offsetZ = System.Math.Cos(freq) * amplitudeUnits * 0.5;
        double offsetY = System.Math.Sin(freq * 0.5) * amplitudeUnits * 0.3;

        // Construct high-precision position
        // We orient the halo relative to the Sun-Earth axis
        Vector3d up = new Vector3d(0, 1, 0);
        Vector3d side = Cross(dir, up).normalized;
        Vector3d localUp = Cross(side, dir).normalized;

        Vector3d finalPos = lCenter + (side * offsetX) + (localUp * offsetY) + (dir * offsetZ);

        // 4. Update Transform
        transform.position = finalPos.ToVector3();

        // 5. Orientation (Always look away from Sun)
        Vector3 lookDir = (transform.position - pSun.ToVector3()).normalized;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    private Vector3d Cross(Vector3d a, Vector3d b)
    {
        return new Vector3d(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }
}
