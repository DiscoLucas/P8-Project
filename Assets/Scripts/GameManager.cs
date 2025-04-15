using System.Collections;
using UnityEngine;
using UnityHFSM;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;
public class GameManager : SingletonPersistent<GameManager>
{
    public Condition currentCondition;
    private StateMachine fsm;
    private InputSystem_Actions inputAction;
    [Header("Events")]
    [Tooltip("Called when the game starts.")]
    public UnityEvent onGameStart;
    [Tooltip("Called when the game ends.")]
    public UnityEvent onFinnishGame;
    [Tooltip("Called when the game change from one phase to anthor.")]
    public UnityEvent onGamePhaseChange;

    [Header("Game Settings")]
    public bool neverEnd = false;

    [Header("Cleaning")]
    public List<GameObject> objectsToClean;
    [SerializeField] int maxAllowedObjects = 100;

    [Header("Managers")]
    public OrderManager orderManager;
    public PhaseManager phaseManager;
    public RecipeManager recipeManager;
    public PerformanceRecorder performanceRecorder;
    void Start()
    {
        // get managers
        orderManager = FindAnyObjectByType<OrderManager>();
        phaseManager = FindAnyObjectByType<PhaseManager>();
        recipeManager = FindAnyObjectByType<RecipeManager>();
        performanceRecorder = FindAnyObjectByType<PerformanceRecorder>();

        inputAction = new InputSystem_Actions();
        inputAction.Enable();
        bool isPaused = false;
        inputAction.UI.Pause.performed += ctx =>
        {
            isPaused = !isPaused;
            fsm.Trigger("Toggle Pause");
        };

        fsm = new StateMachine();


#if UNITY_EDITOR // If the active scene isn't "MainMenu", set the start state to "Game" in the editor.
        // Since the initial scene will always be main menu in builds, this doesn't need to be in the build.
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            fsm.SetStartState("Game");
        }
#endif

        fsm.AddState("Main Menu",
            onEnter => 
            {
                orderManager.gameObject.SetActive(false); 
                phaseManager.gameObject.SetActive(false); 
                recipeManager.gameObject.SetActive(false); 
            }
        );
        fsm.AddState("Load Game",
            onEnter => StartCoroutine(LoadScene("Mitchell"))); // hehe my scene is the main scene >:)
        
        fsm.AddState("Game",
            onEnter =>
            {
                orderManager.gameObject.SetActive(true);
                phaseManager.gameObject.SetActive(true);
                recipeManager.gameObject.SetActive(true);
            }
        );
        fsm.AddState("Paused");
        fsm.AddState("Game Over");

        fsm.AddTriggerTransition("Main Menu", "Game", "Start Game");
        fsm.AddTwoWayTriggerTransition("Toggle Pause", "Game", "Paused", t => isPaused);

        fsm.Init();
    }


    void LateUpdate()
    {
        Janitor();
    }

    public void StartGame() // TODO: call this from button in the main menu
    {
        fsm.Trigger("Start Game");
        onGameStart.Invoke();
    }

    public void LoadBaselineScene()
    {
        StartCoroutine(LoadScene("Baseline"));
    }

    IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void loadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadScene(currentSceneName));
    }

    public void StartBaseline()
    {
        throw new System.NotImplementedException();
    }


    /// <summary>
    /// Removes a GameObject after a specified delay.
    /// </summary>
    /// <param name="target">The GameObject to remove.</param>
    /// <param name="delay">The time in seconds to wait before removing the GameObject.</param>
    public void RemoveAfterDelay(GameObject target, float delay)
    {
        StartCoroutine(RemoveCoroutine(target, delay));
    }

    private IEnumerator RemoveCoroutine(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            Destroy(target);
        }
    }



    public void endGame()
    {
        Debug.Log("Game finnished!");
        onFinnishGame.Invoke();
    }

    private void Janitor()
    {
        if (objectsToClean.Count > maxAllowedObjects)
        {
            GameObject objectToRemove = objectsToClean[0];
            objectsToClean.RemoveAt(0);
        }

    }

    private string GetSceneForCondition(Condition condition)
    {
        switch (condition)
        {
            case Condition.LowFi:
                return "LowFiScene";
            case Condition.MediumFi:
                return "MediumFiScene";
            case Condition.HighFi:
                return "Main";
            default:
                return "DefaultScene";
        }
    }
}


public enum Condition
{
    LowFi,
    MediumFi,
    HighFi,
}
