using UnityEngine;

[System.Serializable]
public class VelocityLayer
{
    [Tooltip("Name of the layer for easier identification.")]
    public string layerName;

    [Tooltip("Minimum velocity for this layer to be active.")]
    public float minVelocity = 0f;

    [Tooltip("Maximum velocity for this layer to be active.")]
    public float maxVelocity = 5f;

    [Tooltip("Audio clips to play when this velocity layer is active.")]
    public AudioClip[] samples;
}
