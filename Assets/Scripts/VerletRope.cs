using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source https://github.com/geegaz/Unity-Workshop---Rope-physics/blob/main/Assets/Ropes/VerletRope/VerletRope.cs

/// <summary>
/// Implementation of a verlet-integration based rope physics system.
/// </summary>
public class VerletRope : MonoBehaviour
{
    public int pointsNb = 20;
    
    [System.Serializable]
    public class AttachedPoint
    {
        public int id = 0;
        public Transform transform;
        [HideInInspector] public Vector3 force = Vector3.zero;

        public AttachedPoint(int _id, Transform _transform) {
            id = _id;
            transform = _transform;
        }

        public bool IsValid(int maxPoints = 0) {
            return !(id < 0 || id >= maxPoints || transform == null);
        }
    }
    
    [Header("Rope")]
    [SerializeField] private float attachedBodiesDamping = 0.5f;

    [SerializeField] private List<AttachedPoint> attachedPoints = new List<AttachedPoint>();
    
    [HideInInspector] public Vector3[] pos;
    [HideInInspector] public Vector3[] prevPos;
    [HideInInspector] public float[] mass;

    [Header("Constraints")]

    public float constraintHeightMin = 0.01f;
    public float constraintHeightFriction = 0.5f;

    [Space]
    public float constraintDistance = 0.1f;
    public int constraintDistanceIterations = 20;
    private LineRenderer line;
    [SerializeField]
    Transform lastpoint;
    private void Awake() {
        line = GetComponent<LineRenderer>();
    }

    private void Start() {
        CreatePoints();
        line.positionCount = line.positionCount+1;
    }

    /// <summary>
    /// Updates the rope physics simulation and renders the rope.
    /// </summary>
    private void FixedUpdate() {
        if (pointsNb > 1) {
            ApplyForces();
            ApplyAttach();
            
            ApplyVerlet();
            ApplyConstraints();
            if(lastpoint != null)
                line.SetPosition(pointsNb, lastpoint.position);
        }

        if (line)
            line.SetPositions(pos);
    }

    /// <summary>
    /// Creates the initial rope points and distributes them between the starting position and target position.
    /// </summary>
    private void CreatePoints() {
        pos = new Vector3[pointsNb];
        prevPos = new Vector3[pointsNb];
        mass = new float[pointsNb];

        Vector3 targetPos = transform.position + Physics.gravity.normalized * (constraintDistance * pointsNb);
        if (attachedPoints.Count > 1) {
            AttachedPoint lastPoint = attachedPoints[attachedPoints.Count - 1];
            if (lastPoint.IsValid(pointsNb)) targetPos = lastPoint.transform.position;
        }
        for (int i = 0; i < pointsNb; i ++) {
            pos[i] = Vector3.Lerp(transform.position, targetPos, (float)i / (pointsNb - 1));
            prevPos[i] = pos[i];
            mass[i] = 1.0f;
        }

        if (line) line.positionCount = pointsNb;
    }

    /// <summary>
    /// Attaches a transform to a specific point on the rope.
    /// </summary>
    /// <param name="id">The index of the point to attach to.</param>
    /// <param name="attach">The transform to attach to the point.</param>
    /// <returns>The AttachedPoint object created or updated.</returns>
    public AttachedPoint AttachPoint(int id, Transform attach) {
        AttachedPoint newPoint = new AttachedPoint(id, attach);
        AttachedPoint point;
        for (int i = 0; i < attachedPoints.Count; i++)
        {
            point = attachedPoints[i];
            if (point.id == id) {
                point.transform = attach;
                point.force = Vector3.zero;
                return point;
            } else if (point.id > id) {
                attachedPoints.Insert(i, newPoint);
                return newPoint;
            }
        }
        attachedPoints.Add(newPoint);
        return newPoint;
    }

    /// <summary>
    /// Detaches a point from the rope using an AttachedPoint reference.
    /// </summary>
    /// <param name="point">The AttachedPoint to remove.</param>
    public void DetachPoint(AttachedPoint point) {
        attachedPoints.Remove(point);
        mass[point.id] = 1.0f;
    }

    /// <summary>
    /// Detaches a point from the rope using the point index.
    /// </summary>
    /// <param name="id">The index of the point to detach.</param>
    public void DetachPoint(int id) {
        foreach (AttachedPoint point in attachedPoints)
        {
            if (point.id == id) {
                DetachPoint(point);
                return;
            }
        }
    }

