using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Il radar di bordo: chi sta per colpire la navicella, e fra quanto.
///
/// <b>Perché sta qui e non nel connettore.</b> Un parametro nel connettore non è
/// portabile: il giorno in cui nasce una seconda modalità di gioco, un radar
/// scritto lì va riscritto. Un componente arriva col prefab e funziona ovunque
/// lo si monti. Ed è anche il verso giusto della finzione — l'asteroide non
/// telefona alla navicella, è la navicella ad avere i sensori.
///
/// <b>Come guarda.</b> Da ogni placca della DamageZone proietta in avanti un
/// corridoio — una scatola virtuale lunga quanto la portata, che esiste solo per
/// la durata della query. Niente collider dedicati e niente trigger: un
/// <c>OverlapBox</c> a mano, a intervalli. Un radar <i>spazza</i>, non vede di
/// continuo.
///
/// <b>Il corridoio scopre, l'aritmetica decide.</b> La scatola non deve essere
/// precisa, deve essere generosa: dice soltanto chi vale la pena esaminare. Poi
/// <see cref="SweptSphere"/> dice se quel corpo colpirà davvero una placca e in
/// che istante. Per questo il margine laterale conta — senza, un sasso che sta
/// per prendere la placca di striscio non entra mai nel corridoio.
///
/// <b>Le posizioni non si memorizzano mai.</b> Nel registro finiscono un
/// riferimento e un istante, mai un punto nello spazio: la posizione si rilegge
/// viva quando serve. È il motivo per cui la cadenza dello sweep non ha niente a
/// che vedere con la fluidità di ciò che si disegna — <b>lo sweep scopre a
/// pochi hertz, la posizione si legge a ogni frame.</b> Congelandola, un
/// marcatore a schermo procederebbe a scatti.
/// </summary>
public class RadarComponent : MonoBehaviour
{
    private const string Tag = "[RadarComponent]";

    /// <summary>
    /// Quanti corpi può restituire un singolo <c>OverlapBox</c>. Non è
    /// <see cref="maxContacts"/> e non deve esserlo: il corridoio raccoglie
    /// <b>candidati</b>, e la gran parte di loro passerà a vuoto. Un buffer
    /// stretto quanto i marcatori butterebbe via candidati a caso, e fra quelli
    /// buttati ci sarebbe anche chi colpisce.
    /// </summary>
    private const int SweepBufferSize = 32;

    /// <summary>Prima i più urgenti: se i contatti eccedono, si perdono i meno vicini.</summary>
    private static readonly Comparison<ThreatContact> ByUrgency =
        (left, right) => left.Seconds.CompareTo(right.Seconds);

    [Header("Cosa difende")]
    [Tooltip("Il nodo che raccoglie le placche della zona di danno. I collider si prendono da lì: aggiungerne una quarta non richiede di toccare niente.")]
    [SerializeField] private Transform damageZone;

    [Header("Cosa cerca")]
    [Tooltip("I layer che il radar considera contatti. Va tenuto uguale a ciò che può fare male: cercare più di così vuol dire annunciare impatti che non avverranno.")]
    [SerializeField] private LayerMask contactMask = 1 << 3;

    [Header("Portata")]
    [Tooltip("Quanto in là arriva il corridoio proiettato da ogni placca. È questa la distanza a cui la minaccia compare, e sostituisce il vecchio preavviso a tempo: essendo spazio e non secondi, un corpo veloce si annuncia da più lontano di uno lento. Gli asteroidi nascono a circa 18 unità e viaggiano fra 2 e 9 al secondo: oltre quella distanza si vedrebbero tutti già dalla partenza.")]
    [Min(0f)]
    [SerializeField] private float range = 15f;

    [Tooltip("Di quanto il corridoio è più largo della placca. Va almeno quanto il raggio del corpo più grosso, o chi sta per prendere la placca di striscio non entra mai nel corridoio e non lo si vede arrivare.")]
    [Min(0f)]
    [SerializeField] private float margin = 1.5f;

    [Header("Funzionamento")]
    [Tooltip("Ogni quanti secondi il radar spazza. Un radar non vede di continuo: 0.1-0.2 basta e avanza, e non cambia nulla della fluidità perché le posizioni si rileggono comunque a ogni frame.")]
    [Min(0.01f)]
    [SerializeField] private float sweepInterval = 0.15f;

    [Tooltip("Quanti contatti riesce a seguire in una volta. Oltre questo numero restano i più imminenti.")]
    [Min(1)]
    [SerializeField] private int maxContacts = 6;

    /// <summary>Un contatto in rotta di collisione, e quando ci arriva.</summary>
    private readonly struct Tracked
    {
        public readonly IRadarContact Source;
        public readonly float ImpactAt;

        public Tracked(IRadarContact source, float impactAt)
        {
            Source = source;
            ImpactAt = impactAt;
        }
    }

    private readonly Dictionary<string, Tracked> tracked = new Dictionary<string, Tracked>();

