using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AutoAssignTextures : EditorWindow
{
    [MenuItem("Tools/Assign Textures to Materials (Enviroment)")]
    public static void ProcessAssetFolders()
    {
        string path = "Assets/saloon_interior_high/Envoriment Textures";
        if (!Directory.Exists(path))
        {
            Debug.LogError("Directory does not exist: " + path);
            return;
        }

        string[] directories = Directory.GetDirectories(path);
        foreach (string dir in directories)
        {
            ProcessDirectory(dir);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Assign Textures to Materials (probes)")]
    public static void ProcessAssetProbes()
    {
        string probesPath = "Assets/saloon_interior_high/Probes";
        if (!Directory.Exists(probesPath))
        {
            Debug.LogError("Directory does not exist: " + probesPath);
            return;
        }

        string[] directories = Directory.GetDirectories(probesPath);
        foreach (string dir in directories)
        {
            ProcessDirectory(dir);
        }
        AssetDatabase.Refresh();
    }

    static void ProcessDirectory(string dir)
    {
        string folderName = Path.GetFileName(dir);

        // Find textures based on their suffix names.
        // Expected suffixes: AO, BaseColor, Bump, Cavity, Displacement, Gloss, Normal, Roughness, Diffuse, Metalness, Specular
        string[] searchPatterns = new string[] { "*.png", "*.jpg", "*.tga", "*.exr" };
        Dictionary<string, Texture2D> texDict = new Dictionary<string, Texture2D>();

        foreach (string pattern in searchPatterns)
        {
            string[] files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                foreach (string suffix in new string[] { "AO", "BaseColor", "Bump", "Cavity", "Displacement", "Gloss", "Normal", "Roughness", "Diffuse", "Metalness", "Specular", "Albedo" })
                {
                    if (fileName.EndsWith(suffix))
                    {
                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace("\\", "/"));
                        if (tex != null)
                        {
                            texDict[suffix] = tex;
                        }
                        break;
                    }
                }
            }
        }

        // Check for essential textures.
        if (!texDict.ContainsKey("BaseColor") && !texDict.ContainsKey("Diffuse") && !texDict.ContainsKey("Albedo"))
        {
            Debug.LogError("Folder " + folderName + " is missing a BaseColor or Diffuse texture. Skipping.");
            return;
        }

        // Create a new material using URP Lit shader.
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = folderName;

        // Assign BaseColor (prefer BaseColor over Diffuse).
        if (texDict.ContainsKey("BaseColor"))
            mat.SetTexture("_BaseMap", texDict["BaseColor"]);
        else if (texDict.ContainsKey("Diffuse"))
            mat.SetTexture("_BaseMap", texDict["Diffuse"]);
        else
            mat.SetTexture("_BaseMap", texDict["Albedo"]);

        // Assign Normal map (if Normal exists; else try Bump).
        if (texDict.ContainsKey("Normal") || texDict.ContainsKey("Bump"))
        {
            Texture2D normalTex = texDict.ContainsKey("Normal") ? texDict["Normal"] : texDict["Bump"];
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        // For height/parallax.
        if (texDict.ContainsKey("Displacement"))
        {
            mat.SetTexture("_ParallaxMap", texDict["Displacement"]);
            mat.EnableKeyword("_PARALLAXMAP");
        }

        // AO map
        if (texDict.ContainsKey("AO"))
        {
            mat.SetTexture("_OcclusionMap", texDict["AO"]);
            mat.EnableKeyword("_OCCLUSIONMAP");
        }

        // Generate a metallic/smoothness map for URP
        // URP uses: R channel for metallic, A channel for smoothness
        if (texDict.ContainsKey("Metalness") || texDict.ContainsKey("Specular") || 
            texDict.ContainsKey("Gloss") || texDict.ContainsKey("Roughness"))
        {
            Texture2D metalTex = texDict.ContainsKey("Metalness") ? texDict["Metalness"] : null;
            // For smoothness: prefer Gloss; if not, use Roughness (inverted).
            Texture2D glossTex = texDict.ContainsKey("Gloss") ? texDict["Gloss"] : null;
            Texture2D roughTex = texDict.ContainsKey("Roughness") ? texDict["Roughness"] : null;

            Texture2D metallicMap = GenerateMetallicMap(metalTex, glossTex, roughTex);
            if (metallicMap != null)
            {
                // Save the metallic map as an asset alongside the textures.
                string metallicPath = Path.Combine(dir, folderName + "_MetallicSmoothness.png").Replace("\\", "/");
                File.WriteAllBytes(metallicPath, metallicMap.EncodeToPNG());
                AssetDatabase.ImportAsset(metallicPath);
                Texture2D importedMetallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                mat.SetTexture("_MetallicGlossMap", importedMetallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
        }

        // Create folder for generated materials.
        string matFolder = "Assets/GeneratedMaterials/Enviroment";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedMaterials"))
                AssetDatabase.CreateFolder("Assets", "GeneratedMaterials");
            AssetDatabase.CreateFolder("Assets/GeneratedMaterials", "Enviroment");
        }

        string materialPath = Path.Combine(matFolder, mat.name + ".mat").Replace("\\", "/");
        AssetDatabase.CreateAsset(mat, materialPath);
        Debug.Log("Created material: " + mat.name);
    }

    /// <summary>
    /// Generates a metallic/smoothness map for URP.
    /// Red channel: Metallic, Alpha channel: Smoothness.
    /// For smoothness, if a Gloss texture is available, use it; otherwise, invert Roughness.
    /// </summary>
    static Texture2D GenerateMetallicMap(Texture2D metal, Texture2D gloss, Texture2D rough)
    {
        // Determine target resolution (using the highest available among inputs).
        int width = 0, height = 0;
        Texture2D[] candidates = { metal, gloss, rough };
        foreach (Texture2D tex in candidates)
        {
            if (tex != null)
            {
                width = Mathf.Max(width, tex.width);
                height = Mathf.Max(height, tex.height);
            }
        }
        if (width == 0 || height == 0)
        {
            Debug.LogError("Cannot generate metallic map: no valid texture resolutions found.");
            return null;
        }

        Texture2D metallicMap = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Loop through each pixel.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float metalVal = SampleTexture(metal, x, y, width, height, 0f);
                float smoothVal = 1f;
                if (gloss != null)
                {
                    smoothVal = SampleTexture(gloss, x, y, width, height, 1f);
                }
                else if (rough != null)
                {
                    // Invert roughness to get smoothness.
                    smoothVal = 1f - SampleTexture(rough, x, y, width, height, 0f);
                }

                Color pixel = new Color(metalVal, 0f, 0f, smoothVal);
                metallicMap.SetPixel(x, y, pixel);
            }
        }
        metallicMap.Apply();
        return metallicMap;
    }

    /// <summary>
    /// Samples the texture at UV coordinates derived from (x,y) relative to target dimensions.
    /// If tex is null, returns the default value.
    /// </summary>
    static float SampleTexture(Texture2D tex, int x, int y, int targetWidth, int targetHeight, float defaultValue)
    {
        if (tex == null)
            return defaultValue;

        float u = (float)x / targetWidth;
        float v = (float)y / targetHeight;
        return tex.GetPixelBilinear(u, v).r;
    }
}
