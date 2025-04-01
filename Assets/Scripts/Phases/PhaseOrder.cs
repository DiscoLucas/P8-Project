using UnityEngine;
using System;
using AYellowpaper.SerializedCollections;
[Serializable]
public class PhaseOrder
{
    public string Name;
    [SerializedDictionary("Key", "Cocktail Recipe")]
    public SerializedDictionary<string, CocktailRecipe> recipes;
}
