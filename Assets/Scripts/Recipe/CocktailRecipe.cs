using Assets.Scripts.Ingridence;
using System;
using UnityEngine;

/// <summary>
/// Contains the cocktail recipe with ingreidents
/// </summary>
[Serializable]
public class CocktailRecipe
{
    public string Name;
    public IngredientBase[] ingredients;
    public float maxScore = 100;
    //Scoring variables
    public float expectedTime;
    public GlassType glassType;
}
