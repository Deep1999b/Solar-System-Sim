using UnityEngine;

/// <summary>
/// Implements station-keeping for a spacecraft at the L2 Lagrange point.
/// Also ensures the spacecraft's sunshield orientation remains fixed relative to the Sun.
/// </summary>
public class LagrangePointKeep : MonoBehaviour
{
    [Header("Orbital Parents")]
    public GravityBody primaryBody;   // e.g., Sun
    public GravityBody secondaryBody; // e.g., Earth
    
    [Header("Station Keeping Settings")]
    public float correctionStrength = 0.8f;
    public float dampening = 0.2f;
    
    private GravityBody myBody;
    private double l2Factor; 

    void Start()
    {
        myBody = GetComponent<GravityBody>();
        
        if (primaryBody == null) {
            SolarSystemRegistry.TryGetGravityBody("Sun", out primaryBody);
            if (primaryBody == null)
            {
                GameObject sun = GameObject.Find("Sun");
                if (sun) primaryBody = sun.GetComponent<GravityBody>();
            }
        }
        if (secondaryBody == null) {
            SolarSystemRegistry.TryGetGravityBody("Earth", out secondaryBody);
            if (secondaryBody == null)
            {
                GameObject earth = GameObject.Find("Earth");
                if (earth) secondaryBody = earth.GetComponent<GravityBody>();
            }
        }

        if (primaryBody && secondaryBody)
        {
            double m1 = primaryBody.physicsBody.mass;
            double m2 = secondaryBody.physicsBody.mass;
            l2Factor = System.Math.Pow(m2 / (3.0 * m1), 1.0 / 3.0);
        }
    }

    void FixedUpdate()
    {
        if (primaryBody == null || secondaryBody == null || myBody == null) return;

        // Use Double Precision for everything
        Vector3d p1 = primaryBody.physicsBody.position;
        Vector3d p2 = secondaryBody.physicsBody.position;
        Vector3d v2 = secondaryBody.physicsBody.velocity;

        Vector3d sunToEarth = p2 - p1;
        double dist = sunToEarth.magnitude;
        Vector3d dir = sunToEarth.normalized;

        // 1. Calculate the Target L2 State
        Vector3d targetPos = p2 + dir * (dist * l2Factor);

        double omega = v2.magnitude / dist;
        double l2DistFromSun = dist * (1.0 + l2Factor);
        
        // Custom Cross Product for Vector3d
        Vector3d v2norm = v2.normalized;
        Vector3d orbitNormal = Cross(dir, v2norm).normalized;
        Vector3d l2Tangent = Cross(orbitNormal, dir).normalized;
        Vector3d targetVel = l2Tangent * (omega * l2DistFromSun);

        // 2. Apply Station-Keeping
        Vector3d posError = targetPos - myBody.physicsBody.position;
        Vector3d velError = targetVel - myBody.physicsBody.velocity;

        myBody.physicsBody.velocity += (posError * (double)correctionStrength + velError * (double)dampening) * (double)Time.fixedDeltaTime;

        // 3. Orientation
        Vector3d dirAwayFromSun = myBody.physicsBody.position - p1;
        if (dirAwayFromSun.sqrMagnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(dirAwayFromSun.ToVector3());
        }
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
