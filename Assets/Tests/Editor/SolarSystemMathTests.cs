using NUnit.Framework;
using UnityEngine;

public class SolarSystemMathTests
{
    [Test]
    public void KmToUnitsAndBack_RoundTrips()
    {
        double expectedKilometers = 149600000.0;
        float units = SolarSystemScale.KmToUnits(expectedKilometers);
        double actualKilometers = SolarSystemScale.UnitsToKm(units);

        Assert.That(actualKilometers, Is.EqualTo(expectedKilometers).Within(0.5d));
    }

    [Test]
    public void Vector3dNormalized_HasUnitLength()
    {
        Vector3d value = new Vector3d(3.0, 4.0, 0.0);

        Assert.That(value.normalized.magnitude, Is.EqualTo(1.0d).Within(1e-10));
    }

    [Test]
    public void TryParseInfo_ParsesKnownFields()
    {
        TextAsset json = new TextAsset("{\"name\":\"Earth\",\"type\":\"Planet\"}");

        bool success = CelestialBodyDataUtility.TryParseInfo(json, out CelestialBodyInfo info);

        Assert.That(success, Is.True);
        Assert.That(info.name, Is.EqualTo("Earth"));
        Assert.That(info.type, Is.EqualTo("Planet"));
    }
}
