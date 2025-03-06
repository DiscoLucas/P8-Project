using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The manager the controlls the order
/// </summary>
public class OrderManager : MonoBehaviour
{
    [SerializedDictionary("Id", "Order")]
    public SerializedDictionary<string, Order> currentOrderList;
    public RecipeManager recipeManager;
    private void Start()
    {
        recipeManager = FindAnyObjectByType<RecipeManager>();
    }

    /// <summary>
    /// Finnish the given order
    /// </summary>
    /// <param name="order"></param>
    public void finnishOrder(Order order) { 

        List<IngredientBase> ideal_List = recipeManager.recipes[order.recipieID].ingredients.ToList();
        List<IngredientBase> order_List = order.containerLimited.getIngreidentsAsOrderedeList();

        float score = recipeManager.compareTwoIngridienseList(ideal_List, order_List, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour);
        currentOrderList.Remove(order.orderID);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public void createOrder() {
        string keyRecipe;
        CocktailRecipe recipe = recipeManager.getRandomCocktailRecipe(out keyRecipe);
        string orderName = recipe.Name + Time.timeSinceLevelLoad;
        Order order = new Order(keyRecipe,orderName);
        currentOrderList.Add(orderName, order);
        
    }
}
