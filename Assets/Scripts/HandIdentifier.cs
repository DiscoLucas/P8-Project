// Attach this to your Left & Right Direct Interactors
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandIdentifier : MonoBehaviour
{
    public XRNode handNode; // Set to LeftHand or RightHand in Inspector

    public XRNode GetNode() => handNode;
}

