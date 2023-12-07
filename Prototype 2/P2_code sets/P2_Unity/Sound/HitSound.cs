/*
The code snippet (8. Play sound when ball hit into hole) below has been adapted from
https://www.youtube.com/watch?v=yE0JdtVTnVk&t=27s
I have changed the music list to play the first music on the music list, instead of the whole range of music, when the object is hit.
*/
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

// End code snippet (8. Play sound when ball hited into hole)