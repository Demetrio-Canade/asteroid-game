using System;
using UnityEngine;

/// <summary>
/// La banda di velocità dentro cui può nascere un asteroide in un dato momento.
/// </summary>
public readonly struct SpeedRange
{
    public readonly float Min;
    public readonly float Max;

    public SpeedRange(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

/// <summary>
/// I numeri della progressione di difficoltà e la curva su cui stanno.
///
/// Non tiene nessuno stato di partita e <b>non tira dadi</b>: dato un istante sa
/// dire quanto si è avanti, con che ritmo si spawna, dentro quale banda si pesca
/// e quanto stretto si punta. Il cronometro sta nel connettore, il sorteggio
/// nello spawner.
/// </summary>
[Serializable]
public class DifficultySettings
{
    [Header("La salita")]
    [Tooltip("In quanti secondi si arriva alla difficoltà massima. Da lì in poi la pressione resta al massimo.")]
    [Min(0.01f)]
    [SerializeField] private float rampSeconds = 120f;
    [Tooltip("La forma della salita, da 0 a 1 su entrambi gli assi. Una retta è la progressione lineare.")]
    [SerializeField] private AnimationCurve ramp = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("In quanti livelli si divide la salita. Serve solo alla lettura sull'HUD: non è lui a guidare niente.")]
    [Min(1)]
    [SerializeField] private int levelCount = 5;

    [Header("Intervallo di spawn (dal lento al rapido)")]
    [Min(0.01f)]
    [SerializeField] private float maxSpawnInterval = 2.2f;
    [Min(0.01f)]
    [SerializeField] private float minSpawnInterval = 0.45f;

    [Header("Velocità — il più lento del campo")]
    [Min(0f)]
    [SerializeField] private float minSpeedStart = 2f;
    [Min(0f)]
    [SerializeField] private float minSpeedEnd = 6f;

    [Header("Velocità — il più veloce del campo")]
    [Min(0f)]
    [SerializeField] private float maxSpeedStart = 3f;
    [Min(0f)]
    [SerializeField] private float maxSpeedEnd = 9f;

    [Header("Quanto stretto puntano")]
    [Tooltip("Quanto la mira si addensa sulla navicella a inizio partita. 0.5 è la dispersione uniforme sul campo: nessun addensamento. Sopra, i lanci si stringono verso il centro.")]
    [Min(0.5f)]
    [SerializeField] private float focusStart = 0.5f;
    [Tooltip("Lo stesso addensamento alla difficoltà massima. Più è alto, più asteroidi arrivano addosso invece che larghi.")]
    [Min(0.5f)]
    [SerializeField] private float focusEnd = 2.5f;

    /// <summary>In quanti livelli si legge la salita. Un'etichetta, non una meccanica.</summary>
    public int LevelCount => levelCount;

    /// <summary>
    /// Quanto si è avanti nella salita, da 0 a 1, dato il tempo trascorso.
    ///
    /// L'uscita della curva si clampa: un <c>AnimationCurve</c> con tangenti
    /// vivaci scavalca gli estremi, e un valore oltre 1 farebbe estrapolare i
    /// Lerp qui sotto — velocità più alte di quelle scritte nell'Inspector,
    /// senza che nessuno le abbia decise.
    /// </summary>
    public float IntensityAt(float elapsedSeconds)
    {
        float linear = Mathf.Clamp01(elapsedSeconds / rampSeconds);
        return Mathf.Clamp01(ramp.Evaluate(linear));
    }

    /// <summary>Intervallo fra uno spawn e il successivo, all'intensità data.</summary>
    public float SpawnIntervalAt(float intensity)
    {
        return Mathf.Lerp(maxSpawnInterval, minSpawnInterval, intensity);
    }

    /// <summary>
    /// La banda entro cui pescare la velocità di un asteroide. I due estremi
    /// hanno ciascuno la propria rampa, così la banda può allargarsi o
    /// stringersi crescendo la difficoltà — è una scelta, non una conseguenza.
    /// </summary>
    public SpeedRange SpeedRangeAt(float intensity)
    {
        float min = Mathf.Lerp(minSpeedStart, minSpeedEnd, intensity);
        float max = Mathf.Lerp(maxSpeedStart, maxSpeedEnd, intensity);

        // Una banda rovesciata sarebbe un errore di configurazione che si
        // trasforma in asteroidi fermi: meglio degenere che al contrario.
        return new SpeedRange(min, Mathf.Max(min, max));
    }

    /// <summary>
    /// Quanto la mira si addensa sulla navicella: l'esponente con cui lo spawner
    /// pesca la distanza dal bersaglio.
    ///
    /// È la pressione vera del gioco. La velocità e il ritmo fanno il rumore, ma
    /// quanti ti arrivano davvero addosso decide quanto devi sparare — e qui non
    /// si sposta il campo, si sposta <b>dove si affollano</b> i lanci dentro il
    /// campo.
    ///
    /// <b>0.5 non è un valore a caso: è la radice quadrata.</b> Pescando la
    /// distanza sotto radice i punti si distribuiscono uniformemente sull'area
    /// del disco, perché l'area cresce col quadrato del raggio. Da lì in su i
    /// lanci si stringono verso il centro, e a 2.5 la gran parte finisce
    /// addosso.
    ///
    /// Non è una probabilità di impatto e non pretende di esserlo: quanti
    /// colpiscano davvero dipende anche dalla stazza del sasso estratto. In
    /// cambio non esiste nessuna fascia vietata attorno alla navicella, e i
    /// sassi possono sfiorarla.
    /// </summary>
    public float FocusAt(float intensity)
    {
        return Mathf.Max(0.5f, Mathf.Lerp(focusStart, focusEnd, intensity));
    }

    /// <summary>
    /// Il livello da mostrare a schermo, da 1 a <see cref="LevelCount"/>.
    ///
    /// Segue l'intensità e non l'orologio: dice quanto è dura <i>adesso</i>. Con
    /// una curva lenta all'inizio il primo livello dura più di un quinto della
    /// rampa, ed è giusto così.
    /// </summary>
    public int LevelAt(float intensity)
    {
        return Mathf.Clamp(Mathf.FloorToInt(intensity * levelCount) + 1, 1, levelCount);
    }
}
