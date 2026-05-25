using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class GravityWellController : MonoBehaviour
{
    [Header("References")]
    public MeshRenderer gridRenderer;
    
    [Header("General Settings")]
    public float globalScaling = 1000.0f;
    public float planetBoost = 150.0f; 
    public float sunSoftening = 1000.0f;
    public float planetSoftening = 35.0f;
    
    [Header("Performance")]
    [Range(1, 32)]
    public int maxSources = 32;

    private Material gridMaterial;
    private Vector4[] sourceData = new Vector4[32]; 
    private Vector4[] paramData = new Vector4[32];  
    
    private static readonly int SourcesID = Shader.PropertyToID("_GravitySources");
    private static readonly int ParamsID = Shader.PropertyToID("_GravityParams");
    private static readonly int CountID = Shader.PropertyToID("_SourceCount");
    private static readonly int GainID = Shader.PropertyToID("_VisualGain");

    private void OnEnable()
    {
        if (gridRenderer != null) gridMaterial = gridRenderer.sharedMaterial;
    }

    private void LateUpdate()
    {
        // 1. Follow Camera (Tethering)
        if (Camera.main != null && Application.isPlaying)
        {
            Vector3 camPos = Camera.main.transform.position;
            transform.position = new Vector3(camPos.x, 0, camPos.z);
        }

        // 2. Update Shader Data
        if (gridRenderer == null) return;
        if (gridMaterial == null) gridMaterial = gridRenderer.sharedMaterial;
        if (gridMaterial == null) return;

        UpdateShaderData();
    }

    private void UpdateShaderData()
    {
        // Fallback search to ensure we ALWAYS find bodies even if Registry is slow
        GravityBody[] bodies = SolarSystemRegistry.GetGravityBodiesSnapshot();
        if (bodies == null || bodies.Length == 0)
        {
            bodies = Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        }

        if (bodies == null || bodies.Length == 0) return;
        if (SimulationManager.Instance == null) return;

        double G = (double)SimulationManager.Instance.gravitationalConstant;
        int count = 0;
        bool sunFound = false;

        for (int i = 0; i < bodies.Length; i++)
        {
            if (count >= maxSources) break;
            if (bodies[i] == null) continue;

            Vector3 pos = bodies[i].transform.position;
            double mass = bodies[i].physicsBody.mass;
            
            float visualMass = (float)(G * mass);
            float softening = planetSoftening;
            float isSun = 0.0f;

            if (bodies[i].name == "Sun" || bodies[i].name == "sun")
            {
                softening = sunSoftening;
                isSun = 1.0f;
                sunFound = true;
            }
            else
            {
                visualMass *= planetBoost;
            }

            sourceData[count] = new Vector4(pos.x, pos.y, pos.z, visualMass);
            paramData[count] = new Vector4(softening, isSun, 0, 0);
            count++;
        }

        gridMaterial.SetVectorArray(SourcesID, sourceData);
        gridMaterial.SetVectorArray(ParamsID, paramData);
        gridMaterial.SetInt(CountID, count);
        gridMaterial.SetFloat(GainID, globalScaling);

        // LOGGING FOR USER
        if (Time.frameCount % 60 == 0) // Log once per second approx
        {
            Debug.Log($"<color=cyan>[Gravity Well]</color> Tracking {count} sources. Sun Found: {sunFound}");
        }
    }
}
