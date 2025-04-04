using UnityEngine;

public class HeadEssentials : MonoBehaviour
{
    // This script is attached to the Head GameObject in the scene
    // It is used to manage the head's position and rotation in the game world
    // and to handle any necessary updates or interactions with other game objects.
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    AudioSource Ears;
    void Start()
    {
        Ears = GetComponent<AudioSource>();
    }

    // Update is called once per frame


    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Garnish"){
            Ears.Play();
            Destroy(other.gameObject);
        }
    }
}
