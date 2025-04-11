using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class PerformanceRecorder : Singleton<PerformanceRecorder>
{
    public string folderPath = "Telemetry";
    private string participantID;
    private int conditionNumber;
    private int SessionID;
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
        throw new NotImplementedException();
    }

    private void InitializeFile()
    {
        throw new NotImplementedException();
    }

    private int GenerateSessionID()
    {
        throw new NotImplementedException();
    }
}
