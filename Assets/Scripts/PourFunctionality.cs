using UnityEngine;
using System.Collections.Generic;

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

    void Update()
    {
        if (isPouring())
        {
            Debug.Log("isPouring pouring");
            calculatePouringSpeed();
            emitParticles();
        }
        else
        {
            Debug.Log("stop piring");
            particles.Stop();
        }
    }

    void FixedUpdate()
    {
        if (isPouring())
        {
            detectCollision();
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
    private void emitParticles()
    {
        var main = particles.main;
        main.startSpeed = pourSpeed;
        main.gravityModifier = 1.0f; 

        if (!particles.isPlaying)
        {
            particles.Play();
        }
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
