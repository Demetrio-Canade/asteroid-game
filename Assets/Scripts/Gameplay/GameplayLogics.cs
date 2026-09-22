using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Il connettore: avvia e chiude la partita e collega Player e Asteroid senza
/// far conoscere i due moduli fra loro.
///
/// È l'unico posto da cui si vede l'insieme, e per questo si legge dall'alto: i
/// metodi del ciclo di vita dicono <b>cosa</b> succede, un elenco di intenzioni
/// per volta. Il <b>come</b> sta un piano sotto, nei metodi raggruppati in fondo
/// e nelle classi di servizio — <see cref="AsteroidPool"/> e
/// <see cref="AsteroidSpawner"/> — che sono sue e non sono moduli.
/// </summary>
public class GameplayLogics : MonoBehaviour
{
    private const string Tag = "[GameplayLogics]";

    [Header("Moduli")]
    [SerializeField] private PlayerManager player;
    [SerializeField] private AsteroidManager asteroidPrefab;
    [SerializeField] private HudManager hud;
    [SerializeField] private GameOverManager gameOver;

    [Header("Spawn asteroidi")]
    [SerializeField] private Transform asteroidSpawnPoint;
    [SerializeField] private Transform asteroidPoolParent;
    [Tooltip("Quanto largo è il cielo da cui scendono: raggio del disco attorno al punto di spawn. A 0 partono tutti dallo stesso punto.")]
    [Min(0f)]
    [SerializeField] private float spawnSpreadRadius = 3f;
    [Tooltip("Quanto largo è il campo che puntano attorno alla navicella. È l'unico raggio: quanti finiscano addosso e quanti larghi non lo decide un secondo raggio ma l'addensamento, che sale con la difficoltà.")]
    [Min(0f)]
    [SerializeField] private float targetSpreadRadius = 5f;
    [Min(0.01f)]
    [SerializeField] private float asteroidLifetime = 10f;
    [Min(0f)]
    [SerializeField] private float playerImpactDamage = 1f;

    [Header("Avviso d'impatto")]
    [Tooltip("La camera da cui si vede il campo: è lei a dire dove cade a schermo un punto del mondo. Chi sia una minaccia lo decide il radar della navicella; qui si traduce soltanto in pixel.")]
    [SerializeField] private Camera viewCamera;

    [Header("Punteggio")]
    [Min(0)]
    [SerializeField] private int pointsPerAsteroid = 100;

    [Header("Difficoltà")]
    [SerializeField] private DifficultySettings difficulty = new DifficultySettings();

    [Header("Stato")]
    [SerializeField] private GameplayState state = new GameplayState();

    [Header("Avvio")]
    [Tooltip("La partita comincia da sé entrando in Play. Finché non c'è il menu, è l'unico modo per cominciare.")]
    [SerializeField] private bool startOnPlay = true;

    private AsteroidPool asteroidPool;
    private AsteroidSpawner asteroidSpawner;

    /// <summary>I marcatori da consegnare all'HUD, riusati: si ricostruiscono a ogni frame.</summary>
    private readonly List<HudThreatMarker> threatMarkers = new List<HudThreatMarker>();

    // ---------- CICLO DI VITA ----------

    private void Awake()
    {
        ValidateReferences();
        CreateAsteroidPool();
        CreateAsteroidSpawner();
    }

    private void OnEnable()
    {
        SubscribeToPlayer();
        SubscribeToGameOver();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayer();
        UnsubscribeFromGameOver();
    }

    private void OnDestroy()
    {
        DestroyAsteroidPool();
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (!state.IsRunning)
        {
            return;
        }

        AdvanceClock();
        AdvanceDifficulty();
        AdvanceSpawn();
        RefreshHud();
    }

    // ---------- INIZIO E FINE ----------

    /// <summary>Comincia una partita. Ricominciare e cominciare sono la stessa cosa.</summary>
    public void StartGame()
    {
        gameOver?.Hide();
        ClearAsteroids();
        ResetRun();

        player?.Spawn();
        hud?.Show();
    }

