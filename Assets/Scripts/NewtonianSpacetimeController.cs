using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// Senior Graphics Engineer implementation of Newtonian Spacetime Fabric.
/// Supports resolutions up to 2048x2048 for cinematic quality.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NewtonianSpacetimeController : MonoBehaviour {
    
    [StructLayout(LayoutKind.Sequential)]
    public struct GravitySourceData {
        public Vector3 position;
        public float mass;
        public float radius;
    }

    [Header("Mesh Architecture")]
    [Range(64, 2048)]
    public int resolution = 512;
    public float size = 2000000f;
    [Range(1f, 10f)]
    [Tooltip("Higher values cluster more geometry directly under the camera. 2.5 is a good balance.")]
    public float lodPower = 2.5f;
    public bool followCamera = true;

    public enum MassScalingMode { Linear, Power, Logarithmic }

    [Header("Newtonian Physics (Synced)")]
    [Tooltip("If checked, uses G from SimulationManager.Instance")]
    public bool syncWithSimulation = true;
    [Tooltip("Exaggerates the physical G for visualization purposes.")]
    public float visualGMultiplier = 150f;
    
    public MassScalingMode scalingMode = MassScalingMode.Power;

    [Range(0.1f, 1.0f)]
    [Tooltip("Used in Power mode. Lower = more planet visibility.")]
    public float massVisualExponent = 0.4f;

    [Tooltip("Used in Logarithmic mode. Higher = deeper wells overall.")]
    public float logStrength = 2.0f;

    public float curvatureScale = 80f;
    public float globalSoftening = 2.5f;
    public float maxDepth = 150000f;
    public bool includeSun = true;

    private Mesh proceduralMesh;
    private ComputeBuffer sourceBuffer;
    private GravitySourceData[] sourceArray;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    
    private static readonly int SourceBufferID = Shader.PropertyToID("_GravitySources");
    private static readonly int SourceCountID = Shader.PropertyToID("_SourceCount");

    private void OnEnable() {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        if (proceduralMesh == null) GenerateMesh();
    }

    private void OnDisable() {
        if (sourceBuffer != null) {
            sourceBuffer.Release();
            sourceBuffer = null;
        }
    }

    private void LateUpdate() {
        if (followCamera && Camera.main != null) {
            Vector3 cp = Camera.main.transform.position;
            float snap = 10f; 
            transform.position = new Vector3(Mathf.Round(cp.x/snap)*snap, 0, Mathf.Round(cp.z/snap)*snap);
        }
        UpdateGPU();
    }

    private void UpdateGPU() {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null) return;

        var bodies = SolarSystemRegistry.GetGravityBodiesSnapshot();
        if (bodies == null || bodies.Length == 0) return;

        // --- PHYSICS SYNC ---
        float physG = 1.0f;
        if (syncWithSimulation && SimulationManager.Instance != null) {
            physG = SimulationManager.Instance.gravitationalConstant;
        }
        float finalG = physG * visualGMultiplier;

        int maxSources = 128; 
        if (sourceBuffer == null || sourceBuffer.count != maxSources) {
            if (sourceBuffer != null) sourceBuffer.Release();
            sourceBuffer = new ComputeBuffer(maxSources, Marshal.SizeOf(typeof(GravitySourceData)));
            sourceArray = new GravitySourceData[maxSources];
        }

        int count = 0;
        for (int i = 0; i < bodies.Length && count < maxSources; i++) {
            if (bodies[i] == null) continue;

            bool isSun = bodies[i].gameObject.name.Equals("Sun", System.StringComparison.OrdinalIgnoreCase);
            if (isSun && !includeSun) continue;
            
            sourceArray[count].position = bodies[i].transform.position;
            
            // --- MASS SCALING LOGIC ---
            float rawMass = (float)bodies[i].physicsBody.mass;
            float visualMass = rawMass;

            if (scalingMode == MassScalingMode.Power) {
                visualMass = Mathf.Pow(Mathf.Max(rawMass, 0.0001f), massVisualExponent);
            }
            else if (scalingMode == MassScalingMode.Logarithmic) {
                // Log(1 + M) ensures that Mass 0 = Potential 0, and avoids negative infinity
                visualMass = Mathf.Log(1f + rawMass) * logStrength;
            }

            sourceArray[count].mass = visualMass;
            sourceArray[count].radius = bodies[i].transform.localScale.x;
            count++;
        }

        sourceBuffer.SetData(sourceArray);
        
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetBuffer(SourceBufferID, sourceBuffer);
        propBlock.SetInt(SourceCountID, count);
        propBlock.SetFloat("_G", finalG);
        propBlock.SetFloat("_CurvatureScale", curvatureScale);
        propBlock.SetFloat("_Softening", globalSoftening);
        propBlock.SetFloat("_MaxDepth", maxDepth);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    [ContextMenu("Force Regenerate Mesh")]
    public void GenerateMesh() {
        if (proceduralMesh != null) {
            if (Application.isPlaying) Destroy(proceduralMesh);
            else DestroyImmediate(proceduralMesh);
        }
        
        proceduralMesh = new Mesh();
        proceduralMesh.name = "NewtonianSpacetime_Adaptive_" + resolution;
        proceduralMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        int vCount = resolution * resolution;
        Vector3[] vertices = new Vector3[vCount];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        float halfSize = size * 0.5f;

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                // Normalized coordinates -1 to 1
                float nx = (x / (float)(resolution - 1)) * 2f - 1f;
                float ny = (y / (float)(resolution - 1)) * 2f - 1f;

                // Adaptive distribution (clustered at 0,0)
                float px = Mathf.Sign(nx) * Mathf.Pow(Mathf.Abs(nx), lodPower);
                float py = Mathf.Sign(ny) * Mathf.Pow(Mathf.Abs(ny), lodPower);

                vertices[y * resolution + x] = new Vector3(px * halfSize, 0, py * halfSize);
            }
        }

        int tri = 0;
        for (int y = 0; y < resolution - 1; y++) {
            for (int x = 0; x < resolution - 1; x++) {
                int i = y * resolution + x;
                triangles[tri++] = i;
                triangles[tri++] = i + resolution;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + resolution;
                triangles[tri++] = i + resolution + 1;
            }
        }

        proceduralMesh.vertices = vertices;
        proceduralMesh.triangles = triangles;
        // Massive bounds to prevent culling since fabric follows camera
        proceduralMesh.bounds = new Bounds(Vector3.zero, new Vector3(size * 4, maxDepth * 4, size * 4));
        
        GetComponent<MeshFilter>().mesh = proceduralMesh;
        Debug.Log($"<b>[Newtonian Spacetime]</b> Adaptive Mesh generated. LOD Power: {lodPower}.");
    }
}
