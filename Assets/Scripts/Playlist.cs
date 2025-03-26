using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Playlist : MonoBehaviour
{
    public AudioSource sourceA, sourceB; // hate that this needs to be assigned in the editor, but I don't know
    public AudioClip[] playlist;
    public int currentSong = 0;
    [Tooltip("Not yet implemented")]
    public bool shuffle = true;
    [Tooltip("Not yet implemented")]
    public bool loop = true;
    public float crossfadeTime = 2.0f;

    private AudioSource activeSource, inactiveSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Assert(playlist.Length > 0, gameObject.name + "'s Playlist is empty");
        sourceA = gameObject.GetComponentAtIndex<AudioSource>(0);
        sourceB = gameObject.GetComponentAtIndex<AudioSource>(1);

        // Make sure the AudioSources have the same settings
    sourceB.volume = sourceA.volume;
    sourceB.pitch = sourceA.pitch;
    sourceB.spatialBlend = sourceA.spatialBlend;
    sourceB.priority = sourceA.priority;
    sourceB.mute = sourceA.mute;
    sourceB.bypassEffects = sourceA.bypassEffects;
    sourceB.bypassListenerEffects = sourceA.bypassListenerEffects;
    sourceB.bypassReverbZones = sourceA.bypassReverbZones;
    sourceB.dopplerLevel = sourceA.dopplerLevel;
    sourceB.spread = sourceA.spread;
    sourceB.rolloffMode = sourceA.rolloffMode;
    sourceB.minDistance = sourceA.minDistance;
    sourceB.maxDistance = sourceA.maxDistance;

        PlayNextClip();
    }

    private void PlayNextClip()
    {
        AudioClip nextClip = playlist[Random.Range(0, playlist.Length)];
        
        // Swap sources
        (activeSource, inactiveSource) = (inactiveSource, activeSource);

        inactiveSource.clip = nextClip;
        inactiveSource.volume = 0;
        inactiveSource.Play();

        StartCoroutine(Crossfade(crossfadeTime));
    }

    IEnumerator Crossfade (float duration)
    {
        float timer = 0;
        float startVolA = activeSource.volume;
        float startVolB = inactiveSource.volume;

        while (timer < duration)
        {
            float t = timer / duration;
            activeSource.volume = Mathf.Lerp(startVolA, 0, t);
            inactiveSource.volume = Mathf.Lerp(startVolB, 1, t);
            timer += Time.deltaTime;
            yield return null;
        }

        // make sure the final volumes are right
        // remember that the inactive source is now actually the active source
        activeSource.volume = 0;
        inactiveSource.volume = 1;

        activeSource.Stop();

        // queue the next clip
        Invoke(nameof(PlayNextClip), inactiveSource.clip.length - crossfadeTime);
    }
}
