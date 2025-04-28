using UnityEngine;
using System.Collections.Generic;

public class GazePointRunner : MonoBehaviour
{

    [Header("Gaze controls")]
    public LayerMask detectedLayers;
    public float detectionRadius = 5.0f; // Maximum distance for raycasts
    public float coneAngle = 30.0f; // Half-angle of the cone in degrees
    public int rayCount = 20; // Number of rays to cast in the cone

    [Header("Gaze Points")]
    public float gazePointThreshold = 2.0f; // Time (in seconds) required to mark an object as a gaze point
    private Dictionary<Collider, float> gazePointTimers = new Dictionary<Collider, float>();
    [SerializeField]
    private List<Collider> gazePoints = new List<Collider>();
    [SerializeField]
    private List<Collider> lookedAtObjects = new List<Collider>();

    [SerializeField]
    string streamName = "GazePointStream";
    void Start()
    {
        if (PerformanceRecorder.Instance != null)
        {
           string[] labels = new string[] { "GazePoint" }; 
            PerformanceRecorder.Instance.InitializeLSLStream(streamName, "Gaze", 1, LSL.LSL.IRREGULAR_RATE, 
            LSL.channel_format_t.cf_string, labels );
        }
    }

    void Update()
    {
        detectGaze();
    }

    public Collider[]  getCurrentGazePoints()
    {
        return gazePoints.ToArray();
    }
    public Collider[] getCurrentLookedAtObjects()
    {
        return lookedAtObjects.ToArray();
    }


    public void detectGaze()
    {
        // Step 1: Detect looked-at objects
        Vector3 gazeOrigin = Camera.main.transform.position;
        Vector3 gazeDirection = Camera.main.transform.forward;

        List<Collider> newLookedAtObjects = new List<Collider>();

        for (int i = 0; i < rayCount; i++)
        {
            for (int j = 0; j < rayCount; j++)
            {
                // Generate a direction within the cone
                float horizontalAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)i / (rayCount - 1));
                float verticalAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)j / (rayCount - 1));

                // Combine horizontal and vertical rotations
                Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
                Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, Camera.main.transform.right);
                Vector3 direction = verticalRotation * horizontalRotation * gazeDirection;

                // Perform a raycast
                if (Physics.Raycast(gazeOrigin, direction, out RaycastHit hit, detectionRadius, detectedLayers))
                {
                    if (!newLookedAtObjects.Contains(hit.collider))
                    {
                        newLookedAtObjects.Add(hit.collider);
                    }
                }
            }
        }

        // Update looked-at objects
        lookedAtObjects = newLookedAtObjects;

        // Step 2: Update gaze points
        foreach (var collider in lookedAtObjects)
        {
            if (!gazePointTimers.ContainsKey(collider))
            {
                gazePointTimers[collider] = 0.0f; // Start tracking time
            }

            // Increment the timer for this collider
            gazePointTimers[collider] += Time.deltaTime;

            // If the timer exceeds the threshold, add it to gaze points
            if (gazePointTimers[collider] >= gazePointThreshold && !gazePoints.Contains(collider))
            {
                gazePoints.Add(collider);
                Debug.Log($"New gaze point added: {collider.name}");
            }
        }

        // Step 3: Remove expired gaze point timers
        List<Collider> expiredTimers = new List<Collider>();
        foreach (var kvp in gazePointTimers)
        {
            if (!lookedAtObjects.Contains(kvp.Key))
            {
                expiredTimers.Add(kvp.Key);
            }
        }

        foreach (var collider in expiredTimers)
        {
            gazePointTimers.Remove(collider);
        }

        // Step 4: Remove gaze points no longer looked at
        for (int i = gazePoints.Count - 1; i >= 0; i--)
        {
            if (!lookedAtObjects.Contains(gazePoints[i]))
            {
                Debug.Log($"Gaze point removed: {gazePoints[i].name}");
                gazePoints.RemoveAt(i);
                i = 0;
            }
        }

        // Debug looked-at objects
        foreach (var collider in lookedAtObjects)
        {
            Debug.Log($"Currently looking at: {collider.name}");
        }
    }

    void FixedUpdate()
    {
        if(gazePoints.Count == 0) return;
        string[] layersLookedAt = new string[gazePoints.Count];
        for (int i = 0; i < gazePoints.Count; i++)
        {
            layersLookedAt[i] = gazePoints[i].transform.gameObject.layer.ToString();
        }
        PerformanceRecorder.Instance?.RecordStreamData(streamName, layersLookedAt);
    }

    void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;

        // Define the cone parameters
        Vector3 gazeOrigin = Camera.main.transform.position;
        Vector3 gazeDirection = Camera.main.transform.forward;

        // Draw the detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(gazeOrigin, detectionRadius);

        // Draw the 3D cone
        Gizmos.color = Color.red;
        int coneResolution = 20; // Number of points to sample on the cone surface

        for (int i = 0; i < coneResolution; i++)
        {
            for (int j = 0; j < coneResolution; j++)
            {
                // Calculate spherical coordinates
                float horizontalAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)i / (coneResolution - 1));
                float verticalAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)j / (coneResolution - 1));

                // Convert spherical coordinates to a direction vector
                Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
                Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, Camera.main.transform.right);
                Vector3 coneDirection = verticalRotation * horizontalRotation * gazeDirection;

                // Draw a ray for each direction
                Gizmos.DrawRay(gazeOrigin, coneDirection * detectionRadius);
            }
        }
    }
}
