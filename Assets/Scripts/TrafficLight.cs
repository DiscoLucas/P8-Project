using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField] private Light greenLight;
    [SerializeField] private Light yellowLight;
    [SerializeField] private Light redLight;
    private bool lightAvailable;

    [Header("Timing")]
    [SerializeField] private float greenTime = 10f;
    [SerializeField] private float yellowTime = 2f;
    [SerializeField] private float redTime = 10f;

    public enum LightState { Green, Yellow, Red }
    private LightState currentState = LightState.Red;

    void Start()
    {
        try 
        {
        greenLight = gameObject.GetNamedChild("Green Light").GetComponent<Light>();
        yellowLight = gameObject.GetNamedChild("Yellow Light").GetComponent<Light>();
        redLight = gameObject.GetNamedChild("Red Light").GetComponent<Light>();
        lightAvailable = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error: " + e.Message);
            lightAvailable = false;
        }
        
    }

    public void Initialize()
    {
        StartCoroutine(LightCycle());
    }

    private IEnumerator LightCycle()
    {
        while (true)
        {
            ChangeLight(LightState.Red);
            yield return new WaitForSeconds(redTime);

            ChangeLight(LightState.Green);
            yield return new WaitForSeconds(greenTime);

            ChangeLight(LightState.Yellow);
            yield return new WaitForSeconds(yellowTime);
        }
    }
    
    private void ChangeLight(LightState newState)
    {
        currentState = newState;

        if (lightAvailable)
        {
            switch (currentState)
            {
                case LightState.Green:
                    greenLight.enabled = true;
                    yellowLight.enabled = false;
                    redLight.enabled = false;
                    break;
                case LightState.Yellow:
                    greenLight.enabled = false;
                    yellowLight.enabled = true;
                    redLight.enabled = false;
                    break;
                case LightState.Red:
                    greenLight.enabled = false;
                    yellowLight.enabled = false;
                    redLight.enabled = true;
                    break;
            }
        }
    }
    

    public bool IsRed()
    {
        return currentState == LightState.Red;
    }

    public bool IsYellow()
    {
        return currentState == LightState.Yellow;
    }
}
