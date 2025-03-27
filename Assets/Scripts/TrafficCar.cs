using UnityEngine;
using UnityEngine.AI;

public class TrafficCar : MonoBehaviour
{
    public NavMeshAgent agent;
    [SerializeField] private float regularSpeed = 8f;
    [SerializeField] private float specialVehicleSpeed = 12f;
    private float stoppingDistance;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource hornSound;
    [SerializeField] private AudioSource sirenSound;

    private TrafficManager trafficManager;
    private Transform destination;
    private Transform targetDespawnPoint;
    private bool isSpecialVehicle;
    private float currentSpeed;
    private bool isAtRedLight;

    /// <summary>
    /// Initializes the car with the given parameters
    /// </summary>
    /// <param name="manager">Reference to the traffic manager, spawning the car</param>
    /// <param name="despawnPoint">The final point that the car will despawn at.</param>
    /// <param name="isSpecialVehicle">Enables emergency vehicle behaviors such as sirenes.</param>
    public void Initialize(TrafficManager manager, Transform despawnPoint, bool isSpecialVehicle)
    {
        agent = GetComponent<NavMeshAgent>();
        stoppingDistance = agent.stoppingDistance;
        agent.speed = currentSpeed;
        trafficManager = manager;
        targetDespawnPoint = despawnPoint;
        this.isSpecialVehicle = isSpecialVehicle;
        currentSpeed = isSpecialVehicle ? specialVehicleSpeed : regularSpeed;
        agent.destination = targetDespawnPoint.position;
        engineSound.Play();

        if (isSpecialVehicle && sirenSound != null)
        {
            sirenSound.Play();
        }
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (destination == null)
        {
            return;
        }

        // Check for red lights ahead
        isAtRedLight = trafficManager.IsRedLightAtPosition(transform.position + transform.forward * detectionRadius);
        // Check for cars ahead
        bool carAhead = Physics.Raycast(transform.position, transform.forward, stoppingDistance, LayerMask.GetMask("Car"));

        if (!isAtRedLight && !carAhead)
        {
            agent.speed = regularSpeed;
            agent.SetDestination(destination.position);

            // Occasionally honk the horn if it's a special vehicle
            if (isSpecialVehicle && hornSound != null && Random.value < 0.001f)
            {
                hornSound.Play();
            }
        }
        else
        {
            // stopped at traffic light or car ahead
            agent.speed = 0f; // todo: change this to set a temporary destination behind the car ahead or light
        }
    }

    
}
