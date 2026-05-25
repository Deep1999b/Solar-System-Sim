using NUnit.Framework;
using UnityEngine;

public class SolarSystemDataCoverageTests
{
    private static readonly string[] RequiredBodies =
    {
        "Sun",
        "Mercury",
        "Venus",
        "Earth",
        "Moon",
        "Mars",
        "Phobos",
        "Deimos",
        "Jupiter",
        "Io",
        "Europa",
        "Ganymede",
        "Callisto",
        "Saturn",
        "Titan",
        "Uranus",
        "Neptune",
        "Triton",
        "Pluto",
        "Charon"
    };

    [TestCaseSource(nameof(RequiredBodies))]
    public void RequiredBodyJsonExistsAndParses(string bodyName)
    {
        bool success = CelestialBodyDataUtility.TryLoadFromAssetDatabase(bodyName, out TextAsset dataJson, out CelestialBodyInfo info);

        Assert.That(success, Is.True, $"Expected valid body data for {bodyName}.");
        Assert.That(dataJson, Is.Not.Null);
        Assert.That(info, Is.Not.Null);
        Assert.That(info.name, Is.Not.Empty);
    }
}
