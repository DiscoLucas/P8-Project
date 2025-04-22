using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;
using System.Linq.Expressions;

public class GameMenuUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startMenuUI;
    public GameObject finishMenuUI;

    [Header("Content Parents")]
    [SerializeField] Transform statBlockParent;

    [Header("Prefabs")]
    [Tooltip("This can be found under the world canvas")]
    public GameObject statBlockPrefab;

    [Header("Information")]
    [SerializeField] string scoreText = "Score: ";

    private bool isFinishMenuActive = false;

    void Start()
    {
        if (finishMenuUI != null) finishMenuUI.SetActive(false);
        if (startMenuUI != null) startMenuUI.SetActive(true);
        isFinishMenuActive = false;
    }

    [ContextMenu("Start Game")]
    public void startGame()
    {
        if (startMenuUI != null) startMenuUI.SetActive(false);
    }

    public void openEndGameMenu()
    {
        if (finishMenuUI == null || statBlockPrefab == null || statBlockParent == null)
        {
            Debug.LogError("Finish Menu UI elements not assigned in GameMenuUIController!");
            return;
        }

        finishMenuUI.SetActive(true);
        isFinishMenuActive = true;

        foreach (Transform child in statBlockParent)
        {
            Destroy(child.gameObject);
        }

        GameObject obj = Instantiate(statBlockPrefab, statBlockParent);
        obj.SetActive(true);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            float score = (GameManager.Instance != null && GameManager.Instance.orderManager != null)
                        ? GameManager.Instance.orderManager.totalScore : 0;
            text.text = scoreText + score.ToString();
        }
        else
        {
            Debug.LogError("StatBlockPrefab is missing TextMeshProUGUI component!");
            Destroy(obj);
        }
    }

    [ContextMenu("Restart Game")]
    public void RestartGame()
    {
        if (finishMenuUI != null) finishMenuUI.SetActive(false);
        isFinishMenuActive = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerFSM("RestartGame");
        }
        else
        {
            Debug.LogError("GameManager not found, cannot restart game via FSM.");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onFinnishGame.AddListener(openEndGameMenu);
        }
        else
        {
            StartCoroutine(DelayedSubscribe());
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onFinnishGame.RemoveListener(openEndGameMenu);
        }
    }

    private System.Collections.IEnumerator DelayedSubscribe()
    {
        yield return null;
        yield return null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onFinnishGame.AddListener(openEndGameMenu);
        }
        else
        {
            Debug.LogWarning("GameManager instance still not found after delay. Cannot subscribe to onFinnishGame event.");
        }
    }
}
