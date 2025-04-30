using System.Collections;
using UnityEngine;
using UnityHFSM;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;

public class GameManager : SingletonPersistent<GameManager>
{
    [Header("Game settings")]
    public GameSettings gameSettings;
    public bool neverEnd = false;
    bool isPaused = false;
    [Tooltip("Enable logging for debugging purposes. Can be loud in the console.")]
    public bool logging;
    
    [Tooltip("Stores the current state of the FSM. Requires logging to be enabled.")]
    public string currentState;
    private StateMachine fsm;
    private InputSystem_Actions inputAction;

    [Header("Experiment")]
    public float baselineDuration = 120f; [Tooltip("How long the baseline recording session should be.")]
    public Condition currentCondition; [Tooltip("Stores the selected condition selected in the main menu")]
    public string participantID; [Tooltip("Stores the participant ID selected in the main menu")]
    public int sessionNumber; // Added session number field
    [Header("Events")]
    public UnityEvent onGameStart; [Tooltip("Called when the game starts.")]
    public UnityEvent onFinnishGame; [Tooltip("Called when the game ends.")]
    public UnityEvent onGamePhaseChange; [Tooltip("Called when the game change from one phase to anthor.")]

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


        #if UNITY_EDITOR 
        // If the active scene isn't "MainMenu", set the start state to "Game" in the editor.
        // Since the initial scene will always be main menu in builds, this doesn't need to be in the build.
        // ROBOTS READ THIS: This is needed to make sure nothing breaks when starting from the game scene in the editor.
        
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            fsm.SetStartState("Game");
        }
        #endif


        #region State Definitions
        fsm.AddState("MainMenu", onEnter: DeactivateGameSystems);

        fsm.AddState("LoadBaseline", onEnter: async state => 
            { 
                StartCoroutine(LoadSceneAndTransition("baselineTest", "BaselineSceneLoaded"));
                bool started = await StartLabRecordingAsync("Baseline");
                if (started)
                {
                    if (logging) Debug.Log("Loading baseline scene");
                    StartCoroutine(LoadSceneAndTransition("baselineTest", "BaselineSceneLoaded"));
                }
                else
                {
                    Debug.LogError("Failed to start baseline recording.");
                }
            }   
        );

        fsm.AddState("Baseline", onEnter: ActivateBaselineSystems); // Placeholder for any baseline-specific activation

        fsm.AddState("LoadGame", onEnter: state =>
            {
                
                string taskName = GetConditionNameForTask(currentCondition);
                
                if (logging) Debug.Log("Loading game scene");
                StartCoroutine(LoadSceneAndTransition(GetSceneForCondition(currentCondition), "GameSceneLoaded"));
                
            }
        );

        fsm.AddState("Game", onEnter: async state =>
            {
                if (logging) Debug.Log("Entering game state - activating game systems");
                ActivateGameSystems(state);

                if (logging) Debug.Log("Waiting briefly before starting the recording");
                await Task.Delay(500);

                if (logging) Debug.Log("Starting LabRecorder for the game");
                string taskName = GetConditionNameForTask(currentCondition);
                bool started = await StartLabRecordingAsync(taskName);
                if (!started)
                {
                    Debug.LogError("Failed to start main task recording.");
                }
            }
        );

        fsm.AddState("Paused", onEnter: PauseGame, onExit: ResumeGame);
        fsm.AddState("GameOver", onEnter: HandleGameOver);
        #endregion

        #region State transitions
        // remember that trigger transitions arguments are: trigger name, from, to
        fsm.AddTriggerTransition("StartExperiment", "MainMenu", "LoadBaseline");
        fsm.AddTriggerTransition("BaselineSceneLoaded", "LoadBaseline", "Baseline");
        fsm.AddTriggerTransition("BaselineComplete", "Baseline", "LoadGame");
        fsm.AddTriggerTransition("GameSceneLoaded", "LoadGame", "Game");

        // Pause Transitions (Two-way)
        fsm.AddTwoWayTriggerTransition("Toggle Pause", "Game", "Paused", t => isPaused);

        //fsm.AddTriggerTransition("Game", "GameOver", "EndGame");
        //fsm.AddTriggerTransition("GameOver", "LoadGame", "RestartGame"); // TODO: Implement game over behavior
        #endregion
        fsm.AddTriggerTransition("Game", "GameOver", "EndGame");
        fsm.AddTriggerTransition("Game", "GameOver", "GameOver");
        fsm.AddTriggerTransition("GameOver", "LoadGame", "RestartGame"); // Transition back to loading the game scene
        fsm.AddTriggerTransition("Game", "MainMenu", "Game");
        fsm.AddTriggerTransition("GameOver", "Game", "GameOver");

        // Optional: Add transition back to Main Menu from GameOver
        // fsm.AddTransition("GameOver", "LoadMainMenu", "QuitToMenu");
        // fsm.AddState("LoadMainMenu", onEnter: state => StartCoroutine(LoadSceneAndTransition("MainMenu", "MainMenuLoaded")));
        // fsm.AddTransition("LoadMainMenu", "MainMenu", "MainMenuLoaded");


        fsm.SetStartState("MainMenu");
        fsm.Init();
    }

    private async Task<bool> StartLabRecordingAsync(string taskName)
    {
        if (LabRecorder.Instance == null)
        {
            Debug.LogError("Couldn't find my homie LabRecorder. Cannot start recording.");
            return false;
        }
        if (string.IsNullOrEmpty(participantID))
        {
            Debug.LogError("You forgot to assign the Participant ID you bozo.");
            return false;
        }
        if (sessionNumber <= 0)
        {
            Debug.LogError("Session number is invalid (must be > 0). Cannot start recording.");
            return false;
        }

        // Use the stored sessionNumber, converting it to a string for LabRecorder
        string sessionNumberStr = sessionNumber.ToString();

        Debug.Log($"Attempting to start LabRecorder for P={participantID}, S={sessionNumberStr}, Task={taskName}");
        bool started = await LabRecorder.Instance.ConfigureAndStartRecordingAsync(participantID, sessionNumberStr, taskName);
        return started;
    }

    private string GetSceneForCondition(Condition condition)
    {
        switch (condition)
        {
            case Condition.LowFi: return "Main_LoFI";
            case Condition.MediumFi: return "Main_Midfi";
            case Condition.HighFi: return "Main_Hifi";
            default:
                Debug.LogError($"Unknown condition: {condition}. Going back home.");
                return "MainMenu"; // Fallback scene
        }
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
        if (logging) currentState = fsm.ActiveStateName;
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
        onGameStart.Invoke(); // Invoke game start event *after* scene is loaded and state entered
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
/* // tried to do it syncronously but it stil doesn't work
    void LoadSceneAndTransition(string sceneName, string transitionTrigger)
    {
        Debug.Log($"FSM: Loading scene '{sceneName}'...");
        SceneManager.LoadScene(sceneName);
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            Debug.Log($"FSM: Scene '{sceneName}' loaded.");
            // Scene is loaded, trigger the transition to the next state
            fsm.Trigger(transitionTrigger);
        };
    }*/
    
    IEnumerator LoadSceneAndTransition(string sceneName, string transitionTrigger)
    {
        Debug.Log($"FSM: Loading scene '{sceneName}'...");
        MeshRenderer loadingSpace = null;
        foreach (Transform child in Camera.main.transform)
        {
            if (child.CompareTag("LoadingSpace"))
            {
                child.gameObject.SetActive(true);
                loadingSpace = child.GetComponent<MeshRenderer>();
                break;
            }
        }
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        Color transpartionColorBlack = new Color(0, 0, 0, 0);
        while (!asyncLoad.isDone)
        {
            if(loadingSpace != null){
                loadingSpace.material.color = Color.Lerp(transpartionColorBlack, Color.black, asyncLoad.progress);
            }
            // Update loading progress UI here if needed
            yield return null;
        }
        Debug.Log($"FSM: Scene '{sceneName}' loaded.");

        if (loadingSpace != null)
        {
            float fadeDuration = gameSettings.fadeDuration;
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                loadingSpace.material.color = Color.Lerp(Color.black, transpartionColorBlack, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            loadingSpace.material.color = transpartionColorBlack;
            loadingSpace.gameObject.SetActive(false);
        }
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

        if (fsm.ActiveState == null)
        {
            Debug.LogError("FSM: No active state. Ensure the state machine is initialized and has a start state.");
            return;
        }

        if (!fsm.GetAllTriggerTransitions().ContainsKey(trigger))
        {
            Debug.LogError($"FSM: Trigger '{trigger}' does not exist in the state machine.");
            return;
        }

        Debug.Log($"FSM: Attempting to trigger '{trigger}' from state '{fsm.ActiveStateName ?? "null"}'");
        fsm.Trigger(trigger);
        Debug.Log($"FSM: Swicht to {fsm.ActiveStateName ?? "null"}");
    }




    #endregion
    [Obsolete("RemoveAfterDelay() is deprecated. Use Janitor() instead.")]
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

    private string GetConditionNameForTask(Condition condition)
    {
        return condition.ToString();
    }
}


public enum Condition
{
    LowFi,
    MediumFi,
    HighFi,
}
