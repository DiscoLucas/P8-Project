using UnityEngine;
using UnityEditor;
using System.Linq;

public class AudioTest : MonoBehaviour
{
    [SerializeField] private int selectedClipIndex = 0; // Stores dropdown selection
    private string[] soundNames; // Stores all available sound names

    private void Start()
    {
        if (AudioManager.instance != null)
        {
            // Get all sound names from the AudioManager
            soundNames = AudioManager.instance.soundsArray.Select(s => s.name).ToArray();
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found!");
            soundNames = new string[] { "No Sounds Found" };
        }

        string selectedSound = soundNames[selectedClipIndex];
        AudioManager.instance.Play(selectedSound);
    }

    public string GetSelectedSoundName()
    {
        return soundNames.Length > 0 ? soundNames[selectedClipIndex] : "";
    }

    public void SetSelectedIndex(int index)
    {
        selectedClipIndex = index;
    }

    public int GetSelectedIndex()
    {
        return selectedClipIndex;
    }
}
