using System.Collections;
using UnityEngine;

public class GameManager : SingletonPersistent<GameManager>
{
    public Condition condition { get; private set; }
    [Header("Game Settings")]
    public bool neverEnd = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
