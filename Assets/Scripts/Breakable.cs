using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Breakable : MonoBehaviour
{
    [Header("Breaking Settings")]
    [SerializeField] private int radialCuts = 5; // Number of vertical cuts around the glass
    [SerializeField] private int heightCuts = 3; // Number of horizontal cuts along the glass height
    [SerializeField] private float randomOffset = 0.1f; // Random offset for cut positions
    [SerializeField] private float explosionForce = 300f; // Force applied to pieces
    [SerializeField] private float explosionRadius = 1.5f; // Radius of explosion
    [SerializeField] private float upwardModifier = 0.4f; // Upward force bias
    [SerializeField] private AudioClip breakSound; // Optional sound effect
    [SerializeField] private Material fragmentMaterial; // Material to apply to fragments
    [SerializeField] private bool useBoxCollidersAsFallback = true; // Use box colliders if mesh collider fails

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
        List<GameObject> fragments = CreateGlassShards();
        
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

    private List<GameObject> CreateGlassShards()
    {
        List<GameObject> shards = new List<GameObject>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("No mesh found on object to break!");
            return shards;
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

        // First, calculate bounds and center of the mesh
        Vector3[] vertices = originalMesh.vertices;
        Bounds bounds = originalMesh.bounds;
        Vector3 center = bounds.center;
        float height = bounds.size.y;
        float radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;

        // Create cutting planes - both vertical and horizontal
        List<Plane> cuttingPlanes = new List<Plane>();
        
        // Create vertical cutting planes (like cutting a pizza)
        for (int i = 0; i < radialCuts; i++)
        {
            float angle = (i * 360f / radialCuts) + Random.Range(-randomOffset, randomOffset) * 10f;
            angle *= Mathf.Deg2Rad; // Convert to radians
            
            Vector3 planeNormal = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            Plane verticalPlane = new Plane(planeNormal, center + Vector3.Scale(planeNormal, new Vector3(Random.Range(-randomOffset, randomOffset), 0, Random.Range(-randomOffset, randomOffset))));
            cuttingPlanes.Add(verticalPlane);
        }
        
        // Create horizontal cutting planes
        float heightStep = height / (heightCuts + 1);
        for (int i = 1; i <= heightCuts; i++)
        {
            float h = bounds.min.y + i * heightStep + Random.Range(-randomOffset, randomOffset);
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(center.x, h, center.z));
            cuttingPlanes.Add(horizontalPlane);
        }
        
        // Create submeshes by fracturing
        List<CellData> cells = new List<CellData>();
        
        // Start with the whole mesh
        CellData initialCell = new CellData
        {
            Vertices = vertices,
            Triangles = originalMesh.triangles,
            UVs = originalMesh.uv
        };
        cells.Add(initialCell);

        // Apply each cutting plane to all existing cells
        foreach (var plane in cuttingPlanes)
        {
            List<CellData> newCells = new List<CellData>();
            
            foreach (var cell in cells)
            {
                // Split the cell by the current plane
                if (SplitCell(cell, plane, out CellData positive, out CellData negative))
                {
                    if (positive.Triangles.Length > 0)
                        newCells.Add(positive);
                    
                    if (negative.Triangles.Length > 0)
                        newCells.Add(negative);
                }
                else
                {
                    // Cell wasn't split, keep it as-is
                    newCells.Add(cell);
                }
            }
            
            cells = newCells;
        }
        
        // Create game objects for each cell
        int index = 0;
        foreach (var cell in cells)
        {
            if (cell.Triangles.Length < 3)
                continue;
                
            // Create a fragment
            GameObject shard = new GameObject($"Shard_{index}");
            index++;
            
            MeshFilter mf = shard.AddComponent<MeshFilter>();
            MeshRenderer mr = shard.AddComponent<MeshRenderer>();
            
            // Create mesh
            Mesh shardMesh = new Mesh();
            shardMesh.vertices = cell.Vertices;
            shardMesh.triangles = cell.Triangles;
            if (cell.UVs != null && cell.UVs.Length == cell.Vertices.Length)
            {
                shardMesh.uv = cell.UVs;
            }
            
            // Recalculate mesh normals and bounds
            shardMesh.RecalculateNormals();
            shardMesh.RecalculateBounds();
            
            // Check if the mesh is valid before applying
            if (ValidateMesh(shardMesh))
            {
                // Apply mesh and material
                mf.mesh = shardMesh;
                mr.material = fragmentMaterial;
                
                // Add physics
                Rigidbody rb = shard.AddComponent<Rigidbody>();
                
                // Try to add mesh collider
                bool colliderSuccess = false;
                if (AddMeshCollider(shard, shardMesh))
                {
                    colliderSuccess = true;
                }
                // Use box collider as fallback if mesh collider fails
                else if (useBoxCollidersAsFallback)
                {
                    BoxCollider boxCollider = shard.AddComponent<BoxCollider>();
                    // Size is automatically calculated from the renderer bounds
                    colliderSuccess = true;
                }
                
                // If we couldn't add any collider, destroy this shard
                if (!colliderSuccess)
                {
                    GameObject.Destroy(shard);
                    continue;
                }
                
                // Apply slight random rotation
                shard.GetComponent<Rigidbody>().AddTorque(Random.insideUnitSphere * explosionForce * 0.5f);
                
                // Position the shard
                shard.transform.SetParent(fragmentParent.transform);
                shard.transform.position = transform.position;
                shard.transform.rotation = transform.rotation;
                shard.transform.localScale = transform.localScale;
                
                shards.Add(shard);
            }
            else
            {
                // Invalid mesh, destroy this shard
                GameObject.Destroy(shard);
            }
        }
        
        return shards;
    }
    
    // Structure to hold mesh data for a cell
    private struct CellData
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        public Vector2[] UVs;
    }
    
    // Helper method to split a cell by a plane
    private bool SplitCell(CellData cell, Plane plane, out CellData positive, out CellData negative)
    {
        positive = new CellData();
        negative = new CellData();
        
        List<Vector3> posVertices = new List<Vector3>();
        List<Vector3> negVertices = new List<Vector3>();
        List<int> posTriangles = new List<int>();
        List<int> negTriangles = new List<int>();
        List<Vector2> posUVs = new List<Vector2>();
        List<Vector2> negUVs = new List<Vector2>();
        
        // For each triangle, determine if it's entirely on one side or needs to be split
        for (int i = 0; i < cell.Triangles.Length; i += 3)
        {
            int i0 = cell.Triangles[i];
            int i1 = cell.Triangles[i + 1];
            int i2 = cell.Triangles[i + 2];
            
            Vector3 v0 = cell.Vertices[i0];
            Vector3 v1 = cell.Vertices[i1];
            Vector3 v2 = cell.Vertices[i2];
            
            Vector2 uv0 = (cell.UVs != null && i0 < cell.UVs.Length) ? cell.UVs[i0] : Vector2.zero;
            Vector2 uv1 = (cell.UVs != null && i1 < cell.UVs.Length) ? cell.UVs[i1] : Vector2.zero;
            Vector2 uv2 = (cell.UVs != null && i2 < cell.UVs.Length) ? cell.UVs[i2] : Vector2.zero;
            
            bool side0 = plane.GetSide(v0);
            bool side1 = plane.GetSide(v1);
            bool side2 = plane.GetSide(v2);
            
            // Triangle is entirely on the positive side
            if (side0 && side1 && side2)
            {
                AddTriangle(posVertices, posTriangles, posUVs, v0, v1, v2, uv0, uv1, uv2);
            }
            // Triangle is entirely on the negative side
            else if (!side0 && !side1 && !side2)
            {
                AddTriangle(negVertices, negTriangles, negUVs, v0, v1, v2, uv0, uv1, uv2);
            }
            // Triangle intersects the plane and needs to be split
            else
            {
                // Split triangle based on the plane and add to respective sides
                SplitTriangle(v0, v1, v2, uv0, uv1, uv2, side0, side1, side2, plane, 
                    posVertices, posTriangles, posUVs, negVertices, negTriangles, negUVs);
            }
        }
        
        // Create and return the split cells
        positive.Vertices = posVertices.ToArray();
        positive.Triangles = posTriangles.ToArray();
        positive.UVs = posUVs.ToArray();
        
        negative.Vertices = negVertices.ToArray();
        negative.Triangles = negTriangles.ToArray();
        negative.UVs = negUVs.ToArray();
        
        return posTriangles.Count > 0 && negTriangles.Count > 0;
    }
    
    // Helper to add a triangle to mesh data
    private void AddTriangle(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
                            Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
    {
        int index = vertices.Count;
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        
        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);
    }
    
    // Helper to split a triangle by a plane
    private void SplitTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2,
                              bool side0, bool side1, bool side2, Plane plane,
                              List<Vector3> posVertices, List<int> posTriangles, List<Vector2> posUVs,
                              List<Vector3> negVertices, List<int> negTriangles, List<Vector2> negUVs)
    {
        // Calculate which vertices are on which side and find intersection points
        List<Vector3> posVerts = new List<Vector3>();
        List<Vector3> negVerts = new List<Vector3>();
        List<Vector2> posUV = new List<Vector2>();
        List<Vector2> negUV = new List<Vector2>();
        
        // Add vertices to their corresponding sides
        if (side0) { posVerts.Add(v0); posUV.Add(uv0); } else { negVerts.Add(v0); negUV.Add(uv0); }
        if (side1) { posVerts.Add(v1); posUV.Add(uv1); } else { negVerts.Add(v1); negUV.Add(uv1); }
        if (side2) { posVerts.Add(v2); posUV.Add(uv2); } else { negVerts.Add(v2); negUV.Add(uv2); }
        
        // Calculate intersection points where the triangle crosses the plane
        if (side0 != side1)
        {
            float t = CalculatePlaneIntersection(v0, v1, plane);
            Vector3 intersection = Vector3.Lerp(v0, v1, t);
            Vector2 uvIntersection = Vector2.Lerp(uv0, uv1, t);
            
            posVerts.Add(intersection);
            negVerts.Add(intersection);
            posUV.Add(uvIntersection);
            negUV.Add(uvIntersection);
        }
        
        if (side0 != side2)
        {
            float t = CalculatePlaneIntersection(v0, v2, plane);
            Vector3 intersection = Vector3.Lerp(v0, v2, t);
            Vector2 uvIntersection = Vector2.Lerp(uv0, uv2, t);
            
            posVerts.Add(intersection);
            negVerts.Add(intersection);
            posUV.Add(uvIntersection);
            negUV.Add(uvIntersection);
        }
        
        if (side1 != side2)
        {
            float t = CalculatePlaneIntersection(v1, v2, plane);
            Vector3 intersection = Vector3.Lerp(v1, v2, t);
            Vector2 uvIntersection = Vector2.Lerp(uv1, uv2, t);
            
            posVerts.Add(intersection);
            negVerts.Add(intersection);
            posUV.Add(uvIntersection);
            negUV.Add(uvIntersection);
        }
        
        // Create triangles for positive side
        if (posVerts.Count >= 3)
        {
            AddTriangle(posVertices, posTriangles, posUVs, posVerts[0], posVerts[1], posVerts[2],
                        posUV[0], posUV[1], posUV[2]);
                        
            // If we have 4 vertices on positive side, make another triangle
            if (posVerts.Count == 4)
            {
                AddTriangle(posVertices, posTriangles, posUVs, posVerts[0], posVerts[2], posVerts[3],
                            posUV[0], posUV[2], posUV[3]);
            }
        }
        
        // Create triangles for negative side
        if (negVerts.Count >= 3)
        {
            AddTriangle(negVertices, negTriangles, negUVs, negVerts[0], negVerts[1], negVerts[2],
                        negUV[0], negUV[1], negUV[2]);
                        
            // If we have 4 vertices on negative side, make another triangle
            if (negVerts.Count == 4)
            {
                AddTriangle(negVertices, negTriangles, negUVs, negVerts[0], negVerts[2], negVerts[3],
                            negUV[0], negUV[2], negUV[3]);
            }
        }
    }
    
    // Calculate where a line segment intersects a plane (returns t value for lerp)
    private float CalculatePlaneIntersection(Vector3 a, Vector3 b, Plane plane)
    {
        Vector3 direction = b - a;
        float den = Vector3.Dot(direction, plane.normal);
        
        if (den == 0) return 0.5f; // Avoid division by zero
        
        float dist = (plane.distance - Vector3.Dot(a, plane.normal)) / den;
        return Mathf.Clamp01(dist);
    }

    // Validate mesh before adding collider
    private bool ValidateMesh(Mesh mesh)
    {
        // Check for degenerate triangles
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        
        if (verts.Length < 3 || tris.Length < 3)
            return false;
            
        // Check if mesh is too complex for PhysX
        if (verts.Length > 255) // PhysX generally limits convex meshes to around 255 vertices
        {
            return false;
        }
        
        // Basic validation is complete
        return true;
    }
    
    // Try to add a mesh collider safely
    private bool AddMeshCollider(GameObject obj, Mesh mesh)
    {
        try
        {
            MeshCollider mc = obj.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning | 
                                MeshColliderCookingOptions.CookForFasterSimulation | 
                                MeshColliderCookingOptions.WeldColocatedVertices;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to add mesh collider: {e.Message}");
            
            // If there's already a collider that failed, try to remove it
            MeshCollider failedCollider = obj.GetComponent<MeshCollider>();
            if (failedCollider != null)
                GameObject.Destroy(failedCollider);
                
            return false;
        }
    }
}
