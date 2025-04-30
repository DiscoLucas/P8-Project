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
    public bool isOrderDelivered = false;

    [Header("Audio Stuff")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip deliveryClip;
    [SerializeField] float minPitch = 0.8f;
    [SerializeField] float maxPitch = 1.2f;
    [SerializeField] float volume = 0.5f;

    public void Start()
    {
        text_title.text = order.orderID;
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.tag == glassTag && !isOrderDelivered) {

            
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
                Debug.Log("Delivering order: " + order.orderID + " with agent: " + agent.name);
                try{
                    orderDeliverede.Invoke(order, agent);
                }catch(System.Exception e)
                {
                    Debug.LogError("Error delivering order because of:\n " + e.Message);
                    Destroy(other.gameObject);
                }
                isOrderDelivered = true;
                audioSource.clip = deliveryClip;
                audioSource.volume = volume;
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.Play();
                Debug.Log("Delivering order: " + order.orderID + " with agent: " + agent.name + " and order: " + order.orderID + " is delivered and removing: " + gameObject.name);
                Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if(tex_feild != null)
            Destroy(tex_feild);
        
    }
}
