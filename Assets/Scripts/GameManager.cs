using System.Collections;
using UnityEngine;
using UnityHFSM;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : SingletonPersistent<GameManager>
{
    public Condition condition { get; private set; }
    private StateMachine fsm;
    private InputSystem_Actions inputAction;
    

    [Header("Game Settings")]
    public bool neverEnd = false;

    [Header("Cleaning")]
    public List<GameObject> objectsToClean;
    [SerializeField] int maxAllowedObjects = 100;


    void Start()
    {
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

        fsm.AddState("Main Menu");
        fsm.AddState("Load Game", 
            onEnter => StartCoroutine(LoadScene("Mitchell"))); // hehe my scene is the main scene >:)
        fsm.AddState("Game");
        fsm.AddState("Paused");
        fsm.AddState("Game Over");

        fsm.AddTriggerTransition("Main Menu", "Game", "Start Game");
        fsm.AddTwoWayTriggerTransition("Toggle Pause", "Game", "Paused", t => isPaused);

        fsm.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame() // TODO: call this from button in the main menu
    {
        fsm.Trigger("Start Game");
    }

    IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
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



    public void endGame(){
        Debug.Log("Game Over!");
    }

}



public enum Condition
{
}
