using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class CustomerAgent : MonoBehaviour
{
    [SerializeField]
    NavMeshAgent navMeshAgent;
    [SerializeField]
    bool isMoving = false;
    [SerializeField]
    public Transform destination;
    [SerializeField]
    float minDistance = 0.2f;
    [SerializeField]
    public UnityEvent<CustomerAgent> reachedDestination;
    [SerializeField]
    Transform hand;
    [SerializeField]
    Animator animator; // Reference to the Animator component
    [SerializeField]
    GameObject model;
    [SerializeField]
    float modelRoationSpeed = 10f;

    public void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>(); // Automatically get Animator if not assigned
            if (animator == null)
            {
                Debug.LogError("Animator component is missing on the customer!");
            }
        }

        if(model == null)
        {
            model = gameObject; 
        }
    }

    public void setDestination(Transform destination)
    {
        this.destination = destination;
        bool pointSet = navMeshAgent.SetDestination(destination.position);
        navMeshAgent.isStopped = false;
        isMoving = true;
    }

    public bool nearDestination()
    {
        if (destination == null)
            return false;
        return (Vector3.Distance(destination.position, transform.position) < minDistance);
    }

    private void FixedUpdate()
    {
        animator.SetFloat("WalkSpeed", navMeshAgent.velocity.magnitude/navMeshAgent.speed);
        
        if (navMeshAgent.hasPath && nearDestination() &&isMoving)
        {

            Vector3 directionToDestination = (destination.position - transform.position).normalized;
            StartCoroutine(RotateOverTime(directionToDestination));
            Debug.Log("Reached destination: " + destination.name);
            reachedDestination.Invoke(this);
            navMeshAgent.isStopped = true; 
            isMoving = false;
        }
        else if(isMoving)
        {
            if (navMeshAgent.velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(navMeshAgent.velocity.normalized);
                model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRotation, Time.deltaTime * modelRoationSpeed);
            }   
        }
    }

    private IEnumerator RotateOverTime(Vector3 direction)
    {
        // Only get the Y rotation we want to face
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsedTime = 0f;
        Quaternion startRotation = model.transform.rotation;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * modelRoationSpeed;
            // Create new rotation that only affects Y axis
            Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime);
            model.transform.rotation = Quaternion.Euler(0, newRotation.eulerAngles.y, 0);
            yield return null;
        }

        // Final rotation - ensure we're exactly at target Y rotation
        model.transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    }
    

    public void startOrder(string orderName, Order order)
    {
    }

    public void endOrder()
    {
        animator.SetBool("HoldingDrink", true);
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

    public void destroyAgent(CustomerAgent agent)
    {
        Destroy(agent.gameObject);
    }
}