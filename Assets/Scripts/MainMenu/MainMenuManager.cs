using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Il modulo Main Menu, tutto qui dentro.
///
/// <b>È un modulo senza Logics, ed è una scelta.</b> Il §3 del Solutioning
/// prevede Manager, State e Logics, ma Logics è il posto dove si <i>elabora</i>:
/// qui non c'è niente da decidere fra il dato e lo schermo — due numeri si
/// leggono e si scrivono. Un Logics sarebbe un corridoio, non una stanza.
///
/// <b>E nemmeno Inputs.</b> Questa schermata non si mostra e non si nasconde:
/// nella sua scena c'è lei sola (§5.5), quindi non esiste nessuno che potrebbe
/// dirglielo, e non esiste un momento in cui debba stare via. Si riempie al
/// primo frame e resta finché la scena non cambia.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuManager : MonoBehaviour
{
    private UIDocument document;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    /// <summary>
    /// In <c>Start</c> e non in <c>Awake</c>: l'albero della UI nasce
    /// nell'<c>OnEnable</c> dell'UIDocument, e al primo <c>Start</c> tutti gli
    /// <c>OnEnable</c> della scena sono già passati.
    ///
    /// Qui sta la deroga del §5.5: questo Manager sa che i record sopravvivono
    /// alla sessione. Lo sa attraverso <see cref="BestRun"/> e non con le chiavi
    /// dei PlayerPrefs in mano.
    /// </summary>
    private void Start()
    {
        VisualElement root = document.rootVisualElement;

        root.SetText("value-best-score", UiFormat.Score(BestRun.Score));
        root.SetText("value-best-time", UiFormat.Time(BestRun.Time));

        // Andare in partita non è gameplay, è navigazione: il pulsante *è*
        // l'azione (§7.2). Il nome della scena arriva da GameScenes, non come
        // stringa scritta qui.
        root.OnClick("button-play", LoadCoreLoop);
    }

    private void LoadCoreLoop()
    {
        SceneManager.LoadScene(GameScenes.CoreLoop);
    }
}
