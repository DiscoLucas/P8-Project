using UnityEngine;
using System.Collections.Generic;

public class TrafficCar : MonoBehaviour
{
    [SerializeField] private float regularSpeed = 8f;
    [SerializeField] private float specialVehicleSpeed = 12f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float braking = 4f;
    [SerializeField] private float steeringSpeed = 3f;
    [SerializeField] private float lookAheadDistance = 10f;
    [SerializeField] private float waypointReachedDistance = 3f;
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource hornSound;
    [SerializeField] private AudioSource sirenSound;

    private TrafficManager trafficManager;
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private float currentSpeed = 0f;
    private float targetSpeed;
    private bool isSpecialVehicle;
    private Rigidbody rb;

    public void Initialize(TrafficManager manager, List<Transform> path, bool specialVehicle)
    {
        trafficManager = manager;
        waypoints = new List<Transform>(path); // Create a copy of the waypoints
        isSpecialVehicle = specialVehicle;
        targetSpeed = specialVehicle ? specialVehicleSpeed : regularSpeed;
        
        rb = GetComponent<Rigidbody>();
        if (rb == null) {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1000f;
            rb.linearDamping = 1f;
            rb.angularDamping = 5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = false;
        }
        
        // Start engine sounds
        if (engineSound != null) engineSound.Play();
        if (isSpecialVehicle && sirenSound != null) sirenSound.Play();
    }

    void FixedUpdate()
    {
        if (waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            return;

        // Get current target waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        
        // Calculate distances and directions
        Vector3 directionToWaypoint = targetWaypoint.position - transform.position;
        directionToWaypoint.y = 0; // Keep on same vertical plane
        float distanceToWaypoint = directionToWaypoint.magnitude;
        
        // Check if we should move to next waypoint
        if (distanceToWaypoint < waypointReachedDistance) {
            currentWaypointIndex++;
            
            // If we reached the last waypoint, destroy the car
            if (currentWaypointIndex >= waypoints.Count) {
                Destroy(gameObject);
                return;
            }
        }
        
        // Check for obstacles
        bool obstacle = CheckForObstacles();
        
        // Adjust target speed based on conditions
        if (obstacle) 
        {
            targetSpeed = 0f;
            if (engineSound != null) engineSound.pitch = 0.6f;
        } 
        else 
        {
            targetSpeed = isSpecialVehicle ? specialVehicleSpeed : regularSpeed;
            
            // Slow down for turns
            if (currentWaypointIndex < waypoints.Count - 1) {
                Vector3 nextDirection = waypoints[currentWaypointIndex+1].position - targetWaypoint.position;
                float turnAngle = Vector3.Angle(directionToWaypoint, nextDirection);
                if (turnAngle > 45f) {
                    targetSpeed *= 0.5f; // Slow down for sharp turns
                }
            }
        }
        
        // Smoothly adjust speed
        if (currentSpeed < targetSpeed) {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, targetSpeed);
        } else if (currentSpeed > targetSpeed) {
            currentSpeed = Mathf.Max(currentSpeed - braking * Time.fixedDeltaTime, targetSpeed);
        }
        
        // Adjust engine sound based on speed
        if (engineSound != null) {
            engineSound.pitch = 0.6f + (currentSpeed / regularSpeed) * 0.6f;
        }
        
        // Apply movement
        if (currentSpeed > 0.1f) {
            // Steering behavior to align with waypoint direction
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                                steeringSpeed * Time.fixedDeltaTime);
            
            // Move forward
            rb.linearVelocity = transform.forward * currentSpeed;
        } else {
            rb.linearVelocity = Vector3.zero;
        }
    }
    
    private bool CheckForObstacles() 
    {
        // Check for red lights
        bool atRedLight = trafficManager.IsRedLightAtPosition(transform.position + transform.forward * lookAheadDistance + transform.up * 5f);
        
        // Raycast to detect other cars
        bool carAhead = Physics.Raycast(transform.position, transform.forward, lookAheadDistance, LayerMask.GetMask("Car"));
        
        return atRedLight || carAhead;
    }
}
