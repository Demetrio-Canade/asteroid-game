using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>
/// I feedback che restituiscono al giocatore ciò che gli succede: uno sparo,
/// un colpo incassato, e quelli che verranno.
///
/// Non sa perché succedono: gli si dice cosa è successo, e lui lo fa sentire.
/// </summary>
public class FeedbackComponent : MonoBehaviour
{
    private const string Tag = "[Feedback]";

    /// <summary>Quanti impulsi possono sovrapporsi. A 10 colpi al secondo ne bastano pochi.</summary>
    private const int MaxPulses = 8;

    [Header("Impulso post-processing")]
    [Tooltip("Un Volume globale con priorità più alta di quello della scena. Il suo profilo è l'aspetto al picco dello sparo: bloom e qualsiasi altro effetto ci metti.")]
    [FormerlySerializedAs("bloomVolume")]
    [SerializeField] private Volume shotVolume;
    [Tooltip("Quanto dura un impulso, in secondi.")]
    [Min(0.01f)]
    [FormerlySerializedAs("bloomDuration")]
    [SerializeField] private float pulseDuration = 0.25f;
    [Tooltip("La forma dell'impulso: tempo e valore vanno da 0 a 1. A 1 si raggiunge il picco dell'arma.")]
    [FormerlySerializedAs("bloomCurve")]
    [SerializeField] private AnimationCurve pulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 1f),
        new Keyframe(1f, 0f));
    [Tooltip("Quanta parte del profilo raggiunge un colpo di mitraglietta.")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("machineGunBloom")]
    [SerializeField] private float machineGunPulse = 0.4f;
    [Tooltip("Quanta parte del profilo raggiunge un colpo di cannone.")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("cannonBloom")]
    [SerializeField] private float cannonPulse = 1f;

    [Header("Audio dello sparo")]
    [SerializeField] private AudioSource shotAudio;
    [SerializeField] private AudioClip machineGunClip;
    [SerializeField] private AudioClip cannonClip;
    [Tooltip("Lo scarto casuale di ogni colpo, in più o in meno rispetto al suo tono: così la raffica non suona come un campione ripetuto. A 0 il tono è fisso.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float shotPitchJitter = 0.1f;
    [Tooltip("Il tono di un colpo di cannone.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float cannonPitch = 1f;
    [Tooltip("Il tono di un colpo di mitraglietta a canna fredda.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float machineGunPitch = 1f;
    [Tooltip("Il tono di un colpo di mitraglietta al blocco. Fra i due si sale col calore: è così che si sente arrivare il surriscaldamento. Sotto il tono a freddo, scende invece di salire.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float machineGunHotPitch = 1.3f;

    [Header("Traccianti")]
    [Tooltip("Il Particle System dei colpi di mitraglietta. Simula in World, senza emissione propria: ogni colpo emette una particella dalla canna al bersaglio.")]
    [SerializeField] private ParticleSystem machineGunTracer;
    [Tooltip("Quanto corre un tracciante, in unità al secondo. La sua vita la decide la distanza: arriva sul bersaglio e lì si spegne.")]
    [Min(1f)]
    [SerializeField] private float machineGunTracerSpeed = 250f;
    [Tooltip("La LineRenderer del raggio del cannone. Compare intera dalla canna al bersaglio e poi sfuma: i suoi colori sono quelli a raggio pieno.")]
    [SerializeField] private LineRenderer cannonBeam;
    [Tooltip("Quanto dura il raggio del cannone, dal colpo alla scomparsa, in secondi.")]
    [Min(0.01f)]
    [SerializeField] private float cannonBeamDuration = 0.5f;
    [Tooltip("L'opacità del raggio nel tempo: tempo e valore vanno da 0 a 1. A 1 il raggio ha i colori della LineRenderer, a 0 è sparito.")]
    [SerializeField] private AnimationCurve cannonBeamFade = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.15f, 1f),
        new Keyframe(1f, 0f));

    [Header("Danno")]
    [Tooltip("Un Volume globale con priorità più alta di quello della scena. Il suo profilo è l'aspetto a danno massimo: il componente ne muove solo il peso.")]
    [SerializeField] private Volume damageVolume;
    [Tooltip("Il peso che resta addosso in base alla vita persa: in orizzontale la vita persa da 0 a 1, in verticale il peso da 0 a 1. È la base su cui arriva l'impulso del colpo.")]
    [SerializeField] private AnimationCurve healthDamageCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Quanto si aggiunge alla base al picco dell'impulso di un colpo subito.")]
    [Range(0f, 1f)]
    [SerializeField] private float damagePulse = 0.15f;
    [Tooltip("Quanto dura l'impulso di un colpo subito, dall'arrivo al ritorno sulla base, in secondi.")]
    [Min(0.01f)]
    [SerializeField] private float damagePulseDuration = 0.4f;
    [Tooltip("La forma dell'impulso: tempo e valore vanno da 0 a 1. A 1 si aggiunge tutto damagePulse, a 0 si è tornati sulla base.")]
    [SerializeField] private AnimationCurve damagePulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.1f, 1f),
        new Keyframe(1f, 0f));

    [Header("Audio del cambio arma")]
    [SerializeField] private AudioSource swapAudio;
    [Tooltip("Suona quando si passa alla mitraglietta.")]
    [SerializeField] private AudioClip machineGunSelectClip;
    [Tooltip("Suona quando si passa al cannone.")]
    [SerializeField] private AudioClip cannonSelectClip;
    [Tooltip("Pitch minimo di un cambio arma. Ogni cambio ne pesca uno fra minimo e massimo.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float minSwapPitch = 0.95f;
    [Tooltip("Pitch massimo di un cambio arma. Uguale al minimo, il pitch resta fisso.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float maxSwapPitch = 1.05f;

    [Header("Audio dello stato armi")]
    [SerializeField] private AudioSource weaponStatusAudio;
    [Tooltip("Suona quando il cannone ha finito di ricaricare.")]
    [SerializeField] private AudioClip cannonReadyClip;
    [Tooltip("Suona quando la mitraglietta si surriscalda e si blocca.")]
    [SerializeField] private AudioClip overheatClip;
    [Tooltip("Pitch minimo di un suono di stato. Ogni suono ne pesca uno fra minimo e massimo.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float minStatusPitch = 0.95f;
    [Tooltip("Pitch massimo di un suono di stato. Uguale al minimo, il pitch resta fisso.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float maxStatusPitch = 1.05f;

    [Header("Audio del radar")]
    [SerializeField] private AudioSource radarAudio;
    [Tooltip("Suona quando il radar aggancia un nuovo contatto in rotta di collisione.")]
    [SerializeField] private AudioClip contactClip;
    [Tooltip("Pitch minimo di un aggancio. Ogni aggancio ne pesca uno fra minimo e massimo.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float minContactPitch = 0.95f;
    [Tooltip("Pitch massimo di un aggancio. Uguale al minimo, il pitch resta fisso.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float maxContactPitch = 1.05f;

    // Ogni colpo apre un impulso che riparte da zero; il peso applicato è il
    // più alto fra quelli ancora vivi. Così una raffica non ricade al valore
    // base a ogni colpo, e un colpo di cannone non viene spento da uno di
    // mitraglietta sparato subito dopo.
    private readonly float[] pulseAge = new float[MaxPulses];
    private readonly float[] pulseStrength = new float[MaxPulses];

    // I colori del raggio a opacità piena, presi dalla LineRenderer all'avvio:
    // la dissolvenza ne abbassa solo l'alpha, il colore resta quello scelto lì.
    private Color beamStartColor;
    private Color beamEndColor;
    private float beamAge = float.MaxValue;

    // Il danno: una base che dipende dalla vita, e un impulso che ci sta sopra
    // e ci ritorna. La base è una condizione, l'impulso un fatto.
    private float damageBase;
    private float damagePulseAge = float.MaxValue;

    private void Awake()
    {
        if (shotVolume == null)
        {
            Debug.LogWarning($"{Tag} il campo 'shotVolume' non è collegato: gli spari non faranno bagliore.");
        }
        else
        {
            // Da fermo il profilo non deve vedersi: parte l'impulso, sale lui.
            shotVolume.weight = 0f;
        }

        if (damageVolume == null)
        {
            Debug.LogWarning($"{Tag} il campo 'damageVolume' non è collegato: i colpi subiti non si vedranno.");
        }
        else
        {
            damageVolume.weight = 0f;
        }

        if (shotAudio == null)
        {
            Debug.LogWarning($"{Tag} il campo 'shotAudio' non è collegato: gli spari saranno muti.");
        }

        if (machineGunTracer == null)
        {
            Debug.LogWarning($"{Tag} il campo 'machineGunTracer' non è collegato: i colpi di mitraglietta non si vedranno.");
        }
        else if (!IsInScene(machineGunTracer))
        {
            Debug.LogError($"{Tag} 'machineGunTracer' punta al prefab nella cartella, non a un'istanza in scena: i colpi di mitraglietta non si vedranno.");
            machineGunTracer = null;
        }

        if (cannonBeam == null)
        {
            Debug.LogWarning($"{Tag} il campo 'cannonBeam' non è collegato: i colpi di cannone non si vedranno.");
        }
        else if (!IsInScene(cannonBeam))
        {
            Debug.LogError($"{Tag} 'cannonBeam' punta al prefab nella cartella, non a un'istanza in scena: il raggio non si vedrà.");
            cannonBeam = null;
        }
        else
        {
            beamStartColor = cannonBeam.startColor;
            beamEndColor = cannonBeam.endColor;
            cannonBeam.useWorldSpace = true;
            cannonBeam.positionCount = 2;
            cannonBeam.enabled = false;
        }

        if (swapAudio == null)
        {
            Debug.LogWarning($"{Tag} il campo 'swapAudio' non è collegato: il cambio arma sarà muto.");
        }

        if (weaponStatusAudio == null)
        {
            Debug.LogWarning($"{Tag} il campo 'weaponStatusAudio' non è collegato: ricarica e surriscaldamento saranno muti.");
        }

        if (radarAudio == null)
        {
            Debug.LogWarning($"{Tag} il campo 'radarAudio' non è collegato: il radar sarà muto.");
        }

        for (int i = 0; i < MaxPulses; i++)
        {
            pulseAge[i] = float.MaxValue;
        }
    }

    private void OnDisable()
    {
        if (shotVolume != null)
        {
            shotVolume.weight = 0f;
        }

        beamAge = float.MaxValue;

        if (cannonBeam != null)
        {
            cannonBeam.enabled = false;
        }

        damageBase = 0f;
        damagePulseAge = float.MaxValue;

        if (damageVolume != null)
        {
            damageVolume.weight = 0f;
        }
    }

    // ---------- INPUTS ----------

    /// <summary>
    /// È partito un colpo. Il calore, da 0 a 1, è quello della mitraglietta dopo
    /// il colpo: ne sposta il tono man mano che si avvicina al blocco.
    /// </summary>
    public void Shot(in ShotInfo shot, float heat)
    {
        bool cannon = shot.Weapon == TurretWeapon.Cannon;

        if (cannon)
        {
            FireBeam(shot);
        }
        else
        {
            EmitTracer(machineGunTracer, shot, machineGunTracerSpeed);
        }

        float pitch = cannon
            ? cannonPitch
            : Mathf.Lerp(machineGunPitch, machineGunHotPitch, heat);

        pitch += Random.Range(-shotPitchJitter, shotPitchJitter);

        Pulse(cannon ? cannonPulse : machineGunPulse);
        PlayOneShot(shotAudio, cannon ? cannonClip : machineGunClip, pitch);
    }

    /// <summary>
    /// Quanta vita resta, da 0 a 1. Fissa la base del danno: quella che resta
    /// addosso finché la vita non torna.
    /// </summary>
    public void UpdateHealth(float ratio)
    {
        damageBase = Mathf.Clamp01(healthDamageCurve.Evaluate(1f - Mathf.Clamp01(ratio)));
    }

    /// <summary>La navicella ha incassato un colpo: l'impulso sale sopra la base e ci ritorna.</summary>
    public void Damaged()
    {
        damagePulseAge = 0f;
    }

    /// <summary>Il giocatore è passato all'arma indicata.</summary>
    public void WeaponSelected(TurretWeapon weapon)
    {
        AudioClip clip = weapon == TurretWeapon.Cannon ? cannonSelectClip : machineGunSelectClip;

        PlayOneShot(swapAudio, clip, minSwapPitch, maxSwapPitch);
    }

    /// <summary>Il cannone è di nuovo carico.</summary>
    public void CannonReady()
    {
        PlayOneShot(weaponStatusAudio, cannonReadyClip, minStatusPitch, maxStatusPitch);
    }

    /// <summary>La mitraglietta si è surriscaldata.</summary>
    public void Overheated()
    {
        PlayOneShot(weaponStatusAudio, overheatClip, minStatusPitch, maxStatusPitch);
    }

    /// <summary>Il radar ha agganciato un nuovo contatto.</summary>
    public void ContactAcquired()
    {
        PlayOneShot(radarAudio, contactClip, minContactPitch, maxContactPitch);
    }

    /// <summary>
    /// In LateUpdate, dopo il Tick della torretta: un colpo partito in questo
    /// frame si vede già in questo frame.
    /// </summary>
    private void LateUpdate()
    {
        UpdatePulse(Time.deltaTime);
        UpdateBeam(Time.deltaTime);
        UpdateDamage(Time.deltaTime);
    }

    // ---------- IMPULSO POST-PROCESSING ----------
    //
    // Il profilo del Volume è l'aspetto al picco dello sparo, e il componente
    // non lo tocca mai: ne muove soltanto il peso. URP miscela da solo fra la
    // scena (peso 0) e il profilo (peso 1), effetto per effetto. Così nel
    // profilo si può mettere quello che si vuole, niente resta scritto negli
    // asset dopo il Play, e il picco si regola lì anche a gioco avviato.

    private void Pulse(float strength)
    {
        if (strength <= 0f)
        {
            return;
        }

        // Si prende il posto di un impulso finito, o in mancanza di quello
        // meno luminoso adesso: è quello che si nota di meno a perderlo.
        int slot = 0;
        float weakest = float.MaxValue;

        for (int i = 0; i < MaxPulses; i++)
        {
            float value = PulseValue(i);

            if (value < weakest)
            {
                weakest = value;
                slot = i;
            }
        }

        pulseAge[slot] = 0f;
        pulseStrength[slot] = strength;
    }

    private void UpdatePulse(float deltaTime)
    {
        if (shotVolume == null)
        {
            return;
        }

        float weight = 0f;

        for (int i = 0; i < MaxPulses; i++)
        {
            if (pulseAge[i] < pulseDuration)
            {
                pulseAge[i] += deltaTime;
            }

            weight = Mathf.Max(weight, PulseValue(i));
        }

        shotVolume.weight = weight;
    }

    private float PulseValue(int index)
    {
        if (pulseAge[index] >= pulseDuration)
        {
            return 0f;
        }

        return Mathf.Clamp01(pulseCurve.Evaluate(pulseAge[index] / pulseDuration)) * pulseStrength[index];
    }

    // ---------- DANNO ----------

    /// <summary>
    /// Il peso del Volume del danno: la base più l'impulso, se ce n'è uno in
    /// corso. L'impulso si somma sempre sopra la base, qualunque essa sia —
    /// a vita bassa il colpo si vede lo stesso, solo partendo più in alto.
    /// </summary>
    private void UpdateDamage(float deltaTime)
    {
        if (damageVolume == null)
        {
            return;
        }

        float pulse = 0f;

        if (damagePulseAge < damagePulseDuration)
        {
            pulse = Mathf.Clamp01(damagePulseCurve.Evaluate(damagePulseAge / damagePulseDuration)) * damagePulse;
            damagePulseAge += deltaTime;
        }

        damageVolume.weight = Mathf.Clamp01(damageBase + pulse);
    }

    // ---------- TRACCIANTI ----------

    /// <summary>
    /// Una particella dalla canna al punto d'arrivo. La velocità è fissa e la
    /// vita si ricava dalla distanza, così il tracciante si spegne proprio sul
    /// bersaglio invece di attraversarlo. È il Particle System a stirarla nella
    /// direzione del moto: più corre, più è lunga.
    /// </summary>
    private static void EmitTracer(ParticleSystem system, in ShotInfo shot, float speed)
    {
        if (system == null)
        {
            return;
        }

        Vector3 path = shot.End - shot.Origin;
        float distance = path.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            position = shot.Origin,
            velocity = path / distance * speed,
            startLifetime = distance / speed
        };

        system.Emit(emit, 1);
    }

    /// <summary>
    /// Il raggio del cannone: compare intero dalla canna al punto d'arrivo e
    /// da lì in poi sfuma. Resta dove è stato sparato — la torretta che ruota
    /// non se lo porta dietro.
    /// </summary>
    private void FireBeam(in ShotInfo shot)
    {
        if (cannonBeam == null)
        {
            return;
        }

        cannonBeam.SetPosition(0, shot.Origin);
        cannonBeam.SetPosition(1, shot.End);
        cannonBeam.enabled = true;

        beamAge = 0f;
        ApplyBeamAlpha(cannonBeamFade.Evaluate(0f));
    }

    private void UpdateBeam(float deltaTime)
    {
        if (cannonBeam == null || !cannonBeam.enabled)
        {
            return;
        }

        beamAge += deltaTime;

        if (beamAge >= cannonBeamDuration)
        {
            cannonBeam.enabled = false;
            return;
        }

        ApplyBeamAlpha(cannonBeamFade.Evaluate(beamAge / cannonBeamDuration));
    }

    private void ApplyBeamAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        Color start = beamStartColor;
        Color end = beamEndColor;
        start.a *= alpha;
        end.a *= alpha;

        cannonBeam.startColor = start;
        cannonBeam.endColor = end;
    }

    /// <summary>
    /// Un effetto trascinato nel campo dalla finestra Project è l'asset su
    /// disco, non un oggetto in scena: lo si modificherebbe senza vederlo mai.
    /// </summary>
    private static bool IsInScene(Component component)
    {
        return component.gameObject.scene.IsValid();
    }

    // ---------- AUDIO ----------

    /// <summary>
    /// Suona la clip con un pitch pescato fra minimo e massimo, così lo stesso
    /// suono ripetuto non sembra un campione in loop.
    /// </summary>
    private static void PlayOneShot(AudioSource source, AudioClip clip, float minPitch, float maxPitch)
    {
        PlayOneShot(source, clip, Random.Range(minPitch, maxPitch));
    }

    private static void PlayOneShot(AudioSource source, AudioClip clip, float pitch)
    {
        if (source == null || clip == null)
        {
            return;
        }

        // Il pitch è della sorgente, non del singolo suono: cambia anche la
        // coda di quelli che stanno ancora suonando. Con uno scarto piccolo
        // non si sente.
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }
}
