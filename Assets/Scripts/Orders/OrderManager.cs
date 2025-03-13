using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
/// <summary>
/// The manager the controlls the order
/// </summary>
public class OrderManager : MonoBehaviour
{
    [SerializedDictionary("Id", "Order")]
    public SerializedDictionary<string, Order> currentOrderList;
    public RecipeManager recipeManager;
    public GameObject deliverArea_Prefab,Text_prefab,parent_to_text;
    public List<Transform> availableSpawnPoints = new List<Transform>();
    public TMP_Text scoreCounter;
    public float totalScore = 0;

    private void Start()
    {
        recipeManager = FindAnyObjectByType<RecipeManager>();

    }

    /// <summary>
    /// Finnish the given order
    /// </summary>
    /// <param name="order"></param>
    public void finnishOrder(Order order) {
        CocktailRecipe recipe = recipeManager.recipes[order.recipieID];
        List<IngredientBase> ideal_List = recipe.ingredients.ToList();
        List<IngredientBase> order_List = order.containerLimited.getIngreidentsAsOrderedeList();
        float timeTaken = Time.timeSinceLevelLoad - order.startPoint;
        float score = recipeManager.compareTwoIngridienseList(ideal_List, order_List, timeTaken, recipe.expectedTime, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour);
        totalScore += score;
        scoreCounter.text = $"Score: {totalScore}";
        Destroy(order.containerLimited.gameObject);
        availableSpawnPoints.Add(order.location);
        currentOrderList.Remove(order.orderID);

    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public void createOrder() {

        if(availableSpawnPoints.Count <=0)
            return;
        string keyRecipe;
        CocktailRecipe recipe = recipeManager.getRandomCocktailRecipe(out keyRecipe);

        Transform spawnPoint = availableSpawnPoints[0];
        availableSpawnPoints.RemoveAt(0);

        string orderName = recipe.Name + Time.timeSinceLevelLoad;
        Order order = new Order(keyRecipe, orderName, spawnPoint);
        GameObject text_probemt = Instantiate(Text_prefab, parent_to_text.transform);
        text_probemt.SetActive(true);
        GameObject deliverArea = Instantiate(deliverArea_Prefab, spawnPoint.position,Quaternion.identity);
        DeliverOrderArea deliverOrderArea = deliverArea.GetComponent<DeliverOrderArea>();
        string recipe_promt = orderName + " - " + recipe.Name + ": ";
        foreach (IngredientBase ingredient in recipe.ingredients) {
            recipe_promt += "[" + ingredient.Name + " " + ingredient.Amount + "]";
        }
        Debug.Log(recipe_promt);
        text_probemt.GetComponent<TMP_Text>().text = recipe_promt;
        deliverOrderArea.tex_feild = text_probemt;
        deliverOrderArea.order = order;
        deliverOrderArea.orderDeliverede.AddListener(finnishOrder);
        currentOrderList.Add(orderName, order);
        
    }
}