    /// <summary>Chiude la partita in corso e rimette tutti gli asteroidi nella pool.</summary>
    public void EndGame()
    {
        if (!state.IsRunning)
        {
            return;
        }

        // Prima si spegne, poi si svuota: da qui in avanti niente di ciò che gli
        // asteroidi hanno da dire deve più contare.
        StopRun();
        ClearAsteroids();

        player?.Despawn();
        hud?.Hide();
        gameOver?.Show(CloseTheBooks());
    }

    // ---------- I FATTI, E COSA NE SEGUE ----------

    // I due danni vanno nelle due direzioni opposte, e per questo stanno vicini.

    private void PlayerHitAsteroid(string asteroidId, float damage)
    {
        if (!state.IsRunning)
        {
            return;
        }

        DamageAsteroid(asteroidId, damage);
    }

    /// <summary>
    /// Il danno lo incassa solo chi era davvero nel registro: un nome che non
    /// c'è è un impatto avvenuto altrove.
    /// </summary>
    private void AsteroidHitPlayer(string asteroidId)
    {
        if (!state.IsRunning)
        {
            return;
        }

        if (!KillAsteroid(asteroidId))
        {
            return;
        }

        // Kill ha già emesso OnDied ed è rientrato di qui a rilasciare
        // l'asteroide. Il danno arriva dopo, e solo se la partita è ancora in
        // piedi: così un EndGame nel frattempo non lo rilascia due volte.
        if (state.IsRunning)
        {
            player?.ApplyDamage(playerImpactDamage);
        }
    }

    private void AsteroidDied(string asteroidId, AsteroidDeathCause cause)
    {
        AwardPointsFor(cause);
        ReleaseAsteroid(asteroidId);
    }

    private void AsteroidWentOutOfBounds(string asteroidId)
    {
        ReleaseAsteroid(asteroidId);
    }

    private void PlayerDied()
    {
        EndGame();
    }

    // ---------- LA PARTITA ----------

    /// <summary>Rimette la partita a zero: tempo, punti, difficoltà e ritmo di spawn.</summary>
    private void ResetRun()
    {
        state.IsRunning = true;
        state.Elapsed = 0f;
        state.Score = 0;
        state.Intensity = 0f;
        state.DifficultyLevel = 1;

        asteroidSpawner?.Reset();
    }

    private void StopRun()
    {
        state.IsRunning = false;
    }

    /// <summary>
    /// Chiude il bilancio della partita. Il salvataggio avviene <b>qui</b>, cioè
    /// prima che la schermata si mostri: così il record a video è già quello
    /// nuovo, e non quello che c'era un attimo fa.
    ///
    /// Il <c>bool</c> di ritorno di Submit è ciò che accende i badge: senza, due
    /// numeri uguali a schermo sarebbero ambigui.
    /// </summary>
    private GameOverSnapshot CloseTheBooks()
    {
        BestRunResult record = BestRun.Submit(state.Score, state.Elapsed);

        return new GameOverSnapshot(
            state.Score,
            BestRun.Score,
            record.IsNewBestScore,
            state.Elapsed,
            BestRun.Time,
            record.IsNewBestTime);
    }

    private void AdvanceClock()
    {
        state.Elapsed += Time.deltaTime;
    }

    /// <summary>
    /// L'intensità si <b>ricava</b> dal tempo trascorso passando per la curva,
    /// non si accumula frame per frame: un frame lungo non può più far perdere
    /// un pezzo di salita, perché non c'è niente da accumulare.
    ///
    /// Il livello viene dietro, ed è solo un'etichetta da mostrare.
    /// </summary>
    private void AdvanceDifficulty()
    {
        state.Intensity = difficulty.IntensityAt(state.Elapsed);
        state.DifficultyLevel = difficulty.LevelAt(state.Intensity);
    }

