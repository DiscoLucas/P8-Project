using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TomatoEffect : MonoBehaviour
{

    public Transform handVisual;
    public NearFarInteractor handTechnical;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        if(handTechnical.interactablesSelected.Count == 0)
        {
            handVisual.localScale = Vector3.one;
        }
        else
        {
            handVisual.localScale = Vector3.zero;
        }
    }
}
