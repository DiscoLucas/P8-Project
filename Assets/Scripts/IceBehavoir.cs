using System.Collections;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using UnityEngine;

public class IceBehavoir : MonoBehaviour
{
    [SerializeField]
    GameSettings gameSettings;
    private bool isMelting = true;

    [SerializeField]
    IngredientScribtiableObject iceConfig;

    public void Start()
    {
        if(gameSettings == null)
            gameSettings = GameManager.Instance.gameSettings;
        
        Invoke("MeltIce", gameSettings.graceTime);
    }

    IEnumerator MeltIce()
    {
        Vector3 originalScale = transform.localScale;

        // Start melting the ice
        float elapsedTime = 0f;
        while (elapsedTime < gameSettings.meltTime)
        {
            
            transform.localScale = Vector3.Lerp(originalScale, originalScale / 100, elapsedTime / gameSettings.meltTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Destroy the ice object after melting
        Destroy(gameObject);
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("VR Hand"))
        {
            if (!isMelting)
            {
                isMelting = true;
                // Start the melting process after a grace period
                Invoke("MeltIce", gameSettings.graceTime);
            }
        }
        else if(collision.gameObject.GetComponent<LiquidContainerLimited>() != null)
        {
            LiquidContainerLimited liquidContainer = collision.gameObject.GetComponent<LiquidContainerLimited>();
            IngredientBase iceBase = iceConfig.ingredientBase;
            bool iceAdded = liquidContainer.addIceToContainer(iceBase);
            if(iceAdded)
                Destroy(gameObject);
        }
    }
}
