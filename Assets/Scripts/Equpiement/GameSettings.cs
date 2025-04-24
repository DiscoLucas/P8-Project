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
}
