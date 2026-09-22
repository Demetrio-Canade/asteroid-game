using UnityEngine;

/// <summary>
/// Quando una sfera che viaggia dritta tocca una scatola ferma — e se la tocca.
///
/// <b>Lo slab test.</b> Una scatola non è altro che l'incrocio di tre lastre
/// (<i>slab</i>): lo spazio fra due piani paralleli, uno per asse. Un punto sta
/// dentro la scatola se e solo se sta dentro tutte e tre insieme.
///
/// Aggiungendo il moto rettilineo uniforme, per ogni lastra c'è una domanda
/// facile: <i>in quale intervallo di tempo sono dentro?</i> Ci si entra una
/// volta e se ne esce una volta, quindi la risposta è un intervallo. Tre lastre,
/// tre intervalli — e la sfera è dentro la scatola esattamente durante la loro
/// <b>intersezione</b>.
///
/// Da cui le due righe che contano: si è dentro solo dopo aver varcato
/// l'<b>ultima</b> lastra (il massimo delle entrate) e si è fuori appena se ne
/// abbandona la <b>prima</b> (il minimo delle uscite). Se il massimo supera il
/// minimo i tre intervalli non si sovrappongono, e la sfera passa a vuoto: era
/// dentro una lastra quando era ancora fuori dall'altra.
///
/// <b>La sfera sparisce dal conto.</b> Non si fa muovere una sfera contro una
/// scatola: si <b>gonfia la scatola</b> del raggio e si fa muovere un punto,
/// perché «la sfera tocca la scatola» e «il centro è entro <c>r</c> dalla
/// scatola» sono la stessa frase. Gli spigoli restano quadrati invece di
/// arrotondarsi, quindi sugli angoli si sovrastima di poco: è il verso giusto in
/// cui sbagliare per un avviso.
///
/// Niente radici quadrate e niente trigonometria — prodotti scalari e divisioni.
/// È lo stesso conto che una <c>SphereCast</c> fa sotto il cofano, ma facendolo
/// in casa resta in mano il numero che serve: il <b>tempo</b>.
/// </summary>
public static class SweptSphere
{
    /// <summary>Sotto questa velocità su un asse si viaggia parallelo alla lastra.</summary>
    private const float Epsilon = 1e-5f;

    /// <summary>
    /// Fra quanti secondi la sfera tocca la scatola. Falso se non la tocca — o
    /// se l'ha già attraversata, che per un avviso è la stessa cosa.
    ///
    /// Il tempo esce <b>già in secondi</b> senza dividere per la velocità: nel
    /// conto entra <c>direzione × velocità</c> e non la sola direzione, quindi
    /// il tempo è dentro l'unità di misura.
    /// </summary>
    /// <param name="origin">Il centro della sfera adesso.</param>
    /// <param name="velocity">Unità al secondo, direzione compresa.</param>
    /// <param name="radius">Il raggio della sfera: è di quanto si gonfia la scatola.</param>
    /// <param name="box">La scatola da difendere, con la sua rotazione e la sua scala.</param>
    /// <param name="seconds">Quanto manca al contatto. Zero se è già dentro.</param>
    public static bool TimeToHit(Vector3 origin, Vector3 velocity, float radius, BoxCollider box, out float seconds)
    {
        seconds = 0f;

        if (box == null)
        {
            return false;
        }

        Transform frame = box.transform;

        // Gli assi del transform sono già normalizzati, la scala sta nelle
        // semi-estensioni: è il motivo per cui si lavora in coordinate di mondo
        // e non in quelle locali. In locale il raggio andrebbe diviso per la
        // scala di ciascun asse, e sulle placche della DamageZone — spesse 0.18
        // — quel numero diventa enorme e la previsione una bugia.
        Vector3 offset = origin - frame.TransformPoint(box.center);
        Vector3 extents = WorldExtents(box);

        // L'entrata parte da zero e non da meno infinito, e questo fa due cose
        // in una: chi è già dentro la scatola riceve zero invece di un tempo
        // passato, e chi l'ha già attraversata si scarta da sé, perché la sua
        // uscita finisce prima dell'entrata.
        float enter = 0f;
        float exit = float.PositiveInfinity;

        if (!Slab(Vector3.Dot(offset, frame.right), Vector3.Dot(velocity, frame.right), extents.x + radius, ref enter, ref exit)
            || !Slab(Vector3.Dot(offset, frame.up), Vector3.Dot(velocity, frame.up), extents.y + radius, ref enter, ref exit)
            || !Slab(Vector3.Dot(offset, frame.forward), Vector3.Dot(velocity, frame.forward), extents.z + radius, ref enter, ref exit))
        {
            return false;
        }

        seconds = enter;
        return true;
    }

    /// <summary>
    /// Le semi-estensioni della scatola in scala di mondo. Il valore assoluto
    /// serve perché una scala negativa specchia l'oggetto ma non gli cambia la
    /// stazza, e una semi-estensione negativa renderebbe la lastra vuota.
    /// </summary>
    public static Vector3 WorldExtents(BoxCollider box)
    {
        Vector3 scaled = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
        return new Vector3(Mathf.Abs(scaled.x), Mathf.Abs(scaled.y), Mathf.Abs(scaled.z));
    }

    /// <summary>
    /// Restringe l'intervallo di tempo con quello di una lastra. Falso quando i
    /// due non si sovrappongono più: da lì in poi non c'è niente da cercare
    /// sugli assi che restano.
    /// </summary>
    private static bool Slab(float offset, float speed, float extent, ref float enter, ref float exit)
    {
        // Parallelo alla lastra: non ci si entra e non se ne esce mai. O ci si è
        // già dentro per sempre, e allora quest'asse non vincola niente, o se ne
        // è fuori per sempre, e la risposta è già arrivata.
        if (Mathf.Abs(speed) < Epsilon)
        {
            return Mathf.Abs(offset) <= extent;
        }

        float inverse = 1f / speed;
        float first = (-extent - offset) * inverse;
        float second = (extent - offset) * inverse;

        // Andando all'indietro su quest'asse si incontra prima il piano lontano.
        if (first > second)
        {
            (first, second) = (second, first);
        }

        if (first > enter)
        {
            enter = first;
        }

        if (second < exit)
        {
            exit = second;
        }

        return enter <= exit;
    }
}
