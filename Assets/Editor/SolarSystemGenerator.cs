using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SolarSystemGenerator : EditorWindow
{
    private struct MoonData {
        public string name; public float diameterKm; public float distanceKm; public float relativeMass; public Color color; public float axialTilt; public float rotDays;
        public MoonData(string n, float d, float dist, float m, Color c, float tilt = 0f, float rot = 1f) { 
            name = n; diameterKm = d; distanceKm = dist; relativeMass = m; color = c; axialTilt = tilt; rotDays = rot; 
        }
    }
    private struct PlanetData {
        public string name; public float diameterKm; public float distanceMkm; public float relativeMass; public Color color; public MoonData[] moons; public float axialTilt; public float rotDays;
        public PlanetData(string n, float d, float dist, float m, Color c, float tilt = 0f, float rot = 1f, MoonData[] ms = null) { 
            name = n; diameterKm = d; distanceMkm = dist; relativeMass = m; color = c; axialTilt = tilt; rotDays = rot; moons = ms ?? new MoonData[0]; 
        }
    }

    private static readonly Color PaleYellow = new Color(0.95f, 0.95f, 0.7f);
    private static readonly Color LightGray = new Color(0.8f, 0.8f, 0.8f);

    private static readonly PlanetData[] Planets = new PlanetData[] {
        new PlanetData("Mercury", 4879f, 57.9f, 0.055f, Color.gray, 0.03f, 58.6f),
        new PlanetData("Venus", 12104f, 108.2f, 0.815f, new Color(1f, 0.64f, 0f), 177.4f, -243f),
        new PlanetData("Earth", 12742f, 149.6f, 1.0f, Color.blue, 23.44f, 1f, new MoonData[] {
            new MoonData("Moon", 3474f, 384400f, 0.0123f, LightGray, 6.68f, 27.3f)
        }),
        new PlanetData("Mars", 6779f, 227.9f, 0.107f, Color.red, 25.19f, 1.03f, new MoonData[] {
            new MoonData("Phobos", 22.2f, 9377f, 0.0000000018f, LightGray, 0f, 0.32f),
            new MoonData("Deimos", 12.4f, 23460f, 0.00000000024f, Color.gray, 0f, 1.26f)
        }),
        new PlanetData("Jupiter", 139822f, 778.6f, 317.8f, new Color(0.6f, 0.4f, 0.2f), 3.13f, 0.41f, new MoonData[] {
            new MoonData("Io", 3643f, 421700f, 0.015f, Color.yellow, 0f, 1.77f),
            new MoonData("Europa", 3122f, 671100f, 0.008f, LightGray, 0.1f, 3.55f),
            new MoonData("Ganymede", 5262f, 1070400f, 0.025f, Color.gray, 0.2f, 7.15f),
            new MoonData("Callisto", 4821f, 1882700f, 0.018f, Color.gray, 0.4f, 16.69f)
        }),
        new PlanetData("Saturn", 116464f, 1433.5f, 95.2f, PaleYellow, 26.73f, 0.45f, new MoonData[] {
            new MoonData("Titan", 5150f, 1221870f, 0.022f, Color.yellow, 0f, 15.95f),
            new MoonData("Enceladus", 504f, 238020f, 0.000018f, Color.white, 0f, 1.37f),
            new MoonData("Rhea", 1527f, 527108f, 0.00039f, LightGray, 0f, 4.52f),
            new MoonData("Iapetus", 1469f, 3560820f, 0.0003f, new Color(0.2f,0.2f,0.2f), 0f, 79.33f),
            new MoonData("Dione", 1122f, 377396f, 0.00018f, LightGray, 0f, 2.74f),
            new MoonData("Tethys", 1062f, 294619f, 0.0001f, LightGray, 0f, 1.89f)
        }),
        new PlanetData("Uranus", 50724f, 2872.5f, 14.5f, Color.cyan, 97.77f, -0.72f, new MoonData[] {
            new MoonData("Titania", 1578f, 435910f, 0.00059f, LightGray, 0f, 8.71f),
            new MoonData("Oberon", 1523f, 583520f, 0.0005f, Color.gray, 0f, 13.46f),
            new MoonData("Umbriel", 1169f, 266000f, 0.0002f, Color.gray, 0f, 4.14f),
            new MoonData("Ariel", 1158f, 191020f, 0.00022f, Color.white, 0f, 2.52f),
            new MoonData("Miranda", 472f, 129390f, 0.000001f, LightGray, 0f, 1.41f)
        }),
        new PlanetData("Neptune", 49244f, 4495.1f, 17.1f, Color.blue, 28.32f, 0.67f, new MoonData[] {
            new MoonData("Triton", 2706f, 354759f, 0.0035f, LightGray, 0f, 5.88f),
            new MoonData("Nereid", 340f, 5513400f, 0.000005f, Color.gray, 0f, 360.13f)
        }),
        new PlanetData("Pluto", 2370f, 5906.4f, 0.0022f, LightGray, 122.53f, -6.39f, new MoonData[] {
            new MoonData("Charon", 1212f, 19571f, 0.00025f, Color.gray, 0f, 6.39f),
            new MoonData("Nix", 49.8f, 48671f, 0.00000001f, LightGray, 0f, 1.83f),
            new MoonData("Hydra", 50.9f, 64698f, 0.00000001f, LightGray, 0f, 38.2f)
        })
    };

    private const double SunMass = 333000.0; 
    private const double SunDiameterKm = 1392700.0;

    [MenuItem("Tools/Generate Solar System")]
    public static void Generate() {
        GameObject root = GameObject.Find("SolarSystem");
        if (root != null) Undo.DestroyObjectImmediate(root);
        
        root = new GameObject("SolarSystem");
        Undo.RegisterCreatedObjectUndo(root, "Create Solar System");
        
        var simMgr = root.AddComponent<SimulationManager>();
        simMgr.gravitationalConstant = 2.975f; 

        var beltGen = root.AddComponent<AsteroidBeltGenerator>();
        beltGen.asteroidCount = 20000;
        beltGen.innerRadius = SolarSystemScale.KmToUnits(314000000); 
        beltGen.outerRadius = SolarSystemScale.KmToUnits(493000000); 
        beltGen.minSize = 0.5f;
        beltGen.maxSize = 2.0f;
        
        root.AddComponent<CometSystem>();

        if (Camera.main != null) {
            CameraFollow cf = Camera.main.gameObject.GetComponent<CameraFollow>();
            if (cf == null) cf = Camera.main.gameObject.AddComponent<CameraFollow>();
            Camera.main.nearClipPlane = 1.0f; 
            Camera.main.farClipPlane = 10000000f; 
        }

        // 1. Create Sun
        float sunSize = SolarSystemScale.KmToUnits(SunDiameterKm); 
        GameObject sun = CreateBody("Sun", sunSize, Color.yellow, root.transform);
        sun.AddComponent<SphereCollider>();

        var sunBody = sun.AddComponent<GravityBody>(); 
        sunBody.physicsBody.mass = SunMass; 
        sunBody.physicsBody.position = Vector3d.zero;

        var sunCelestial = sun.AddComponent<CelestialBody>();
        ApplyBodyData(sunCelestial, "Sun");

        var exposure = sun.AddComponent<DynamicSunExposure>();
        exposure.maxEmission = 50f;  
        exposure.minEmission = 1f;    
        exposure.minDistanceKm = 2000000f; // 2M KM
        exposure.maxDistanceKm = 150000000f; // 150M KM (Earth Dist)

        // 2. Create Planets and Moons
        foreach (var p in Planets) {
            float pDist = SolarSystemScale.KmToUnits(p.distanceMkm * 1000000.0); 
            float pSize = SolarSystemScale.KmToUnits(p.diameterKm); 
            
            GameObject pObj = CreateBody(p.name, pSize, p.color, root.transform);
            pObj.transform.position = new Vector3(pDist, 0, 0);
            pObj.AddComponent<SphereCollider>();
            
            var pBody = pObj.AddComponent<GravityBody>(); 
            pBody.physicsBody.mass = p.relativeMass;
            pBody.physicsBody.position = new Vector3d(pObj.transform.position);
            pBody.axialTilt = p.axialTilt;
            pBody.rotationPeriodDays = p.rotDays;

            var pCelestial = pObj.AddComponent<CelestialBody>();
            ApplyBodyData(pCelestial, p.name);
            
            double circularVel = System.Math.Sqrt((double)simMgr.gravitationalConstant * SunMass / (double)pDist);
            pBody.physicsBody.velocity = new Vector3d(0, 0, circularVel);

            TrailRenderer pTr = pObj.AddComponent<TrailRenderer>();
            pTr.time = 500f;
            pTr.startWidth = pSize * 0.3f;
            pTr.endWidth = 0f;
            pTr.material = new Material(Shader.Find("Sprites/Default"));
            pTr.startColor = new Color(p.color.r, p.color.g, p.color.b, 0.5f);
            pTr.endColor = new Color(p.color.r, p.color.g, p.color.b, 0.0f);

            var moonsList = new List<CelestialBody>();
            foreach(var m in p.moons) {
                float mDist = SolarSystemScale.KmToUnits(m.distanceKm);
                float mSize = SolarSystemScale.KmToUnits(m.diameterKm);
                
                GameObject mObj = CreateBody(m.name, mSize, m.color, root.transform);
                mObj.transform.position = pObj.transform.position + new Vector3(mDist, 0, 0);
                mObj.AddComponent<SphereCollider>();
                
                var mBody = mObj.AddComponent<GravityBody>();
                mBody.physicsBody.mass = Mathf.Max(m.relativeMass, 0.0000001f); 
                mBody.physicsBody.position = new Vector3d(mObj.transform.position);
                mBody.axialTilt = m.axialTilt;
                mBody.rotationPeriodDays = m.rotDays;

                var mCelestial = mObj.AddComponent<CelestialBody>();
                ApplyBodyData(mCelestial, m.name);
                
                mCelestial.parentBody = pCelestial;
                moonsList.Add(mCelestial);
                
                double mCircularVel = System.Math.Sqrt((double)simMgr.gravitationalConstant * pBody.physicsBody.mass / (double)mDist);
                mBody.physicsBody.velocity = pBody.physicsBody.velocity + new Vector3d(0, 0, mCircularVel);

                TrailRenderer mTr = mObj.AddComponent<TrailRenderer>();
                mTr.time = 10f;
                mTr.startWidth = mSize * 0.3f;
                mTr.endWidth = 0f;
                mTr.material = new Material(Shader.Find("Sprites/Default"));
                mTr.startColor = new Color(m.color.r, m.color.g, m.color.b, 0.6f);
                mTr.endColor = new Color(m.color.r, m.color.g, m.color.b, 0.0f);
            }
            pCelestial.childMoons = moonsList.ToArray();

            if (p.name == "Earth") {
                // 1. Create Lagrange Missions
                string[] l1Missions = { "SOHO", "DSCOVR", "ACE", "WIND" };
                string[] l2Missions = { "JWST", "Gaia", "Euclid", "Spektr-RG" };

                var l1Sats = SpawnLagrangeMissions(l1Missions, LagrangeSatellite.LPoint.L1, sun, pObj, root.transform);
                var l2Sats = SpawnLagrangeMissions(l2Missions, LagrangeSatellite.LPoint.L2, sun, pObj, root.transform);

                // Merge and link to Earth
                var allSats = new List<CelestialBody>();
                allSats.AddRange(l1Sats);
                allSats.AddRange(l2Sats);
                pCelestial.childSatellites = allSats.ToArray();
            }
        }

        // --- NEW: Barycenter Momentum Balancing ---
        Vector3d totalMomentum = Vector3d.zero;
        GravityBody[] allBodies = root.GetComponentsInChildren<GravityBody>();
        int planetCount = 0;
        
        foreach (var gb in allBodies)
        {
            if (gb.gameObject.name == "Sun") continue; 
            totalMomentum += gb.physicsBody.velocity * gb.physicsBody.mass;
            planetCount++;
        }

        sunBody.physicsBody.velocity = (totalMomentum * -1.0) / SunMass;
        
        Debug.Log($"<b>[Barycenter]</b> Balancing momentum for {planetCount} bodies. Sun initial velocity: <b>{sunBody.physicsBody.velocity.magnitude:F8}</b> units/day.");
        // ------------------------------------------
        
        Selection.activeGameObject = root;
        Debug.Log("Solar System Generated with L1/L2 High-Precision Missions!");
    }

    private static CelestialBody[] SpawnLagrangeMissions(string[] names, LagrangeSatellite.LPoint point, GameObject sun, GameObject earth, Transform parent)
    {
        CelestialBody[] created = new CelestialBody[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject sat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sat.name = names[i];
            sat.transform.SetParent(parent);
            sat.transform.localScale = Vector3.one * 0.05f;
            
            // Visuals
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", (point == LagrangeSatellite.LPoint.L1) ? Color.yellow : new Color(1f, 0.84f, 0f));
            sat.GetComponent<Renderer>().sharedMaterial = mat;

            // Halo Logic
            var ls = sat.AddComponent<LagrangeSatellite>();
            ls.sun = sun.GetComponent<GravityBody>();
            ls.earth = earth.GetComponent<GravityBody>();
            ls.targetPoint = point;
            ls.amplitudeUnits = (point == LagrangeSatellite.LPoint.L1) ? 1.0f : 2.0f; // 100k km vs 200k km
            ls.timeOffset = i * 45f; // Spread them out in the halo orbit

            // Meta-data
            var cb = sat.AddComponent<CelestialBody>();
            cb.info = new CelestialBodyInfo {
                name = names[i], type = "Satellite", composition = "Scientific Instrumentation",
                discovered_by = "International Space Agencies"
            };
            cb.parentBody = earth.GetComponent<CelestialBody>();
            created[i] = cb;
        }
        return created;
    }

    private static void ApplyBodyData(CelestialBody celestialBody, string bodyName)
    {
        if (CelestialBodyDataUtility.TryLoadFromAssetDatabase(bodyName, out TextAsset dataJson, out CelestialBodyInfo info))
        {
            celestialBody.dataJson = dataJson;
            celestialBody.info = info;
        }
    }

    private static GameObject CreateBody(string name, float scale, Color color, Transform parent) {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name; obj.transform.SetParent(parent); obj.transform.localScale = Vector3.one * scale;
        DestroyImmediate(obj.GetComponent<Collider>());
        
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null) {
            Shader s = (name == "Sun") ? Shader.Find("Custom/AnimatedSun") : Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(s ?? Shader.Find("Standard"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.SetColor("_Color", color);
            rend.sharedMaterial = mat;
        }
        return obj;
    }
}
