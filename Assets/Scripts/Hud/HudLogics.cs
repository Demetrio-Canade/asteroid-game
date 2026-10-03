using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Elaborazione del modulo HUD: prende una <see cref="HudSnapshot"/> e la
/// riversa sugli elementi dell'interfaccia.
///
/// Non tiene nessuno State, ed è voluto: lo stato dell'HUD è già a schermo, e
/// tenerne una copia in memoria vorrebbe dire avere due verità da allineare a
/// mano. Non legge niente da solo — nemmeno il mouse: la posizione del mirino
/// arriva nello snapshot, dalla stessa sorgente da cui parte il colpo.
///
/// <b>È l'unica delle tre schermate che ha ancora un Logics.</b> Game Over e
/// Main Menu non elaborano niente — scrivono quello che gli passano — e hanno
/// tutto dentro il Manager. Qui invece si decide: soglie di colore, coordinate
/// da ribaltare, marcatori da accendere. E si decide a ogni frame, quindi gli
/// elementi si tengono da parte una volta sola invece di ricercarli ogni volta.
///
/// Se l'HUD sia a schermo o no non lo sa: quello è un Input, e gli Inputs
/// stanno sul Manager.
/// </summary>
public class HudLogics
{
    private const string WarnClass = "is-warn";
    private const string CriticalClass = "is-critical";
    private const string BlockedClass = "is-blocked";
    private const string CannonClass = "is-cannon";
    private const string ChargingClass = "is-charging";
    private const string HeatingClass = "is-heating";
    private const string ShownClass = "is-shown";
    private const string VisibleClass = "is-visible";
    private const string ImminentClass = "is-imminent";

    private const string ThreatClass = "threat";
    private const string ThreatTimerClass = "threat__timer";
    private const string PipClass = "pip";
    private const string PipOnClass = "is-on";

    /// <summary>Sotto questo tempo all'impatto il marcatore passa all'allarme.</summary>
    private const float ImminentSeconds = 1f;

    /// <summary>Sopra questa frazione di calore la mitraglietta è "quasi finita".</summary>
    private const float HeatWarnThreshold = 0.7f;

    /// <summary>Quanto resta a schermo una notifica prima di andarsene, in millisecondi.</summary>
    private const long NoticeHoldMs = 1400;

    private readonly VisualElement root;
    private readonly VisualElement crosshair;
    private readonly VisualElement heatGauge;
    private readonly VisualElement heatFill;
    private readonly VisualElement chargeFill;
    private readonly Label notice;

    /// <summary>
    /// L'uscita programmata della notifica a schermo. Non è uno State del
    /// gioco: è l'orologio di un'animazione, e serve solo a spostarla in avanti
    /// se nel frattempo ne arriva un'altra.
    /// </summary>
    private IVisualElementScheduledItem noticeExit;

    // I marcatori d'impatto ci sono sempre tutti, come i due reticoli: si
    // raccolgono una volta sola e poi si accendono e si spengono. Quanti siano
    // lo dice soltanto l'UXML — qui li si conta, non li si decide.
    private readonly List<VisualElement> threatSlots;
    private readonly List<Label> threatTimers;

    // I segmenti di integrità e di minaccia: ci sono sempre tutti, se ne
    // accendono quanti servono. Quanti siano lo dice l'UXML.
    private readonly VisualElement integrityPanel;
    private readonly VisualElement difficultyPanel;
    private readonly List<VisualElement> integrityPips;
    private readonly List<VisualElement> difficultyPips;

    private readonly Label timeValue;
    private readonly Label scoreValue;
    private readonly Label levelValue;
    private readonly Label integrityValue;

    public HudLogics(VisualElement root)
    {
        this.root = root;

        crosshair = root.GetVisualElement("crosshair");
        heatGauge = root.GetVisualElement("gauge-heat");
        heatFill = root.GetVisualElement("fill-heat");
        chargeFill = root.GetVisualElement("fill-charge");
        notice = root.GetLabel("notice");

        threatSlots = root.GetVisualElementsByClass(ThreatClass);
        threatTimers = new List<Label>(threatSlots.Count);
        foreach (VisualElement slot in threatSlots)
        {
            threatTimers.Add(slot.GetLabelByClass(ThreatTimerClass));
        }

        integrityPanel = root.GetVisualElement("panel-integrity");
        difficultyPanel = root.GetVisualElement("panel-difficulty");
        integrityPips = root.GetVisualElement("pips-integrity").GetVisualElementsByClass(PipClass);
        difficultyPips = root.GetVisualElement("pips-difficulty").GetVisualElementsByClass(PipClass);

        timeValue = root.GetLabel("value-time");
        scoreValue = root.GetLabel("value-score");
        levelValue = root.GetLabel("value-level");
        integrityValue = root.GetLabel("value-integrity");

        // L'HUD non ha niente su cui si possa cliccare: non deve nemmeno poter
        // intercettare un click diretto al gioco che sta sotto.
        root.IgnoreClicks();
    }

