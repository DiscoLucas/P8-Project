using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [Header("Car Prefabs")]
    [SerializeField] List<GameObject> carPrefabs = new List<GameObject>();
    [SerializeField] GameObject ambulancePrefab;

    [Header("Spawn Settings")]
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] List<Transform> destinationPoints = new List<Transform>();
    [SerializeField] float minTimeBetweenSpawns = 4f;
    [SerializeField] float maxTimeBetweenSpawns = 20f;
    [SerializeField] int maxCars = 20;
    [SerializeField] float specialVehicleChance = 0.01f;

    [Header("Traffic Lights")]
    [SerializeField] private List<TrafficLight> trafficLights = new List<TrafficLight>();

    [Header("Route System")]
    [Tooltip("Each element represents a route, containing waypoints from start to finish")]
    [SerializeField] private List<Route> routes = new List<Route>();

    List<TrafficCar> activeCars = new List<TrafficCar>();
    float nextSpawnTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var light in trafficLights)
        {
            light.Initialize();
        }
        
        // Initialize routes if they're empty
        if (routes.Count == 0)
        {
            Debug.LogWarning("No routes defined. Creating basic routes between spawn and destination points.");
            GenerateBasicRoutes();
        }
        
        //SpawnCar();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.time >= nextSpawnTime && activeCars.Count < maxCars)
        {
            SpawnCar();
            SetNextSpawnTime();
        }
        CleanupCars();
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Time.time + Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns);
        //Debug.Log("Next spawn time: " + nextSpawnTime);
    }

    private void GenerateBasicRoutes()
    {
        // Create simple direct routes between each spawn point and destination point
        foreach (var spawnPoint in spawnPoints)
        {
            foreach (var destinationPoint in destinationPoints)
            {
                Route newRoute = new Route();
                newRoute.name = $"Route {spawnPoint.name} to {destinationPoint.name}";
                newRoute.startPoint = spawnPoint;
                newRoute.endPoint = destinationPoint;
                
                // Add start point as first waypoint
                newRoute.waypoints.Add(spawnPoint);
                
                // Add end point as final waypoint
                newRoute.waypoints.Add(destinationPoint);
                
                routes.Add(newRoute);
            }
        }
    }

    private void SpawnCar()
    {
        // Find a valid route to use
        if (routes.Count == 0)
        {
            Debug.LogError("No routes available for spawning cars");
            return;
        }
        
        // Select a random route
        Route selectedRoute = routes[Random.Range(0, routes.Count)];
        Transform spawnPoint = selectedRoute.startPoint;
        
        // Select the car type
        GameObject prefabToSpawn;
        bool isSpecialVehicle = Random.value < specialVehicleChance;

        if (isSpecialVehicle && ambulancePrefab != null)
        {
            prefabToSpawn = ambulancePrefab;
        }
        else if (carPrefabs.Count > 0)
        {
            prefabToSpawn = carPrefabs[Random.Range(0, carPrefabs.Count)];
        }
        else 
        {
            Debug.LogWarning("No car prefabs assigned to TrafficManager");
            return;
        }
        
        // Spawn the car
        GameObject carObject = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        TrafficCar car = carObject.GetComponent<TrafficCar>();
        if (car == null)
        {
            car = carObject.AddComponent<TrafficCar>();
        }

        // Pass the waypoints from the selected route to the car
        car.Initialize(this, selectedRoute.waypoints, isSpecialVehicle);
        activeCars.Add(car);
    }

    private void CleanupCars()
    {
        // Remove any null entries (destroyed cars)
        activeCars.RemoveAll(car => car == null);
    }

    /// <summary>
    /// are there any red lights within a certain distance of the given position?
    /// </summary>
    /// <param name="position">Is there a red light at the given position?</param>
    /// <param name="checkDistance"></param>
    /// <returns>true if the distance between <see cref="position"/> and a traffic light is less than <see cref="checkDistance"/></returns>
    public bool IsRedLightAtPosition(Vector3 position, float checkDistance = 5f)
    {
        foreach (var light in trafficLights)
        {
            Debug.DrawLine(position, light.transform.position, Color.red);
            
            if (Vector3.Distance(position, light.transform.position) < checkDistance
                && light.IsRed())
            {
                Debug.DrawLine(position, light.transform.position, Color.green);
                return true;
            }
        }
        return false;
    }
    
    // For debugging - visualize routes in Scene view
    private void OnDrawGizmos()
    {
        if (routes == null || routes.Count == 0)
            return;
            
        foreach (var route in routes)
        {
            if (route.waypoints.Count < 2)
                continue;
                
            // Draw route lines
            Gizmos.color = Color.yellow;
            for (int i = 0; i < route.waypoints.Count - 1; i++)
            {
                if (route.waypoints[i] != null && route.waypoints[i+1] != null)
                {
                    Gizmos.DrawLine(route.waypoints[i].position, route.waypoints[i+1].position);
                }
            }
            
            // Draw waypoint markers
            Gizmos.color = Color.blue;
            foreach (var waypoint in route.waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawSphere(waypoint.position, 1f);
                }
            }
        }
    }
}

// Serializable class to represent a route
[System.Serializable]
public class Route
{
    public string name = "New Route";
    public Transform startPoint;
    public Transform endPoint;
    public List<Transform> waypoints = new List<Transform>();
}