    private void AdvanceSpawn()
    {
        asteroidSpawner?.Tick(Time.deltaTime, state.Intensity);
    }

    /// <summary>
    /// I punti non li dà la morte in sé, li dà il modo in cui è arrivata: uno
    /// schianto sulla navicella passa da Kill() e non vale niente.
    /// </summary>
    private void AwardPointsFor(AsteroidDeathCause cause)
    {
        if (cause == AsteroidDeathCause.DestroyedByDamage)
        {
            state.Score += pointsPerAsteroid;
        }
    }

    // ---------- GLI ASTEROIDI ----------

    private void RegisterAsteroid(AsteroidManager asteroid)
    {
        asteroid.OnDied += AsteroidDied;
        asteroid.OnImpact += AsteroidHitPlayer;
        asteroid.OnOutOfBounds += AsteroidWentOutOfBounds;
    }

    /// <summary>
    /// Svuota il campo in blocco. È l'unica uscita che <b>non</b> passa da
    /// <see cref="ReleaseAsteroid"/>: il pool rimette dentro le istanze in
    /// silenzio, senza che nessun asteroide emetta niente.
    ///
    /// Nessuno deve avvisare il radar della navicella: rimessi nel pool, questi
    /// asteroidi smettono di dichiararsi in campo, e al primo giro il radar li
    /// lascia cadere da sé. È il vantaggio di un registro che <b>chiede</b>
    /// invece di essere informato.
    /// </summary>
    private void ClearAsteroids()
    {
        asteroidPool?.ReleaseAll();
    }

    private void DamageAsteroid(string asteroidId, float damage)
    {
        if (asteroidPool != null && asteroidPool.TryGet(asteroidId, out AsteroidManager asteroid))
        {
            asteroid.ApplyDamage(damage);
        }
    }

    /// <summary>Ammazza l'asteroide indicato. Falso se quel nome non è nel registro.</summary>
    private bool KillAsteroid(string asteroidId)
    {
        if (asteroidPool == null || !asteroidPool.TryGet(asteroidId, out AsteroidManager asteroid))
        {
            return false;
        }

        asteroid.Kill();
        return true;
    }

    /// <summary>L'uscita di scena di un asteroide: morte, fuori campo e impatto passano tutti e tre di qui.</summary>
    private void ReleaseAsteroid(string asteroidId)
    {
        if (asteroidPool != null && asteroidPool.TryGet(asteroidId, out AsteroidManager asteroid))
        {
            asteroidPool.Release(asteroid);
        }
    }

    // ---------- L'HUD ----------

    private void RefreshHud()
    {
        hud?.Present(BuildHudSnapshot());
    }

    /// <summary>
    /// La fotografia per l'HUD. Il connettore mette i quattro numeri che sa —
    /// tempo, punti, difficoltà — e ci allega quella del Player così com'è:
    /// inoltra, non traduce. Un dato nuovo della navicella arriva a schermo
    /// senza che questo metodo cambi.
    ///
    /// I marcatori sono l'unica cosa che il connettore lavora davvero, e il
    /// lavoro è tutto qui: <b>tradurre</b>. Chi sia una minaccia e fra quanto
    /// arriva lo ha già deciso il radar della navicella; da mondo a pixel può
    /// passarci solo chi ha la camera, e la camera ce l'ha lui.
    /// </summary>
    private HudSnapshot BuildHudSnapshot()
    {
        PlayerSnapshot snapshot = player != null ? player.Snapshot : default;

        return new HudSnapshot(
            state.Elapsed,
            state.Score,
            state.DifficultyLevel,
            difficulty.LevelCount,
            snapshot,
            ProjectThreats(snapshot.Threats));
    }

