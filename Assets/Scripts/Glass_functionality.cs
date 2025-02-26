using UnityEngine;

public class Glass_functionality : MonoBehaviour
{
    [SerializeField] 
    private float maxFill = 1.0f;
    [SerializeField]
    private float fillAmount = 0f; 

    private void Start()
    {
    }

    /// <summary>
    /// Adds liquid to the glass up to the max fill level.
    /// </summary>
    public void addLiquid(float amount)
    {
        fillAmount = Mathf.Min(fillAmount + amount, maxFill);
        Debug.Log($"Glass filled: {fillAmount}/{maxFill}");

        updateLiquidVisual();
    }

    /// <summary>
    /// Updates the liquid level visualization
    /// TODO: implement
    /// </summary>
    private void updateLiquidVisual()
    {

    }

    /// <summary>
    /// Checks if the glass is full.
    /// </summary>
    public bool isFull()
    {
        return fillAmount >= maxFill;
    }
}
