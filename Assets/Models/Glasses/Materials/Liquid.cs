using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Drink_interaction;
using UnityEngine;

[ExecuteInEditMode]
public class Liquid : MonoBehaviour
{
    // Enum to determine whether to use normal time or unscaled time for updates
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;

    // Serialized fields for wobble effect, liquid color, and fill properties
    [SerializeField] float MaxWobble = 0.03f; // Maximum wobble effect
    [SerializeField] float WobbleSpeedMove = 1f; // Speed of wobble movement
    [SerializeField] public Color DrinkColor; // Color of the liquid
    [SerializeField] public LiquidContainerLimited liquidProperty; // Reference to the liquid container
    public float fillAmount = 0.5f; // Current fill amount (0 to 1)
    public float fillAmountLerpMax = 1; // Maximum fill amount for lerping
    public float fillAmountLerpMin = 0; // Minimum fill amount for lerping
    float fillAmountScaled; // Scaled fill amount based on lerp values
    [SerializeField] float Recovery = 1f; // Recovery speed for wobble effect
    [SerializeField] float Thickness = 1f; // Thickness of the liquid
    [Range(0, 1)] public float CompensateShapeAmount; // Compensation for shape deformation
    [SerializeField] Mesh mesh; // Mesh of the liquid object
    [SerializeField] Renderer rend; // Renderer for the liquid object

    // Variables for tracking position, velocity, and wobble
    Vector3 pos;
    Vector3 lastPos;
    Vector3 velocity;
    Quaternion lastRot;
    Vector3 angularVelocity;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float sinewave;
    float time = 0.5f; // Time tracker for wobble effect
    Vector3 comp; // Compensation vector for shape deformation
    MaterialPropertyBlock propBlock; // Material property block for shader updates

    // Cached values for optimization
    float cachedLowestY = float.MaxValue; // Cached lowest Y value of the mesh
    bool needsRecalculateLowestY = true; // Flag to recalculate the lowest Y value

    // Called when the script starts
    void Start()
    {
        GetMeshAndRend(); // Initialize mesh and renderer
        liquidProperty = GetComponentInParent<LiquidContainerLimited>(); // Get reference to the parent liquid container
        propBlock = new MaterialPropertyBlock(); // Initialize material property block

        // Calculate scale ratios relative to the Y-axis
      /*  scaleRatios = new Vector3(
            transform.localScale.x / transform.localScale.y, // X relative to Y
            1f, // Y relative to itself
            transform.localScale.z / transform.localScale.y // Z relative to Y
        );*/
    }

    // Called when a value is changed in the inspector
    private void OnValidate()
    {
        GetMeshAndRend(); // Ensure mesh and renderer are initialized
    }

