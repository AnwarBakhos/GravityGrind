using System.Collections.Generic;
using UnityEngine;

// Plays every sound in the game. Sounds are loaded from Resources/Audio.
// Files like "wood_000", "wood_001" are grouped as "wood" and a random one plays each time.
public class Sfx : MonoBehaviour
{
    public static Sfx I { get; private set; }

    readonly Dictionary<string, List<AudioClip>> groups = new Dictionary<string, List<AudioClip>>();
    readonly List<AudioSource> voices = new List<AudioSource>();
    AudioSource rollSource;
    int nextVoice;

    void Awake()
    {
        I = this;

        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audio"))
        {
            string group = clip.name;
            int cut = group.LastIndexOf('_');
            if (cut > 0 && int.TryParse(group.Substring(cut + 1), out _)) group = group.Substring(0, cut);

            if (!groups.ContainsKey(group)) groups[group] = new List<AudioClip>();
            groups[group].Add(clip);
        }

        // Several sources so sounds can overlap and each can have its own pitch
        for (int i = 0; i < 10; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            voices.Add(source);
        }

        // Rolling sound: a looping rumble whose volume and pitch follow the ball's speed
        rollSource = gameObject.AddComponent<AudioSource>();
        rollSource.clip = Resources.Load<AudioClip>("Audio/rolling");
        rollSource.loop = true;
        rollSource.volume = 0f;
        rollSource.Play();
    }

    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        if (I == null || !I.groups.TryGetValue(name, out List<AudioClip> clips)) return;

        AudioSource source = I.voices[I.nextVoice];
        I.nextVoice = (I.nextVoice + 1) % I.voices.Count;
        source.pitch = pitch * Random.Range(0.95f, 1.05f);
        source.PlayOneShot(clips[Random.Range(0, clips.Count)], volume);
    }

    public static void SetRoll(float volume, float pitch)
    {
        if (I == null) return;
        I.rollSource.volume = Mathf.Lerp(I.rollSource.volume, volume, 0.2f);
        I.rollSource.pitch = pitch;
    }
}
