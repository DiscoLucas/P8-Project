using SteamAudio;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class ImpactSound : MonoBehaviour
{
    [Tooltip("If true, when layers are overlapping a random layer will be selected.")]
    public bool randomLayers = false;

    [Tooltip("Define the velocity layers for this objetct.")]
    public VelocityLayer[] velocityLayers;

    [Tooltip("Optional: Enable debug logging for collision velocity and active layers.")]
    public bool debugLog = false;
    
    [Tooltip("Minimum volume scaling factor.")]
    [Range(0f, 1f)]
    public float minVolume = 0.1f;
    
    [Tooltip("Maximum volume scaling factor.")]
    [Range(0f, 1f)]
    public float maxVolume = 1.0f;
    
    [Tooltip("Maximum velocity to use for volume scaling.")]
    public float maxScalingVelocity = 10f;

    private AudioSource audioSource;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("You forgot to add the AudioSource component, dumbass.");
        }
    }

    

    void OnCollisionEnter(Collision collision)
    {
        float impactVelocity = collision.relativeVelocity.magnitude;
        
        // Calculate volume based on velocity
        float volumeScale = Mathf.Lerp(minVolume, maxVolume, Mathf.Clamp01(impactVelocity / maxScalingVelocity));
        
        List<VelocityLayer> activeLayers = new List<VelocityLayer>();
        foreach (VelocityLayer layer in velocityLayers)
        {
            if (impactVelocity >= layer.minVelocity && impactVelocity <= layer.maxVelocity)
            {
                activeLayers.Add(layer);
            }
        }
        
        if (activeLayers.Count > 0)
        {
            if (randomLayers)
            {
                // Select a random layer from the active layers
                VelocityLayer chosenLayer = activeLayers[Random.Range(0, activeLayers.Count)];
                AudioClip clip = PickRandomClip(chosenLayer);
                if (clip != null) PlayClip(clip, volumeScale);
            }
            else
            {
                // play any overlapping layers at the same time
                foreach (VelocityLayer layer in activeLayers)
                {
                    AudioClip clip = PickRandomClip(layer);
                    if (clip != null) PlayClip(clip, volumeScale);
                }
            }
        }

        if (debugLog)
        {
            Debug.Log($"Impact Velocity: {impactVelocity:F2}. Active Layers: {activeLayers.Count}");
            Debug.Log($"Active Layers: {string.Join(", ", activeLayers.ConvertAll(layer => layer.layerName))}");
        }
    }

    AudioClip PickRandomClip(VelocityLayer layer)
    {
        if (layer.samples != null && layer.samples.Length > 0)
        {
            return layer.samples[Random.Range(0, layer.samples.Length)];
        }
        else
        {
            Debug.LogWarning($"No audio clips found in layer: {layer.layerName}");
            return null;
        }
    }

    
    void PlayClip(AudioClip clip, float volumeScale)
    {
        audioSource.PlayOneShot(clip, volumeScale);
    }
}
