using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using UnityEngine.XR;
using System.Collections.Generic;
using Oculus.Interaction;
using Unity.VisualScripting;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class DebugClassMenu : MonoBehaviour
{
    [Header("Liquid Display")]
    public GameObject liquidDisplay;
    [SerializeField]
    Slider drinkSlider;
    [SerializeField]
    TMP_Text glassTypeText, IceInText, alcoholTypeText, softDrinkText, garnishText;

    [Header("Object State")]
    [SerializeField]
    Transform stirredStateObject, ShakenStateObject, StrainedStateObject;
    [SerializeField]
    string iceInTextString = "Ice", noIceTextString = "No Ice";
    string softDrinkContain = "", alcoholDrinkContain = "";

    [Header("Liquid Container")]
    public LiquidContainerLimited liquidContainer;

    private bool lastButtonState = false;
    private InputDevice currentDevice; 
    bool isGrabbed = false;
    private List<InputDevice> devicesWithPrimaryButton;

    [Header("Grabinteractiable")]
    public XRGrabInteractable grabInteractable;

    void Start()
    {
        if (liquidContainer == null)
            liquidContainer = GetComponent<LiquidContainerLimited>();
        if(grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
        if(grabInteractable == null)
            grabInteractable = transform.parent.GetComponent<XRGrabInteractable>();
        if(liquidContainer == null)
            liquidContainer = transform.parent.GetComponent<LiquidContainerLimited>();


        liquidDisplay.SetActive(false);
        stirredStateObject.gameObject.SetActive(false);
        ShakenStateObject.gameObject.SetActive(false);
        StrainedStateObject.gameObject.SetActive(false);

        devicesWithPrimaryButton = new List<InputDevice>();
        InitializeDevices();
    }


    void OnEnable()
    {
        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
        grabInteractable.selectEntered.AddListener(toogleGrabbedState);
        grabInteractable.selectExited.AddListener(toogleGrabbedState); 
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
        devicesWithPrimaryButton.Clear();
        grabInteractable.selectEntered.RemoveListener(toogleGrabbedState);
        grabInteractable.selectExited.RemoveListener(toogleGrabbedState);
    }

    private void InitializeDevices()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach (InputDevice device in allDevices)
            InputDevices_deviceConnected(device);
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
        {
            devicesWithPrimaryButton.Add(device); // Add any devices that have a primary button.
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        if (devicesWithPrimaryButton.Contains(device))
            devicesWithPrimaryButton.Remove(device);
    }

    void Update()
    {
        bool tempState = false;
        foreach (var device in devicesWithPrimaryButton)
        {
            bool primaryButtonState = false;
            tempState = device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out primaryButtonState) // did get a value
                        && primaryButtonState // the value we got
                        || tempState; // cumulative result from other controllers
        }

        if (tempState != lastButtonState) // Button state changed since last frame
        {
            if (tempState) // Button pressed
            {
                ToggleDisplay();
            }
            lastButtonState = tempState;
        }
    }

    private void ToggleDisplay()
    {
        if (liquidDisplay != null)
        {
            bool isActive = liquidDisplay.activeSelf;
            liquidDisplay.SetActive(!isActive);

            if (!isActive)
            {
                updateLiquidDisplay(); // Update the display when showing the menu
            }
        }
    }

    void updateTheIngredientDisplay()
    {
        softDrinkContain = "";
        alcoholDrinkContain = "";
        Debug.Log(" Looping through ingredients: " + liquidContainer.ingredients.Count);
        foreach (IngredientBase ingredientBase in liquidContainer.ingredients.Values)
        {
            Debug.Log("Drink step: " + ingredientBase.step.action);
            if (ingredientBase.Type == IngredientType.Mixer || ingredientBase.Type == IngredientType.Sirup)
            {
                softDrinkContain += $"\n[{ingredientBase.Amount}]{ingredientBase.Name},";
            }
            else if (ingredientBase.Type == IngredientType.Spirit)
            {
                alcoholDrinkContain += $"\n[{ingredientBase.Amount}]{ingredientBase.Name},";
            }
            if (ingredientBase.step.action == DrinkAction.Stirred)
            {
                stirredStateObject.gameObject.SetActive(true);
            }
            if (ingredientBase.step.action == DrinkAction.Shaked)
            {
                ShakenStateObject.gameObject.SetActive(true);
            }
            if (ingredientBase.step.action == DrinkAction.Strained)
            {
                StrainedStateObject.gameObject.SetActive(true);
            }
        }
    }

    public void updateLiquidDisplay()
    {
        updateTheIngredientDisplay();
        drinkSlider.value = liquidContainer.FillPercentage();
        glassTypeText.text = liquidContainer.glassType.ToString();
        IceInText.text = liquidContainer.hasIce ? iceInTextString : noIceTextString;
        alcoholTypeText.text = alcoholDrinkContain;
        softDrinkText.text = softDrinkContain;
        garnishText.text = liquidContainer.garnishIngredient != null ? liquidContainer.garnishIngredient.Name : "No Garnish";
    }

    void toogleGrabbedState(SelectEnterEventArgs args)
    {
        isGrabbed = true;

    }

    void toogleGrabbedState(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }
}
