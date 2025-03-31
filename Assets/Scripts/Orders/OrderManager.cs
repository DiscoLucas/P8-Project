using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// The manager that controls order generation.
/// </summary>
public class OrderManager : MonoBehaviour
{
    [SerializedDictionary("Id", "Order")]
    public SerializedDictionary<string, Order> currentOrderList;
    public RecipeManager recipeManager;
    public GameObject deliverArea_Prefab, Text_prefab, parent_to_text, Agent_prefab;
    public Transform agentSpawnPoint, agentEndPoint;
    public List<Transform> availableSpawnPoints = new List<Transform>();
    public TMP_Text scoreCounter;
    public float totalScore = 0;

    [Header("Order Generation Settings")]
    public Vector2 orderSpawnTimeRange = new Vector2(5f, 15f); // Min & Max time between orders
    public float doubleOrderChance = 0.3f; 

    private void Start()
    {
        recipeManager = FindAnyObjectByType<RecipeManager>();
    }

    /// <summary>
    /// Repeatedly generates orders at random time intervals.
    /// </summary>
    private IEnumerator GenerateOrders()
    {

        float waitTime = Random.Range(orderSpawnTimeRange.x, orderSpawnTimeRange.y);
        yield return new WaitForSeconds(waitTime);

        createOrder();

        // Random chance to create a second order
        if (Random.value < doubleOrderChance)
        {
            yield return new WaitForSeconds(1f); 
            createOrder();

        }
        
    }

    /// <summary>
    /// Finishes the given order and moves the agent to exit.
    /// </summary>
    public void finnishOrder(Order order, CustomerAgenet agent)
    {
        CocktailRecipe recipe = recipeManager.recipes[order.recipieID];
        List<IngredientBase> ideal_List = recipe.ingredients.ToList();
        List<IngredientBase> order_List = order.containerLimited.getIngreidentsAsOrderedeList();
        float timeTaken = Time.timeSinceLevelLoad - order.startPoint;
        float score = recipeManager.compareTwoIngridienseList(ideal_List, order_List, order.recipieID,order.containerLimited.glassType, timeTaken, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour);

        totalScore += score;
        scoreCounter.text = $"Score: {totalScore}";

        agent.AddObjectToHand(order.containerLimited.gameObject);

        agent.reachedDistation.RemoveAllListeners();
        agent.reachedDistation.AddListener(agent.destoryAgent);
        agent.setDestionation(agentEndPoint);

        availableSpawnPoints.Add(order.location);
        currentOrderList.Remove(order.orderID);
        StartCoroutine(GenerateOrders());
    }

    /// <summary>
    /// Creates a new order if there are available spawn points.
    /// </summary>
    public void createOrder()
    {
        if (availableSpawnPoints.Count <= 0)
            return;

        Transform spawnPoint = availableSpawnPoints[0];
        availableSpawnPoints.RemoveAt(0);

        GameObject agent = Instantiate(Agent_prefab, agentSpawnPoint.position, Quaternion.identity);
        CustomerAgenet customerAgenet = agent.GetComponent<CustomerAgenet>();
        customerAgenet.reachedDistation.AddListener(placeOrder);
        customerAgenet.setDestionation(spawnPoint);
    }

    /// <summary>
    /// Places an order at the customer's location.
    /// </summary>
    public void placeOrder(CustomerAgenet agent)
    {
        Transform spawnPoint = agent.destionation;
        string keyRecipe;
        CocktailRecipe recipe = recipeManager.getRandomCocktailRecipe(out keyRecipe);
        string orderName = recipe.Name +"#" +Mathf.FloorToInt((Time.timeSinceLevelLoad * 100));

        Order order = new Order(keyRecipe, orderName, spawnPoint);

        GameObject textProbemt = Instantiate(Text_prefab, parent_to_text.transform);
        textProbemt.SetActive(true);
        textProbemt.GetComponent<TMP_Text>().text = $"{orderName} - {recipe.Name}: " +
            string.Join(" ", recipe.ingredients.Select(i => $"[{i.Name} {i.Amount}]"));

        GameObject deliverArea = Instantiate(deliverArea_Prefab, spawnPoint.position, Quaternion.identity);
        DeliverOrderArea deliverOrderArea = deliverArea.GetComponent<DeliverOrderArea>();
        deliverOrderArea.tex_feild = textProbemt;
        deliverOrderArea.order = order;
        deliverOrderArea.orderDeliverede.AddListener(finnishOrder);
        deliverOrderArea.agent = agent;

        currentOrderList.Add(orderName, order);

        agent.reachedDistation.RemoveAllListeners();
        agent.startOrder(orderName, order);
    }
}
