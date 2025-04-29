using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using LSL;

/// <summary>
/// This class is responsible for initializing participant info and managing LSL streams
/// for recording continuous data and event markers.
/// </summary>
public class PerformanceRecorder : SingletonPersistent<PerformanceRecorder>
{
    private string participantID;
    private int conditionNumber;
    private int sessionNumber; // Added to store the session number
    private string sessionID; // Unique ID for this specific run/instance

    public GazePointRunner gazePointRunner;

    // Store multiple outlets and their info
    private Dictionary<string, StreamOutlet> outlets = new Dictionary<string, StreamOutlet>();
    private Dictionary<string, StreamInfo> streamInfos = new Dictionary<string, StreamInfo>();

    // Constants for common stream names
    private const string MarkerStreamName = "UnityMarkers";

    // Reusable buffer for marker stream
    private string[] markerSample = new string[1];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        // Initialization moved to InitializeParticipantID or Start
    }

    void Start()
    {
        // Ensure GazePointRunner is assigned
        if (gazePointRunner == null)
        {
            gazePointRunner = FindAnyObjectByType<GazePointRunner>();
            if (gazePointRunner == null)
            {
                Debug.LogWarning("GazePointRunner not found initially. Will attempt to find later if needed.");
            }
        }

        // Consider initializing streams here if participant ID is set externally before Start,
        // otherwise, initialization happens in InitializeParticipantID.
    }

    /// <summary>
    /// Initializes the participant ID, condition number, and session number.
    /// Generates a unique session instance ID and sets up the necessary LSL streams.
    /// This method MUST be called before recording any data or markers.
    /// </summary>
    /// <param name="partID">Unique ID for the given test participant</param>
    /// <param name="condition">Condition number</param>
    /// <param name="sessionNum">The session number for this experiment run</param>
    public void InitializeParticipant(string partID, int condition, int sessionNum) // Added sessionNum parameter
    {
        if (!string.IsNullOrEmpty(participantID))
        {
            Debug.LogWarning($"Participant already initialized with ID: {participantID}. Re-initializing.");
            // Consider cleanup of existing streams if re-initializing
            CleanupLSLStreams();
        }

        participantID = partID;
        conditionNumber = condition;
        sessionNumber = sessionNum; // Store the session number
        sessionID = GenerateSessionInstanceID(); // Keep this for unique run identification

        Debug.Log($"Initializing Participant: ID={participantID}, Condition={conditionNumber}, SessionNumber={sessionNumber}, SessionInstanceID={sessionID}");

        // Initialize standard streams
        SetupStandardStreams();

        // Record session start marker including the session number
        RecordMarker($"SessionStart_ParticipantID:{participantID}_Condition:{conditionNumber}_SessionNumber:{sessionNumber}_SessionInstanceID:{sessionID}");
    }

    /// <summary>
    /// Sets up the standard LSL streams (e.g., for markers).
    /// Call this after participant info is set.
    /// </summary>
    private void SetupStandardStreams()
    {
        // --- Initialize Marker Stream ---
        InitializeLSLStream(
            streamName: MarkerStreamName,
            streamType: "Markers",
            channelCount: 1, // Single marker string channel
            nominalRate: LSL.LSL.IRREGULAR_RATE, // Markers are typically irregular
            channelFormat: LSL.channel_format_t.cf_string,
            channelLabels: new string[] { "MarkerString" }
        );

        // --- Initialize Example Float Data Stream (Add more as needed) ---
        /*
        InitializeLSLStream(
            streamName: "GazePosition",
            streamType: "GazeData",
            channelCount: 3, // e.g., X, Y, Z or HitType, X, Y
            nominalRate: 60.0, // Match FixedUpdate or desired sampling rate
            channelFormat: LSL.channel_format_t.cf_float32,
            channelLabels: new string[] { "GazeX", "GazeY", "GazeConfidence" } // Example labels
        );
        */
    }

    /// <summary>
    /// Initializes a specific LSL data stream.
    /// </summary>
    /// <param name="streamName">Unique name for the stream (used as key).</param>
    /// <param name="streamType">LSL stream type (e.g., "Markers", "Gaze", "EEG").</param>
    /// <param name="channelCount">Number of channels in the stream.</param>
    /// <param name="nominalRate">Expected sampling rate (LSL.IRREGULAR_RATE for variable rate).</param>
    /// <param name="channelFormat">Data type of the channels (e.g., cf_float32, cf_string).</param>
    /// <param name="channelLabels">Array of names for each channel.</param>
    /// <param name="additionalMetadata">Optional dictionary for extra stream metadata.</param>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    public bool InitializeLSLStream(string streamName, string streamType, int channelCount, double nominalRate, LSL.channel_format_t channelFormat, string[] channelLabels, Dictionary<string, string> additionalMetadata = null)
    {
        //Debug.Log("participantID: " + participantID + "Session ID:  " + sessionID);
        if (string.IsNullOrEmpty(participantID) || sessionNumber <= 0) // Check sessionNumber instead of sessionID
        {
            Debug.LogError($"Cannot initialize stream '{streamName}'. Participant/Session not initialized yet. Call InitializeParticipant first.");
            return false;
        }

        if (outlets.ContainsKey(streamName))
        {
            Debug.LogWarning($"Stream '{streamName}' already initialized. Skipping.");
            return true; // Already exists
        }

        if (channelLabels == null || channelLabels.Length != channelCount)
        {
             Debug.LogError($"Cannot initialize stream '{streamName}'. Channel count ({channelCount}) does not match number of channel labels ({channelLabels?.Length ?? 0}).");
             return false;
        }

        try
        {
            // Modified sourceId to include the actual session number
            string sourceId = $"P{participantID}_C{conditionNumber}_S{sessionNumber}_{streamName}";
            StreamInfo info = new StreamInfo(streamName, streamType, channelCount, nominalRate, channelFormat, sourceId);

            // Add common metadata
            XMLElement desc = info.desc();
            desc.append_child_value("participant_id", participantID);
            desc.append_child_value("condition_number", conditionNumber.ToString());
            desc.append_child_value("session_number", sessionNumber.ToString()); // Add session number metadata
            desc.append_child_value("session_instance_id", sessionID); // Keep unique instance ID
            desc.append_child_value("start_time_iso", DateTime.UtcNow.ToString("o")); // ISO 8601 format
            desc.append_child_value("unity_version", Application.unityVersion);
            desc.append_child_value("game_version", Application.version); // Set this in Project Settings

            // Add channel labels
            XMLElement channels = desc.append_child("channels");
            for (int i = 0; i < channelCount; i++)
            {
                channels.append_child("channel")
                        .append_child_value("label", channelLabels[i])
                        .append_child_value("type", streamType) // Use stream type or be more specific if needed
                        .append_child_value("unit", GetUnitForFormat(channelFormat)); // Assign default units based on format
            }

            // Add any additional custom metadata
            if (additionalMetadata != null)
            {
                XMLElement meta = desc.append_child("additional_metadata");
                foreach (var kvp in additionalMetadata)
                {
                    meta.append_child_value(kvp.Key, kvp.Value);
                }
            }

            // Create the outlet
            StreamOutlet outlet = new StreamOutlet(info);

            // Store the info and outlet
            streamInfos[streamName] = info;
            outlets[streamName] = outlet;

            Debug.Log($"LSL Stream '{streamName}' initialized. Source ID: {sourceId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing LSL stream '{streamName}': {e.Message}\n{e.StackTrace}");
            // Clean up partial state if necessary
            if (streamInfos.ContainsKey(streamName)) streamInfos.Remove(streamName);
            if (outlets.ContainsKey(streamName)) outlets.Remove(streamName);
            return false;
        }
    }

    // Helper to get default unit string based on LSL format
    private string GetUnitForFormat(LSL.channel_format_t format)
    {
        switch (format)
        {
            case LSL.channel_format_t.cf_float32: return "float";
            case LSL.channel_format_t.cf_double64: return "double";
            case LSL.channel_format_t.cf_string: return "string";
            case LSL.channel_format_t.cf_int32: return "integer";
            case LSL.channel_format_t.cf_int16: return "integer";
            case LSL.channel_format_t.cf_int8: return "integer";
            case LSL.channel_format_t.cf_int64: return "integer";
            default: return "unknown";
        }
    }

    /// <summary>
    /// Generates a unique ID for this specific instance/run of the session.
    /// </summary>
    /// <returns>A unique string identifier.</returns>
    private string GenerateSessionInstanceID() // Renamed from GenerateSessionID
    {
        Guid guid = Guid.NewGuid();
        // Using 'N' format for GUID for compactness
        return DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + guid.ToString("N");
    }

    /// <summary>
    /// Records (pushes) a string marker to the dedicated marker LSL stream.
    /// </summary>
    /// <param name="markerString">The marker message to send.</param>
    public void RecordMarker(string markerString)
    {
        RecordMarker(MarkerStreamName, markerString);
    }

    /// <summary>
    /// Records (pushes) a string marker to a specified LSL stream (must be string format).
    /// </summary>
    /// <param name="targetStreamName">The name of the string-based stream to push to.</param>
    /// <param name="markerString">The marker message to send.</param>
    public void RecordMarker(string targetStreamName, string markerString)
    {
        if (outlets.TryGetValue(targetStreamName, out StreamOutlet outlet))
        {
             if (streamInfos.TryGetValue(targetStreamName, out StreamInfo info) && info.channel_format() != LSL.channel_format_t.cf_string)
             {
                 Debug.LogError($"Attempted to send string marker to stream '{targetStreamName}' which has format {info.channel_format()}. Use RecordStreamData for non-string streams.");
                 return;
             }

            try
            {
                markerSample[0] = markerString; // Use reusable buffer
                outlet.push_sample(markerSample);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error pushing LSL marker to stream '{targetStreamName}': {e.Message}");
            }
        }
        else
        {
             Debug.LogWarning($"LSL Outlet '{targetStreamName}' not found or not initialized. Cannot record marker.");
        }
    }

    /// <summary>
    /// Records (pushes) a sample of float data to the specified LSL stream.
    /// The stream must have been initialized with channel_format_t.cf_float32
    /// and the correct channel count.
    /// </summary>
    /// <param name="streamName">The name of the target LSL stream.</param>
    /// <param name="dataSample">The float array data sample to push.</param>
    public void RecordStreamData(string streamName, float[] dataSample)
    {
        if (outlets.TryGetValue(streamName, out StreamOutlet outlet))
        {
            // Optional: Add checks for channel count and format match for robustness
            // if (streamInfos.TryGetValue(streamName, out StreamInfo info)) {
            //     if (info.channel_format() != LSL.channel_format_t.cf_float32) { /* Error */ }
            //     if (info.channel_count() != dataSample.Length) { /* Error */ }
            // }

            try
            {
                outlet.push_sample(dataSample);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error pushing LSL float sample to stream '{streamName}': {e.Message}");
            }
        }
        else
        {
             Debug.LogWarning($"LSL Outlet '{streamName}' not found or not initialized. Cannot record float data.");
        }
    }

     /// <summary>
    /// Records (pushes) a sample of double data to the specified LSL stream.
    /// The stream must have been initialized with channel_format_t.cf_double64
    /// and the correct channel count.
    /// </summary>
    /// <param name="streamName">The name of the target LSL stream.</param>
    /// <param name="dataSample">The double array data sample to push.</param>
    public void RecordStreamData(string streamName, double[] dataSample)
    {
        if (outlets.TryGetValue(streamName, out StreamOutlet outlet))
        {
            try
            {
                outlet.push_sample(dataSample);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error pushing LSL double sample to stream '{streamName}': {e.Message}");
            }
        }
        else
        {
             Debug.LogWarning($"LSL Outlet '{streamName}' not found or not initialized. Cannot record double data.");
        }
    }

     /// <summary>
    /// Records (pushes) a sample of integer data to the specified LSL stream.
    /// The stream must have been initialized with channel_format_t.cf_int32
    /// and the correct channel count.
    /// </summary>
    /// <param name="streamName">The name of the target LSL stream.</param>
    /// <param name="dataSample">The int array data sample to push.</param>
    public void RecordStreamData(string streamName, int[] dataSample)
    {
        if (outlets.TryGetValue(streamName, out StreamOutlet outlet))
        {
            try
            {
                outlet.push_sample(dataSample);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error pushing LSL int sample to stream '{streamName}': {e.Message}");
            }
        }
        else
        {
             Debug.LogWarning($"LSL Outlet '{streamName}' not found or not initialized. Cannot record int data.");
        }
    }

    /// <summary>
    /// Records (pushes) a sample of integer data to the specified LSL stream.
    /// The stream must have been initialized with channel_format_t.cf_string
    /// and the correct channel count.
    /// </summary>
    /// <param name="streamName">The name of the target LSL stream.</param>
    /// <param name="dataSample">The int array data sample to push.</param>
    public void RecordStreamData(string streamName, string[] dataSample)
    {
        if (outlets.TryGetValue(streamName, out StreamOutlet outlet))
        {
            try
            {
                outlet.push_sample(dataSample);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error pushing LSL string sample to stream '{streamName}': {e.Message}");
            }
        }
        else
        {
             Debug.LogWarning($"LSL Outlet '{streamName}' not found or not initialized. Cannot record int data.");
        }
    }

    // Add similar RecordStreamData overloads for other LSL types (short[], string[], char[]) if needed.

    /// <summary>
    /// Disposes of all active LSL outlets and clears dictionaries.
    /// </summary>
    private void CleanupLSLStreams()
    {
        foreach (var kvp in outlets)
        {
            try
            {
                Debug.Log($"Disposing LSL outlet for stream '{kvp.Key}'...");
                // LSL.StreamOutlet doesn't implement IDisposable directly in the C# wrapper typically used.
                // Proper cleanup might involve ensuring no more pushes happen and letting GC handle it,
                // or checking the specific LSL wrapper version for a Dispose/Close method.
                // For now, we just clear the references.
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during cleanup of LSL outlet '{kvp.Key}': {e.Message}");
            }
        }
        outlets.Clear();
        streamInfos.Clear();
        Debug.Log("LSL streams cleared.");
    }
}
