using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Audio;

//[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(MeshCollider))]
public class Glass : MonoBehaviour
{
    public Rigidbody rb;
    Breakable breakable;
    public float breakForce = 1f;
    public bool canBreak = false;
    public GameObject glassShatter;

    [Header("Breaking Settings")]
    [SerializeField] private int radialCuts = 5; // Number of vertical cuts around the glass
    [SerializeField] private int heightCuts = 3; // Number of horizontal cuts along the glass height
    [SerializeField] private float randomOffset = 0.1f; // Random offset for cut positions
    [SerializeField] private float explosionForce = 300f; // Force applied to pieces
    [SerializeField] private float explosionRadius = 1.5f; // Radius of explosion
    [SerializeField] private float upwardModifier = 0.4f; // Upward force bias

    [SerializeField] private float deSpawnTime = 5f; // Time before despawning broken glass
    [Header("Audio")]
    [SerializeField] private AudioClip breakSound; // Optional sound effect
    [SerializeField] private float beackSoundMinPitch = 0.8f; // Minimum pitch for break sound
    [SerializeField] private float breakSoundMaxPitch = 1.2f; // Maximum pitch for break sound

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        breakable = GetComponent<Breakable>();
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision!: " + collision.gameObject.name + " with force: " + collision.impulse.magnitude);
        if (collision.impulse.magnitude > breakForce && canBreak)
        {
            //rb.AddForce(collision.impulse, ForceMode.Impulse);
            //breakable.Break(collision.contacts[0].point, collision.impulse.magnitude);
            GameObject brokenGlass = Instantiate(glassShatter, transform.position, transform.rotation);
            float hitForce = (explosionForce * collision.impulse.magnitude)/brokenGlass.transform.childCount;
            float pieceMass = rb.mass / brokenGlass.transform.childCount;
            foreach (Transform child in brokenGlass.transform)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if(rb == null){
                    rb = child.AddComponent<Rigidbody>();
                    BoxCollider boxCollider = child.AddComponent<BoxCollider>();
                }    
                rb.mass = pieceMass;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.AddExplosionForce(
                    hitForce ,
                    collision.contacts[0].point,
                    explosionRadius,
                    upwardModifier);
                rb.AddTorque(Random.insideUnitSphere * hitForce);

            }
            GameManager.Instance.RemoveAfterDelay(brokenGlass, deSpawnTime);
            Destroy(gameObject);
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && breakSound != null)
            {
                audioSource.clip = breakSound;
                audioSource.pitch = Random.Range(beackSoundMinPitch, breakSoundMaxPitch);
                audioSource.Play();
            }
            Debug.Log("Glass broken! with force: " + collision.impulse.magnitude);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    public void SetCanBreak(bool canBreak)
    {
        this.canBreak = canBreak;
    }
}
