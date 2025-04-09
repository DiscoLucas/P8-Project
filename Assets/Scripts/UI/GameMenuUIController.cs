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

    [Header("Content parrents")]
    [SerializeField]
    Transform statBlockParent;
    [Header("Prefabs")]
    [Tooltip("This can be found under the world canvas")]
    public GameObject statBlockPrefab;

    [Header("Infomations")]
    [SerializeField]
    string scoreText = "Score: ";
    void Start()
    {
        finishMenuUI.SetActive(false);
        startMenuUI.SetActive(true);    
    }

    [ContextMenu("Start Game")]
    public void startGame()
    {
        GameManager.Instance.StartGame();
        startMenuUI.SetActive(false);
    }

    public void openEndGameMenu(){
        finishMenuUI.SetActive(true);
        GameObject obj = Instantiate(statBlockPrefab, statBlockParent);
        obj.SetActive(true);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = scoreText + GameManager.Instance.orderManager.totalScore.ToString();
    }
    [ContextMenu("Restart Game")]
    public void closeEndGameMenu(){
        GameManager.Instance.loadCurrentScene();
    }

    private void OnEnable()
    {
        GameManager.Instance.onFinnishGame.AddListener(openEndGameMenu);
    }

    private void OnDisable()
    {
        GameManager.Instance.onFinnishGame.RemoveListener(openEndGameMenu);
    }
}
