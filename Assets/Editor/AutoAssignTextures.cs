using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AutoAssignTextures : EditorWindow
{
    [MenuItem("Tools/Assign Textures to Materials")]
    public static void ProcessAssetFolders()
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
                foreach (string suffix in new string[] { "AO", "BaseColor", "Bump", "Cavity", "Displacement", "Gloss", "Normal", "Roughness", "Diffuse", "Metalness", "Specular" })
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
        if (!texDict.ContainsKey("BaseColor") && !texDict.ContainsKey("Diffuse"))
        {
            Debug.LogError("Folder " + folderName + " is missing a BaseColor or Diffuse texture. Skipping.");
            return;
        }

        // Create a new material using HDRP Lit shader.
        Material mat = new Material(Shader.Find("HDRP/Lit"));
        mat.name = folderName;

        // Assign BaseColor (prefer BaseColor over Diffuse).
        if (texDict.ContainsKey("BaseColor"))
            mat.SetTexture("_BaseColorMap", texDict["BaseColor"]);
        else
            mat.SetTexture("_BaseColorMap", texDict["Diffuse"]);

        // Assign Normal map (if Normal exists; else try Bump).
        if (texDict.ContainsKey("Normal"))
            mat.SetTexture("_NormalMap", texDict["Normal"]);
        else if (texDict.ContainsKey("Bump"))
            mat.SetTexture("_NormalMap", texDict["Bump"]);

        // For displacement/height.
        if (texDict.ContainsKey("Displacement"))
            mat.SetTexture("_HeightMap", texDict["Displacement"]);

        // Generate a mask map if using a metallic workflow (if Metalness or Specular exists).
        // HDRP Lit expects the Mask Map channels:
        //   R = Metallic, G = Ambient Occlusion, B = Detail Mask, A = Smoothness.
        if (texDict.ContainsKey("Metalness") || texDict.ContainsKey("Specular"))
        {
            Texture2D metalTex = texDict.ContainsKey("Metalness") ? texDict["Metalness"] : null;
            Texture2D aoTex = texDict.ContainsKey("AO") ? texDict["AO"] : null;
            Texture2D detailTex = texDict.ContainsKey("Cavity") ? texDict["Cavity"] : null;
            // For smoothness: prefer Gloss; if not, use Roughness (inverted).
            Texture2D glossTex = texDict.ContainsKey("Gloss") ? texDict["Gloss"] : null;
            Texture2D roughTex = texDict.ContainsKey("Roughness") ? texDict["Roughness"] : null;

            Texture2D maskMap = GenerateMaskMap(metalTex, aoTex, detailTex, glossTex, roughTex);
            if (maskMap != null)
            {
                // Save the mask map as an asset alongside the textures.
                string maskPath = Path.Combine(dir, folderName + "_MaskMap.png").Replace("\\", "/");
                File.WriteAllBytes(maskPath, maskMap.EncodeToPNG());
                AssetDatabase.ImportAsset(maskPath);
                Texture2D importedMask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                mat.SetTexture("_MaskMap", importedMask);
            }
        }

        // Optionally assign a detail albedo map if a Diffuse texture is available.
        if (texDict.ContainsKey("Diffuse"))
            mat.SetTexture("_DetailAlbedoMap", texDict["Diffuse"]);

        // Create folder for generated materials.
        string matFolder = "Assets/GeneratedMaterials";
        if (!AssetDatabase.IsValidFolder(matFolder))
            AssetDatabase.CreateFolder("Assets", "GeneratedMaterials");

        string materialPath = Path.Combine(matFolder, mat.name + ".mat").Replace("\\", "/");
        AssetDatabase.CreateAsset(mat, materialPath);
        Debug.Log("Created material: " + mat.name);
    }

    /// <summary>
    /// Generates a mask map by combining available textures.
    /// Red: Metallic, Green: AO, Blue: Detail Mask, Alpha: Smoothness.
    /// For smoothness, if a Gloss texture is available, use it; otherwise, invert Roughness.
    /// </summary>
    static Texture2D GenerateMaskMap(Texture2D metal, Texture2D ao, Texture2D detail, Texture2D gloss, Texture2D rough)
    {
        // Determine target resolution (using the highest available among inputs).
        int width = 0, height = 0;
        Texture2D[] candidates = { metal, ao, detail, gloss, rough };
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
            Debug.LogError("Cannot generate mask map: no valid texture resolutions found.");
            return null;
        }

        Texture2D mask = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Loop through each pixel.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float metalVal = SampleTexture(metal, x, y, width, height, 0f);
                float aoVal = SampleTexture(ao, x, y, width, height, 1f);
                float detailVal = SampleTexture(detail, x, y, width, height, 1f);
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

                Color pixel = new Color(metalVal, aoVal, detailVal, smoothVal);
                mask.SetPixel(x, y, pixel);
            }
        }
        mask.Apply();
        return mask;
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
