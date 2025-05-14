using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor; // Required for checking if in editor play mode
#endif

public class MainMenuUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField participantIdField;
    [SerializeField] private TMP_Dropdown conditionDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TMP_InputField sessionNumberField; // Added session number field
    [SerializeField] private Button incrementSessionButton; // Added increment button
    [SerializeField] private Button decrementSessionButton; // Added decrement button

    private void Start()
    {
        // Setup the condition dropdown
        conditionDropdown.ClearOptions();
        conditionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {   // Obscured condition names as to not affect the experiment
            "Condition 0",
            "Condition 1",
            "Condition 2",
            "Condition low dif",
            "Condition high dif",
        });

        errorText.gameObject.SetActive(false);
        startButton.onClick.AddListener(HandleStartExperiment);

        // In Build, initialize to 1 and enable controls
        sessionNumberField.text = "1";
        sessionNumberField.interactable = true; // Ensure interactable in build
        incrementSessionButton.interactable = true;
        decrementSessionButton.interactable = true; // Start disabled as initial value is 1
        decrementSessionButton.onClick.AddListener(DecrementSessionNumber);
        incrementSessionButton.onClick.AddListener(IncrementSessionNumber);
        sessionNumberField.onValueChanged.AddListener(ValidateSessionNumberInput); // Add listener for direct input validation
        UpdateDecrementButtonState(1); // Initial state check for decrement button

        if (loadingOverlay) loadingOverlay.SetActive(false);
    }

    // Wrapper to handle async logic for the button click
    private void HandleStartExperiment()
    {
        // Disable button to prevent multiple clicks
        startButton.interactable = false;
        if (loadingOverlay) loadingOverlay.SetActive(true);

        _ = StartExperimentSetupAsync(); // Discard Task with _
    }

    private async Task StartExperimentSetupAsync() // Make the method async
    {
        string participantId = participantIdField.text;
        errorText.gameObject.SetActive(false);

        // Validate participant ID input
        if (string.IsNullOrWhiteSpace(participantId))
        {
            ShowError("Please enter a participant ID");
            ResetUIState();
            return;
        }

        // Validate and parse session number input
        int sessionNumber;
        if (!int.TryParse(sessionNumberField.text, out sessionNumber) || sessionNumber < 1)
        {
            ShowError("Invalid Session Number. Please enter a positive integer.");
            ResetUIState();
            return;
        }

        // Get the selected condition
        int conditionIndex = conditionDropdown.value;
        Condition selectedConditionEnum = (Condition)conditionIndex; // Assuming enum matches dropdown order

        // --- LSL Initialization ---
        // 1. Initialize LSL stream so its ready for LabRecorder
        if (PerformanceRecorder.Instance == null)
        {
             ShowError("PerformanceRecorder not found!");
             ResetUIState();
             return;
        }
        // Pass parsed sessionNumber to InitializeParticipant
        PerformanceRecorder.Instance.InitializeParticipant(participantId, conditionIndex, sessionNumber);
        await Task.Delay(200); // Give LSL stream a moment to register on the network

        if (GameManager.Instance != null)
        {
            // store the selected condition enum, participant ID, and parsed session number in GameManager
            GameManager.Instance.participantID = participantId;
            GameManager.Instance.currentCondition = selectedConditionEnum;
            GameManager.Instance.sessionNumber = sessionNumber; // Use parsed session number
            //Debug.Log("Attempting to trigger FSM on GameManager instance."); // Added log
            GameManager.Instance.TriggerFSM("StartExperiment");
        }
        else
        {
            ShowError("GameManager not found!");
            ResetUIState();
            return;
        }

        // might also want to hide the main menu UI here if loading happens immediately
        // gameObject.SetActive(false);
        if (loadingOverlay) loadingOverlay.SetActive(false); // Hide overlay once loading starts
    }

    private void IncrementSessionNumber()
    {
        if (int.TryParse(sessionNumberField.text, out int currentSession))
        {
            currentSession++;
            sessionNumberField.text = currentSession.ToString();
            UpdateDecrementButtonState(currentSession);
        }
        else
        {
            // Handle potential invalid text in the field if manually edited
            sessionNumberField.text = "1";
            UpdateDecrementButtonState(1);
        }
    }

    private void DecrementSessionNumber()
    {
        if (int.TryParse(sessionNumberField.text, out int currentSession))
        {
            if (currentSession > 1)
            {
                currentSession--;
                sessionNumberField.text = currentSession.ToString();
                UpdateDecrementButtonState(currentSession);
            }
        }
        else
        {
            // Handle potential invalid text in the field if manually edited
            sessionNumberField.text = "1";
            UpdateDecrementButtonState(1);
        }
    }

    // Optional: Validate input if user types directly into the field
    private void ValidateSessionNumberInput(string input)
    {
#if !UNITY_EDITOR // Only run validation logic in builds
        if (int.TryParse(input, out int currentSession))
        {
            if (currentSession < 1)
            {
                sessionNumberField.text = "1"; // Reset to 1 if below 1
                UpdateDecrementButtonState(1);
            }
            else
            {
                UpdateDecrementButtonState(currentSession);
            }
        }
        else if (!string.IsNullOrEmpty(input)) // If not empty and not a valid int
        {
             // Optionally revert to last valid number or reset to 1
             // For simplicity, let's reset to 1 if input is invalid
             sessionNumberField.text = "1";
             UpdateDecrementButtonState(1);
        }
        else // Handle empty input case if needed
        {
             // Maybe default to 1 or show an error, for now, disable decrement
             UpdateDecrementButtonState(0); // Pass 0 or similar to indicate invalid/empty state
        }
#endif
    }

     private void UpdateDecrementButtonState(int currentSession)
     {
#if !UNITY_EDITOR
        decrementSessionButton.interactable = currentSession > 1;
#endif
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
#if !UNITY_EDITOR
        // Also re-enable session buttons if they were active
        if (int.TryParse(sessionNumberField.text, out int currentSession))
        {
             UpdateDecrementButtonState(currentSession);
        }
        else
        {
             UpdateDecrementButtonState(1); // Default state on error
        }
        incrementSessionButton.interactable = true;
        sessionNumberField.interactable = true;
#endif
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }
}