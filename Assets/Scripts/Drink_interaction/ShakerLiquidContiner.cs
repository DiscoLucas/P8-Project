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
        newIngredientsNeedShaking = true;
        shakeCount = 0; // Reset shake count when new ingredients are added
        lastPosition = transform.position;
        lastDirection = Vector3.zero;
        Debug.Log($"New ingredient added: {newIngredientsNeedShaking}");
        base.AddIngredient(ingredient, inputAmount);
    }

    public override void AddIngredient(IngredientBase ingredient, float inputAmount, out float actualAddedAmount)
    {
        newIngredientsNeedShaking = true;
        shakeCount = 0; // Reset shake count when new ingredients are added
        lastPosition = transform.position;
        lastDirection = Vector3.zero;
        Debug.Log($"New ingredient added: {newIngredientsNeedShaking}");
        base.AddIngredient(ingredient, inputAmount, out actualAddedAmount);
    }
    

    private void FixedUpdate()
    {
        if(pouringSession && hapticCoroutine != null){
                pouringSession = false;
        }
        Debug.Log($"FixedUpdate: newIngredientsNeedShaking: {newIngredientsNeedShaking}, shakeCount: {shakeCount}");
        if (newIngredientsNeedShaking && shakerLiquidPourer != null && shakerLiquidPourer.canShake())
        {
            Debug.Log("Shake detected in FixedUpdate!");
            DetectShaking();
        }
    }

    private void DetectShaking()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movementDirection = (currentPosition - lastPosition).normalized;
        float movementMagnitude = (currentPosition - lastPosition).magnitude;

        bool directionChanged = Vector3.Dot(movementDirection, lastDirection) < 0;
        bool isShaking = movementMagnitude > minShakeForce && directionChanged;
        Debug.Log($"Movement Direction: {movementDirection}, Magnitude: {movementMagnitude}, Direction Changed: {directionChanged}, Is Shaking: {isShaking}");
        if (isShaking)
        {
            shakeCount++;
            Debug.Log($"Shake detected! Count: {shakeCount}");

            // Play audio
            if (!audioSource.isPlaying) // Prevent overlapping
            {
                audioSource.clip = shakeSound;
                audioSource.volume = shakeVolume;
                audioSource.pitch = Random.Range(shackerMinPitch, shackerMaxPitch);
                audioSource.PlayOneShot(shakeSound);
            }

            // Haptic feedback
            sendOneShotHaptic(fillHapticIntensity, fillHapticDuration);
        }

        lastPosition = currentPosition;
        lastDirection = movementDirection;

        if (shakeCount >= amountOfShakes)
        {
            newIngredientsNeedShaking = false;
            Debug.Log("Shaking complete! Ingredients are now shaken.");
            UpdateIngredientsToShaken();
            iceCount = 0;
            for(int i = 0; i < iceFill.childCount; i++){
                iceFill.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    private void UpdateIngredientsToShaken()
    {
        foreach (var ingredient in ingredients.Values)
        {
            ingredient.step.action = DrinkAction.Shaked;
        }
    }

    public override void setGarnish(GameObject garnish)
    {
    }
}
