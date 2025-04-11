using UnityEngine;

public class PerformanceRecorder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Write XML file
    public void WriteXML(string filePath, string content)
    {
        System.IO.File.WriteAllText(filePath, content);
    }
}
