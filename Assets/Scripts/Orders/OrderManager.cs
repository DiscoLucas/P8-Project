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
    [Header("Game settings")]
    public GameSettings gameSettings;
    bool gameFinished = false;
    [Header("Orders")]
    [SerializedDictionary("Id", "Order")]
    public SerializedDictionary<string, Order> currentOrderList;

    [Header("Managers *Will be set in runtime*")]
    public RecipeManager recipeManager;
    [SerializeField]
    PhaseManager phaseManager;

    [Header("Order Prefabs")]
    public GameObject deliverArea_Prefab;
    public GameObject[] Agent_prefab;
    [Header("Agent Settings")]
    public Transform agentSpawnPoint, agentEndPoint;
    public List<Transform> availableSpawnPoints = new List<Transform>();

    bool agentPoitionSet = false;

    [Header("UI debug ")]
    public TMP_Text scoreCounter;
    public GameObject Text_prefab, parent_to_text;

    [Header("Order Generation Settings")]
    public float totalScore = 0;
    public Vector2 orderSpawnTimeRange = new Vector2(5f, 15f); // Min & Max time between orders
    public float doubleOrderChance = 0.3f; 

    public Transform customerOrderDistination;

    private void Start()
    {
        recipeManager = FindAnyObjectByType<RecipeManager>();
        phaseManager = FindAnyObjectByType<PhaseManager>();
        GameManager.Instance.orderManager = this;
        if (gameSettings == null)
            gameSettings = GameManager.Instance.gameSettings;
        else
            Debug.LogError("Game settings have not been assigned in the inspector.");
       
    }

    private void OnEnable()
    {
        GameManager.Instance.onGameStart.AddListener(createOrder);
        GameManager.Instance.onGameStart.AddListener(onGameStart);
    }

    private void OnDisable()
    {
        GameManager.Instance.onGameStart.RemoveListener(createOrder);
        GameManager.Instance.onGameStart.RemoveListener(onGameStart);
    }

    public void onGameStart(){
         StartCoroutine(TrackRoundTime());
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
    public void finnishOrder(Order order, CustomerAgent agent)
    {
        CocktailRecipe recipe = phaseManager.getRecipe(order.recipieID);
        List<IngredientBase> ideal_List = recipe.ingredients.ToList();
        List<IngredientBase> order_List = order.containerLimited.getIngreidentsAsOrderedeList();
        float timeTaken = Time.timeSinceLevelLoad - order.startPoint;
        float score = 0f;
        try{
             score = recipeManager.compareTwoIngridienseList(ideal_List, order_List, order.recipieID,order.containerLimited.glassType, timeTaken, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour);
        }catch(System.Exception e)
        {
            Debug.LogError("Error in order delivery: " + e.Message);
        }
        

        totalScore += score;

        if(scoreCounter != null)
            scoreCounter.text = $"Score: {totalScore}";

        Debug.Log($"Order {order.orderID} finished with score: {score}");
        agent.AddObjectToHand(order.containerLimited.gameObject);

        agent.reachedDestination.RemoveAllListeners();
        agent.reachedDestination.AddListener(agent.destroyAgent);
        agent.setDestination(agentEndPoint);

        availableSpawnPoints.Add(order.location);
        currentOrderList.Remove(order.orderID);
        phaseManager.updatePhaseIndex();
        if(!gameFinished){
            StartCoroutine(GenerateOrders());
        }    
    }

    /// <summary>
    /// Creates a new order if there are available spawn points.
    /// </summary>
    [ContextMenu("Generate New Order")]
    public void createOrder()
    {
        Debug.Log("Creating new order");
        if (availableSpawnPoints.Count <= 0){
            Debug.Log("No available spawn points for new order.");
            return;
        }
        Transform spawnPoint = availableSpawnPoints[0];
        availableSpawnPoints.RemoveAt(0);
        NavMeshHit hit;
        if (!agentPoitionSet && NavMesh.SamplePosition(agentSpawnPoint.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agentPoitionSet = true;
            agentSpawnPoint.position = hit.position;
        }
        GameObject agent = Instantiate(Agent_prefab[(int)Random.Range(0,Agent_prefab.Length)], agentSpawnPoint.position, Quaternion.identity);
        CustomerAgent customerAgenet = agent.GetComponent<CustomerAgent>();
        customerAgenet.reachedDestination.AddListener(placeOrder);
        customerAgenet.setDestination(customerOrderDistination);
        customerAgenet.orderDestination = spawnPoint;
    }

    /// <summary>
    /// Places an order at the customer's location.
    /// </summary>
    public void placeOrder(CustomerAgent agent)
    {
        Transform spawnPoint = agent.orderDestination;
        string keyRecipe;
        CocktailRecipe recipe = recipeManager.getCocktailRecipe(out keyRecipe);
        string orderName = recipe.Name +"#" +Mathf.FloorToInt((Time.timeSinceLevelLoad * 100));

        Order order = new Order(keyRecipe, orderName, spawnPoint);
        GameObject textProbemt = null;
        if(Text_prefab != null && parent_to_text != null)
        {
            textProbemt = Instantiate(Text_prefab, parent_to_text.transform);
            textProbemt.SetActive(true);
            textProbemt.GetComponent<TMP_Text>().text = $"{orderName} - {recipe.Name}: " +
            string.Join(" ", recipe.ingredients.Select(i => $"[{i.Name} {i.Amount}]"));
        }


        GameObject deliverArea = Instantiate(deliverArea_Prefab, spawnPoint.position, Quaternion.identity);
        DeliverOrderArea deliverOrderArea = deliverArea.GetComponent<DeliverOrderArea>();
        deliverOrderArea.tex_feild = textProbemt;
        deliverOrderArea.order = order;
        deliverOrderArea.orderDeliverede.AddListener(finnishOrder);
        deliverOrderArea.agent = agent;

        currentOrderList.Add(orderName, order);

        agent.reachedDestination.RemoveAllListeners();
        //agent.startOrder(orderName, order);
    }

    /// <summary>
    /// Coroutine to track the round time and set gameFinished to true when time elapses.
    /// </summary>
    private IEnumerator TrackRoundTime()
    {
        yield return new WaitForSeconds(gameSettings.roundTime*60f);
        gameFinished = true;
        GameManager.Instance.TriggerFSM("GameOver");
    }
}
