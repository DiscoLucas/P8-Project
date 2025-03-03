using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Glass : MonoBehaviour
{
    Rigidbody rb;
    public float breakForce = 1f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > breakForce)
        {
            rb.AddForce(collision.impulse, ForceMode.Impulse);
            Debug.Log("Glass broken!");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
}
