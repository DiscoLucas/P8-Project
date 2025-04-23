using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using LSL;

/// <summary>
/// This class is responsible for initializing the participant, recording performance data and sending it to <see cref="LabRecorder">LabRecorder.cs</see>.
/// </summary>
public class PerformanceRecorder : Singleton<PerformanceRecorder>
{
    
    private string participantID;
    private int conditionNumber;
    private string sessionID;
    
    private StreamOutlet outlet;
    private StreamInfo streamInfo;
    [Tooltip("Max amount of expected value channels")]
    private const int MaxValueChannels = 3; // Adjust as needed
    private string[] currentSample; //Reusable array for LSL

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        currentSample = new string[1 + MaxValueChannels]; // 1 for event type + max value channels
    }

    /// <summary>
    /// Initializes the participant ID and condition number.
    /// This method should be called before starting the recording session.
    /// </summary>
    /// <param name="partID">Unique ID for the given test participant</param>
    /// <param name="condition">Condition number</param>
    public void InitializeParticipantID(string partID, int condition)
    {
        participantID = partID;
        conditionNumber = condition;
        sessionID = GenerateSessionID();
        
        InitializeLSLStream();

        RecordData("SessionStart", $"ParticipantID:{participantID}", $"Condition:{conditionNumber}", $"SessionID:{sessionID}");
    }

    private void InitializeLSLStream()
    {
        try
        {
            // Define stream information
            string streamName = "UnityGameEvents";
            string streamType = "Markers"; // Common type for event markers
            int channelCount = 1 + MaxValueChannels; // EventType + Values
            double nominalRate = LSL.LSL.IRREGULAR_RATE; // Events are not periodic
            LSL.channel_format_t channelFormat = LSL.channel_format_t.cf_string; // Use strings for flexibility
            string sourceId = $"P{participantID}_C{conditionNumber}_S{sessionID}";

            streamInfo = new StreamInfo(streamName, streamType, channelCount, nominalRate, channelFormat, sourceId);

            // Add detailed metadata to the stream description
            XMLElement desc = streamInfo.desc();
            desc.append_child_value("participant_id", participantID);
            desc.append_child_value("condition_number", conditionNumber.ToString());
            desc.append_child_value("session_id", sessionID);
            desc.append_child_value("start_time_iso", DateTime.UtcNow.ToString("o")); // ISO 8601 format
            desc.append_child_value("unity_version", Application.unityVersion);
            desc.append_child_value("game_version", Application.version); // set this in Project Settings

            // Define channel labels
            XMLElement channels = desc.append_child("channels");
            channels.append_child("channel")
                    .append_child_value("label", "EventType")
                    .append_child_value("type", "Marker")
                    .append_child_value("unit", "string");

            for (int i = 1; i <= MaxValueChannels; i++)
            {
                channels.append_child("channel")
                        .append_child_value("label", $"Value{i}")
                        .append_child_value("type", "Data")
                        .append_child_value("unit", "string"); // Keep as string for flexibility or change if always numeric
            }

            // Create the outlet
            outlet = new StreamOutlet(streamInfo);

            Debug.Log($"LSL Stream '{streamName}' initialized with Source ID: {sourceId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing LSL stream: {e.Message}\n{e.StackTrace}");
            outlet = null; // Ensure outlet is null if initialization failed
        }
    }





    private string GenerateSessionID()
    {
        Guid guid = Guid.NewGuid();
        return DateTime.Now.ToString("ddMMyyyy_HHmmss") + "_" + guid.ToString();
    }

    
    /// <summary>
    /// Records data to the LSL stream.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="values"></param>
    public void RecordData(string eventType, params object[] values)
    {
        if (outlet == null)
        {
            // Debug.LogWarning("LSL Outlet not initialized. Cannot record data."); // Optional: Can be noisy
            return;
        }

        // Prepare the sample array
        currentSample[0] = eventType;

        // Fill value channels, padding with empty strings if necessary
        for (int i = 0; i < MaxValueChannels; i++)
        {
            if (i < values.Length && values[i] != null)
            {
                // Convert value to string. Handle potential formatting needs here.
                currentSample[i + 1] = values[i].ToString();
            }
            else
            {
                currentSample[i + 1] = ""; // Use empty string for unused/null values
            }
        }

        try
        {
            // Push the sample to the LSL outlet. LSL handles timestamping.
            outlet.push_sample(currentSample);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error pushing LSL sample: {e.Message}");
        }

    }
    



}
