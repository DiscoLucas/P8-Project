using System.Collections;
using UnityEngine;
using UnityHFSM;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{
    public Condition condition { get; private set; }
    private StateMachine fsm;
    private InputSystem_Actions inputAction;
    

    [Header("Game Settings")]
    public bool neverEnd = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        fsm.AddState("Main Menu");
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