    /// <summary>Chi va tolto dal registro. Riusata: <see cref="Collect"/> gira a ogni frame.</summary>
    private readonly List<string> stale = new List<string>();

    /// <summary>I contatti da consegnare, riusati. Una lista nuova ogni frame sarebbe spazzatura costante.</summary>
    private readonly List<ThreatContact> contacts = new List<ThreatContact>();

    private BoxCollider[] plates;
    private Collider[] sweepBuffer;
    private float sweepTimer;
    private bool active;

    private void Awake()
    {
        sweepBuffer = new Collider[SweepBufferSize];
        CollectPlates();
    }

    // ---------- INPUTS ----------

    /// <summary>
    /// Accende o spegne il radar. Spegnendolo il registro si svuota: fuori
    /// partita non ci sono minacce, e quelle di prima non devono sopravvivere a
    /// un Retry.
    /// </summary>
    public void SetActive(bool value)
    {
        active = value;
        sweepTimer = 0f;

        if (!value)
        {
            tracked.Clear();
        }
    }

    /// <summary>Un frame. Lo sweep parte solo quando il suo intervallo è scaduto.</summary>
    public void Tick(float deltaTime)
    {
        if (!active)
        {
            return;
        }

        sweepTimer -= deltaTime;

        if (sweepTimer > 0f)
        {
            return;
        }

        sweepTimer = sweepInterval;
        Sweep();
    }

    // ---------- OUTPUT ----------

    /// <summary>
    /// I contatti in rotta di collisione adesso, dal più imminente in poi.
    ///
    /// Qui si fanno due cose che lo sweep non può fare: si <b>rileggono le
    /// posizioni vive</b>, ed è quello che tiene i contatti incollati ai corpi
    /// anche fra uno sweep e l'altro; e si <b>ripulisce il registro</b>, che è il
    /// solo posto in cui può essere ripulito, perché la navicella non riceve
    /// nessuna notizia di abbattimenti e fuoricampo. Non le riceve e non le
    /// vuole: le chiede.
    ///
    /// La lista è <b>prestata</b>, non regalata: va letta subito.
    /// </summary>
    public IReadOnlyList<ThreatContact> Collect()
    {
        contacts.Clear();

        if (!active || tracked.Count == 0)
        {
            return contacts;
        }

        stale.Clear();
        float now = Time.time;

        foreach (KeyValuePair<string, Tracked> entry in tracked)
        {
            IRadarContact source = entry.Value.Source;

            if (IsGone(source))
            {
                stale.Add(entry.Key);
                continue;
            }

            float remaining = entry.Value.ImpactAt - now;

            // Il momento è passato: o ha colpito — e allora non è più in campo,
            // e la riga sopra l'avrebbe già tolto — o l'ha sfiorata. In tutti e
            // due i casi non c'è più niente da annunciare.
            if (remaining < 0f)
            {
                stale.Add(entry.Key);
                continue;
            }

            contacts.Add(new ThreatContact(source.Position, remaining));
        }

        foreach (string id in stale)
        {
            tracked.Remove(id);
        }

        contacts.Sort(ByUrgency);

        if (contacts.Count > maxContacts)
        {
            contacts.RemoveRange(maxContacts, contacts.Count - maxContacts);
        }

        return contacts;
    }

    // ---------- LA SPAZZATA ----------

    /// <summary>
    /// Un giro completo: ogni placca proietta il proprio corridoio e ciò che ci
    /// trova dentro viene messo alla prova contro <b>tutte</b> le placche.
    ///
    /// Non serve tenere il conto di chi si è già visto in questo giro, e i
    /// corridoi si accavallano parecchio. Il motivo è che l'esame di un corpo
    /// non dipende da quale corridoio l'abbia scovato: si prova sempre contro
    /// tutte le placche e si tiene la più vicina nel tempo. Vedere lo stesso
    /// sasso due volte riscrive lo stesso numero.
    /// </summary>
    private void Sweep()
    {
        if (plates == null)
        {
            return;
        }

        foreach (BoxCollider plate in plates)
        {
            if (plate == null)
            {
                continue;
            }

            SweepCorridor(plate);
        }
    }

    private void SweepCorridor(BoxCollider plate)
    {
        Vector3 extents = SweptSphere.WorldExtents(plate);
        int thin = ThinnestAxis(extents);
        Vector3 outward = OutwardAxis(plate, thin);

        // Il corridoio ha la stessa faccia della placca, allargata del margine,
        // e per profondità la portata: è la placca stessa trascinata in avanti.
        Vector3 half = new Vector3(extents.x + margin, extents.y + margin, extents.z + margin);
        half[thin] = range * 0.5f;

        Vector3 center = plate.transform.TransformPoint(plate.center) + outward * (range * 0.5f);

        // QueryTriggerInteraction esplicito: cosa sia trigger e cosa no è una
        // scelta dei prefab, e il radar non deve dipendere da un interruttore
        // globale del progetto che nessuno ricorda di aver messo.
        int found = Physics.OverlapBoxNonAlloc(
            center,
            half,
            sweepBuffer,
            plate.transform.rotation,
            contactMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < found; i++)
        {
            Examine(sweepBuffer[i]);
        }
    }

