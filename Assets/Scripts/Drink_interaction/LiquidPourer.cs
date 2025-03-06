using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.UIElements;
using Assets.Scripts.Ingridence;

public class LiquidPourer : MonoBehaviour
{
    [Header("Drink")]
    public LiquidContainer liquidContainer;
    [Header("Visuals")]
    [SerializeField] protected ParticleSystem particles;
    [SerializeField] protected Transform pourPoint;

    [Header("Properties")]
    [SerializeField] protected float pourMultipliere = 10;
    [SerializeField] protected float pourThreshold = 80f;
    [SerializeField] protected int arcResolution = 30;
    [SerializeField] protected float timeStep = 0.05f;
    [SerializeField] protected float gravity = 9.81f;
    [SerializeField] protected LayerMask collisionLayers;
    [SerializeField] protected float pourAmount = 0.01f;

    [SerializeField]
    protected float pourSpeed;
    protected Vector3 lastHitPoint;

    void Start()
    {
        if (particles == null) Debug.LogError("Particale effect have not been assigned");

    }

    void FixedUpdate()
    {
        if (isPouring())
        {
            calculatePouringSpeed();
            emitParticles();
            detectCollision();
        }
        else
        {
            if(particles != null)
                particles.Stop();
        }
    }

    /// <summary>
    /// Check if the bottle is tilted enough to pour.
    /// </summary>
    private bool isPouring()
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
        pourSpeed = Vector3.Dot(transform.up, Vector3.down) * pourMultipliere; 
    }

    /// <summary>
    /// Emit particles and set their velocity.
    /// </summary>
    private List<ParticleSystem.Particle> activeParticles = new List<ParticleSystem.Particle>();

    private void emitParticles()
    {
        if (particles == null) return;

        var main = particles.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 2f;
        main.startSpeed = 0;

        Color liquidColor = liquidContainer.getLiquidColor();

        var renderer = particles.GetComponent<ParticleSystemRenderer>();

        if (renderer != null && liquidContainer.materialHaveBeenChange)
        {
            renderer.material = new Material(renderer.material);
            renderer.trailMaterial = new Material(renderer.trailMaterial);
            renderer.material.color = liquidColor;
            renderer.trailMaterial.color = liquidColor;
            liquidContainer.materialHaveBeenChange = false; 
        }

        if (!particles.isPlaying)
        {
            particles.Play();
        }

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = pourPoint.position;
        emitParams.velocity = pourPoint.up * pourSpeed;
        particles.Emit(emitParams, 1);
    }




    /// <summary>
    /// Detect where the liquid lands.
    /// </summary>
    private void detectCollision()
    {
        Vector3 start = pourPoint.position;
        Vector3 velocity = pourPoint.up * pourSpeed;
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
                    IngredientBase pouredMixture = liquidContainer.createPouredMixture(pourAmount);
                    if (pouredMixture != null)
                    {
                        glass.AddIngredient(pouredMixture, pourAmount);
                    }
                }
                break;
            }

            point = newPoint;
        }
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
}