    // Initializes the mesh and renderer if they are not already set
    void GetMeshAndRend()
    {
        if (mesh == null)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
                mesh = meshFilter.sharedMesh; // Get the shared mesh
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>(); // Get the renderer
        }
    }

    // Called once per frame
    void Update()
    {
        // Scale the fill amount based on lerp values
        fillAmountScaled = Mathf.Lerp(fillAmountLerpMin, fillAmountLerpMax, fillAmount);
        

        // Update liquid properties if a liquid container is present
        if (liquidProperty != null)
        {
            DrinkColor = liquidProperty.getLiquidColor(); // Get the liquid color
            fillAmount = liquidProperty.FillPercentage(); // Get the fill percentage
        }

        // Determine delta time based on the update mode
        float deltaTime = updateMode == UpdateMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime;
        time += deltaTime; // Increment time

        // Calculate velocity and angular velocity
        if (deltaTime > 0)
        {
            velocity = (lastPos - transform.position) / deltaTime;
            angularVelocity = GetAngularVelocity(lastRot, transform.rotation);
        }
        else
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
        }

        // Update material properties for the wobble effect
        try{
            rend.GetPropertyBlock(propBlock);
            propBlock.SetFloat("_WobbleX", wobbleAmountX);
            propBlock.SetFloat("_WobbleZ", wobbleAmountZ);

        }catch(System.Exception e){
        }
        
        UpdatePos(deltaTime); // Update the position of the liquid
        UpdateMat(propBlock); // Update the material properties
        rend.SetPropertyBlock(propBlock);

        // Store the current position and rotation for the next frame
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    // Updates the position of the liquid based on wobble and fill amount
    void UpdatePos(float deltaTime)
    {
        // Get the world position of the mesh's center
        Vector3 worldPos = transform.TransformPoint(mesh.bounds.center);

        // Calculate the fill amount transformation
        float transformFillAmount = -2 * fillAmount + 1;
        float dotX = Mathf.Abs(Vector3.Dot(Vector3.up, transform.right));   // Alignment with X-axis
        float dotY = Mathf.Abs(Vector3.Dot(Vector3.up.normalized, transform.up));      // Alignment with Y-axis
        float dotZ = Mathf.Abs(Vector3.Dot(Vector3.up.normalized, transform.forward)); // Alignment with Z-axis

        // Normalize the dot products to ensure they sum to 1
        float totalDot = dotX + dotY + dotZ;
        dotX /= totalDot;
        dotY /= totalDot;
        dotZ /= totalDot;

        // Lerp the scale ratios based on the alignment
        float scaledTransformFillAmount = 
            (dotX * mesh.bounds.extents.x) +  // Contribution from X-axis
            (dotY * mesh.bounds.extents.y) +  // Contribution from Y-axis
            (dotZ * mesh.bounds.extents.z);   // Contribution from Z-axis

;
        transformFillAmount *= scaledTransformFillAmount; // Scale the fill amount
        // Adjust the compensation vector if needed
        if (CompensateShapeAmount > 0)
        {
            Vector3 targetComp = worldPos - new Vector3(0, GetLowestPoint(), 0); 

            if (deltaTime > 0)
            {
                comp = Vector3.Lerp(comp, targetComp, deltaTime * 10);
            }
            else
            {
                comp = targetComp;
            }

            // Adjust the position based on the calculated "up" direction
            pos = worldPos - transform.position - (Vector3.up * (transformFillAmount- (comp.y * CompensateShapeAmount)));
        }
        else
        {
            // Adjust the position based on the calculated "up" direction
            pos = worldPos - transform.position - (Vector3.up* transformFillAmount*transform.localScale.y);
        }

        // Update the shader with the new position
        rend.sharedMaterial.SetVector("_FillAmount", pos);
    }

    // Updates the material properties for the liquid
    void UpdateMat(MaterialPropertyBlock propBlock)
    {
        try{
            propBlock.SetVector("_FillAmount", pos); // Set the fill amount position
            propBlock.SetColor("_BottomColor", DrinkColor); // Set the bottom color
            propBlock.SetColor("_TopColor", DrinkColor); // Set the top color
        }catch(System.Exception e){
        }
    }

    // Calculates the lowest point of the mesh in world space
    float GetLowestPoint()
    {
        if (!needsRecalculateLowestY)
            return cachedLowestY;

        float lowestY = float.MaxValue;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            float worldY = transform.TransformPoint(vertices[i]).y;
            if (worldY < lowestY)
                lowestY = worldY;
        }

        cachedLowestY = lowestY; // Cache the lowest Y value
        needsRecalculateLowestY = false; // Mark as recalculated
        return cachedLowestY;
    }

    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        // no rotation?
        // You may want to increase this closer to 1 if you want to handle very small rotations.
        // Beware, if it is too close to one your answer will be Nan
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            return Vector3.zero;
        float gain;
        // handle negatives, we could just flip it but this is faster
        if (q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        Vector3 angularVelocity = new Vector3(q.x * gain, q.y * gain, q.z * gain);
 
        if (float.IsNaN(angularVelocity.z))
        {
            angularVelocity = Vector3.zero;
        }
        return angularVelocity;
    }

    void OnDrawGizmos()
    {
        // Set the color for the gizmo
        Gizmos.color = Color.red;

        // Draw a sphere at the position of 'pos'
        Gizmos.DrawSphere(transform.position + pos, 0.05f); // Adjust the size (0.05f) as needed

        // Optionally, draw a line from the object's position to 'pos'
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + pos);

        // Visualize transform.position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.05f); // Draw a yellow sphere at transform.position

        // Visualize worldPos
        if (mesh != null)
        {
            Vector3 worldPos = transform.TransformPoint(mesh.bounds.center); // Calculate worldPos
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(worldPos, 0.05f); // Draw a green sphere at worldPos
            Gizmos.DrawLine(transform.position, worldPos); // Draw a line from the object's position to worldPos

            // Visualize (worldPos - transform.position)
            Gizmos.color = Color.magenta;
            Vector3 offset = worldPos - transform.position;
            Gizmos.DrawSphere(transform.position + offset, 0.05f); // Draw a magenta sphere at the offset position
            Gizmos.DrawLine(transform.position, transform.position + offset); // Draw a line to the offset position
        }
    }
}