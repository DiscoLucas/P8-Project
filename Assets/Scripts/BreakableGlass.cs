using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Audio;

public class Glass : MonoBehaviour
{
    public Rigidbody rb;
    Breakable breakable;
    public float breakForce = 1f;
    public bool canBreak = false;
    public GameObject glassShatter;

    [Header("Breaking Settings")]
    [SerializeField] private float explosionForce = 300f; // Force applied to pieces
    [SerializeField] private float explosionRadius = 1.5f; // Radius of explosion
    [SerializeField] private float upwardModifier = 0.4f; // Upward force bias

    [SerializeField] private float deSpawnTime = 5f; // Time before despawning broken glass
    [Header("Audio")]
    [SerializeField] private AudioClip breakSound; // Optional sound effect
    [SerializeField] private float beackSoundMinPitch = 0.8f; // Minimum pitch for break sound
    [SerializeField] private float breakSoundMaxPitch = 1.2f; // Maximum pitch for break sound
    [SerializeField] AudioSource audioSource;

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
        if (collision.impulse.magnitude > breakForce && canBreak)
        {
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
                GameManager.Instance.objectsToClean.Add(child.gameObject);

            }
            GameManager.Instance.RemoveAfterDelay(brokenGlass, deSpawnTime);
            if (audioSource != null && breakSound != null)
            {
                audioSource.clip = breakSound;
                audioSource.pitch = Random.Range(beackSoundMinPitch, breakSoundMaxPitch);
                audioSource.Play();
            }
            Destroy(gameObject);
        }
    }


    public void SetCanBreak(bool canBreak)
    {
        this.canBreak = canBreak;
    }
}
