using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// Senior Graphics Engineer implementation of a physics-accurate Spacetime Fabric.
/// Optimized for world-space displacement and ultra-high resolution meshes.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpacetimeFabricController : MonoBehaviour {
    
    [StructLayout(LayoutKind.Sequential)]
    public struct GravitySourceData {
        public Vector3 position;
        public float mass;
        public float radius;
    }

    [Header("Mesh Architecture")]
    [Range(32, 2048)]
    public int resolution = 512;
    public float meshSize = 1000000f;
    public bool followCamera = true;

    [Header("Visual Physics Settings")]
    [Tooltip("Exaggerates the mass of planets for visualization.")]
    public float gravityVisualMultiplier = 150f;
    [Tooltip("Flattens or deepens the overall grid wells.")]
    public float verticalScale = 0.5f;
    [Tooltip("Multiplies the softening radius of all bodies.")]
    public float softeningMultiplier = 2.0f;

    [Header("Debug")]
    public bool debugMode = false;

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
        ReleaseBuffer();
    }

    private void ReleaseBuffer() {
        if (sourceBuffer != null) {
            sourceBuffer.Release();
            sourceBuffer = null;
        }
    }

    private void LateUpdate() {
        // 1. Follow Camera
        if (followCamera && Camera.main != null) {
            Vector3 camPos = Camera.main.transform.position;
            // Pin to Y=0 but move with camera
            transform.position = new Vector3(camPos.x, 0, camPos.z);
        }

        // 2. Upload Data to GPU
        UpdateGPUData();
    }

    private void UpdateGPUData() {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null) return;

        // Fetch registered bodies
        GravityBody[] bodies = SolarSystemRegistry.GetGravityBodiesSnapshot();
        if (bodies == null || bodies.Length == 0) bodies = Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        if (bodies == null || bodies.Length == 0) return;

        // Configure Buffer (support up to 128 sources for high complexity)
        int bufferSize = 128; 
        if (sourceBuffer == null || sourceBuffer.count != bufferSize) {
            ReleaseBuffer();
            sourceBuffer = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(GravitySourceData)));
            sourceArray = new GravitySourceData[bufferSize];
        }

        int count = 0;
        for (int i = 0; i < bodies.Length && count < bufferSize; i++) {
            if (bodies[i] == null) continue;

            // --- THE FIX: IGNORE THE SUN ---
            if (bodies[i].gameObject.name.Equals("Sun", System.StringComparison.OrdinalIgnoreCase)) continue;

            sourceArray[count].position = bodies[i].transform.position;
            sourceArray[count].mass = (float)bodies[i].physicsBody.mass;
            // Use the object's local scale as the physical softening radius
            sourceArray[count].radius = bodies[i].transform.localScale.x;

            if (debugMode) {
                Debug.DrawRay(sourceArray[count].position, Vector3.up * 5000f, Color.green);
            }
            count++;
        }

        sourceBuffer.SetData(sourceArray);
        
        // Push settings to GPU
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetBuffer(SourceBufferID, sourceBuffer);
        propBlock.SetInt(SourceCountID, count);
        propBlock.SetFloat("_GravityVisualMultiplier", gravityVisualMultiplier);
        propBlock.SetFloat("_VerticalScale", verticalScale);
        propBlock.SetFloat("_SofteningMultiplier", softeningMultiplier);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    private void OnDrawGizmos() {
        if (debugMode && sourceArray != null) {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            for (int i = 0; i < sourceArray.Length; i++) {
                if (sourceArray[i].mass > 0)
                    Gizmos.DrawWireSphere(sourceArray[i].position, sourceArray[i].radius * softeningMultiplier);
            }
        }
    }

    [ContextMenu("Force Regenerate Mesh")]
    public void GenerateMesh() {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        
        proceduralMesh = new Mesh();
        proceduralMesh.name = "SpacetimeFabric_HighRes";
        // CRITICAL: Use 32-bit indices for resolutions > 256
        proceduralMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        int vCount = resolution * resolution;
        Vector3[] vertices = new Vector3[vCount];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        float step = meshSize / resolution;
        float offset = meshSize * 0.5f;

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                vertices[y * resolution + x] = new Vector3(x * step - offset, 0, y * step - offset);
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
        // Large bounds to prevent culling at extreme zoom
        proceduralMesh.bounds = new Bounds(Vector3.zero, new Vector3(meshSize * 2, meshSize, meshSize * 2));
        
        GetComponent<MeshFilter>().mesh = proceduralMesh;
        Debug.Log($"<b>[Spacetime Fabric]</b> Mesh regenerated at {resolution}x{resolution} resolution ({vCount} vertices).");
    }
}
