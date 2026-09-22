using UnityEngine;

/// <summary>Riproduce gli effetti audio dell'asteroide senza conoscerne le regole.</summary>
public class AsteroidEffects : MonoBehaviour
{
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] destructionClips;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    public void PlayHit(Vector3 position)
    {
        PlayRandom(hitClips, position);
    }

    public void PlayDestruction(Vector3 position)
    {
        PlayRandom(destructionClips, position);
    }

    private void PlayRandom(AudioClip[] clips, Vector3 position)
    {
        AudioClip clip = FindRandomClip(clips);
        if (clip != null)
        {
            // PlayClipAtPoint crea una sorgente indipendente: il clip continua
            // anche quando l'asteroide viene disattivato e restituito alla pool.
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    private static AudioClip FindRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int startIndex = UnityEngine.Random.Range(0, clips.Length);
        for (int offset = 0; offset < clips.Length; offset++)
        {
            AudioClip clip = clips[(startIndex + offset) % clips.Length];
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }
}
