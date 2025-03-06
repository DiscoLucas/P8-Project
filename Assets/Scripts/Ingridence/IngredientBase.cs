using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Ingridence
{
    /// <summary>
    /// Base Class for all ingeriends both solid and liquid
    /// </summary>
    [Serializable]
    public class IngredientBase 
    {
        public string Name;
        public float Amount;
        public bool solid = false;
        public IngredientType Type;
        public Color Color;
        public float AlcoholContent;
        public DrinkStep step;
        [SerializedDictionary("Name", "Ingredient")]
        public SerializedDictionary<string, IngredientBase> ingredients;


        public IngredientBase(string name, float amount, IngredientType type, Color color, float alcoholContent = 0,int order = 0, DrinkAction action = DrinkAction.None)
        {
            Name = name;
            Amount = amount;
            Type = type;
            Color = color;
            ingredients = new SerializedDictionary<string, IngredientBase>();
            step = new DrinkStep(order, action);
        }

        /// <summary>
        /// Copy the ingredient
        /// </summary>
        /// <returns></returns>
        internal IngredientBase copy()
        {
            IngredientBase baseIn = new IngredientBase(Name, Amount, Type, Color, AlcoholContent, step.order, step.action);
            baseIn.ingredients = ingredients;
            return baseIn;
        }

        internal IngredientBase copy(int order)
        {
            IngredientBase baseIn = new IngredientBase(Name, Amount, Type, Color, AlcoholContent, order, step.action);
            baseIn.ingredients = ingredients;
            return baseIn;
        }

    }
}