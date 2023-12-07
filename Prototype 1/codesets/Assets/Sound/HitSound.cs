using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSound : MonoBehaviour
{
    //The collision sound's Audio Source
    public AudioSource hitSound;

    //A list where your collision sound clips are stored
    public List<AudioClip> hitSoundClips;

    //"OnCollisionEnter" refers to when something enters collision with the object this script is attached to
    void OnCollisionEnter()
    {
        //A random audio clip will be chosen from the list of collision sounds and applied to the Audio Source
        hitSound.clip = hitSoundClips[0];

        //The collision sound will play
        hitSound.Play();
    }
}