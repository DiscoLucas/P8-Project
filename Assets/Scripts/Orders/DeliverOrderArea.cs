using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Orders;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
public class DeliverOrderArea : MonoBehaviour
{
    private string glassTag = "Glass";
    public UnityEvent<Order,CustomerAgent> orderDeliverede = new UnityEvent<Order, CustomerAgent>();
    public Order order;
    public GameObject tex_feild;
    public CustomerAgent agent;
    public TMP_Text text_title;


    public void Start()
    {
        text_title.text = order.orderID;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == glassTag) {
            Debug.Log("Delivering order: " + other.gameObject.name);
            LiquidContainerLimited container = other.GetComponent<LiquidContainerLimited>();
            if (container == null)
            {
                Debug.LogError("LiquidContainerLimited component not found on the object.");
                return;
            }
            if (container.FillPercentage()<= 0)
            {
                Debug.Log("Not enough liquid in the container to deliver the order.");
                return;
            }
            order.containerLimited = container;
            agent.endOrder();
            orderDeliverede.Invoke(order, agent);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if(tex_feild != null)
            Destroy(tex_feild);
        
    }
}
