using UnityEngine;
using UnityEditor;
using System.Linq;

[System.Serializable]
public class AudioData
{
    [HideInInspector]
    public string name; // Still needed for internal logic

    [SerializeField]
    private int selectedClipIndex = 0; // Used to store dropdown selection

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(-3f, 3f)]
    public float pitch = 1f;
    [Range(0f, 1)]
    public float spatialBlend = 1f;

    public bool spatialize;
    public bool loop;

    [HideInInspector]
    public float doblerEffect = 0f;
    [HideInInspector]
    public AudioSource source;

    // Dropdown list for selecting a sound
    public string GetClipName()
    {
        return clip ? clip.name : "None";
    }

    public void SetClipFromList(string[] clipNames)
    {
        if (selectedClipIndex < clipNames.Length)
        {
            name = clipNames[selectedClipIndex];
        }
    }

    public void SetClipIndex(int index)
    {
        selectedClipIndex = index;
    }

    public int GetClipIndex()
    {
        return selectedClipIndex;
    }
}
