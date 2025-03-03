using UnityEngine;
using System.Collections.Generic;

public class Breakable : MonoBehaviour
{
    [Header("Breaking Settings")]
    [SerializeField] private int fragmentCount = 10; // Number of fragments to create
    [SerializeField] private float explosionForce = 300f; // Force applied to pieces
    [SerializeField] private float explosionRadius = 1.5f; // Radius of explosion
    [SerializeField] private float upwardModifier = 0.4f; // Upward force bias
    [SerializeField] private AudioClip breakSound; // Optional sound effect
    [SerializeField] private Material fragmentMaterial; // Material to apply to fragments

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Breaks the object into multiple pieces with physics
    /// </summary>
    /// <param name="breakPoint">The point where the break originated</param>
    /// <param name="forceMultiplier">Optional force multiplier</param>
    public void Break(Vector3 breakPoint, float forceMultiplier = 1.0f)
    {
        // Play sound if available
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        // Create fragments
        List<GameObject> fragments = CreateFragments();
        
        // Apply explosion force to fragments
        foreach (GameObject fragment in fragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(
                    explosionForce * forceMultiplier,
                    breakPoint,
                    explosionRadius,
                    upwardModifier);
            }
        }

        // Disable the original intact object
        gameObject.SetActive(false);
    }

    private List<GameObject> CreateFragments()
    {
        List<GameObject> fragments = new List<GameObject>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("No mesh found on object to break!");
            return fragments;
        }

        Mesh originalMesh = meshFilter.mesh;
        Renderer originalRenderer = GetComponent<Renderer>();
        
        // If no material specified, use the original object's material
        if (fragmentMaterial == null && originalRenderer != null)
        {
            fragmentMaterial = originalRenderer.material;
        }
        
        // Create fragment parent
        GameObject fragmentParent = new GameObject($"{gameObject.name}_Fragments");
        fragmentParent.transform.position = transform.position;
        fragmentParent.transform.rotation = transform.rotation;
        
        // Create fragments
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = CreateRandomFragment(originalMesh, i);
            
            // Position and parent the fragment
            fragment.transform.SetParent(fragmentParent.transform);
            fragment.transform.position = transform.position;
            fragment.transform.rotation = transform.rotation;
            fragment.transform.localScale = transform.localScale;
            
            // Add physics components
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            fragment.AddComponent<MeshCollider>().convex = true;
            
            // Add slight rotation
            rb.AddTorque(Random.insideUnitSphere * explosionForce * 0.5f);
            
            fragments.Add(fragment);
        }
        
        return fragments;
    }

    private GameObject CreateRandomFragment(Mesh originalMesh, int fragmentIndex)
    {
        GameObject fragment = new GameObject($"Fragment_{fragmentIndex}");
        MeshFilter meshFilter = fragment.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = fragment.AddComponent<MeshRenderer>();
        
        // Create a new mesh for the fragment
        Mesh mesh = new Mesh();
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] uvs = originalMesh.uv;
        
        // Create lists to store fragment mesh data
        List<Vector3> fragmentVertices = new List<Vector3>();
        List<int> fragmentTriangles = new List<int>();
        List<Vector2> fragmentUVs = new List<Vector2>();
        
        // Pick random triangles from the original mesh
        int triangleCount = triangles.Length / 3;
        int fragmentTriangleCount = triangleCount / fragmentCount;
        int startTriangle = fragmentIndex * fragmentTriangleCount;
        int endTriangle = (fragmentIndex == fragmentCount - 1) ? 
            triangleCount : (fragmentIndex + 1) * fragmentTriangleCount;
        
        // Dictionary to remap vertex indices
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();
        
        // Add triangles to the fragment mesh
        for (int i = startTriangle; i < endTriangle; i++)
        {
            int baseIndex = i * 3;
            for (int j = 0; j < 3; j++)
            {
                int originalVertexIndex = triangles[baseIndex + j];
                
                // If we haven't added this vertex yet, add it
                if (!vertexMap.ContainsKey(originalVertexIndex))
                {
                    vertexMap[originalVertexIndex] = fragmentVertices.Count;
                    fragmentVertices.Add(vertices[originalVertexIndex]);
                    if (uvs.Length > originalVertexIndex)
                        fragmentUVs.Add(uvs[originalVertexIndex]);
                }
                
                // Add the remapped vertex index to triangles
                fragmentTriangles.Add(vertexMap[originalVertexIndex]);
            }
        }
        
        // Set mesh data
        mesh.vertices = fragmentVertices.ToArray();
        mesh.triangles = fragmentTriangles.ToArray();
        if (fragmentUVs.Count == fragmentVertices.Count)
            mesh.uv = fragmentUVs.ToArray();
        
        // Recalculate mesh normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Assign mesh and material
        meshFilter.mesh = mesh;
        meshRenderer.material = fragmentMaterial;
        
        return fragment;
    }
}
