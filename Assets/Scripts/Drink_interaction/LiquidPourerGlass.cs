using UnityEngine;
using System.Collections;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;

public class LiquidPourerGlass : LiquidPourer
{
    [Header("Glass Pouring Settings")]
    [SerializeField] private ParticleSystem streamParticles;
    [SerializeField] private ParticleSystem splashParticles;
    [SerializeField] private float raycastDistance = 0.5f;
    [SerializeField] private float streamDuration = 0.3f;  // How long the fast stream lasts

    private bool isPouringStream = false;

    void Update()
    {
        if (isPouring() && !isPouringStream)
        {
            StartCoroutine(PourLiquid());
        }
    }

    private IEnumerator PourLiquid()
    {
        isPouringStream = true;


        if (streamParticles != null)
        {
            streamParticles.transform.position = pourPoint.position;
            streamParticles.Play();
        }

        yield return new WaitForSeconds(streamDuration);  

        detectCollision(); 

        isPouringStream = false;
    }

    protected override void detectCollision()
    {
        if (liquidContainer == null) return;

        float transferAmount = liquidContainer.getCurrentLiquidAmount(); 
        if (Physics.Raycast(pourPoint.position, Vector3.down, out RaycastHit hit, raycastDistance, collisionLayers))
        {
            LiquidContainerLimited targetContainer = hit.collider.GetComponent<LiquidContainerLimited>();
            if (targetContainer != null)
            {
                IngredientBase pouredMixture = liquidContainer.createPouredMixture(transferAmount);
                if (pouredMixture != null)
                {
                    targetContainer.AddIngredient(pouredMixture, transferAmount);
                }
            }
            if (splashParticles != null)
            {
                splashParticles.transform.position = hit.point;
                splashParticles.Play();
            }
        }
        else
        {
            if (splashParticles != null)
            {
                splashParticles.transform.position = pourPoint.position + Vector3.down * raycastDistance;
                splashParticles.Play();
            }
        }
        liquidContainer.depleateLiqued(transferAmount);
    }

    protected override void emitParticles()
    {

    }
}
