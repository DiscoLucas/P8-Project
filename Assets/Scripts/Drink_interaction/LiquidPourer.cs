using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.UIElements;
using Assets.Scripts.Ingridence;
using TMPro;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Unity.VisualScripting;
using System;
using UnityEngine.InputSystem.XR.Haptics;
public class LiquidPourer : MonoBehaviour
{
    [Header("Game settings")]
    public GameSettings gameSettings;
    [Header("Drink")]
    public LiquidContainer liquidContainer;
    [Header("Visuals")]
    [SerializeField] protected ParticleSystem particles;
    [SerializeField] protected Transform pourPoint;

    [Header("Properties")]
    [SerializeField] internal bool deepleteWithooutConatiner = false;
    [SerializeField] protected float pourMultiplier = 8;
    [SerializeField] protected float pourThreshold = 80f;
    [SerializeField] protected int arcResolution = 10;
    [SerializeField] protected float timeStep = 0.05f;
    [SerializeField] protected float gravity = 9.81f;
    [SerializeField] protected LayerMask collisionLayers;
    [SerializeField] protected float pourAmount = 0.01f;
    [Tooltip("Defines how strictly the liquid must hit the top of the glass to be considered valid. A value closer to 1 means only near-perfect top hits count, while lower values allow slight angles.")]
    [SerializeField] protected float hitThreashold = 0.5f;
    [SerializeField]
    protected float pourSpeed;
    protected Vector3 lastHitPoint;

    [SerializeField]
    internal TMP_Text fillamountText;
    [SerializeField]
    private float disableTextDelay = 5f; // Time in seconds before disabling the text

    private Coroutine disableTextCoroutine;

    [Header("Haptics")]
    [Range(0,1)]
    public float intensity = 0.01f;
    public float duration = 0.04f;
    internal Coroutine hapticCoroutine;
    [SerializeField] internal HapticImpulsePlayer currentController;
    [SerializeField] internal float minIntensity = 0.01f;
    [SerializeField] internal float maxIntensity = 0.7f;
    [SerializeField] internal float routineWait = 0.05f;

    [Header("Grab")]
    [SerializeField] internal XRGrabInteractable grabInteractable;
    [Header("Audio")]
    [SerializeField]internal AudioSource liquid_audioSource;
    [SerializeField]internal float minvolunme = 0.1f;
    [SerializeField]internal float maxvolunme = 0.5f;
    [SerializeField]internal float minPitch = 0.8f;
    [SerializeField]internal float maxPitch = 1.2f;
    private void Start()
    {
        gameSettings = GameManager.Instance.gameSettings;
        if (particles == null) Debug.LogError("Particale effect have not been assigned");

        if (fillamountText != null)
        {
            fillamountText.gameObject.SetActive(false); // Ensure the text is initially disabled
        }
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.selectEntered.AddListener(OnGrab);
        updateHapticSettings();

        
    }

    public virtual void updateHapticSettings()
    {
        if(gameSettings == null) return;
        duration = gameSettings.HapticDuration;
        minIntensity = gameSettings.HapticMinIntensity;
        maxIntensity = gameSettings.HapticMaxIntensity;
        routineWait = gameSettings.HapticRoutineWait;
    }


    private void OnGrab(SelectEnterEventArgs arg0)
    {
        findHapticController(arg0);
    }

    internal void findHapticController(SelectEnterEventArgs arg0){
        currentController = arg0.interactorObject.transform.parent.GetComponent<HapticImpulsePlayer>();
        if (currentController == null)
        {
            Debug.LogWarning("Interactor does not have a HapticImpulsePlayer component.");
            return;
        }

        Debug.Log("Haptic controller found: " + currentController.name);
    }

    private void OnRelease(SelectExitEventArgs arg0)
    {
        currentController = null;
    }

    internal void ShowFillAmountText(string text)
    {
        
        if (fillamountText != null)
        {
            fillamountText.text = text;
            fillamountText.gameObject.SetActive(true);

            // Restart the coroutine to disable the text after the delay
            if (disableTextCoroutine != null)
            {
                StopCoroutine(disableTextCoroutine);
            }
            disableTextCoroutine = StartCoroutine(DisableFillAmountTextAfterDelay());
        }
    }

    internal IEnumerator DisableFillAmountTextAfterDelay()
    {
        yield return new WaitForSeconds(disableTextDelay);

        if (fillamountText != null)
        {
            fillamountText.gameObject.SetActive(false);
        }
        lastGlass = null;
    }
    public void playAudio()
    {
        if (liquid_audioSource != null && !liquid_audioSource.isPlaying)
        {
            liquid_audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            liquid_audioSource.Play();
        }
        liquid_audioSource.volume = Mathf.Lerp(minvolunme, maxvolunme, pourSpeed / pourMultiplier);
    }
    void FixedUpdate()
    {
        if (isPouring())
        {
            calculatePouringSpeed();
            emitParticles();
            detectCollision();
            playAudio();
            
            if (hapticCoroutine == null)
                hapticCoroutine = StartCoroutine(HapticFeedbackRoutine(false));
        }
        else
        {
            if(liquid_audioSource.isPlaying)
            {
                liquid_audioSource.Stop();
            }
            currentPourSessionAmout = 0f;
            if (particles != null)
                particles.Stop();

            stopHapticFeedback();
            currentPourSessionAmout = 0f;
        }
    }

