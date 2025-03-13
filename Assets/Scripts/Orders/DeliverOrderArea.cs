using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Orders;
using UnityEngine;
using UnityEngine.Events;

public class DeliverOrderArea : MonoBehaviour
{
    private string glassTag = "Glass";
    public UnityEvent<Order> orderDeliverede = new UnityEvent<Order>();
    public Order order;
    public GameObject tex_feild;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == glassTag) {
            LiquidContainerLimited container = other.GetComponent<LiquidContainerLimited>();
            order.containerLimited = container;
            orderDeliverede.Invoke(order);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Destroy(tex_feild);
    }
}
