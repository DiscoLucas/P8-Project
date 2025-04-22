using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RegionLightmapBaker : EditorWindow
{
    // The box in world‑space you want to bake
    Bounds bakeBounds = new Bounds(Vector3.zero, Vector3.one * 10f);

    // Stores each object’s old static flags so we can restore them later
    Dictionary<GameObject, StaticEditorFlags> previousFlags = new Dictionary<GameObject, StaticEditorFlags>();

    [MenuItem("Tools/Lighting/Bake Region")]
    static void ShowWindow()
    {
        GetWindow<RegionLightmapBaker>("Bake Region");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Region Lightmap Baker", EditorStyles.boldLabel);

        bakeBounds.center = EditorGUILayout.Vector3Field("Center", bakeBounds.center);
        bakeBounds.size = EditorGUILayout.Vector3Field("Size", bakeBounds.size);

        if (GUILayout.Button("Mark Region Objects"))
            MarkRegionObjects();

        if (GUILayout.Button("Bake Lighting"))
        {
            Lightmapping.Bake();  // kicks off the bake
            Debug.Log("🎉 Region bake complete");
        }

        if (GUILayout.Button("Restore All Objects"))
            RestoreAllObjects();
    }

    void MarkRegionObjects()
    {
        previousFlags.Clear();
        foreach (var r in FindObjectsOfType<Renderer>())
        {
            var go = r.gameObject;
            var oldFlags = GameObjectUtility.GetStaticEditorFlags(go);
            previousFlags[go] = oldFlags;

            var newFlags = oldFlags;
            if (bakeBounds.Contains(r.bounds.center))
                newFlags |= StaticEditorFlags.ContributeGI;   // include in bake
            else
                newFlags &= ~StaticEditorFlags.ContributeGI;  // exclude from bake

            GameObjectUtility.SetStaticEditorFlags(go, newFlags); // set the flag

            // make sure LODGroups in the region stay at full LOD
            var lod = go.GetComponentInParent<LODGroup>();
            if (lod != null)
                lod.ForceLOD(0);
        }
        Debug.Log("✅ Marked objects inside region for lightmapping");
    }

    void RestoreAllObjects()
    {
        // restore everyone’s original static flags
        foreach (var kv in previousFlags)
            GameObjectUtility.SetStaticEditorFlags(kv.Key, kv.Value);

        // reset any LODGroup overrides
        foreach (var lod in FindObjectsOfType<LODGroup>())
            lod.ForceLOD(-1);

        previousFlags.Clear();
        Debug.Log("🔄 Restored all objects to their original state");
    }
}
