using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks; // Required for async

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
        conditionDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Condition 0", 
            "Condition 1",
            "Condition 2"
        });

        errorText.gameObject.SetActive(false);
        startButton.onClick.AddListener(HandleStartExperiment); // Changed listener name
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }

    // Wrapper to handle async logic for the button click
    private void HandleStartExperiment()
    {
        // Disable button to prevent multiple clicks
        startButton.interactable = false;
        if (loadingOverlay) loadingOverlay.SetActive(true);

        // Run the async part without blocking the main thread
        _ = StartExperimentAsync(); // Discard Task with _
    }

    private async Task StartExperimentAsync() // Make the method async
    {
        string participantId = participantIdField.text;
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
        string conditionName = conditionDropdown.options[conditionIndex].text; // Use text for task name
        Condition selectedCondition = (Condition)conditionIndex; // Assuming enum matches dropdown order

        // --- LSL and LabRecorder Initialization ---
        // 1. Initialize your LSL stream via PerformanceRecorder
        // Make sure PerformanceRecorder is ready
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
        string sessionNumber = "1";

        bool started = await LabRecorder.Instance.ConfigureAndStartRecordingAsync(participantId, sessionNumber, conditionName);

        if (!started)
        {
            ShowError("Failed to start LabRecorder. Check connection and settings.");
            // Optionally try to stop/disconnect LSL stream if needed
            ResetUIState();
            return;
        }
        // --- End LSL/LabRecorder ---


        // Proceed with loading game scenes
        // Initialize GameManager settings if needed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentCondition = selectedCondition; // Pass condition to GameManager
            GameManager.Instance.LoadBaselineScene(); // Or directly to the game scene
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