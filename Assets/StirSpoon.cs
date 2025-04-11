using System.Linq;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using UnityEngine;
using UnityEngine.InputSystem;

public class StirSpoon : MonoBehaviour
{
    AudioSource ass;
    Vector3 lastPosition;
    float stirTimer = 0f;
    float stirThreshold = 1f; // seconds of stirring needed
    float stirSpeedThreshold = 0.1f; // minimum movement speed to count as stirring

    void Start()
    {
        ass = GetComponent<AudioSource>();
        lastPosition = transform.position;
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Glass")) return;

        // Spoon is inside the glass
        if (collision.transform.position.y < transform.position.y)
        {
            Vector3 movement = transform.position - lastPosition;
            float speed = movement.magnitude / Time.deltaTime;

            if (speed > stirSpeedThreshold)
            {
                stirTimer += Time.deltaTime;
                Debug.Log($"Stirring... {stirTimer:0.00}s");
            }
            else
            {
                stirTimer = 0f; // reset if they stop moving
            }

            if (stirTimer >= stirThreshold)
            {
                if (!ass.isPlaying)
                {
                    ass.Play();
                    Debug.Log("Spoon stirred enough!");
                }

                LiquidContainerLimited lcl = collision.gameObject.GetComponent<LiquidContainerLimited>();
                foreach (IngredientBase ingredient in lcl.ingredients.Values)
                {
                    ingredient.step.action = DrinkAction.Stirred;
                }

                stirTimer = 0f; // reset after successful stir
            }

            lastPosition = transform.position;
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