    /// <summary>
    /// Le minacce del radar portate sullo schermo. Arrivano già ordinate per
    /// urgenza, quindi qui non si riordina niente: si proietta e basta.
    ///
    /// Chi finisce dietro la camera resta fuori. Lì WorldToScreenPoint
    /// restituisce coordinate specchiate: il marcatore comparirebbe in un punto
    /// a caso, e sarebbe un avviso che indica la direzione sbagliata.
    /// </summary>
    private IReadOnlyList<HudThreatMarker> ProjectThreats(IReadOnlyList<ThreatContact> threats)
    {
        threatMarkers.Clear();

        if (viewCamera == null || threats == null)
        {
            return threatMarkers;
        }

        for (int i = 0; i < threats.Count; i++)
        {
            Vector3 projected = viewCamera.WorldToScreenPoint(threats[i].Position);

            if (projected.z <= 0f)
            {
                continue;
            }

            threatMarkers.Add(new HudThreatMarker(
                new Vector2(projected.x, projected.y),
                threats[i].Seconds));
        }

        return threatMarkers;
    }

    // ---------- MONTAGGIO ----------

    /// <summary>
    /// Il pool nasce qui e non allo start della partita: è infrastruttura, non un
    /// fatto della singola partita. Fra un Game Over e un Retry le istanze
    /// restano, con i loro nomi.
    /// </summary>
    private void CreateAsteroidPool()
    {
        if (asteroidPrefab == null || asteroidPoolParent == null)
        {
            return;
        }

        asteroidPool = new AsteroidPool(
            asteroidPrefab,
            asteroidPoolParent,
            RegisterAsteroid,
            10,
            100);
    }

    private void CreateAsteroidSpawner()
    {
        asteroidSpawner = new AsteroidSpawner(
            asteroidPool,
            difficulty,
            asteroidSpawnPoint,
            spawnSpreadRadius,
            player != null ? player.transform : null,
            targetSpreadRadius,
            asteroidLifetime);
    }

    private void DestroyAsteroidPool()
    {
        asteroidPool?.Dispose();
        asteroidPool = null;
    }

    private void SubscribeToPlayer()
    {
        if (player == null)
        {
            return;
        }

        player.OnDied += PlayerDied;
        player.OnAsteroidHit += PlayerHitAsteroid;
    }

    private void UnsubscribeFromPlayer()
    {
        if (player == null)
        {
            return;
        }

        player.OnDied -= PlayerDied;
        player.OnAsteroidHit -= PlayerHitAsteroid;
    }

    /// <summary>
    /// Il Retry va dritto su StartGame: ricominciare e cominciare sono la stessa
    /// cosa, e un percorso separato non farebbe niente di diverso.
    /// </summary>
    private void SubscribeToGameOver()
    {
        if (gameOver == null)
        {
            return;
        }

        gameOver.OnRetryRequested += StartGame;
    }

    private void UnsubscribeFromGameOver()
    {
        if (gameOver == null)
        {
            return;
        }

        gameOver.OnRetryRequested -= StartGame;
    }

    private void ValidateReferences()
    {
        if (player == null)
        {
            Debug.LogError($"{Tag} il campo 'player' non è collegato.");
        }

        if (asteroidPrefab == null)
        {
            Debug.LogError($"{Tag} il campo 'asteroidPrefab' non è collegato.");
        }

        if (hud == null)
        {
            Debug.LogWarning($"{Tag} il campo 'hud' non è collegato: si gioca al buio.");
        }

        if (gameOver == null)
        {
            Debug.LogWarning($"{Tag} il campo 'gameOver' non è collegato: la partita finirà senza dirlo a nessuno.");
        }

        if (asteroidSpawnPoint == null)
        {
            Debug.LogError($"{Tag} il campo 'asteroidSpawnPoint' non è collegato.");
        }

        if (asteroidPoolParent == null)
        {
            Debug.LogError($"{Tag} il campo 'asteroidPoolParent' non è collegato.");
        }

        if (viewCamera == null)
        {
            Debug.LogWarning($"{Tag} il campo 'viewCamera' non è collegato: gli avvisi d'impatto non compariranno.");
        }
    }
}
