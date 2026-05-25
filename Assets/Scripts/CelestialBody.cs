using UnityEngine;

[System.Serializable]
public class CelestialBodyInfo
{
    public string name;
    public string type;
    public float mass_10e24_kg;
    public float diameter_km;
    public float distance_from_sun_10e6_km;
    public float orbital_period_days;
    public float rotation_period_hours;
    public float gravity_m_s2;
    public float mean_temp_c;
    public int moons;
    
    [Header("Scientific Data")]
    public float orbital_velocity_km_s;
    public float escape_velocity_km_s;
    public float density_kg_m3;
    public float surface_area_10e6_km2;
    public float volume_10e9_km3;
    public float axial_tilt_deg;
    public float eccentricity;
    public float inclination_deg;
    public float albedo;
    public string magnetic_field;
    public string composition;
    public string atmospheric_pressure;
    public string discovered_by;
    public string discovery_year;
}

public class CelestialBody : MonoBehaviour
{
    public TextAsset dataJson;
    public CelestialBodyInfo info;
    
    [Header("Hierarchy")]
    public CelestialBody parentBody; // For moons, this would be their planet
    public CelestialBody[] childMoons; // For planets, their moons
    public CelestialBody[] childSatellites; // For planets, artificial satellites (L1/L2/etc)

    private void Awake()
    {
        LoadInfoFromJson();
    }

    private void OnEnable()
    {
        SolarSystemRegistry.Register(this);
    }

    private void OnDisable()
    {
        SolarSystemRegistry.Unregister(this);
    }

    public bool LoadInfoFromJson()
    {
        if (!CelestialBodyDataUtility.TryParseInfo(dataJson, out CelestialBodyInfo loadedInfo))
        {
            return false;
        }

        info = loadedInfo;
        return true;
    }
}
