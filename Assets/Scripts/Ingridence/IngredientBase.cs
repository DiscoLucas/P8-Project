using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Ingridence
{
    [Serializable]
    public class IngredientBase 
    {
        public string Name;
        public float Amount;
        public bool solid = false;
        public IngredientType Type;
        public Color Color;
        public float AlcoholContent;
        public IngredientBase[] componentIngrediences;


        public IngredientBase(string name, float amount, IngredientType type, Color color, float alcoholContent = 0)
        {
            Name = name;
            Amount = amount;
            Type = type;
            Color = color;
        }
    }
}