using System.Collections.Generic;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class LiquidComparingAlgor : MonoBehaviour
{
    
    public LiquidContainerLimited drink;
    public LiquidContainerLimited test;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        Compare(drink, test);
    }

    void Compare(LiquidContainerLimited ideal, LiquidContainerLimited actual)
    {
        List<IngredientBase> idealList = ideal.getIngreidentsAsOrderedeList();
        List<IngredientBase> actualList = actual.getIngreidentsAsOrderedeList();

        Debug.Log(idealList);
        Debug.Log(actualList);
    }
}
