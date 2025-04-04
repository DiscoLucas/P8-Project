using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;
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

    [SerializedDictionary("Key", "Cocktail Recipe")]
    public SerializedDictionary<int, string> infomations;
    void Start()
    {
        finishMenuUI.SetActive(false);
        startMenuUI.SetActive(true);    
    }

    public void startGame()
    {
        GameManager.Instance.StartGame();
        startMenuUI.SetActive(false);
    }

    public void openEndGameMenu(){
        finishMenuUI.SetActive(true);
        GameObject obj = Instantiate(statBlockPrefab, statBlockParent);
        obj.SetActive(true);
        Text text = obj.GetComponentInChildren<Text>();
        text.text = infomations[0] + GameManager.Instance.orderManager.totalScore.ToString();        
    }

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
