using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class HandAnimationController : MonoBehaviour
{
    public InputActionProperty pintchAni;
    public InputActionProperty grabAni;

    public Animator handAni;

    void Update()
    {
        float pintchVal = pintchAni.action.ReadValue<float>();
        handAni.SetFloat("Pintch", pintchVal);

        float grabVal = grabAni.action.ReadValue<float>();
        handAni.SetFloat("Grab", grabVal);
    }

    void FixedUpdate()
    {
        ResetHandAnimation();
    }
    public void ResetHandAnimation()
    {
        handAni.SetFloat("Pintch", 0f);
        handAni.SetFloat("Grab", 0f);
    }

}
