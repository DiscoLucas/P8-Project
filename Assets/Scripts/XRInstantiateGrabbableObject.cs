using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRInstantiateGrabbableObject : MonoBehaviour
{
    public GameObject DispensedObject;
    XRGrabInteractable handle;
    [SerializeField]
    private Transform transformToInstantiate;

    void Start()
    {
        handle = GetComponent<XRGrabInteractable>();
        handle.selectEntered.AddListener(attachNewPrefab);
    }

    private void attachNewPrefab(SelectEnterEventArgs arg0)
    {
        // Instantiate object
        GameObject newObject = Instantiate(DispensedObject, transformToInstantiate.position, Quaternion.identity);

        // Get grab interactable from prefab
        XRGrabInteractable objectInteractable = newObject.GetComponent<XRGrabInteractable>();

        // Select object into same interactor
        handle.interactionManager.SelectEnter(arg0.interactorObject, objectInteractable);
    }
}