using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public class Glass : MonoBehaviour
{
    Rigidbody rb;
    Breakable breakable;
    public float breakForce = 1f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        breakable = GetComponent<Breakable>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > breakForce)
        {
            //rb.AddForce(collision.impulse, ForceMode.Impulse);
            breakable.Break(collision.contacts[0].point, collision.impulse.magnitude);
            Debug.Log("Glass broken!");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
}
