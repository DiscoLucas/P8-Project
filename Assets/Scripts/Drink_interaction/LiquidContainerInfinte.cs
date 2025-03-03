using Assets.Scripts.Ingridence;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Drink_interaction
{
    /// <summary>
    /// A container used for bottles that should have an infinte amount of liqued
    /// </summary>
    public class LiquidContainerInfinte : LiquidContainer
    {
        /// <summary>
        /// The ingerident that is poured in other glasses
        /// </summary>
        [Header("**Ingerident that should be filled out**")]
        public IngredientBase ingredient;

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

        // Override createPouredMixture for Infinite containers (ingredients don't deplete).
        public override IngredientBase createPouredMixture(float pourAmount)
        {
            if (fillAmount <= 0)
                return null;

            return ingredient;
        }

        public override bool canPoourer()
        {
            return true;
        }
    }
}
