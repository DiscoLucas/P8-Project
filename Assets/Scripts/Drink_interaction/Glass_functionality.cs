using Assets.Scripts.Ingridence;
using System.Collections.Generic;
using UnityEngine;

public class Glass_functionality : MonoBehaviour
{
    [SerializeField] 
    private float maxFill = 1.0f;
    [SerializeField]
    private float fillAmount = 0f;
    [SerializeField]
    private Dictionary<string, IngredientBase> ingredients = new Dictionary<string, IngredientBase>();


    private void Start()
    {
    }

    /// <summary>
    /// Adds liquid to the glass up to the max fill level.
    /// </summary>
    public void AddIngredient(float amount)
    {
        fillAmount = Mathf.Min(fillAmount + amount, maxFill);
        Debug.Log($"Glass filled: {fillAmount}/{maxFill}");

        updateLiquidVisual();
    }

    public void AddIngredient(IngredientBase ingredient, float inputAmount)
    {
        if (ingredient.solid == false)
        {
            float availableSpace = maxFill - fillAmount;
            float actualAddedAmount = Mathf.Min(inputAmount, availableSpace);

            if (actualAddedAmount <= 0)
            {
                Debug.Log($"Glass is full! Cannot add more {ingredient.Name}.");
                return;
            }

            fillAmount += actualAddedAmount;
            Debug.Log($"Liquid added: {ingredient.Name} ({actualAddedAmount}ml). Total: {fillAmount}/{maxFill}");

            if (ingredients.ContainsKey(ingredient.Name))
                ingredients[ingredient.Name].Amount += actualAddedAmount;
            else
                ingredients[ingredient.Name] = new IngredientBase(ingredient.Name, actualAddedAmount, ingredient.Type, ingredient.Color, ingredient.AlcoholContent);

            updateLiquidVisual();
        }
        else
        {
            if (ingredients.ContainsKey(ingredient.Name))
                ingredients[ingredient.Name].Amount += inputAmount;
            else
                ingredients[ingredient.Name] = new IngredientBase(ingredient.Name, inputAmount, ingredient.Type, ingredient.Color);

        }
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
