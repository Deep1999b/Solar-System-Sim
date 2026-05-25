using System.Collections.Generic;
using UnityEngine;

public static class SolarSystemRegistry
{
    private static readonly Dictionary<string, CelestialBody> BodiesByName = new Dictionary<string, CelestialBody>();
    private static readonly HashSet<CelestialBody> Bodies = new HashSet<CelestialBody>();
    private static readonly Dictionary<string, GravityBody> GravityBodiesByName = new Dictionary<string, GravityBody>();
    private static readonly HashSet<GravityBody> GravityBodies = new HashSet<GravityBody>();

    // Public accessors for external systems
    public static HashSet<CelestialBody> RegisteredBodies => Bodies;
    public static HashSet<GravityBody> RegisteredGravityBodies => GravityBodies;

    public static void Register(CelestialBody body)
    {
        if (body == null)
        {
            return;
        }

        Bodies.Add(body);
        BodiesByName[body.gameObject.name] = body;
    }

    public static void Unregister(CelestialBody body)
    {
        if (body == null)
        {
            return;
        }

        Bodies.Remove(body);
        if (BodiesByName.TryGetValue(body.gameObject.name, out CelestialBody current) && current == body)
        {
            BodiesByName.Remove(body.gameObject.name);
        }
    }

    public static void Register(GravityBody body)
    {
        if (body == null)
        {
            return;
        }

        GravityBodies.Add(body);
        GravityBodiesByName[body.gameObject.name] = body;
    }

    public static void Unregister(GravityBody body)
    {
        if (body == null)
        {
            return;
        }

        GravityBodies.Remove(body);
        if (GravityBodiesByName.TryGetValue(body.gameObject.name, out GravityBody current) && current == body)
        {
            GravityBodiesByName.Remove(body.gameObject.name);
        }
    }

    public static CelestialBody[] GetBodiesSnapshot()
    {
        if (Bodies.Count == 0)
        {
            RebuildBodyCache();
        }

        List<CelestialBody> snapshot = new List<CelestialBody>(Bodies.Count);
        bool requiresRefresh = false;
        foreach (CelestialBody body in Bodies)
        {
            if (body == null)
            {
                requiresRefresh = true;
                continue;
            }

            snapshot.Add(body);
        }

        if (!requiresRefresh)
        {
            return snapshot.ToArray();
        }

        RebuildBodyCache();
        snapshot.Clear();
        foreach (CelestialBody body in Bodies)
        {
            if (body != null)
            {
                snapshot.Add(body);
            }
        }

        return snapshot.ToArray();
    }

    public static GravityBody[] GetGravityBodiesSnapshot()
    {
        if (GravityBodies.Count == 0)
        {
            RebuildGravityBodyCache();
        }

        List<GravityBody> snapshot = new List<GravityBody>(GravityBodies.Count);
        bool requiresRefresh = false;
        foreach (GravityBody body in GravityBodies)
        {
            if (body == null)
            {
                requiresRefresh = true;
                continue;
            }
            snapshot.Add(body);
        }

        if (!requiresRefresh)
        {
            return snapshot.ToArray();
        }

        RebuildGravityBodyCache();
        snapshot.Clear();
        foreach (GravityBody body in GravityBodies)
        {
            if (body != null)
            {
                snapshot.Add(body);
            }
        }

        return snapshot.ToArray();
    }

    public static bool TryGetBody(string bodyName, out CelestialBody body)
    {
        if (BodiesByName.TryGetValue(bodyName, out body) && body != null)
        {
            return true;
        }

        RebuildBodyCache();
        return BodiesByName.TryGetValue(bodyName, out body) && body != null;
    }

    public static bool TryGetGravityBody(string bodyName, out GravityBody body)
    {
        if (GravityBodiesByName.TryGetValue(bodyName, out body) && body != null)
        {
            return true;
        }

        RebuildGravityBodyCache();
        return GravityBodiesByName.TryGetValue(bodyName, out body) && body != null;
    }

    public static Transform FindBodyTransform(string bodyName)
    {
        return TryGetBody(bodyName, out CelestialBody body) ? body.transform : null;
    }

    private static void RebuildBodyCache()
    {
        Bodies.Clear();
        BodiesByName.Clear();

        CelestialBody[] bodies = Object.FindObjectsByType<CelestialBody>(FindObjectsInactive.Include);
        foreach (CelestialBody body in bodies)
        {
            Register(body);
        }
    }

    private static void RebuildGravityBodyCache()
    {
        GravityBodies.Clear();
        GravityBodiesByName.Clear();

        GravityBody[] bodies = Object.FindObjectsByType<GravityBody>(FindObjectsInactive.Include);
        foreach (GravityBody body in bodies)
        {
            Register(body);
        }
    }
}
