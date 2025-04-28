using System.Linq;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using UnityEngine;
using UnityEngine.InputSystem;

public class StirSpoon : MonoBehaviour
{
    float stirTimer = 0f;
    float stirThreshold = 0.5f; // seconds of stirring needed

    void Start()
    {

    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Glass")) return;

        // Spoon is inside the glass
        if (collision.transform.position.y < transform.position.y)
        {
            stirTimer += Time.deltaTime;
        }
        else
            {
                stirTimer = 0f; // reset if they stop moving
            }

            if (stirTimer >= stirThreshold)
            {

                LiquidContainerLimited lcl = collision.gameObject.GetComponent<LiquidContainerLimited>();
                if (lcl == null) return;
                foreach (IngredientBase ingredient in lcl.ingredients.Values)
                {
                    ingredient.step.action = DrinkAction.Stirred;
                }

                stirTimer = 0f; // reset after successful stir
            }
        }
        
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Glass"))
        {
            stirTimer = 0f; // reset if spoon leaves the glass
        }
    }
}
