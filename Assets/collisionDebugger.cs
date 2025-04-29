using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class collisionDebugger : MonoBehaviour
{

    public XRSimpleInteractable xr;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(xr.isHovered){
            Debug.LogWarning("Activated");
        }
    }
}
