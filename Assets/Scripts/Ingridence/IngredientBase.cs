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
        [SerializedDictionary("Name", "Ingredient")]
        public SerializedDictionary<string, IngredientBase> ingredients;


        public IngredientBase(string name, float amount, IngredientType type, Color color, float alcoholContent = 0)
        {
            Name = name;
            Amount = amount;
            Type = type;
            Color = color;
            ingredients = new SerializedDictionary<string, IngredientBase>();
        }

        /// <summary>
        /// Copy the ingredient
        /// </summary>
        /// <returns></returns>
        internal IngredientBase copy()
        {
            IngredientBase baseIn = new IngredientBase(Name, Amount, Type, Color, AlcoholContent);
            baseIn.ingredients = ingredients;
            return baseIn;
        }
    }
}