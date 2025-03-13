using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Orders;
using UnityEngine;
using UnityEngine.Events;

public class DeliverOrderArea : MonoBehaviour
{
    private string glassTag = "Glass";
    public UnityEvent<Order,CustomerAgenet> orderDeliverede = new UnityEvent<Order, CustomerAgenet>();
    public Order order;
    public GameObject tex_feild;
    public CustomerAgenet agent;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == glassTag) {
            LiquidContainerLimited container = other.GetComponent<LiquidContainerLimited>();
            order.containerLimited = container;
            orderDeliverede.Invoke(order, agent);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Destroy(tex_feild);
    }
}
