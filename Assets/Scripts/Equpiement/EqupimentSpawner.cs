using UnityEngine;
using System.Collections;

public class EqupimentSpawner : MonoBehaviour
{
    public GameSettings gameSettings;
    [Header("Settings")]
    [SerializeField] private Transform spawnPoint; // The point where the equipment will respawn
    private float maxDistanceFromPlayer = 10f; // Maximum distance from the player before respawning
    private float checkInterval = 1f; // Time interval (in seconds) between distance checks
    [SerializeField] private Transform middelpoint; // Reference to the player (or their hand/controller)

    Rigidbody rigidbody;


    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        // Find the object with the tag "MiddelPoint" and assign it to middelpoint
        if (middelpoint == null)
        {
            GameObject middelPointObject = GameObject.FindWithTag("MiddelPoint");
            if (middelPointObject != null)
            {
                middelpoint = middelPointObject.transform;
            }
            else
            {
                Debug.LogError("No object with the tag 'MiddelPoint' found in the scene!");
            }
        }

        // Create a spawn point if not already assigned
        if (spawnPoint == null)
        {
            GameObject spawnPointObject = Instantiate(new GameObject(), transform.position, transform.rotation);
            spawnPointObject.name = "SpawnPoint_for_" + gameObject.name;
            spawnPoint = spawnPointObject.transform;
        }

        // Assign game settings if not already assigned
        if (gameSettings == null)
        {
            gameSettings = GameManager.Instance.gameSettings;
        }

        // Get settings from GameSettings
        maxDistanceFromPlayer = gameSettings.maxDistanceFromPlayerToObj;
        checkInterval = gameSettings.checkIntervalForObj;

        // Start the coroutine to periodically check the distance
        StartCoroutine(CheckDistanceRoutine());
    }

    private IEnumerator CheckDistanceRoutine()
    {
        while (true)
        {
            // Wait for the specified interval before checking again
            yield return new WaitForSeconds(checkInterval);
            
            // Check distance to the player
            if (middelpoint != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, middelpoint.position);
                if (distanceToPlayer > maxDistanceFromPlayer)
                {
                    Debug.Log($"{gameObject.name} is too far from the player! Respawning...");
                    Respawn();
                }
            }
        }
    }

    private void Respawn()
    {
        rigidbody.linearVelocity = Vector3.zero;
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            Debug.Log($"{gameObject.name} has been respawned at the spawn point.");
        }
        else
        {
            transform.position = middelpoint.position;
            transform.rotation = middelpoint.rotation;
            Debug.LogWarning("Spawn point is not assigned!");
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the maximum distance from the player
        if (middelpoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(middelpoint.position, maxDistanceFromPlayer);
        }
    }
}
