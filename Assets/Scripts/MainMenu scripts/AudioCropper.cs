using UnityEngine;
using System.Collections;

public class AudioCropper : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Crop Timings (in seconds)")]
    public float startTime = 0.5f; // Where to start playing
    public float endTime = 2.3f;   // Where to stop playing

    public void PlayCropped()
    {
        if (audioSource == null || audioSource.clip == null) return;

        StopAllCoroutines();
        StartCoroutine(PlayTrimmedSegment());
    }

    IEnumerator PlayTrimmedSegment()
    {
        // Set playback start point
        audioSource.time = Mathf.Clamp(startTime, 0f, audioSource.clip.length);
        audioSource.Play();

        // Calculate duration to play
        float playDuration = Mathf.Max(0f, endTime - startTime);
        yield return new WaitForSeconds(playDuration);

        // Stop playback at end point
        audioSource.Stop();
    }
}