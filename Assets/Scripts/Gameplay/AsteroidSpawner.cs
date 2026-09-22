using UnityEngine;

// Detto per esteso: senza alias, "Random" da solo è ambiguo appena qualcuno
// aggiunge un using System in cima.
using Random = UnityEngine.Random;

/// <summary>
/// Il ritmo con cui gli asteroidi entrano in campo, e da dove entrano.
///
/// Non è un modulo: è un servizio del connettore, come <see cref="AsteroidPool"/>.
/// Non conosce la partita — non sa se si sta giocando, non tiene punteggio, non
/// decide quando smettere: gli si dà un frame e un livello di difficoltà, e
/// quando è il momento pesca dal pool e lancia.
///
/// Delle sue partenze non annuncia niente a nessuno: se un lancio sia pericoloso
/// è una domanda che da qui non si può nemmeno porre, perché da qui si vede il
/// cielo da cui scendono gli asteroidi e non la navicella. A quella domanda
/// risponde il radar di chi rischia.
/// </summary>
public sealed class AsteroidSpawner
{
    private readonly AsteroidPool pool;
    private readonly DifficultySettings difficulty;

    // Da dove partono e dove vanno. Ciascun punto ha il proprio raggio: né la
    // partenza né l'arrivo sono un punto solo, altrimenti gli asteroidi
    // percorrerebbero sempre la stessa manciata di traiettorie.
    private readonly Transform origin;
    private readonly float originSpreadRadius;
    private readonly Transform target;
    private readonly float targetSpreadRadius;

    private readonly float lifetime;

    private float timer;

    public AsteroidSpawner(
        AsteroidPool pool,
        DifficultySettings difficulty,
        Transform origin,
        float originSpreadRadius,
        Transform target,
        float targetSpreadRadius,
        float lifetime)
    {
        this.pool = pool;
        this.difficulty = difficulty;
        this.origin = origin;
        this.originSpreadRadius = originSpreadRadius;
        this.target = target;
        this.targetSpreadRadius = targetSpreadRadius;
        this.lifetime = lifetime;
    }

    /// <summary>Azzera l'attesa: il prossimo asteroide parte subito.</summary>
    public void Reset()
    {
        timer = 0f;
    }

    /// <summary>
    /// Un frame di spawn. Il cronometro riparte anche quando il lancio non è
    /// andato a buon fine: un frame senza asteroide non deve trasformarsi in una
    /// raffica al frame dopo.
    /// </summary>
    public void Tick(float deltaTime, float intensity)
    {
        timer -= deltaTime;

        if (timer > 0f)
        {
            return;
        }

        Spawn(intensity);

        // L'intervallo si rilegge adesso e non appena l'intensità cambia: il
        // nuovo ritmo entra in vigore col prossimo asteroide, e nessuno deve
        // mettere le mani su un cronometro già in volo.
        timer = difficulty.SpawnIntervalAt(intensity);
    }

    /// <summary>
    /// La difficoltà dice dentro quale banda si può andare; cosa tocchi a questo
    /// asteroide lo decide qui il caso. Due asteroidi nati nello stesso momento
    /// non viaggiano alla stessa andatura e non puntano lo stesso posto, ed è il
    /// punto: un campo tutto uniforme non ha né respiri né sorprese.
    ///
    /// Il dado si tira due volte, per due cose diverse: quanto corre, e quanto
    /// vicino alla navicella punta. La seconda è la pressione vera del gioco, e
    /// non è una scelta fra due categorie: il campo resta sempre lo stesso, è
    /// l'<b>addensamento</b> dentro quel campo a cambiare nel tempo.
    /// </summary>
    private void Spawn(float intensity)
    {
        if (pool == null || origin == null || target == null)
        {
            return;
        }

        Vector3 start = ScatterAround(origin, originSpreadRadius, 0.5f);
        Vector3 destination = ScatterAround(target, targetSpreadRadius, difficulty.FocusAt(intensity));

        // Partenza e arrivo coincidenti: Begin scarterebbe il volo, e tanto vale
        // non sprecare un'istanza del pool per un asteroide che non parte.
        if ((destination - start).sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        SpeedRange band = difficulty.SpeedRangeAt(intensity);
        float speed = Random.Range(band.Min, band.Max);

        pool.Get().Spawn(start, destination, speed, lifetime);
    }

    /// <summary>
    /// Un punto a caso dentro un disco attorno all'ancora, con l'addensamento
    /// deciso da <paramref name="focus"/>. Vale sia per la partenza sia per
    /// l'arrivo: né l'una né l'altro devono essere un punto esatto, o gli
    /// asteroidi ripeterebbero sempre le stesse traiettorie.
    ///
    /// <b>Perché l'esponente.</b> L'area di un disco cresce col quadrato del
    /// raggio, quindi pescando la distanza in modo lineare i punti si
    /// affollerebbero al centro solo per un artificio del conto, e la parte
    /// larga resterebbe vuota. La radice quadrata — cioè <c>focus = 0.5</c> —
    /// rimette la densità uniforme sull'area. Da lì in su l'addensamento
    /// diventa una scelta invece che un effetto collaterale: più l'esponente è
    /// alto, più i lanci si stringono sul bersaglio.
    ///
    /// Nessuna distanza è vietata, nemmeno a difficoltà zero. È voluto: senza
    /// una fascia proibita attorno alla navicella, i sassi possono sfiorarla.
    ///
    /// Il disco sta sul piano XY perché è quello che il giocatore ha davanti:
    /// la navicella guarda lungo -Z, quindi disperdere in X e in Y vuol dire
    /// disperdere in larghezza e in altezza. A raggio zero si torna al punto.
    /// </summary>
    private static Vector3 ScatterAround(Transform anchor, float radius, float focus)
    {
        if (radius <= 0f)
        {
            return anchor.position;
        }

        float distance = radius * Mathf.Pow(Random.value, focus);
        float angle = Random.value * Mathf.PI * 2f;

        return anchor.position + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
    }
}
