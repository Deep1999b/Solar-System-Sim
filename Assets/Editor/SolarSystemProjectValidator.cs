using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class SolarSystemProjectValidator
{
    private static readonly string[] ExpectedDataFiles =
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

    [MenuItem("Solar System/Validate Project")]
    public static void ValidateProject()
    {
        List<string> issues = new List<string>();

        foreach (string bodyName in ExpectedDataFiles)
        {
            if (!CelestialBodyDataUtility.TryLoadFromAssetDatabase(bodyName, out TextAsset dataJson, out CelestialBodyInfo info))
            {
                issues.Add($"Missing or invalid data JSON for '{bodyName}'.");
                continue;
            }

            if (info == null || string.IsNullOrWhiteSpace(info.name))
            {
                issues.Add($"Parsed metadata for '{bodyName}' is empty.");
            }

            if (dataJson == null)
            {
                issues.Add($"Data asset lookup returned null for '{bodyName}'.");
            }
        }

        SelectionManager selectionManager = Object.FindAnyObjectByType<SelectionManager>();
        if (selectionManager != null)
        {
            if (selectionManager.detailsUI == null)
            {
                issues.Add("SelectionManager is missing a ScientificDetailsUI reference.");
            }

            if (selectionManager.minimap == null)
            {
                issues.Add("SelectionManager is missing a MinimapController reference.");
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            issues.Add("Main Camera was not found in the active scene.");
        }
        else if (mainCamera.GetComponent<CameraFollow>() == null)
        {
            issues.Add("Main Camera is missing CameraFollow.");
        }

        if (issues.Count == 0)
        {
            Debug.Log("<b>[Solar System]</b> Validation passed. Project wiring and core data assets look consistent.");
            return;
        }

        foreach (string issue in issues)
        {
            Debug.LogWarning($"[Solar System Validation] {issue}");
        }

        Assert.Fail($"Solar System validation found {issues.Count} issue(s). See Console output for details.");
    }
}
