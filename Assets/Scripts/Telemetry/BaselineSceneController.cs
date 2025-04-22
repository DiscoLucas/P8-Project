using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BaselineSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Button skipButton;

    [Header("Settings")]
    [SerializeField] private float baselineDuration = 30f; // Default duration

    private Coroutine countdownCoroutine;
    private bool finished = false;

    void Start()
    {
        // Optionally get duration from GameManager if it's configurable there
        if (GameManager.Instance != null)
        {
            baselineDuration = GameManager.Instance.baselineDuration;
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipBaseline);
        }
        else
        {
            Debug.LogError("Skip Button is not assigned in BaselineSceneController.");
        }

        if (countdownText == null)
        {
            Debug.LogError("Countdown Text is not assigned in BaselineSceneController.");
        }

        // Send Baseline Start Marker via LSL
        if (PerformanceRecorder.Instance != null)
        {
            PerformanceRecorder.Instance.RecordData("BaselineStart");
            Debug.Log("Sent BaselineStart marker.");
        }
        else
        {
            Debug.LogError("PerformanceRecorder instance not found. Cannot send BaselineStart marker.");
        }

        // Start the countdown
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    void Update()
    {
        // Update logic can go here if needed, but countdown is handled by coroutine
    }

    private IEnumerator CountdownCoroutine()
    {
        float remainingTime = baselineDuration;

        while (remainingTime > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Baseline Recording: {Mathf.CeilToInt(remainingTime)}s";
            }
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        // Timer finished
        EndBaseline();
    }

    public void SkipBaseline()
    {
        Debug.Log("Baseline skipped by user.");
        EndBaseline();
    }

    private void EndBaseline()
    {
        // Prevent running multiple times
        if (finished) return;
        finished = true;

        // Stop the countdown if it's still running
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        // Send Baseline End Marker via LSL
        if (PerformanceRecorder.Instance != null)
        {
            PerformanceRecorder.Instance.RecordData("BaselineEnd");
            Debug.Log("Sent BaselineEnd marker.");
        }
        else
        {
            Debug.LogError("PerformanceRecorder instance not found. Cannot send BaselineEnd marker.");
        }

        // Update UI one last time
         if (countdownText != null)
         {
            countdownText.text = "Baseline Complete. Loading Game...";
         }
         if (skipButton != null)
         {
             skipButton.interactable = false; // Disable skip button
         }


        // Tell GameManager to load the actual game scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("GameManager instance not found. Cannot load game scene.");
            // Handle this error case appropriately, maybe load a default scene or show an error message
        }
    }

     void OnDestroy()
     {
         // Clean up listeners if the object is destroyed unexpectedly
         if (skipButton != null)
         {
             skipButton.onClick.RemoveListener(SkipBaseline);
         }
     }
}