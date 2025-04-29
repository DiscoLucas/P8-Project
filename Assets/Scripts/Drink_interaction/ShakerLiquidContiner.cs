using UnityEngine;
using Assets.Scripts.Ingridence;
using Assets.Scripts.Drink_interaction;

public class ShakerLiquidContiner : LiquidContainerLimited
{
    [Header("Shaker Stuff IG")]
    public ShakerLiquidPourer shakerLiquidPourer;
    bool newIngredientsNeedShaking = false;
    [SerializeField]
    int amountOfShakes = 6;
    [SerializeField]
    float minShakeForce = 0.1f;

    private int shakeCount = 0;
    private Vector3 lastPosition;
    private Vector3 lastDirection;

    [Header("Shacker Audio or smthin")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip shakeSound;
    [SerializeField] float shakeVolume = 1f;
    [SerializeField] float shackerMaxPitch = 1.2f;
    [SerializeField] float shackerMinPitch = 0.8f;


    public override void AddIngredient(IngredientBase ingredient, float inputAmount)
    {
        base.AddIngredient(ingredient, inputAmount);
        newIngredientsNeedShaking = true;
        shakeCount = 0; // Reset shake count when new ingredients are added
        lastPosition = transform.position;
        lastDirection = Vector3.zero;
    }



    private void FixedUpdate()
    {
        if (newIngredientsNeedShaking && shakerLiquidPourer != null && shakerLiquidPourer.canShake())
        {
            // Check if the shaker is being shaken
            if (Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(minShakeForce * Mathf.Deg2Rad))
            {
                // Call the shaking detection method
                DetectShaking();
            }
        }
        else if (newIngredientsNeedShaking)
        {
            DetectShaking();
        }
    }

    private void DetectShaking()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movementDirection = (currentPosition - lastPosition).normalized;
        float movementMagnitude = (currentPosition - lastPosition).magnitude;

        bool directionChanged = Vector3.Dot(movementDirection, lastDirection) < 0;

        if (movementMagnitude >= minShakeForce && directionChanged)
        {
            shakeCount++;
            Debug.Log($"Shake detected! Count: {shakeCount}");
            sendOneShotHaptic(fillHapticIntensity, fillHapticDuration);
            audioSource.pitch = Random.Range(shackerMinPitch, shackerMaxPitch);
            audioSource.PlayOneShot(shakeSound, shakeVolume);
        }

        lastPosition = currentPosition;
        lastDirection = movementDirection;

        if (shakeCount >= amountOfShakes)
        {
            newIngredientsNeedShaking = false;
            Debug.Log("Shaking complete! Ingredients are now shaken.");
            UpdateIngredientsToShaken();
        }
    }

    private void UpdateIngredientsToShaken()
    {
        foreach (var ingredient in ingredients.Values)
        {
            ingredient.step.action = DrinkAction.Shaked;
        }
    }
}
