using UnityEngine;
using System.Collections.Generic;

public class AsteroidBeltGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int asteroidCount = 15000; 
    public float innerRadius = 314f; 
    public float outerRadius = 493f; 
    public float minSize = 0.1f; // Increased size for visibility
    public float maxSize = 0.5f;
    public float verticalSpread = 2.0f;

    [Header("Rendering")]
    public Mesh asteroidMesh;
    public Material asteroidMaterial;
    
    private sealed class AsteroidBatch
    {
        public readonly Matrix4x4[] matrices;
        public readonly MaterialPropertyBlock propertyBlock;
        public readonly int count;

        public AsteroidBatch(Matrix4x4[] matrices, Vector4[] orbitParams, Vector4[] rotationParams, int count)
        {
            this.matrices = matrices;
            this.count = count;
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVectorArray("_OrbitParams", orbitParams);
            propertyBlock.SetVectorArray("_RotationParams", rotationParams);
        }
    }

    private AsteroidBatch[] batches = System.Array.Empty<AsteroidBatch>();
    private int batchCount;
    private const int MAX_BATCH_SIZE = 1023;

    void Start()
    {
        InitializeBelt();
    }

    public void InitializeBelt()
    {
        if (asteroidMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroidMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }

        // CRITICAL: Expand mesh bounds so Unity doesn't cull the asteroids.
        // Since we move them in the shader, Unity thinks they are all at (0,0,0).
        asteroidMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);

        if (asteroidMaterial == null)
        {
            asteroidMaterial = new Material(Shader.Find("Custom/AsteroidOrbit"));
        }
        asteroidMaterial.enableInstancing = true;

        Matrix4x4[] allMatrices = new Matrix4x4[asteroidCount];
        Vector4[] allOrbitParams = new Vector4[asteroidCount];
        Vector4[] allRotationParams = new Vector4[asteroidCount];

        float G = 2.975f;
        float sunMass = 333000f;

        for (int i = 0; i < asteroidCount; i++)
        {
            float radius = Random.Range(innerRadius, outerRadius);
            float phase = Random.Range(0, 2f * Mathf.PI);
            float orbitSpeed = Mathf.Sqrt(G * sunMass / radius) / radius; 
            float scale = Random.Range(minSize, maxSize);
            
            Vector3 rotAxis = Random.onUnitSphere;
            float rotSpeed = Random.Range(1.0f, 5.0f);
            
            allMatrices[i] = Matrix4x4.identity;
            allOrbitParams[i] = new Vector4(radius, phase, orbitSpeed, scale);
            allRotationParams[i] = new Vector4(rotAxis.x * rotSpeed, rotAxis.y * rotSpeed, rotAxis.z * rotAxis.z, 0);
        }

        batchCount = Mathf.CeilToInt((float)asteroidCount / MAX_BATCH_SIZE);
        batches = new AsteroidBatch[batchCount];
        for (int i = 0; i < batchCount; i++)
        {
            int offset = i * MAX_BATCH_SIZE;
            int count = Mathf.Min(MAX_BATCH_SIZE, asteroidCount - offset);

            Matrix4x4[] subMatrices = new Matrix4x4[count];
            Vector4[] subOrbit = new Vector4[count];
            Vector4[] subRotation = new Vector4[count];

            System.Array.Copy(allMatrices, offset, subMatrices, 0, count);
            System.Array.Copy(allOrbitParams, offset, subOrbit, 0, count);
            System.Array.Copy(allRotationParams, offset, subRotation, 0, count);

            batches[i] = new AsteroidBatch(subMatrices, subOrbit, subRotation, count);
        }

        Debug.Log($"[Asteroid Belt] Initialized {asteroidCount} asteroids across {batchCount} batches.");
    }

    void Update()
    {
        if (asteroidMesh == null || asteroidMaterial == null) return;

        for (int i = 0; i < batches.Length; i++)
        {
            AsteroidBatch batch = batches[i];
            Graphics.DrawMeshInstanced(asteroidMesh, 0, asteroidMaterial, batch.matrices, batch.count, batch.propertyBlock);
        }
    }
}
