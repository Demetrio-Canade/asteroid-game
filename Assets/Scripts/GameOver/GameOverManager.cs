using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Il modulo Game Over, tutto qui dentro: due Inputs, un Output, e in mezzo
/// quattro numeri da scrivere.
///
/// <b>È un modulo senza Logics, ed è una scelta.</b> Logics è il posto dove si
/// <i>elabora</i>, e qui non si elabora niente: il bilancio arriva già chiuso
/// dal connettore, e quello che resta da fare è riversarlo. Separare non
/// proteggerebbe niente — aggiungerebbe solo un muro che l'evento del Retry
/// dovrebbe attraversare per uscire uguale da entrambe le parti.
///
/// Come l'HUD non ha nemmeno uno State: quello che c'è da sapere è già scritto
/// a schermo. E il cursore non lo tocca: lo spegne e lo riaccende l'HUD, che è
/// l'unico ad averne motivo (§4.3).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameOverManager : MonoBehaviour
{
    private const string NewRecordClass = "is-new";

    private UIDocument document;

    // ---------- OUTPUTS ----------

    /// <summary>Il giocatore ha chiesto di rigiocare.</summary>
    public event Action OnRetryRequested;

    private VisualElement Root => document.rootVisualElement;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    /// <summary>
    /// In <c>Start</c> e non in <c>Awake</c>: l'albero della UI nasce
    /// nell'<c>OnEnable</c> dell'UIDocument, e al primo <c>Start</c> tutti gli
    /// <c>OnEnable</c> della scena sono già passati.
    ///
    /// I pulsanti si agganciano una volta sola perché questo GameObject non si
    /// spegne mai: a togliere di mezzo la schermata è <c>display</c>, non
    /// <c>SetActive</c>. Il giorno in cui lo si spegnesse davvero, l'UIDocument
    /// ricostruirebbe l'albero e questi due pulsanti andrebbero riagganciati.
    /// </summary>
    private void Start()
    {
        Root.SetVisible(false);

        // Ricominciare è una decisione di gioco — riazzera punteggio, timer e
        // difficoltà, e rimette in campo la navicella — e quelle cose le sa fare
        // solo il connettore. Quindi qui si chiede, non si fa.
        Root.OnClick("button-retry", RequestRetry);

        // Tornare al menu invece non è gameplay: il pulsante *è* l'azione, e
        // farla rimbalzare dal connettore aggiungerebbe un passaggio che non
        // decide niente (§7.2).
        Root.OnClick("button-menu", LoadMainMenu);
    }

    // ---------- INPUTS ----------

    /// <summary>
    /// Mostra il bilancio della partita appena chiusa.
    ///
    /// I dati arrivano <b>insieme</b> all'ordine di mostrarsi, ed è la deroga
    /// del §7.4: questa schermata si riempie una volta sola, e un momento in cui
    /// fosse a video senza niente da dire sarebbe uno stato che non deve esistere.
    /// </summary>
    public void Show(in GameOverSnapshot snapshot)
    {
        Root.SetText("value-score", UiFormat.Score(snapshot.Score));
        Root.SetText("value-best-score", UiFormat.Score(snapshot.BestScore));
        Root.SetText("value-time", UiFormat.Time(snapshot.Time));
        Root.SetText("value-best-time", UiFormat.Time(snapshot.BestTime));

        // I due badge sono indipendenti come lo sono i record: si può battere il
        // punteggio senza battere il tempo, e viceversa.
        Root.SetClass("badge-score", NewRecordClass, snapshot.IsNewBestScore);
        Root.SetClass("badge-time", NewRecordClass, snapshot.IsNewBestTime);

        Root.SetVisible(true);
    }

    /// <summary>Toglie la schermata. La chiama il connettore quando riparte una partita.</summary>
    public void Hide()
    {
        Root.SetVisible(false);
    }

    // ---------- I DUE PULSANTI ----------

    private void RequestRetry()
    {
        OnRetryRequested?.Invoke();
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(GameScenes.MainMenu);
    }
}
