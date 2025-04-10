using UnityEngine;

public class IngredientDisplayOrienter : MonoBehaviour
{
    Canvas c;

    void Start()
    {
        c = GetComponent<Canvas>();
    }

    void LateUpdate() {
        if(c.isActiveAndEnabled)
    transform.forward = Camera.main.transform.forward;
}
}
