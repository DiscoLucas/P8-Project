using System;
using Assets.Scripts.Ingridence;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
public class SodaGunInteractable : LiquidPourer
{
    private XRGrabInteractable grabInteractable;
    private UnityEngine.XR.InputDevice currentDevice;
    private bool isHeld = false;

    private bool lastPrimaryButtonState = false;
    private bool lastSecondaryButtonState = false;

    [Header("Ingreidents")]
    [SerializeField]
    IngredientScribtiableObject[] ingredients; 
    [SerializeField]
    int currentIngredientIndex = 0;
    [SerializeField]
    bool liquidHaveBeenChanged = false;

    bool isShooting = false;

    [Header("Display")]
    public TMP_Text displayText;
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
        grabInteractable.activated.AddListener(OnFireGun);
        grabInteractable.deactivated.AddListener(OnStopFireGun);
        liquidHaveBeenChanged = true;
        updateDisplay();
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        grabInteractable.selectExited.RemoveListener(OnSelectExited);
        grabInteractable.activated.RemoveListener(OnFireGun);
        grabInteractable.deactivated.AddListener(OnStopFireGun);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactorTransform = args.interactorObject.transform;
        var handIdentifier = interactorTransform.GetComponent<HandIdentifier>();

        if (handIdentifier != null)
        {
            var node = handIdentifier.GetNode();
            currentDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
            isHeld = currentDevice.isValid;
        }
        else
        {
            Debug.LogError("HandIdentifier not found on interactor transform.");
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isHeld = false;
        currentDevice = default;
        lastPrimaryButtonState = false;
        lastSecondaryButtonState = false;
        isShooting = false;
        if(particles != null)
            particles.Stop();
    }

    private void OnFireGun(ActivateEventArgs arg0)
    {
        isShooting = true;
    }

    private void OnStopFireGun(DeactivateEventArgs arg0)
    {
        isShooting = false;
        if(particles != null)
            particles.Stop();
    }

    void changeLiquidUp(){
        currentIngredientIndex++;
        if(currentIngredientIndex >= ingredients.Length)
        {
            currentIngredientIndex = 0;
        }

        liquidHaveBeenChanged = true;
        updateDisplay();
    }

    void changeLiquidDown(){
        currentIngredientIndex--;
        if(currentIngredientIndex < 0)
        {
            currentIngredientIndex = ingredients.Length - 1;
        }
        liquidHaveBeenChanged = true;
        updateDisplay();
    }

    void updateDisplay(){
        displayText.text = "Current liquid:\n" + ingredients[currentIngredientIndex].ingredientBase.Name;
    }

    void FixedUpdate()
    {
        if(isShooting){
            emitParticles();
            detectCollision();
        }
    }
    private void Update()
    {
        if (!isHeld || !currentDevice.isValid)
        {
            return;
        }

        if (currentDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed))
        {
            if (primaryPressed && !lastPrimaryButtonState)
            {
                changeLiquidUp();
            }
            lastPrimaryButtonState = primaryPressed;
        }

        if (currentDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed))
        {
            if (secondaryPressed && !lastSecondaryButtonState)
            {
                changeLiquidDown();
            }
            lastSecondaryButtonState = secondaryPressed;
        }
    }

    internal override void changeMaterialFlag()
    {
        liquidHaveBeenChanged = false; 
    }

    internal override bool liquidHaveBeenChange()
    {
        return liquidHaveBeenChanged;
    }
    internal override Color getLiquidColor()
    {
        Color liquidColor = ingredients[currentIngredientIndex].ingredientBase.Color;
        Debug.Log("Current liquid color: " + liquidColor);
        return liquidColor;
    }
    internal override IngredientBase getIngredientBase()
    {
        Debug.Log("Current ingredient name: " + ingredients[currentIngredientIndex].ingredientBase.Name);
        return ingredients[currentIngredientIndex].ingredientBase;
    }

}