    // ---------- SCRITTURA ----------

    public void Present(in HudSnapshot snapshot)
    {
        PresentRun(snapshot);
        PresentShip(snapshot.Player);
        PresentCrosshair(snapshot.Player);
        PresentGauges(snapshot.Player);
        PresentThreats(snapshot.Threats);
    }

    /// <summary>
    /// Tempo, punti e difficoltà: i fatti della partita, quelli che si
    /// consultano quando serve. La difficoltà sale di colore verso l'ultimo
    /// gradino, perché "quanto è dura adesso" è un avviso e non un dato.
    /// </summary>
    private void PresentRun(in HudSnapshot snapshot)
    {
        timeValue.SetText(UiFormat.Time(snapshot.ElapsedSeconds));
        scoreValue.SetText(UiFormat.Score(snapshot.Score));

        int level = snapshot.DifficultyLevel;
        int levels = snapshot.DifficultyLevelCount;

        levelValue.SetText($"{level}/{levels}");
        SetPips(difficultyPips, level);
        SetSeverity(difficultyPanel, level >= levels - 1, level >= levels);
    }

    /// <summary>
    /// L'integrità della navicella, con la scala di colore del GDD §13. È un
    /// numero piccolo e intero, quindi si disegna a segmenti: contarne tre
    /// accesi è più veloce che valutare quanto è piena una barra.
    /// </summary>
    private void PresentShip(in PlayerSnapshot player)
    {
        int health = Mathf.Max(0, Mathf.CeilToInt(player.Health));
        int maxHealth = Mathf.Max(0, Mathf.CeilToInt(player.MaxHealth));

        integrityValue.SetText($"{health}/{maxHealth}");
        SetPips(integrityPips, health);

        SetSeverity(integrityPanel, health <= 2, health <= 1);
    }

    /// <summary>
    /// Il mirino segue la mira, non il mouse: è la stessa <c>AimData</c> da cui
    /// parte il raggio, quindi dove si vede la croce è dove finisce il colpo.
    ///
    /// L'Input System conta i pixel dal basso, UI Toolkit dall'alto: la Y va
    /// ribaltata prima di dare il punto al pannello, che poi ci mette del suo la
    /// scala del PanelSettings.
    ///
    /// Qui si sposta un punto e si accendono due classi. Che aspetto abbia il
    /// reticolo di ciascuna arma, e quanto sia grande, sta soltanto nell'USS:
    /// per questo si posiziona il contenitore e non il disegno.
    /// </summary>
    private void PresentCrosshair(in PlayerSnapshot player)
    {
        if (crosshair == null || root.panel == null)
        {
            return;
        }

        Vector2 flipped = new Vector2(
            player.AimScreenPosition.x,
            Screen.height - player.AimScreenPosition.y);

        Vector2 point = RuntimePanelUtils.ScreenToPanel(root.panel, flipped);

        crosshair.style.left = point.x;
        crosshair.style.top = point.y;

        bool isCannon = player.Weapon == TurretWeapon.Cannon;
        bool cannonReady = player.CannonCooldown <= 0f;

        // Quale reticolo si accende: la raffica e il colpo singolo non si
        // giocano allo stesso modo, e non devono nemmeno somigliarsi.
        crosshair.SetClass(CannonClass, isCannon);

        // Rosso quando l'arma in mano non può sparare: il mirino dice anche
        // se in questo istante premere serve a qualcosa.
        crosshair.SetClass(
            BlockedClass,
            isCannon ? !cannonReady : player.MachineGunOverheated);
    }

    /// <summary>
    /// Le due barre ai lati del mirino: il calore della mitraglietta a sinistra,
    /// la carica del cannone a destra.
    ///
    /// Valgono per l'arma e non per quella in mano: il calore si smaltisce e il
    /// cannone ricarica anche mentre si usa l'altra, ed è proprio lì che
    /// servono. Ciascuna c'è solo quando ha qualcosa da dire — a canna fredda e
    /// a cannone carico il mirino resta pulito.
    /// </summary>
    private void PresentGauges(in PlayerSnapshot player)
    {
        if (crosshair == null)
        {
            return;
        }

        float heat = Mathf.Clamp01(player.MachineGunHeat);

        crosshair.SetClass(HeatingClass, heat > 0f);
        heatFill.SetVerticalFill(heat);
        SetSeverity(heatGauge, heat >= HeatWarnThreshold, player.MachineGunOverheated);

        crosshair.SetClass(ChargingClass, player.CannonCooldown > 0f);
        chargeFill.SetVerticalFill(1f - Mathf.Clamp01(player.CannonCooldownNormalized));
    }

