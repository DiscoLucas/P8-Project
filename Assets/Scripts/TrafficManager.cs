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

    List<TrafficCar> activeCars = new List<TrafficCar>();
    float nextSpawnTime;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var light in trafficLights)
        {
            light.Initialize();
        }
        SpawnCar();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.time >= nextSpawnTime && activeCars.Count < maxCars)
        {
            Debug.Log("Spawning car");
            SpawnCar();
            SetNextSpawnTime();
        }
        // remove cars that have reached their destination
        //CleanupCars();
    }

    

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Time.time + Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns);
        Debug.Log("Next spawn time: " + nextSpawnTime);
    }

    private void SpawnCar()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        //Debug.Log("Spawning car at " + spawnPoint.position);

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
        
        // spawn the car
        GameObject carObject = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        TrafficCar car = carObject.GetComponent<TrafficCar>();

        if (car == null)
        {
            car = carObject.AddComponent<TrafficCar>();
        }

        // Initialize car behavior
        car.Initialize(this, destinationPoints[Random.Range(0, destinationPoints.Count)], isSpecialVehicle);
        activeCars.Add(car);
    }


List<Transform> GeneratePathForCar(Transform startPoint) 
{
    List<Transform> path = new List<Transform>();
    // Add waypoints along the route
    // ...
    // Add despawn point as final waypoint
    path.Add(FindClosestDespawnPoint(startPoint));
    return path;
}
    
    /*
    private void CleanupCars()
    {
        List<TrafficCar> carsToRemove = new List<TrafficCar>();
        foreach (var car in activeCars)
        {
            if (car.agent.remainingDistance < 0.1f)
            {
                carsToRemove.Add(car);
                Destroy(car.gameObject);
            }
        }

        foreach (var car in carsToRemove)
        {
            activeCars.Remove(car);
        }
        
    }
*/
    public bool IsRedLightAtPosition(Vector3 position, float checkDistance = 5f)
    {
        foreach (var light in trafficLights)
        {
            if (Vector3.Distance(position, light.transform.position) < checkDistance
            && light.IsRed())
            {
                return true;
            }
        }

        return false;
    }
}
