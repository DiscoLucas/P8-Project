using Assets.Scripts.Orders;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class CustomerAgenet : MonoBehaviour
{
    [SerializeField]
    NavMeshAgent navMeshAgent;
    [SerializeField]
    public Transform destionation;
    [SerializeField]
    float minDistance = 0.2f;
    [SerializeField]
    public UnityEvent<CustomerAgenet> reachedDistation;
    [SerializeField]
    Transform hand;
    public void Start()
    {
        
    }

    public void setDestionation(Transform destionation) {
        this.destionation = destionation;
        navMeshAgent.SetDestination(destionation.position);
    }

    public bool nearDistination() {
        if(destionation == null)
            return false;
        return (Vector3.Distance(destionation.position, transform.position) < minDistance);
    }


    public void startOrder(string orderName, Order order)
    {
        
    }
    private void FixedUpdate()
    {
        if (navMeshAgent.hasPath && nearDistination()) {
            reachedDistation.Invoke(this);
        }
    }

    public void AddObjectToHand(GameObject objectToHand)
    {
        if (objectToHand == null || hand == null)
        {
            Debug.LogWarning("Object or Hand is null!");
            return;
        }

        // Remove XR components first (before Rigidbody)
        if (objectToHand.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab))
        {
            Destroy(grab);
        }
        if (objectToHand.TryGetComponent<XRGeneralGrabTransformer>(out XRGeneralGrabTransformer grabTransformer))
        {
            Destroy(grabTransformer);
        }

        // Remove Rigidbody after XR components
        if (objectToHand.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true; // Prevent physics effects before removing
            rb.useGravity = false;
            Destroy(rb);
        }

        // Set as child of hand (using workaround if SetParent fails)
        StartCoroutine(SetParentAfterFrame(objectToHand));
    }

    // Workaround: Set Parent after a frame delay
    private IEnumerator SetParentAfterFrame(GameObject obj)
    {
        yield return new WaitForEndOfFrame(); // Wait until next frame
        obj.transform.SetParent(hand, false); // Now set the parent
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }




    public void destoryAgent(CustomerAgenet agent) { 
        Destroy(agent.gameObject);
    }


}
