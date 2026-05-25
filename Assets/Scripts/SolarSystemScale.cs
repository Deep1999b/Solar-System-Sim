using UnityEngine;

public static class SolarSystemScale
{
    public const double KM_PER_UNIT = 100000.0;

    public static float KmToUnits(double km)
    {
        return (float)(km / KM_PER_UNIT);
    }

    public static float UnitsToKm(double units)
    {
        return (float)(units * KM_PER_UNIT);
    }
}
