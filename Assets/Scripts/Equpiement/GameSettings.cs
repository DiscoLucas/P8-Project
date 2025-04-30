using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Object Respawn Settings")]
    [SerializeField]
    public float maxDistanceFromPlayerToObj = 10f; // Maximum distance from the player before respawning
    [SerializeField]
    public float checkIntervalForObj = 1f; // Time interval (in seconds) between distance checks

    [Header("Round Settings")]
    [Tooltip("Time for the round to end in minutes")]
    public float roundTime = 2f;

    [Header("Ice Melting Settings")]
    public float graceTime = 10f;
    public float meltTime = 360f;

    [Header("Haptics Settings")]
    public float HapticDuration = 0.04f;
    public float HapticMinIntensity = 0.01f;
    public float HapticMaxIntensity = 0.7f;
    public float HapticRoutineWait = 0.05f;
    public float HapticIntensityOfButtonpress = 0.5f;
    public float HapticDurationOfButtonPress = 1f;

    public float Haptic_Fill_Contatiner_HapticDuration = 0.1f;
    public float Haptic_Fill_Contatiner_HapticIntensity = 0.5f;

    [Header("Scene loadning settings")]
    public float fadeDuration = 1f;
    public float startLoadningInFade = 0.8f;

    [Header("Experinces ")]
    public float baselineDuration = 5f;
}
