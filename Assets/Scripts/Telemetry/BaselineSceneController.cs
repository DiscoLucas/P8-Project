using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using System;

public class BaselineSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Button skipButton;

    private Coroutine countdownCoroutine;
    private bool finished = false;
    private float actualBaselineDuration; // Store the duration for this instance

    void Start()
    {
        // Get duration from GameManager
        actualBaselineDuration = (GameManager.Instance != null) ? GameManager.Instance.baselineDuration : 30f; // Default if GM not found

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

    private IEnumerator CountdownCoroutine()
    {
        float remainingTime = actualBaselineDuration;

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

        _ = EndBaselineAsync();
    }


    public void SkipBaseline()
    {
        Debug.Log("Baseline skipped by user.");
        _ = EndBaselineAsync();
    }


    private async Task EndBaselineAsync()
    {
        if (finished) return;
        finished = true;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        // Stop LabRecorder recording first
        if (LabRecorder.Instance != null)
        {
            try
            {
                bool stopped = await LabRecorder.Instance.StopRecordingAsync();
                if (!stopped) Debug.LogWarning("I don't think the recording stopped properly 🤔.");
                else Debug.Log("Recording stopped successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"AAAAAAAA I COULD NOT STOP THE RECORDING! {e.Message}");
            }
        }
        else
        {
            Debug.LogError("Where tf did the labrecorder go? Can't stop recording without it.");
        }

        if (countdownText != null)
        {
            countdownText.text = "Baseline Complete. Loading Game...";
        }
        if (skipButton != null)
        {
            skipButton.interactable = false; // Disable skip button
        }


        // Tell GameManager FSM to proceed to loading the game scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerFSM("BaselineComplete");
        }
        else
        {
            Debug.LogError("GameManager instance not found. Cannot trigger FSM.");
        }

    }
    private async Task EndBaseline()
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
            // Add duration as metadata if desired
            float elapsed = actualBaselineDuration - (countdownCoroutine == null ? 0 : GetRemainingTime()); // Calculate actual elapsed time
            PerformanceRecorder.Instance.RecordData("BaselineEnd", $"DurationSec:{elapsed:F1}");
            Debug.Log($"Sent BaselineEnd marker. Duration: {elapsed:F1}s");

            // Stop recording asynchronously, robot said this was better practice to avoid race conditions.
            try
            {
                bool stopped = await LabRecorder.Instance.StopRecordingAsync();
                if (!stopped) Debug.LogWarning("I don't think the recording stopped properly 🤔.");
            }
            catch (Exception e) 
            {
                Debug.LogError($"AAAAAAAA I COULD NOT STOP THE RECORDING! {e.Message}");
            }
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


        // Tell GameManager FSM to proceed to loading the game scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerFSM("BaselineComplete");
        }
        else
        {
            Debug.LogError("GameManager instance not found. Cannot trigger FSM.");
            // Handle this error case appropriately
        }
    }

    // Helper to get remaining time if needed for accurate duration logging on skip
    private float GetRemainingTime()
    {
        // TODO: Implement a way to track remaining time accurately
         // This requires tracking remaining time outside the coroutine or passing it
         // For simplicity, we'll assume full duration if skipped early,
         // or refine this if exact skipped duration is critical.
         return 0; // Placeholder - needs better tracking if exact skip time matters
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