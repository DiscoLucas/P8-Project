using UnityEngine;

public class TrafficDespawner : MonoBehaviour
{
    private BoxCollider boxCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider collider)
    {
        Debug.Log("Despawning car");
        Destroy(collider.gameObject);
    }
}
