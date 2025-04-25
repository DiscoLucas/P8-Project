using UnityEngine;

public class BaselineDummyData : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // move object from left to right with sinewave
        float x = Mathf.Sin(Time.time) * 5;
        gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, gameObject.transform.position.z);
        PerformanceRecorder.Instance.RecordData("DummyData", gameObject.transform.position.ToString());
    }
}
