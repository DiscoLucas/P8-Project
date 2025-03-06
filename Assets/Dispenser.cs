using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DispenseObjectFunction : MonoBehaviour
{
    public GameObject DispensedObject;
    XRGrabInteractable handle;

    void Start()
    {
        handle = GetComponent<XRGrabInteractable>();
    }

    void Update()
    {
        // Check if it's a VR hand and it has not been grabbed yet
        if (handle.isSelected)
        {
            GameObject DispensedObj = Instantiate<GameObject>(DispensedObject,this.transform.position, this.transform.rotation);
            Rigidbody drb = DispensedObj.GetComponent<Rigidbody>();
            drb.isKinematic = false;
            drb.useGravity = true;
            Destroy(this);
        }
    }
}