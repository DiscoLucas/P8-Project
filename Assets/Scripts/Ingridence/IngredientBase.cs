using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Ingridence
{
    [Serializable]
    public class IngredientBase 
    {
        public string Name { get; private set; }
        public float Amount { get; set; }
        public bool solid = false;
        public IngredientType Type { get; private set; }
        public Color Color { get; private set; }
        public float AlcoholContent { get; private set; } 

        public IngredientBase(string name, float amount, IngredientType type, Color color, float alcoholContent = 0)
        {
            Name = name;
            Amount = amount;
            Type = type;
            Color = color;
        }
    }
}