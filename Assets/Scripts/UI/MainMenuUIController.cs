using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUIController : Singleton<MainMenuUIController>
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField participantIdField;
    [SerializeField] private TMP_Dropdown conditionDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI errorText;
    GameManager gameManager;

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
        startButton.onClick.AddListener(StartExperiment);

        gameManager = FindFirstObjectByType<GameManager>();
    }

    public void StartExperiment()
    {
        string participantId = participantIdField.text;
        
        // Validate input
        if (string.IsNullOrWhiteSpace(participantId))
        {
            ShowError("Please enter a participant ID");
            return;
        }

        // Get the selected condition
        Condition selectedCondition = (Condition)conditionDropdown.value;
        
        // Initialize PerformanceRecorder
        gameManager.performanceRecorder.InitializeParticpantID(participantId, (int)selectedCondition);
        
        // Start baseline recording
        gameManager.currentCondition = selectedCondition;
        gameManager.LoadBaselineScene();
    }
    
    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }
}