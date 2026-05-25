using UnityEngine;

// Removed Rigidbody component entirely to ensure pure mathematical simulation
public class GravityBody : MonoBehaviour
{
    [Header("Physics Data")]
    public CelestialBodyPhysics physicsBody = new CelestialBodyPhysics();
    
    [Header("Rotation")]
    public float axialTilt = 0f;
    public float rotationPeriodDays = 1f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(0, 0, axialTilt);
        physicsBody.position = new Vector3d(transform.position);
    }

    private void OnEnable()
    {
        SolarSystemRegistry.Register(this);
        if (SimulationManager.Instance != null) SimulationManager.Instance.RegisterBody(this);
    }

    private void OnDisable()
    {
        SolarSystemRegistry.Unregister(this);
        if (SimulationManager.Instance != null) SimulationManager.Instance.UnregisterBody(this);
    }
}