    internal void stopHapticFeedback()
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            intensity = 0.01f;
            hapticCoroutine = null;
        }
    }

    /// <summary>
    /// Check if the bottle is tilted enough to pour.
    /// </summary>
    internal virtual bool isPouring()
    {
        bool isPouring = Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad);
        bool haveEnoughtLiqquid = false;
        if (liquidContainer != null)
            haveEnoughtLiqquid = liquidContainer.canPoourer();
        return isPouring && haveEnoughtLiqquid;
    }

    /// <summary>
    /// Calculate the pouring speed based on tilt angle.
    /// </summary>
    private void calculatePouringSpeed()
    {
        pourSpeed = Vector3.Dot(transform.up, Vector3.down) * pourMultiplier; 
    }

    /// <summary>
    /// Emit particles and set their velocity.
    /// </summary>
    private List<ParticleSystem.Particle> activeParticles = new List<ParticleSystem.Particle>();

    protected virtual void emitParticles()
    {
        if (particles == null) return;

        var main = particles.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 2f;
        main.startSpeed = 0;

        Color liquidColor = getLiquidColor();

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();

        if (renderer != null && liquidHaveBeenChange())
        {
            renderer.material = new Material(renderer.material);
            renderer.trailMaterial = new Material(renderer.trailMaterial);
            renderer.material.color = liquidColor;
            renderer.trailMaterial.color = liquidColor;
            changeMaterialFlag();
            
        }

        if (!particles.isPlaying)
        {
            particles.Play();
        }

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = getPourPoint().position;
        emitParams.velocity = getPourPoint().up * pourSpeed;
        particles.Emit(emitParams, 1);
    }

    internal virtual void changeMaterialFlag(){
        liquidContainer.materialHaveBeenChange = false; 
    }

    internal virtual Transform getPourPoint()
    {
        return pourPoint;
    }

    internal float currentPourSessionAmout = 0f;
    internal LiquidContainer lastGlass = null;
    /// <summary>
    /// Detect where the liquid lands.
    /// </summary>
    protected virtual void detectCollision()
    {
        Vector3 start = getPourPoint().position;
        Vector3 velocity = getPourPoint().up * pourSpeed;
        Vector3 point = start;

        for (int i = 0; i < arcResolution; i++)
        {
            float t = i * timeStep;
            Vector3 newPoint = start + (velocity * t) + (0.5f * Physics.gravity * t * t);

            if (Physics.Raycast(point, newPoint - point, out RaycastHit hit, Vector3.Distance(point, newPoint), collisionLayers))
            {
                lastHitPoint = hit.point;
                LiquidContainer glass = hit.collider.GetComponent<LiquidContainer>();

                if (glass != null)
                {
                    Vector3 glassUp = glass.transform.up;
                    if (Vector3.Dot(hit.normal, glassUp) > hitThreashold)
                    {
                        IngredientBase pouredMixture = getIngredientBase();
                        Debug.Log("Pouring into glass: " + glass?.name + " pour mixture: " + pouredMixture?.Name);
                        if (pouredMixture != null)
                        {
                            float actialAmount = 0;
                            glass.AddIngredient(pouredMixture, pourAmount, out actialAmount);
                            Debug.Log("Actual amount out: " + actialAmount);
                            if(lastGlass != glass)
                            {
                                currentPourSessionAmout = 0f;
                                lastGlass = glass;
                            }
                            currentPourSessionAmout += actialAmount;
                            if (fillamountText != null)
                            {
                                ShowFillAmountText($"Pouring: {currentPourSessionAmout:F2}ml");
                            }
                        }
                    }
                } else if (deepleteWithooutConatiner){
                    // Deplete the liquid in the container
                    liquidContainer.depleateLiqued(pourAmount);
                    if(lastGlass != glass)
                    {
                        currentPourSessionAmout = 0f;
                        lastGlass = glass;
                    }
                    currentPourSessionAmout += pourAmount;

                    // Update the fill amount text
                    if (fillamountText != null)
                    {
                        ShowFillAmountText($"Pouring: {currentPourSessionAmout:F2}ml");
                    }
                }
                break;
            }

            point = newPoint;
        }    
    }

    internal virtual bool liquidHaveBeenChange()
    {
        return liquidContainer.materialHaveBeenChange;
    }

    internal virtual Color getLiquidColor()
    {
        return liquidContainer.getLiquidColor();
    }
    internal virtual IngredientBase getIngredientBase()
    {
        return liquidContainer.createPouredMixture(pourAmount);
    }




    /// <summary>
    /// Draw the debug pouring arc in the scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!isPouring()) return;

        Gizmos.color = Color.cyan;
        Vector3 start = pourPoint.position;
        Vector3 velocity = pourPoint.up * pourSpeed;
        Vector3 point = start;

        for (int i = 0; i < arcResolution; i++)
        {
            float t = i * timeStep;
            Vector3 newPoint = start + (velocity * t) + (0.5f * Physics.gravity * t * t);
            Gizmos.DrawLine(point, newPoint);
            point = newPoint;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastHitPoint, 0.02f);
    }




    /// <summary>
    /// Depleate the liqued in the container
    /// </summary>
    public void depleateLiqued()
    {
        liquidContainer.depleateLiqued(pourAmount);
    }

    internal IEnumerator HapticFeedbackRoutine(bool needToFireHaptic)
    {
        
        while (isPouring()||needToFireHaptic)
        {
            if (currentController != null)
                currentController.SendHapticImpulse(intensity, duration);

            // Increase intensity, but clamp to 1
            intensity = Mathf.Min(intensity + minIntensity, maxIntensity);

            yield return new WaitForSeconds(routineWait);
        }

    }
}
