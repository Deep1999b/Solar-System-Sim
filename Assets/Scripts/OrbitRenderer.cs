using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OrbitRenderer : MonoBehaviour
{
    public int segments = 120;
    public float width = 0.5f;
    public Color orbitColor = new Color(0, 1, 1, 0.3f);
    
    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.loop = true;
        
        // Sci-Fi look: Additive transparency
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = orbitColor;
        lr.endColor = orbitColor;
    }

    void Start()
    {
        DrawOrbit();
    }

    // Update in case we want dynamic orbits, but static circles are usually enough for visual reference
    public void DrawOrbit()
    {
        float radius = transform.position.magnitude; // Distance from Sun at (0,0,0)
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = ((float)i / segments) * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0, z));
        }
    }
}
