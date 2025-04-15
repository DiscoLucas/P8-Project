using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

public class PerformanceRecorder : Singleton<PerformanceRecorder>
{
    public string folderPath = "Telemetry";
    private string participantID;
    private int conditionNumber;
    private string SessionID;
    private string currentFilePath;
    private List<string> dataBuffer = new List<string>();
    private float autoSaveInterval = 20f;
    private float timeSinceLastSave = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    /// <summary>
    /// Initializes the participant ID and condition number.
    /// This method should be called before starting the recording session.
    /// </summary>
    /// <param name="partID">Unique ID for the given test participant</param>
    /// <param name="condition"></param>
    public void InitializeParticpantID(string partID, int condition)
    {
        participantID = partID;
        conditionNumber = condition;
        SessionID = GenerateSessionID();
        InitializeFile();
        WriteMetaData();
    }

    private void WriteMetaData()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# MetaData");
        sb.AppendLine($"Participant ID: {participantID}");
        sb.AppendLine($"Condition Number: {conditionNumber}");
        sb.AppendLine($"Session ID: {SessionID}");
        sb.AppendLine($"Start Time: {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}");
        sb.AppendLine("# Data");
        sb.AppendLine("Timestamp;EventType;value1;value2;value3"); // TODO: add actual data header

        File.WriteAllText(currentFilePath, sb.ToString());
    }

    private void InitializeFile()
    {
        // Create a structured filename
        string fileName = $"{participantID}_C{conditionNumber}_S{SessionID}.csv";
        currentFilePath = Path.Combine(folderPath, fileName);

        // TODO: also create a backup file
    }

    private string GenerateSessionID()
    {
        Guid guid = Guid.NewGuid();
        return DateTime.Now.ToString("ddMMyyyy_HHmmss") + "_" + guid.ToString();
    }
    
    /// <summary>
    /// Records telemetry data to csv file.
    /// <see cref="InitializeParticpantID">InitializeParticpantID</see>
    /// Needs to be called first.
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <param name="eventType"></param>
    /// <param name="values"></param>
    public void RecordData(double milliseconds, string eventType, params object[] values)
    {
        if (string.IsNullOrEmpty(currentFilePath)) return;

        StringBuilder sb = new StringBuilder();
        sb.Append(milliseconds.ToString("F2")); // i dont really like this, it depends on the function calling it not fucking up time
        sb.Append(";");
        sb.Append(eventType);

        foreach (var value in values)
        {
            sb.Append(";");
            sb.Append(value);
        }

        // Add the data to the buffer
        dataBuffer.Add(sb.ToString());
        // save the buffer if it exceeds a certain size
        if (dataBuffer.Count >= 100)
        {
            SaveBufferedData();
        }
    }

    private void SaveBufferedData()
    {
        if (dataBuffer.Count == 0) return;

        try
        {
            File.AppendAllLines(currentFilePath, dataBuffer);
            dataBuffer.Clear();
            timeSinceLastSave = 0f; // Reset the timer after saving
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
    }

    void OPause(bool pause)
    {
        if (pause) SaveBufferedData();
    }

    void OnAppltionQuit()
    {
        SaveBufferedData();
    }

    void FixedUpdate()
    {
        timeSinceLastSave += Time.fixedDeltaTime;
        if (timeSinceLastSave >= autoSaveInterval)
        {
            SaveBufferedData();
        }
    }
}
