using UnityEngine;
using System.Collections.Generic;

public class CometSystem : MonoBehaviour
{
    private struct CometData {
        public string name;
        public float semiMajorAxis; // AU
        public float eccentricity;
        public float inclination; // Degrees
        public Color tailColor;
        public float period; // Years
        
        public CometData(string n, float a, float e, float i, Color c, float p) {
            name = n; semiMajorAxis = a; eccentricity = e; inclination = i; tailColor = c; period = p;
        }
    }

    private static readonly CometData[] FamousComets = new CometData[] {
        new CometData("Halley", 17.8f, 0.967f, 18.0f, new Color(0.7f, 0.8f, 1f, 0.6f), 76f),
        new CometData("Encke", 2.21f, 0.848f, 11.8f, new Color(0.9f, 0.9f, 1f, 0.5f), 3.3f),
        new CometData("Hale-Bopp", 186.0f, 0.995f, 89.4f, new Color(0.6f, 1f, 1f, 0.7f), 2533f)
    };

    public GameObject cometPrefab; // If null, use a sphere

    void Start()
    {
        GenerateComets();
    }

    public void GenerateComets()
    {
        const float AU_TO_KM = 149600000f;
        
        foreach (var data in FamousComets)
        {
            GameObject cometObj = cometPrefab != null ? Instantiate(cometPrefab, transform) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cometObj.name = "Comet_" + data.name;
            cometObj.transform.SetParent(transform);
            
            float a = SolarSystemScale.KmToUnits(data.semiMajorAxis * AU_TO_KM);
            float e = data.eccentricity;
            
            Comet cometScript = cometObj.GetComponent<Comet>();
            if (cometScript == null) cometScript = cometObj.AddComponent<Comet>();
            
            cometScript.semiMajorAxis = a;
            cometScript.eccentricity = e;
            cometScript.orbitalPeriod = data.period;

            // Apply inclination
            cometObj.transform.rotation = Quaternion.Euler(data.inclination, 0, 0);

            // Add Trail if missing
            TrailRenderer tr = cometObj.GetComponent<TrailRenderer>();
            if (tr == null) tr = cometObj.AddComponent<TrailRenderer>();
            
            tr.time = 50f;
            tr.startWidth = 0.2f;
            tr.endWidth = 0f;
            tr.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            tr.startColor = data.tailColor;
            tr.endColor = new Color(data.tailColor.r, data.tailColor.g, data.tailColor.b, 0f);
        }
    }
}
