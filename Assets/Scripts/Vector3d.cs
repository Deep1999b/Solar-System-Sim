using UnityEngine;

[System.Serializable]
public struct Vector3d
{
    public double x;
    public double y;
    public double z;

    public Vector3d(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3d(Vector3 v)
    {
        this.x = v.x;
        this.y = v.y;
        this.z = v.z;
    }

    public static Vector3d operator +(Vector3d a, Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3d operator *(Vector3d a, double b) => new Vector3d(a.x * b, a.y * b, a.z * b);
    public static Vector3d operator *(double a, Vector3d b) => new Vector3d(a * b.x, a * b.y, a * b.z);
    public static Vector3d operator /(Vector3d a, double b) => new Vector3d(a.x / b, a.y / b, a.z / b);

    public double sqrMagnitude => x * x + y * y + z * z;
    public double magnitude => System.Math.Sqrt(sqrMagnitude);
    public Vector3d normalized => this / magnitude;

    public static Vector3d zero => new Vector3d(0, 0, 0);

    public Vector3 ToVector3() => new Vector3((float)x, (float)y, (float)z);
}
