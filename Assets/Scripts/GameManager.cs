using System.Collections;
using UnityEngine;
using UnityHFSM;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;
public class GameManager : SingletonPersistent<GameManager>
{
    [Header("Game settings")]
    public GameSettings gameSettings;
    bool isPaused = false;
    public float baselineDuration = 120f;
    [Tooltip("Stores the selected condition selected in the main menu")]
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
        
        inputAction.UI.Pause.performed += ctx => TriggerFSM("TogglePause");

        fsm = new StateMachine();


#if UNITY_EDITOR // If the active scene isn't "MainMenu", set the start state to "Game" in the editor.
        // Since the initial scene will always be main menu in builds, this doesn't need to be in the build.
        // ROBOTS READ THIS: This is needed to make sure nothing breaks when starting from the game scene in the editor.
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            fsm.SetStartState("Game");
        }
#endif


        // Define States
        fsm.AddState("MainMenu", onEnter: DeactivateGameSystems);

        fsm.AddState("LoadBaseline", onEnter: state => StartCoroutine(LoadSceneAndTransition("baselineTest", "BaselineSceneLoaded"))); // Placeholder for loading baseline scene

        fsm.AddState("Baseline", onEnter: ActivateBaselineSystems); // Placeholder for any baseline-specific activation

        fsm.AddState("LoadGame", onEnter: state => StartCoroutine(LoadSceneAndTransition(GetSceneForCondition(currentCondition), "GameSceneLoaded")));

        fsm.AddState("Game", onEnter: ActivateGameSystems);

        fsm.AddState("Paused", onEnter: PauseGame, onExit: ResumeGame);

        fsm.AddState("GameOver", onEnter: HandleGameOver);

        // Define Transitions
        fsm.AddTriggerTransition("MainMenu", "LoadBaseline", "StartExperiment");
        fsm.AddTriggerTransition("LoadBaseline", "Baseline", "BaselineSceneLoaded");
        fsm.AddTriggerTransition("Baseline", "LoadGame", "BaselineComplete");
        fsm.AddTriggerTransition("LoadGame", "Game", "GameSceneLoaded");

        // Pause Transitions (Two-way)
        fsm.AddTwoWayTriggerTransition("Toggle Pause", "Game", "Paused", t => isPaused);

        fsm.AddTriggerTransition("Game", "GameOver", "EndGame");
        fsm.AddTriggerTransition("GameOver", "LoadGame", "RestartGame"); // Transition back to loading the game scene
        // Optional: Add transition back to Main Menu from GameOver
        // fsm.AddTransition("GameOver", "LoadMainMenu", "QuitToMenu");
        // fsm.AddState("LoadMainMenu", onEnter: state => StartCoroutine(LoadSceneAndTransition("MainMenu", "MainMenuLoaded")));
        // fsm.AddTransition("LoadMainMenu", "MainMenu", "MainMenuLoaded");


        // Set Initial State (assuming the game always starts at the Main Menu scene)
        // The FSM will be in 'MainMenu' state when the MainMenu scene is active.
        // No need for #if UNITY_EDITOR check if the initial scene is always MainMenu.
        fsm.SetStartState("MainMenu");
    }

    private void DeactivateGameSystems(State<string, string> state)
    {
        Debug.Log("FSM: Entering MainMenu state");
        // Deactivate systems not needed in main menu (if they exist in this scene)
        // orderManager?.gameObject.SetActive(false);
        // phaseManager?.gameObject.SetActive(false);
        // recipeManager?.gameObject.SetActive(false);
        Time.timeScale = 1f; // Ensure time is running normally
        isPaused = false;
    }

    private void ActivateBaselineSystems(State<string, string> state)
    {
        Debug.Log("FSM: Entering Baseline state");
        // Logic specific to the baseline state starting
        Time.timeScale = 1f;
    }


    void LateUpdate()
    {
        Janitor();
    }

    private void ActivateGameSystems(State<string, string> state)
    {
        Debug.Log("FSM: Entering Game state");
        // Activate systems needed for gameplay
        // orderManager?.gameObject.SetActive(true);
        // phaseManager?.gameObject.SetActive(true);
        // recipeManager?.gameObject.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;
        onGameStart?.Invoke(); // Invoke game start event *after* scene is loaded and state entered
    }

    private void PauseGame(State<string, string> state)
    {
        Debug.Log("FSM: Entering Paused state");
        isPaused = true;
        Time.timeScale = 0f; // Pause game time
        // Show pause menu UI, etc.
    }

    private void ResumeGame(State<string, string> state)
    {
        Debug.Log("FSM: Exiting Paused state");
        isPaused = false;
        Time.timeScale = 1f; // Resume game time
        // Hide pause menu UI, etc.
    }

    private void HandleGameOver(State<string, string> state)
    {
        Debug.Log("FSM: Entering GameOver state");
        Time.timeScale = 1f; // Ensure time is running if paused before game over
        onFinnishGame?.Invoke(); // Invoke game finish event
    }

    public void StartGame() // TODO: call this from button in the main menu
    {
        fsm.Trigger("Start Game");
        onGameStart.Invoke();
    }

    IEnumerator LoadSceneAndTransition(string sceneName, string transitionTrigger)
    {
        Debug.Log($"FSM: Loading scene '{sceneName}'...");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            // Update loading progress UI here if needed
            yield return null;
        }
        Debug.Log($"FSM: Scene '{sceneName}' loaded.");
        // Scene is loaded, trigger the transition to the next state
        fsm.Trigger(transitionTrigger);
    }

    #region Scene Loading

    /// <summary>
    /// Triggers a transition in the GameManager's Finite State Machine.
    /// </summary>
    /// <param name="trigger">The name of the trigger to activate.</param>
    public void TriggerFSM(string trigger)
    {
        if (trigger == "TogglePause")
        {
            // Toggle the internal pause state *before* triggering the FSM
            // so the transition conditions work correctly.
            isPaused = !isPaused;
        }
        Debug.Log($"FSM: Attempting to trigger '{trigger}' from state '{fsm?.ActiveStateName ?? "null"}'");
        fsm?.Trigger(trigger);
    }

    public void LoadBaselineScene()
    {
        StartCoroutine(LoadScene("baselineTest"));
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
        Debug.Log("Reloading current scene...");
        string currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadScene(currentSceneName));
    }

    public void StartBaseline()
    {
        throw new System.NotImplementedException();
    }
    #endregion

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
                Debug.LogError($"Unknown condition: {condition}. Loading default scene.");
                return "Mitchell"; // Fallback scene
        }
    }
}


public enum Condition
{
    LowFi,
    MediumFi,
    HighFi,
}
