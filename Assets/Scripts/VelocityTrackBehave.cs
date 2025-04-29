using System.Collections.Generic;
using UnityEngine;

public class VelocityTrackBehave : MonoBehaviour
{
    Rigidbody rb;

    [Header("Audio")]
    //List of sound effects
    [SerializeField] List<AudioClip> spoonPlaceSounds;
    [SerializeField] AudioClip spoonPlaceSound;
    [SerializeField] float spoonVolume = 1f;
    [SerializeField] float maxPitch = 1.2f;
    [SerializeField] float minPitch = 0.8f;
    [SerializeField] AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = 200f;
        rb.maxAngularVelocity = 200f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(spoonPlaceSounds.Count == 0)
        {
            return;
        }
        int randomIndex = Random.Range(0, spoonPlaceSounds.Count);
        AudioClip randomSound = spoonPlaceSounds[randomIndex];
        audioSource.PlayOneShot(randomSound, spoonVolume);
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(spoonPlaceSound, spoonVolume);
    }
}
