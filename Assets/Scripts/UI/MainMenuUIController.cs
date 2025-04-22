using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

/// <summary>
/// Ensure LabRecorder is ready and configure the LabRecorderController in your Unity scene.
/// </summary>
/// <remarks>
/// <para>**Ensure LabRecorder is Ready**</para>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>Run LabRecorder</c>: Start the LabRecorder application on the machine specified by <c>labRecorderHost</c> (usually the same machine or one on the same network).
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>Enable RCS</c>: In LabRecorder’s UI, <b>you MUST check the “Enable RCS” checkbox</b>. Otherwise, the TCP connection will fail.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>Firewall</c>: Ensure firewalls (Windows Firewall, etc.) allow incoming connections on the specified <c>labRecorderPort</c> (default 22345) on the machine running LabRecorder.
///     </description>
///   </item>
/// </list>
/// 
/// <para>**Add Controller to Scene**</para>
/// <list type="bullet">
///   <item>
///     <description>
///       Add the <c>LabRecorder.cs</c> script to a persistent GameObject in your scene (for example, the one holding your <c>GameManager</c> or <c>PerformanceRecorder</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       Configure the <c>Study Root</c> path in the Inspector to point to a valid directory on the machine where LabRecorder is running. Use the correct path format for that OS (e.g., <c>C:\LSL_Data</c> for Windows, <c>/home/user/lsl_data</c> for Linux).
///     </description>
///   </item>
///   <item>
///     <description>
///       Adjust the <c>Filename Template</c> if desired.
///     </description>
///   </item>
/// </list>
/// 
/// <para>When you enter the participant ID, select a condition, and click “Start” in your Unity UI, the controller will:</para>
/// <list type="number">
///   <item><description>Initialize the LSL stream via <c>PerformanceRecorder</c>.</description></item>
///   <item><description>Connect to LabRecorder via TCP.</description></item>
///   <item><description>Tell LabRecorder to select all streams.</description></item>
///   <item><description>Set the filename based on the template and the provided participant ID, session (“1”), and condition name.</description></item>
///   <item><description>Tell LabRecorder to start recording.</description></item>
///   <item><description>Load your baseline/game scene.</description></item>
///   <item><description>When the Unity application quits, send a “stop” command to LabRecorder.</description></item>
/// </list>
/// </remarks>
public class MainMenuUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField participantIdField;
    [SerializeField] private TMP_Dropdown conditionDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingOverlay; // Optional: UI to show while connecting/starting

    private void Start()
    {
        // Setup the condition dropdown
        conditionDropdown.ClearOptions();
        conditionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {   // Obscured condition names as to not affect the experiment
            "Condition 0",
            "Condition 1",
            "Condition 2"
        });

        errorText.gameObject.SetActive(false);
        startButton.onClick.AddListener(HandleStartExperiment);
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }

    // Wrapper to handle async logic for the button click
    private void HandleStartExperiment()
    {
        Debug.Log("start button clicked");
        // Disable button to prevent multiple clicks
        startButton.interactable = false;
        if (loadingOverlay) loadingOverlay.SetActive(true);

        // Run the async part without blocking the main thread
        _ = StartExperimentAsync(); // Discard Task with _
    }

    private async Task StartExperimentAsync() // Make the method async
    {
        Debug.Log("Starting experiment...");
        string participantId = participantIdField.text; // TODO: automate this
        errorText.gameObject.SetActive(false);

        // Validate input
        if (string.IsNullOrWhiteSpace(participantId))
        {
            ShowError("Please enter a participant ID");
            ResetUIState();
            return;
        }

        // Get the selected condition
        int conditionIndex = conditionDropdown.value;
        string conditionNameForTask = conditionDropdown.options[conditionIndex].text.Replace(" ", ""); // Remove spaces for filename
        Condition selectedConditionEnum = (Condition)conditionIndex; // Assuming enum matches dropdown order

        // --- LSL and LabRecorder Initialization ---
        // 1. Initialize your LSL stream via PerformanceRecorder
        if (PerformanceRecorder.Instance == null)
        {
             ShowError("PerformanceRecorder not found!");
             ResetUIState();
             return;
        }
        PerformanceRecorder.Instance.InitializeParticipantID(participantId, conditionIndex);
        await Task.Delay(200); // Give LSL stream a moment to register on the network

        // 2. Configure and Start LabRecorder
        if (LabRecorder.Instance == null)
        {
             ShowError("LabRecorderController not found!");
             ResetUIState();
             return;
        }

        // Define session number (e.g., always "1" for this setup, or get from elsewhere)
        string sessionNumber = "1"; // TODO: make this smarter

        bool started = await LabRecorder.Instance.ConfigureAndStartRecordingAsync(participantId, sessionNumber, conditionNameForTask);

        if (!started)
        {
            ShowError("Failed to start LabRecorder. Check connection and settings.");
            ResetUIState();
            return;
        }
        // --- End LSL/LabRecorder ---

        // Proceed by triggering the FSM in GameManager
        if (GameManager.Instance != null)
        {
            // Store the selected condition Enum in GameManager for later use
            GameManager.Instance.currentCondition = selectedConditionEnum;
            // Trigger the FSM to start the process (loading baseline)
            GameManager.Instance.TriggerFSM("StartExperiment");
        }
        else
        {
             ShowError("GameManager not found!");
             await LabRecorder.Instance.StopRecordingAsync(); // Stop recording if game can't start
             ResetUIState();
             return;
        }

        // Optionally hide the main menu UI here if loading happens immediately
        // gameObject.SetActive(false);
        if (loadingOverlay) loadingOverlay.SetActive(false); // Hide overlay once loading starts
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        Debug.LogError(message); // Also log error
    }

    private void ResetUIState()
    {
         // Re-enable button and hide loading overlay on error
        startButton.interactable = true;
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }
}