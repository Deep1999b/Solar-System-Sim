using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CelestialBodyDataUtility
{
    public const string DataFolder = "Assets/Celestial Bodies Data";

    public static string GetJsonPath(string bodyName)
    {
        return $"{DataFolder}/{bodyName}.json";
    }

    public static bool TryParseInfo(TextAsset dataJson, out CelestialBodyInfo info)
    {
        info = null;
        if (dataJson == null || string.IsNullOrWhiteSpace(dataJson.text))
        {
            return false;
        }

        info = JsonUtility.FromJson<CelestialBodyInfo>(dataJson.text);
        return info != null;
    }

#if UNITY_EDITOR
    public static bool TryLoadFromAssetDatabase(string bodyName, out TextAsset dataJson, out CelestialBodyInfo info)
    {
        dataJson = AssetDatabase.LoadAssetAtPath<TextAsset>(GetJsonPath(bodyName));
        return TryParseInfo(dataJson, out info);
    }
#endif
}
