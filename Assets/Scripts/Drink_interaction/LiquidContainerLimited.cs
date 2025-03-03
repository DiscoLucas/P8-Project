using Assets.Scripts.Ingridence;
using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Drink_interaction
{
    /// <summary>
    /// A liquid container that contain a finiate amount. used for glasses and alike
    /// </summary>
    public class LiquidContainerLimited : LiquidContainer
    {

        public override void AddIngredient(IngredientBase ingredient, float inputAmount)
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

                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += actualAddedAmount;
                else
                    ingredients[ingredient.Name] = ingredient.copy();

                updateLiquidVisual();
            }
            else
            {
                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += inputAmount;
                else
                    ingredients[ingredient.Name] = ingredient.copy();
            }
        }

        public override IngredientBase createPouredMixture(float pourAmount)
        {
            if (fillAmount <= 0)
                return null;

            float totalCurrentLiquid = fillAmount;
            float actualPouredAmount = Mathf.Min(pourAmount, totalCurrentLiquid);

            if (ingredients.Count == 1)
            {
                // Handle single ingredient (as before)
                IngredientBase singleIngredient = ingredients.Values.First();
                float amountToPour = Mathf.Min(singleIngredient.Amount, actualPouredAmount);

                // Deplete ingredient (limited container behavior)
                singleIngredient.Amount -= amountToPour;
                fillAmount -= amountToPour;

                return new IngredientBase(singleIngredient.Name, amountToPour, singleIngredient.Type, singleIngredient.Color, singleIngredient.AlcoholContent);
            }

            // Multiple ingredients, create mixture
            List<string> ingredientNames = new List<string>();
            IngredientBase pouredMixture = new IngredientBase("", 0, IngredientType.MixedLiquid, Color.clear);

            foreach (var kvp in ingredients)
            {
                IngredientBase ingredient = kvp.Value;
                float proportion = ingredient.Amount / totalCurrentLiquid;
                float amountToPour = actualPouredAmount * proportion;

                if (amountToPour > 0)
                {
                    SerializedDictionary<string, IngredientBase> ingredientsList = pouredMixture.ingredients;
                    // Modify the serialized dictionary correctly
                    if (ingredientsList.ContainsKey(ingredient.Name))
                    {
                        // If the ingredient already exists, just update it
                        ingredientsList[ingredient.Name].Amount += amountToPour;
                    }
                    else
                    {
                        IngredientBase ind = ingredient.copy();
                        Debug.Log(ind.Name + " Added  " + pouredMixture.ingredients.Count);
                        ingredientsList.Add(ind.Name,ind);
                        Debug.Log(ind.Name + " have been added " + pouredMixture.ingredients.Count);
                    }

                    ingredientNames.Add(ingredient.Name);

                    // Deplete ingredient (limited container behavior)
                    ingredient.Amount -= amountToPour;
                    ingredient.ingredients = ingredientsList;
                }
            }

            fillAmount -= actualPouredAmount;

            // Generate a readable name (max 3 ingredients in name)
            pouredMixture.Name = string.Join(", ", ingredientNames.Take(3));
            if (ingredientNames.Count > 3)
                pouredMixture.Name += " & more";

            return pouredMixture;
        }

    }
}