    /// <summary>
    /// Gli avvisi d'impatto: un marcatore per ciascuno, dove si vede adesso, con
    /// i secondi che mancano.
    ///
    /// L'HUD non sa cosa siano — riceve punti e secondi, non asteroidi — e non
    /// sceglie quali mostrare: arrivano già in ordine di urgenza, e se sono più
    /// dei marcatori disponibili restano fuori i meno vicini. Quanti marcatori
    /// esistano lo dice l'UXML, non questo metodo.
    ///
    /// Stessa convenzione del mirino: pixel con l'origine in basso a sinistra,
    /// quindi la Y va ribaltata prima di darla al pannello.
    /// </summary>
    private void PresentThreats(IReadOnlyList<HudThreatMarker> threats)
    {
        if (threatSlots == null || root.panel == null)
        {
            return;
        }

        int shown = threats == null ? 0 : Mathf.Min(threats.Count, threatSlots.Count);

        for (int i = 0; i < shown; i++)
        {
            // Indicizzato e non foreach: su IReadOnlyList<T> il foreach boxa
            // l'enumeratore, e qui si passa a ogni frame.
            HudThreatMarker marker = threats[i];
            VisualElement slot = threatSlots[i];

            Vector2 flipped = new Vector2(
                marker.ScreenPosition.x,
                Screen.height - marker.ScreenPosition.y);

            Vector2 point = RuntimePanelUtils.ScreenToPanel(root.panel, flipped);

            slot.style.left = point.x;
            slot.style.top = point.y;

            slot.SetClass(VisibleClass, true);
            slot.SetClass(ImminentClass, marker.Seconds <= ImminentSeconds);

            threatTimers[i].SetText($"{marker.Seconds:0.0}");
        }

        // I marcatori avanzati non si tolgono di mezzo: si spengono. Sono gli
        // stessi elementi di prima, e al prossimo frame potrebbero riservire.
        for (int i = shown; i < threatSlots.Count; i++)
        {
            threatSlots[i].SetClass(VisibleClass, false);
        }
    }

    // ---------- NOTIFICHE ----------

    /// <summary>
    /// Annuncia un fatto appena successo: la scritta entra, resta un momento e
    /// se ne va. Se ne arriva un'altra mentre è a schermo, riparte da capo.
    ///
    /// Qui si decidono il testo e i tempi; l'entrata e l'uscita sono una
    /// transizione dell'USS, che scatta accendendo e spegnendo una classe.
    /// </summary>
    public void Notify(HudNotice value)
    {
        if (notice == null)
        {
            return;
        }

        notice.SetText(NoticeText(value));

        // Spenta e riaccesa al frame dopo: se era già a schermo, la transizione
        // riparte e l'entrata si rivede invece di non succedere niente.
        noticeExit?.Pause();
        notice.SetClass(ShownClass, false);
        notice.schedule.Execute(() => notice.SetClass(ShownClass, true));

        noticeExit = notice.schedule
            .Execute(() => notice.SetClass(ShownClass, false))
            .StartingIn(NoticeHoldMs);
    }

    private static string NoticeText(HudNotice value)
    {
        switch (value)
        {
            case HudNotice.CannonReady:
                return "> CANNON READY";
            default:
                return string.Empty;
        }
    }

    // ---------- UTILITÀ ----------

    /// <summary>
    /// Le soglie di colore sono classi, non colori scritti a codice: il giorno
    /// in cui il verde diventa un altro verde si tocca solo l'USS.
    /// </summary>
    private static void SetSeverity(VisualElement element, bool warn, bool critical)
    {
        element.SetClass(WarnClass, warn && !critical);
        element.SetClass(CriticalClass, critical);
    }

    /// <summary>
    /// Accende i primi <paramref name="count"/> segmenti e spegne gli altri.
    /// Se i segmenti dell'UXML sono meno del valore, si accendono tutti: a
    /// schermo il numero accanto resta comunque la verità.
    /// </summary>
    private static void SetPips(List<VisualElement> pips, int count)
    {
        for (int i = 0; i < pips.Count; i++)
        {
            pips[i].SetClass(PipOnClass, i < count);
        }
    }
}
