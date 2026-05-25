using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Simulation Settings")]
    public float gravitationalConstant = 2.975f; 
    public int subSteps = 10;
    public float simulationTimeStep = 1.0f; 

    public float totalSimTime { get; private set; } = 0f;

    private List<GravityBody> bodies = new List<GravityBody>();

    private void Awake()
    {
        Instance = this;
        RefreshBodyList();
    }

    public void RefreshBodyList()
    {
        bodies.Clear();
        bodies.AddRange(Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None));
        
        foreach (var body in bodies)
        {
            body.physicsBody.position = new Vector3d(body.transform.position);
        }
        
        Debug.Log($"[Simulation Manager] Initialized with {bodies.Count} celestial bodies (Double Precision).");
    }

    public void RegisterBody(GravityBody body)
    {
        if (!bodies.Contains(body))
        {
            bodies.Add(body);
            body.physicsBody.position = new Vector3d(body.transform.position);
        }
    }

    public void UnregisterBody(GravityBody body)
    {
        bodies.Remove(body);
    }

    private void FixedUpdate()
    {
        if (bodies.Count == 0) return;

        double totalElapsed = (double)Time.fixedDeltaTime * (double)simulationTimeStep;
        totalSimTime += (float)totalElapsed;

        double dt = totalElapsed / (double)subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            RunVerletStep(dt);
        }

        UpdateTransforms((float)totalElapsed);
    }

    private void RunVerletStep(double dt)
    {
        // 1. Position update and Half-step velocity
        foreach (var body in bodies)
        {
            Vector3d oldAcceleration = body.physicsBody.acceleration;
            body.physicsBody.position += body.physicsBody.velocity * dt + 0.5 * oldAcceleration * dt * dt;
            body.physicsBody.velocity += 0.5 * oldAcceleration * dt;
        }

        // 2. Recalculate accelerations based on NEW positions
        CalculateAllAccelerations();

        // 3. Final half-step velocity using NEW acceleration
        foreach (var body in bodies)
        {
            body.physicsBody.velocity += 0.5 * body.physicsBody.acceleration * dt;
        }
    }

    private void CalculateAllAccelerations()
    {
        double G = (double)gravitationalConstant;

        foreach (var bodyA in bodies)
        {
            Vector3d totalAcc = Vector3d.zero;

            foreach (var bodyB in bodies)
            {
                if (bodyA == bodyB) continue;

                Vector3d direction = bodyB.physicsBody.position - bodyA.physicsBody.position;
                double distSq = direction.sqrMagnitude;

                if (distSq < 0.0000001) continue;

                double magnitude = (G * bodyB.physicsBody.mass) / distSq;
                totalAcc += direction.normalized * magnitude;
            }

            bodyA.physicsBody.acceleration = totalAcc;
        }
    }

    private void UpdateTransforms(float dt)
    {
        foreach (var body in bodies)
        {
            body.transform.position = body.physicsBody.position.ToVector3();

            if (body.rotationPeriodDays != 0)
            {
                float rotationSpeed = 360f / body.rotationPeriodDays;
                body.transform.Rotate(Vector3.up, rotationSpeed * dt, Space.Self);
            }
        }
    }
}
