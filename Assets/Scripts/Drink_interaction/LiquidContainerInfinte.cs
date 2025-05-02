using Assets.Scripts.Ingridence;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        [Header("**Ingerident that should be filled out OR SET THE SCRIBTLABLE OBJECT**")]
        public IngredientBase ingredient;
        public IngredientScribtiableObject ingredientScribtiableObject;

        void Start()
        {
            if(ingredientScribtiableObject != null){
                ingredient = ingredientScribtiableObject.ingredientBase.copy();
                ingredient.step = new DrinkStep(0);
            }

            drinkOnStart();
            setDrinkDisplay(true);
        }


        internal override void drinkOnStart()
        {
            base.drinkOnStart();
            if(ingridentTextDisplay != null){
                TextMeshProUGUI text = ingridentTextDisplay.GetComponentInChildren<TextMeshProUGUI>();
                if(text != null)
                    text.text = ingredient.Name;
            }
        }

        public override void AddIngredient(IngredientBase ingredient, float inputAmount, out float actualAddedAmount)
        {   
            actualAddedAmount= 0;
            if (ingredient.solid == false)
            {
                float availableSpace = maxFill - fillAmount;
                actualAddedAmount = Mathf.Min(inputAmount, availableSpace);

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

        public override IngredientBase createPouredMixture(float pourAmount,bool removeAmount)
        {
            return ingredient;
        }

        public override bool canPoourer()
        {
            return true;
        }

        public override Color getLiquidColor()
        {
            return ingredient.Color;
        }
    }
}
