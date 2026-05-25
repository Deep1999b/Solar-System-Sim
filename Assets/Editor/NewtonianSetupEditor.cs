using UnityEngine;
using UnityEditor;

public class NewtonianSetupEditor : EditorWindow
{
    [MenuItem("Solar System/Deploy Newtonian Spacetime (GPU Accelerated)")]
    public static void Setup()
    {
        // 1. Clean old fabrics for a fresh Newtonian install
        GameObject oldFabric = GameObject.Find("SpacetimeFabric");
        if (oldFabric != null) Undo.DestroyObjectImmediate(oldFabric);
        
        GameObject oldNewtonian = GameObject.Find("NewtonianSpacetimeFabric");
        if (oldNewtonian != null) Undo.DestroyObjectImmediate(oldNewtonian);

        // 2. Create the Newtonian Core
        GameObject fabricObj = new GameObject("NewtonianSpacetimeFabric");
        Undo.RegisterCreatedObjectUndo(fabricObj, "Deploy Newtonian Fabric");
        
        fabricObj.transform.position = Vector3.zero;
        
        // 3. Setup Components
        MeshFilter mf = fabricObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fabricObj.AddComponent<MeshRenderer>();
        NewtonianSpacetimeController controller = fabricObj.AddComponent<NewtonianSpacetimeController>();
        
        // High fidelity defaults
        controller.resolution = 512;
        controller.size = 2000000f;
        controller.lodPower = 2.5f;
        controller.syncWithSimulation = true;
        controller.visualGMultiplier = 150f;
        controller.curvatureScale = 80f;
        controller.globalSoftening = 2.5f;
        controller.maxDepth = 150000f;
        controller.includeSun = true;

        // 4. Link Physic Shader
        Shader shader = Shader.Find("Custom/NewtonianSpacetime");
        if (shader == null)
        {
            Debug.LogError("Newtonian shader not found! Ensure Assets/Shaders/NewtonianSpacetime.shader exists.");
            return;
        }

        Material mat = new Material(shader);
        mat.name = "NewtonianFabric_Mat";
        mr.sharedMaterial = mat;

        // 5. Initial Generation
        controller.GenerateMesh();

        // 6. Focus the Scene View
        Selection.activeGameObject = fabricObj;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("<b>[Newtonian Spacetime]</b> Deployment successful. The universe is now mathematically consistent.");
    }
}
