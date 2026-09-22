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
    private const string VisibleClass = "is-visible";
    private const string ImminentClass = "is-imminent";

    private const string ThreatClass = "threat";
    private const string ThreatTimerClass = "threat__timer";

    /// <summary>Sotto questo tempo all'impatto il marcatore passa all'allarme.</summary>
    private const float ImminentSeconds = 1f;

    /// <summary>Sopra questa frazione di calore la mitraglietta è "quasi finita".</summary>
    private const float HeatWarnThreshold = 0.7f;

    private readonly VisualElement root;
    private readonly VisualElement crosshair;
    private readonly VisualElement chargeFill;

    // I marcatori d'impatto ci sono sempre tutti, come i due reticoli: si
    // raccolgono una volta sola e poi si accendono e si spengono. Quanti siano
    // lo dice soltanto l'UXML — qui li si conta, non li si decide.
    private readonly List<VisualElement> threatSlots;
    private readonly List<Label> threatTimers;

    private readonly VisualElement hullRow;
    private readonly VisualElement machineGunRow;
    private readonly VisualElement cannonRow;

    private readonly VisualElement hullFill;
    private readonly VisualElement machineGunFill;
    private readonly VisualElement cannonFill;

    private readonly Label timeValue;
    private readonly Label scoreValue;
    private readonly Label levelValue;
    private readonly Label hullValue;
    private readonly Label weaponValue;
    private readonly Label machineGunValue;
    private readonly Label cannonValue;

    public HudLogics(VisualElement root)
    {
        this.root = root;

        crosshair = root.GetVisualElement("crosshair");
        chargeFill = root.GetVisualElement("fill-charge");

        threatSlots = root.GetVisualElementsByClass(ThreatClass);
        threatTimers = new List<Label>(threatSlots.Count);
        foreach (VisualElement slot in threatSlots)
        {
            threatTimers.Add(slot.GetLabelByClass(ThreatTimerClass));
        }

        hullRow = root.GetVisualElement("row-hull");
        machineGunRow = root.GetVisualElement("row-machinegun");
        cannonRow = root.GetVisualElement("row-cannon");

        hullFill = root.GetVisualElement("fill-hull");
        machineGunFill = root.GetVisualElement("fill-machinegun");
        cannonFill = root.GetVisualElement("fill-cannon");

        timeValue = root.GetLabel("value-time");
        scoreValue = root.GetLabel("value-score");
        levelValue = root.GetLabel("value-level");
        hullValue = root.GetLabel("value-hull");
        weaponValue = root.GetLabel("value-weapon");
        machineGunValue = root.GetLabel("value-machinegun");
        cannonValue = root.GetLabel("value-cannon");

        // L'HUD non ha niente su cui si possa cliccare: non deve nemmeno poter
        // intercettare un click diretto al gioco che sta sotto.
        root.IgnoreClicks();
    }

    // ---------- SCRITTURA ----------

    public void Present(in HudSnapshot snapshot)
    {
        PresentRun(snapshot);
        PresentShip(snapshot.Player);
        PresentWeapons(snapshot.Player);
        PresentCrosshair(snapshot.Player);
        PresentThreats(snapshot.Threats);
    }

    /// <summary>Tempo, punti e difficoltà: i fatti della partita.</summary>
    private void PresentRun(in HudSnapshot snapshot)
    {
        timeValue.SetText(UiFormat.Time(snapshot.ElapsedSeconds));
        scoreValue.SetText(UiFormat.Score(snapshot.Score));
        levelValue.SetText($"{snapshot.DifficultyLevel}/{snapshot.DifficultyLevelCount}");
    }

    /// <summary>La vita della navicella, con la scala di colore del GDD §11.</summary>
    private void PresentShip(in PlayerSnapshot player)
    {
        int health = Mathf.Max(0, Mathf.CeilToInt(player.Health));
        int maxHealth = Mathf.Max(0, Mathf.CeilToInt(player.MaxHealth));

        hullValue.SetText($"{health}/{maxHealth}");
        hullFill.SetFill(Ratio(player.Health, player.MaxHealth));

        SetSeverity(hullRow, health <= 2, health <= 1);
    }

    /// <summary>Arma selezionata, calore della mitraglietta, ricarica del cannone.</summary>
    private void PresentWeapons(in PlayerSnapshot player)
    {
        weaponValue.SetText(player.Weapon == TurretWeapon.MachineGun ? "MACHINE GUN" : "CANNON");

        // Mitraglietta: la barra è il calore, quindi piena vuol dire ferma.
        machineGunFill.SetFill(Mathf.Clamp01(player.MachineGunHeat));
        machineGunValue.SetText(
            player.MachineGunOverheated
                ? "OVERHEAT"
                : $"{Mathf.RoundToInt(Mathf.Clamp01(player.MachineGunHeat) * 100f)}%");

        SetSeverity(
            machineGunRow,
            player.MachineGunHeat >= HeatWarnThreshold,
            player.MachineGunOverheated);

        // Cannone: la barra è la carica, quindi piena vuol dire pronto.
        float charge = 1f - Mathf.Clamp01(player.CannonCooldownNormalized);
        bool ready = player.CannonCooldown <= 0f;

        cannonFill.SetFill(charge);
        cannonValue.SetText(ready ? "READY" : $"{player.CannonCooldown:0.0}s");
        SetSeverity(cannonRow, !ready, false);
    }

    /// <summary>
    /// Il mirino segue la mira, non il mouse: è la stessa <c>AimData</c> da cui
    /// parte il raggio, quindi dove si vede la croce è dove finisce il colpo.
    ///
    /// L'Input System conta i pixel dal basso, UI Toolkit dall'alto: la Y va
    /// ribaltata prima di dare il punto al pannello, che poi ci mette del suo la
    /// scala del PanelSettings.
    ///
    /// Qui si sposta un punto e si accendono tre classi. Che aspetto abbia il
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

        // La barretta esiste solo mentre il cannone ricarica: quando è pronto
        // non avrebbe niente da dire, e sotto il mirino sarebbe solo rumore.
        crosshair.SetClass(ChargingClass, isCannon && !cannonReady);
        chargeFill.SetFill(1f - Mathf.Clamp01(player.CannonCooldownNormalized));

        // Rosso quando l'arma in mano non può sparare: il mirino dice anche
        // se in questo istante premere serve a qualcosa.
        crosshair.SetClass(
            BlockedClass,
            isCannon ? !cannonReady : player.MachineGunOverheated);
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

    // ---------- UTILITÀ ----------

    /// <summary>
    /// Le soglie di colore sono classi, non colori scritti a codice: il giorno
    /// in cui il verde diventa un altro verde si tocca solo l'USS.
    /// </summary>
    private static void SetSeverity(VisualElement row, bool warn, bool critical)
    {
        row.SetClass(WarnClass, warn && !critical);
        row.SetClass(CriticalClass, critical);
    }

    private static float Ratio(float value, float max)
    {
        return max <= 0f ? 0f : Mathf.Clamp01(value / max);
    }
}
