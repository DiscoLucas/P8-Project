using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class LabRecorder : SingletonPersistent<LabRecorder>
{
    [Header("Connection Settings")]
    public string labRecorderHost = "localhost";
    public int labRecorderPort = 22345;
    public bool debugLogging = false;

    [Header("Recording Settings")]
    [Tooltip("Root directory for recordings on the machine that runs LabRecorder")]
    public string studyRoot; // I don't remeber how to do relative paths in Unity ;_;
    [Tooltip("Filename template using LabRecorder placeholders (%p=participant, %s=session, %b=task/block, %n=run, %m=modality).")]
    public string filenameTemplate = "sub-%p\\ses-%s\\sub-%p_ses-%s_task-%b_run-01_beh.xdf";

    TcpClient client;
    NetworkStream stream;
    bool isConnected = false;
    bool isRecording = false;

    /// <summary>
    /// Attempts to connect to LabRecorder RCS.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> ConnectAsync()
    {
        if (isConnected) return true;

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(labRecorderHost, labRecorderPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log($"Successfully connected to LabRecorder RCS at {labRecorderHost}:{labRecorderPort} \n ready to roll!");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Bruh {e.Message} straight up ghosted you. nothing to see at {labRecorderHost}:{labRecorderPort}. Did your dumbass forget to start LabRecorder with RCS enabled?");
            client?.Close();
            client = null;
            stream = null;
            isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Sends a string command to the LabRecorder
    /// </summary>
    /// <param name="command">The command to send (e.g., "start", "stop").</param>
    /// <returns>True if the command was sent successfully, false otherwise.</returns>
    async Task<bool> SendCommandAsync(string command)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogError("How tf am i supposed to send a command to something that I'm not connected to?");
            return false;
        }

        try
        {
            string commandToSend = command.Trim() + "\n";
            byte[] data = Encoding.UTF8.GetBytes(commandToSend);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync(); // Make sure the data is sent now
            Debug.Log($"Sliding into LabRecorder DMs with: {command.Trim()} ✨");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Yo, your command '{command.Trim()}' just got ghosted by LabRecorder RCS. Error be like: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Configure the LabRecorder filename and starts recording.
    /// </summary>
    /// <param name="participantID">Participant ID (for %p).</param>
    /// <param name="sessionNumber">Session number (for %s).</param>
    /// <param name="taskName">Task or condition name (for %b).</param>
    /// <returns></returns>
    public async Task<bool> ConfigureAndStartRecordingAsync(string participantID, string sessionNumber, string taskName)
    {
        if (isRecording)
        {
            Debug.LogWarning("Bruh, you already started recording. You can't just start over like that.");
            return false;
        }

        if (!isConnected)
        {
            Debug.Log("Not connected yet fam, let's see if you pass the vibe check first...");
            if (!await ConnectAsync())
            {
                Debug.LogError("Bruh, you really thought you could just slide into LabRecorder's DMs without a connection?");
                return false;
            }
        }

        // 1. Select streams (optional, "select all" is usually fine)
        if (!await SendCommandAsync("select all")) return false;
        await Task.Delay(100); // Give it a moment to process

        // 2. Set the filename using a placeholder
        // Ensure the studyRoot uses double backslashes for Windows paths
        string formattedRoot = studyRoot.Replace('/', '\\');
        string filenameCommand = $"filename {{root:{formattedRoot}}}  {{template:{filenameTemplate}}} {{participant:{participantID}}} {{session:{sessionNumber}}} {{task:{taskName}}}";
        await Task.Delay(100);

        // 3. start recording
        if (!await SendCommandAsync("start")) return false;

        isRecording = true;
        Debug.Log("Damn I didn't know you were chill like that, recording started 😎");
        return true;
    }

    /// <summary>
    /// Stops the current LabRecorder recording.
    /// </summary>
    /// <returns>True if the stop command was sent successfully, false otherwise.</returns>
    public async Task<bool> StopRecordingAsync()
    {
        if (!isRecording)
        {
            if (debugLogging) Debug.Log("Bruh, you can't stop something that ain't even started yet. You trippin'?");
            
            return true; // Not an error if we weren't recording
        }

        if (!await SendCommandAsync("stop"))
        {
            // Even if sending fails, update state
            isRecording = false;
            return false;
        }

        isRecording = false;
        Debug.Log("LabRecorder recording stopped. You did it, fam! 🎉");
        // Optionally disconnect after stopping
        // Disconnect();
        return true;
    }

    /// <summary>
    /// Disconnects from the LabRecorder RCS.
    /// </summary>
    public void Disconnect()
    {
        if (!isConnected) return;

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"You know what? I don't even care. {e.Message} is just a minor inconvenience.");
        }
        finally
        {
            stream = null;
            client = null;
            isConnected = false;
            isRecording = false; // Ensure recording state is reset on disconnect
            Debug.Log("Labrecorder has now fully dipped.");
        }
    }

    void OnApplicationQuit()
    {
        // Attempt to stop recording cleanly if it's running
        if (isRecording)
        {
            // Run synchronously on quit if possible, or log intent
            Debug.Log("Ight, I'm out. Stopping recording before I leave...");
            // Best effort synchronous send on quit (async might not complete)
            try
            {
                 if (isConnected && stream != null)
                 {
                    string commandToSend = "stop\n";
                    byte[] data = Encoding.UTF8.GetBytes(commandToSend);
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                 }
            } catch (Exception e) { Debug.LogError($"Big oof, stopping on quit: {e.Message}"); }
        }
        Disconnect();
    }

    // Also disconnect if the GameObject is destroyed or disabled
    void OnDestroy()
    {
        Disconnect();
    }

    void OnDisable()
    {
        // Decide if we want to stop recording/disconnect when this specific object is disabled
        // StopRecordingAsync(); // Maybe too aggressive?
        // Disconnect();
    }
    
}