using UnityEngine;

public class VelocityTrackBehave : MonoBehaviour
{
    Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.maxLinearVelocity = 200f;
        rb.maxAngularVelocity = 200f;
    }
}
