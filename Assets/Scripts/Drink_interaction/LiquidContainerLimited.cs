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
        protected int lastCheckColorCount = 0;
        protected Color outputColor = Color.white;
        protected int orderCounter = 0;

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
                else {
                    ingredients[ingredient.Name] = ingredient.copy();
                    orderCounter++;
                }
                updateLiquidVisual();
            }
            else
            {
                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += inputAmount;
                else {
                    ingredients[ingredient.Name] = ingredient.copy(orderCounter);
                    orderCounter++;
                }
                    
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
                IngredientBase singleIngredient = ingredients.Values.First();
                float amountToPour = Mathf.Min(singleIngredient.Amount, actualPouredAmount);
                singleIngredient.Amount -= amountToPour;
                fillAmount -= amountToPour;

                return new IngredientBase(singleIngredient.Name, amountToPour, singleIngredient.Type, singleIngredient.Color, singleIngredient.AlcoholContent);
            }

            List<string> ingredientNames = new List<string>();
            IngredientBase pouredMixture = new IngredientBase("", 0, IngredientType.MixedLiquid, Color.clear);
            Color objectColor = new Color(0, 0, 0, 0);
            foreach (var kvp in ingredients)
            {
                IngredientBase ingredient = kvp.Value;
                float proportion = ingredient.Amount / totalCurrentLiquid;
                float amountToPour = actualPouredAmount * proportion;

                if (amountToPour > 0)
                {
                    SerializedDictionary<string, IngredientBase> ingredientsList = pouredMixture.ingredients;
                    if (ingredientsList.ContainsKey(ingredient.Name))
                    {
                        ingredientsList[ingredient.Name].Amount += amountToPour;
                    }
                    else
                    {
                        IngredientBase ind = ingredient.copy();
                        ingredientsList.Add(ind.Name,ind);
                    }

                    ingredientNames.Add(ingredient.Name);
                    ingredient.Amount -= amountToPour;
                    ingredient.ingredients = ingredientsList;
                    objectColor += new Color(ingredient.Color.r / ingredients.Count, ingredient.Color.g / ingredients.Count, ingredient.Color.b / ingredients.Count, 1);
                }
            }
            pouredMixture.Color = objectColor;
            fillAmount -= actualPouredAmount;

            pouredMixture.Name = string.Join(", ", ingredientNames.Take(3));
            if (ingredientNames.Count > 3)
                pouredMixture.Name += " & more";

            

            return pouredMixture;
        }

        public override Color getLiquidColor()
        {
            if (lastCheckColorCount != ingredients.Count) {
                materialHaveBeenChange = true;
                lastCheckColorCount = ingredients.Count;
                IngredientBase mix = createPouredMixture(1);
                outputColor = mix.Color;
            }
            return outputColor;
        }

        /// <summary>
        /// Sort this dictionary of container and its ingredients into a sorted list
        /// and filter out mixtures, keeping only base components.
        /// </summary>
        /// <returns>List of base ingredients in order</returns>
        public List<IngredientBase> getIngreidentsAsOrderedeList()
        {
            List<IngredientBase> orderedIngredients = new List<IngredientBase>();
            Debug.Log("orderingrend null: " + (orderedIngredients == null));

            var sortedIngredients = ingredients.Values.OrderBy(ing => ing.step.order);
            Debug.Log("sortedIngredients null: " + (sortedIngredients == null));

            foreach (IngredientBase ingredient in sortedIngredients)
            {
                addIngredientRecursively(ingredient, orderedIngredients);
            }
            orderedIngredients = orderedIngredients
                .Where(ing => ing.ingredients == null || ing.ingredients.Count == 0)
                .ToList();

            return orderedIngredients;
        }


        /// <summary>
        /// Recursively adds an ingredient and its nested ingredients to the list.
        /// </summary>
        private void addIngredientRecursively(IngredientBase ingredient, List<IngredientBase> orderedList)
        {
            if (!orderedList.Contains(ingredient))
                orderedList.Add(ingredient);

            var nestedIngredients = ingredient.ingredients.Values.OrderBy(ing => ing.step.order);
            foreach (var nestedIngredient in nestedIngredients)
            {
                addIngredientRecursively(nestedIngredient, orderedList);
            }
        }

    }
}

