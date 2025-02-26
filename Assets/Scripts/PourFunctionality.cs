using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.UIElements;

public class PourFunctionality : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Transform pourPoint; 

    [Header("Properties")]
    [SerializeField] private float pourMultipliere = 10;
    [SerializeField] private float pourThreshold = 80f;
    [SerializeField] private int arcResolution = 30;
    [SerializeField] private float timeStep = 0.05f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private LayerMask collisionLayers;

    [SerializeField]
    private float pourSpeed;
    private Vector3 lastHitPoint;

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
            Debug.Log("stop piring");
            particles.Stop();
        }
    }

    /// <summary>
    /// Check if the bottle is tilted enough to pour.
    /// </summary>
    private bool isPouring()
    {
        return Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad);
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
                Debug.Log("Liquid hit: " + hit.collider.name);
                Glass_functionality glass = hit.collider.GetComponent<Glass_functionality>();
                if (glass != null)
                {
                    glass.addLiquid(0.01f); 
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
}