    /// <summary>
    /// Un candidato alla prova. Se colpirà, il suo istante entra nel registro —
    /// e ci rientra a ogni giro, sovrascrivendo il precedente.
    ///
    /// <b>Il ricalcolo non è uno spreco, è la correzione di un bug.</b> I corpi
    /// arrivano da un pool: la stessa istanza torna in campo con lo stesso nome
    /// ma una rotta nuova, e un registro scritto una volta sola conserverebbe
    /// l'impatto del volo precedente. Un pugno di contatti per tre placche e tre
    /// assi, a pochi hertz, non si misura.
    /// </summary>
    private void Examine(Collider candidate)
    {
        if (candidate == null)
        {
            return;
        }

        // I collider stanno sulle mesh figlie, il Rigidbody e il Manager sul
        // root: attachedRigidbody ci arriva in un passo solo.
        Rigidbody body = candidate.attachedRigidbody;

        if (body == null)
        {
            return;
        }

        IRadarContact source = body.GetComponent<IRadarContact>();

        // Non è roba che dichiari una rotta: non è affare del radar. Un corpo
        // fermo, invece, non arriva addosso a nessuno.
        if (source == null || !source.IsActive || source.Velocity.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        if (!TimeToImpact(source, out float seconds))
        {
            return;
        }

        tracked[source.Id] = new Tracked(source, Time.time + seconds);
    }

    /// <summary>
    /// Fra quanto questo corpo tocca la zona di danno: la più vicina nel tempo
    /// fra le placche che colpisce. Falso se non ne colpisce nessuna.
    /// </summary>
    private bool TimeToImpact(IRadarContact source, out float seconds)
    {
        seconds = float.PositiveInfinity;
        bool hits = false;

        foreach (BoxCollider plate in plates)
        {
            if (plate == null)
            {
                continue;
            }

            if (!SweptSphere.TimeToHit(source.Position, source.Velocity, source.Radius, plate, out float candidate))
            {
                continue;
            }

            if (candidate < seconds)
            {
                seconds = candidate;
                hits = true;
            }
        }

        return hits;
    }

    // ---------- LA GEOMETRIA DELLE PLACCHE ----------

    /// <summary>
    /// Da che parte guarda una placca: il suo asse più sottile, verso l'esterno.
    ///
    /// Si ricava invece di dichiararlo perché così è la placca stessa a dire da
    /// che parte protegge: ruotarne una nell'Inspector per coprire un fianco
    /// porta con sé il suo corridoio, senza che si tocchi una riga.
    /// </summary>
    private Vector3 OutwardAxis(BoxCollider plate, int thin)
    {
        Transform frame = plate.transform;
        Vector3 axis = thin == 0 ? frame.right : thin == 1 ? frame.up : frame.forward;

        // "Fuori" vuol dire lontano dal centro della navicella. Una placca
        // piazzata esattamente lì non ha un verso da dichiarare, e in quel caso
        // ci si tiene quello che ha.
        Vector3 fromCentre = frame.TransformPoint(plate.center) - damageZone.position;

        return Vector3.Dot(axis, fromCentre) >= 0f ? axis : -axis;
    }

    /// <summary>Lo spessore di una placca è il suo lato più corto: è quello l'asse che guarda fuori.</summary>
    private static int ThinnestAxis(Vector3 extents)
    {
        if (extents.x <= extents.y && extents.x <= extents.z)
        {
            return 0;
        }

        return extents.y <= extents.z ? 1 : 2;
    }

    private void CollectPlates()
    {
        if (damageZone == null)
        {
            Debug.LogError($"{Tag} il campo 'damageZone' non è collegato: il radar non sa cosa difendere.");
            plates = Array.Empty<BoxCollider>();
            return;
        }

        plates = damageZone.GetComponentsInChildren<BoxCollider>(true);

        if (plates.Length == 0)
        {
            Debug.LogError($"{Tag} sotto '{damageZone.name}' non c'è nessun BoxCollider: il radar non vedrà niente.");
        }
    }

    /// <summary>
    /// Quel contatto non c'è più.
    ///
    /// Il doppio controllo non è pignoleria: un riferimento tenuto come
    /// <b>interfaccia</b> non passa dall'operatore <c>==</c> di Unity, e un
    /// oggetto distrutto sembrerebbe ancora buono fino al primo accesso. Il cast
    /// a <c>Object</c> rimette in mezzo il controllo vero.
    /// </summary>
    private static bool IsGone(IRadarContact contact)
    {
        if (contact == null)
        {
            return true;
        }

        if (contact is UnityEngine.Object carrier && carrier == null)
        {
            return true;
        }

        return !contact.IsActive;
    }
}
