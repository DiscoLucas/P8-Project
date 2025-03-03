using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LiquidContainer : MonoBehaviour
{
    [SerializeField]
    protected float maxFill = 1.0f;
    [SerializeField]
    protected float fillAmount = 0f;
    [SerializedDictionary("Name","Ingredient")]
    public SerializedDictionary<string, IngredientBase> ingredients = new SerializedDictionary<string, IngredientBase>();
    public bool materialHaveBeenChange = true;

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

    /// <summary>
    /// Add ingreident to the container
    /// </summary>
    /// <param name="ingredient">What you add</param>
    /// <param name="inputAmount">The amount you add</param>
    public virtual void AddIngredient(IngredientBase ingredient, float inputAmount)
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
    /// If it is possiable to por'our
    /// </summary>
    /// <returns></returns>
    public virtual bool canPoourer()
    {
        return ingredients.Values.Any(ing => ing.Amount > 0);
    }



    /// <summary>
    /// Updates the liquid level visualization
    /// TODO: implement
    /// </summary>
    internal void updateLiquidVisual()
    {

    }

    /// <summary>
    /// Checks if the glass is full.
    /// </summary>
    public virtual bool isFull()
    {
        return fillAmount >= maxFill;
    }

    public virtual void depleateLiqued(float amount)
    {
        if (fillAmount <= 0) return;

        float totalLiquid = fillAmount;
        float actualPouredAmount = Mathf.Min(amount, totalLiquid);

        foreach (var kvp in ingredients.ToList()) // ToList() prevents modifying while iterating
        {
            IngredientBase ingredient = kvp.Value;
            float proportion = ingredient.Amount / totalLiquid;
            float amountToReduce = actualPouredAmount * proportion;

            ingredient.Amount -= amountToReduce;
            if (ingredient.Amount <= 0)
                ingredients.Remove(kvp.Key);
        }

        fillAmount -= actualPouredAmount;
    }

    /// <summary>
    /// Get the color for the current liquid;
    /// </summary>
    /// <returns></returns>
    public virtual Color getLiquidColor() { 
        return Color.magenta;
    }

    /// <summary>
    /// Createe a mixture ingerident with all the ingreidents in the container
    /// </summary>
    /// <param name="pourAmount">Amount that needs to be poured out </param>
    /// <returns></returns>
    public virtual IngredientBase createPouredMixture(float pourAmount)
    {
       return null;
    }



}
