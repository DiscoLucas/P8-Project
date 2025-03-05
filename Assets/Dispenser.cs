using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Dispenser : MonoBehaviour
{
    public GameObject DispensedObject;
    XRGrabInteractable grab;
    private bool hasBeenGrabbed = false; // Flag to track if the hand has grabbed the dispenser

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if it's a VR hand and it has not been grabbed yet
        if (other.CompareTag("VR Hand") && !hasBeenGrabbed && grab.isSelected)
        {
            Debug.Log("ugga");
            hasBeenGrabbed = true; // Set the flag to true so it won't trigger again
            GameObject DispensedObj = Instantiate<GameObject>(DispensedObject, other.transform.position,other.transform.rotation); //I want to rename DispensedObj

            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Reset the flag when the hand exits the dispenser, allowing the message to trigger again when the hand re-enters
        if (other.CompareTag("VR Hand"))
        {
            hasBeenGrabbed = false;
        }
    }
}