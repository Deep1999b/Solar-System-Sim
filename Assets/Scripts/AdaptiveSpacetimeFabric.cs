using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// Senior Graphics Engineer Adaptive Spacetime Fabric.
/// Optimized for 1 Unit = 100,000 km scale.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdaptiveSpacetimeFabric : MonoBehaviour {
    
    [StructLayout(LayoutKind.Sequential)]
    public struct GravitySourceData {
        public Vector3 position;
        public float mass;
        public float radius;
        public float softness;
        public float verticalScale;
    }

    [Header("Astronomical Architecture")]
    [Range(64, 512)]
    public int resolution = 256;
    [Tooltip("Total reach of the grid. 1.5M units covers most of the system.")]
    public float fabricRadius = 1500000f;
    [Range(1f, 5f)]
    [Tooltip("Higher values provide more detail under the camera. 2.0 is a good balance for seeing distant planets.")]
    public float lodPower = 2.0f;
    public bool followCamera = true;

    [Header("Physics Scale (1 Unit = 100,000 km)")]
    public float globalVerticalScale = 0.08f;
    public float globalSoftness = 1.0f;
    public bool includeSun = true;

    [Header("Debug")]
    public bool showGravitySources = false;

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
        GenerateMesh();
    }

    private void OnDisable() {
        if (sourceBuffer != null) {
            sourceBuffer.Release();
            sourceBuffer = null;
        }
    }

    private void LateUpdate() {
        if (followCamera && Camera.main != null && Application.isPlaying) {
            Vector3 camPos = Camera.main.transform.position;
            // Move mesh but snap to a large-ish grid to prevent vertex jitter
            float snap = 100f;
            float px = Mathf.Round(camPos.x / snap) * snap;
            float pz = Mathf.Round(camPos.z / snap) * snap;
            transform.position = new Vector3(px, 0, pz);
        }
        UpdateGPUData();
    }

    private void UpdateGPUData() {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null) return;

        GravityBody[] bodies = SolarSystemRegistry.GetGravityBodiesSnapshot();
        if (bodies == null || bodies.Length == 0) bodies = Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        if (bodies == null || bodies.Length == 0) return;

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

            SpacetimeInfluence custom = bodies[i].GetComponent<SpacetimeInfluence>();
            sourceArray[count].position = bodies[i].transform.position;
            
            if (custom != null) {
                sourceArray[count].mass = custom.mass;
                sourceArray[count].radius = custom.radius;
                sourceArray[count].softness = custom.softness;
                sourceArray[count].verticalScale = custom.verticalScale;
            } else {
                float bodyScale = bodies[i].transform.localScale.x;
                if (isSun) {
                    // Sun has a deep but localized well to prevent system-wide orange tint
                    sourceArray[count].mass = 12000f;
                    sourceArray[count].radius = 120f; // Slightly wider for smoothness
                    sourceArray[count].softness = 3.5f;
                    sourceArray[count].verticalScale = 1.0f;
                } else {
                    // Planets: Use Cube-Root scaling (inspired by the Hill Sphere)
                    // This makes the well width proportional to the 'volume' of gravitational influence.
                    float realMass = (float)bodies[i].physicsBody.mass;
                    sourceArray[count].mass = Mathf.Sqrt(Mathf.Max(realMass, 0.01f)) * 4500f;

                    // Radius is proportional to the cube root of mass (realistic gravity reach)
                    // with a multiplier (50) to keep it visible at 100,000km/unit scale.
                    float hillRadius = Mathf.Pow(Mathf.Max(realMass, 0.01f), 0.33f) * 50f;
                    sourceArray[count].radius = Mathf.Max(hillRadius, 20f); 

                    sourceArray[count].softness = 1.2f; 
                    sourceArray[count].verticalScale = 0.7f;
                }            }
            count++;
        }

        sourceBuffer.SetData(sourceArray);
        
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetBuffer(SourceBufferID, sourceBuffer);
        propBlock.SetInt(SourceCountID, count);
        propBlock.SetFloat("_GlobalVerticalScale", globalVerticalScale);
        propBlock.SetFloat("_GlobalSoftnessMultiplier", globalSoftness);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    [ContextMenu("Regenerate Mesh")]
    public void GenerateMesh() {
        if (proceduralMesh != null) {
            if (Application.isPlaying) Destroy(proceduralMesh);
            else DestroyImmediate(proceduralMesh);
        }

        proceduralMesh = new Mesh();
        proceduralMesh.name = "SpacetimeFabric_Astronomical";
        proceduralMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        int vCount = resolution * resolution;
        Vector3[] vertices = new Vector3[vCount];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                float nx = (x / (float)(resolution - 1)) * 2f - 1f;
                float ny = (y / (float)(resolution - 1)) * 2f - 1f;
                float px = Mathf.Sign(nx) * Mathf.Pow(Mathf.Abs(nx), lodPower);
                float py = Mathf.Sign(ny) * Mathf.Pow(Mathf.Abs(ny), lodPower);
                vertices[y * resolution + x] = new Vector3(px * fabricRadius, 0, py * fabricRadius);
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
        proceduralMesh.bounds = new Bounds(Vector3.zero, new Vector3(fabricRadius * 4, fabricRadius, fabricRadius * 4));
        GetComponent<MeshFilter>().mesh = proceduralMesh;
        Debug.Log($"<b>[Spacetime Fabric]</b> Mesh regenerated for 100,000km/Unit scale.");
    }
}

/// <summary>
/// Attach to any body to override trampoline deformation parameters.
/// </summary>
public class SpacetimeInfluence : MonoBehaviour {
    public float mass = 100f;
    public float radius = 500f;
    public float softness = 0.6f;
    public float verticalScale = 0.6f;
}
