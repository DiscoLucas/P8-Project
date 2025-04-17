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
                    // Get the local up direction of the glass
                    Vector3 glassUp = glass.transform.up;

                    // Compare hit normal to the glass's up direction
                    if (Vector3.Dot(hit.normal, glassUp) > hitThreashold) // Adjust threshold as needed
                    {
                        IngredientBase pouredMixture = getIngredientBase();
                        Debug.Log("Pouring " + pouredMixture.Name + " into " + glass.name);
                        if (pouredMixture != null)
                        {
                            glass.AddIngredient(pouredMixture, pourAmount);
                        }
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
}