    /// <summary>
    /// Finds the index of the rope point closest to the specified position.
    /// </summary>
    /// <param name="targetPos">The position to find the closest point to.</param>
    /// <param name="range">Maximum search range. Points beyond this range will be ignored.</param>
    /// <returns>The index of the closest point, or -1 if no point is within range.</returns>
    public int GetClosestPoint(Vector3 targetPos, float range = float.PositiveInfinity) {
        float distance;
        float distanceMin = range;
        int pointMin = -1;
        for (int i = 0; i < pointsNb; i++)
        {
            distance = Vector3.Distance(targetPos, pos[i]);
            if (distance < distanceMin) {
                distanceMin = distance;
                pointMin = i;
            }
        }
        return pointMin;
    }

    /// <summary>
    /// Calculates the constraint forces between two points to maintain the desired distance.
    /// </summary>
    /// <param name="p1">Index of the first point.</param>
    /// <param name="p2">Index of the second point.</param>
    /// <param name="distance">The desired distance between points.</param>
    /// <param name="constraint">Array to store the calculated constraint forces.</param>
    /// <param name="useMass">Whether to consider point masses in the calculation.</param>
    /// <returns>The difference factor between current and desired distance.</returns>
    private float GetConstraint(int p1, int p2, float distance, Vector3[] constraint, bool useMass = true) {
        Vector3 delta = pos[p2] - pos[p1];
        float length = delta.magnitude;
        float difference;
        if (useMass) {
            float invmass1 = InverseMass(mass[p1]);
            float invmass2 = InverseMass(mass[p2]);
            difference = (length - distance) / (length * (invmass1 + invmass2));
            constraint[0] = delta * difference * invmass1;
            constraint[1] = -delta * difference * invmass2;
            return difference;
        } else {
            difference = (length - distance) / length;
            constraint[0] = delta * difference * 0.5f;
            constraint[1] = -delta * difference * 0.5f;
            return difference;
        }
    }

    /// <summary>
    /// Applies verlet integration to update point positions based on previous positions and forces.
    /// </summary>
    private void ApplyVerlet() {
        Vector3 temp;
        for (int i = 0; i < pointsNb; i++) {
            temp = pos[i];
            pos[i] += pos[i] - prevPos[i];
            pos[i] += mass[i] * Physics.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            prevPos[i] = temp;
        }
    }

    /// <summary>
    /// Applies distance constraints between adjacent points and height constraints.
    /// </summary>
    private void ApplyConstraints() {
        Vector3[] constraint = new Vector3[2];
        for (int iteration = 0; iteration < constraintDistanceIterations; iteration++) {
            for (int i = 1; i < pointsNb; i++) {
                if (pos[i].y < constraintHeightMin) {
                    prevPos[i] = Vector3.Lerp(prevPos[i], pos[i], constraintHeightFriction);
                    pos[i].y = constraintHeightMin;
                }

                float diff = GetConstraint(i-1, i, constraintDistance, constraint);
                pos[i - 1] += constraint[0];
                pos[i] += constraint[1];
            }
        }
    }

    /// <summary>
    /// Updates the positions of attached points and calculates forces between them.
    /// </summary>
    private void ApplyAttach() {
        Vector3[] constraint = new Vector3[2];
        AttachedPoint previousPoint = null;
        foreach (AttachedPoint point in attachedPoints){
            if (!point.IsValid(pointsNb)) continue;

            pos[point.id] = point.transform.position;
            prevPos[point.id] = point.transform.position;
            mass[point.id] = 0.0f;

            if (previousPoint != null) {
                int points = point.id - previousPoint.id;
                float diff = GetConstraint(previousPoint.id, point.id, constraintDistance * points, constraint);
                if (diff > 0.0f){
                    previousPoint.force += constraint[0];
                    point.force = constraint[1];
                }
            } else {
                point.force = Vector3.zero;
            }
            previousPoint = point;
        }
    }

    /// <summary>
    /// Applies forces to attached rigidbodies.
    /// </summary>
    private void ApplyForces() {
        Rigidbody body = null;
        foreach (AttachedPoint point in attachedPoints) {
            if (!point.IsValid(pointsNb)) continue;

            body = point.transform.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic) {
                mass[point.id] = body.mass;
                body.linearVelocity += point.force * attachedBodiesDamping;
            }
        }
    }

    /// <summary>
    /// Calculates the inverse of a mass value, handling zero mass as a very small value.
    /// </summary>
    /// <param name="mass">The mass value to invert.</param>
    /// <returns>The inverse of the mass value.</returns>
    private static float InverseMass(float mass) {
        return mass == 0.0f ? 0.00000001f : 1.0f / mass;
    }
}
