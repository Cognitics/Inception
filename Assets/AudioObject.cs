using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



public static class AudioObject
{


    public static GameObject PlayAudioDataAtPosition(Vector3 position, byte[] data)
    {
        var go = new GameObject();
        var audioSource = go.AddComponent<AudioSource>();
        AudioClip clip = WavUtility.ToAudioClip(data);
        go.transform.position = position;

        audioSource.clip = clip;
        //audioSource.loop = true;
        audioSource.loop = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 2000;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0.0f;
        audioSource.Play();

        return go;
    }
}
