using UnityEngine;
using UnityEngine.UIElements;

// UI Toolkit ha un proprio Cursor, che è l'icona di un elemento: qui serve
// quello di sistema.
using Cursor = UnityEngine.Cursor;

/// <summary>
/// Unica porta d'ingresso del modulo HUD: espone gli Inputs come metodi pubblici
/// e smista a Logics. Non contiene logica di gioco.
/// Va montato sul GameObject HUD, accanto al suo UIDocument.
///
/// Non ha Outputs e non ha uno State: non c'è niente che l'HUD possa decidere,
/// e il suo stato è già a schermo.
///
/// <b>È l'unica delle tre schermate che tiene ancora un Logics</b>, e il motivo
/// è che è l'unica che elabora davvero: soglie di colore, coordinate da
/// ribaltare, marcatori da accendere e spegnere. E lo fa a ogni frame, quindi
/// gli elementi li tiene da parte invece di ricercarli ogni volta.
///
/// <b>Il cursore è suo.</b> Il mirino ne prende il posto mentre si gioca, quindi
/// è l'HUD a spegnerlo — ed è sempre l'HUD a riaccenderlo: chi lo spegne lo
/// riaccende, e per questo le altre due schermate non lo toccano.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HudManager : MonoBehaviour
{
    private UIDocument document;
    private HudLogics logics;

    /// <summary>
    /// Logics nasce alla prima occasione utile e non in <c>Awake</c>:
    /// <c>rootVisualElement</c> non esiste ancora lì, e fra due componenti dello
    /// stesso GameObject l'ordine degli <c>OnEnable</c> non è garantito.
    /// </summary>
    private HudLogics Logics => logics ??= new HudLogics(document.rootVisualElement);

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // Riaccendendosi, l'UIDocument ricostruisce il proprio albero: gli
        // elementi tenuti da Logics sarebbero quelli di prima, che non sono più
        // a schermo. Si riparte dall'aggancio.
        logics = null;
    }

    private void OnDisable()
    {
        // Se si esce dal Play a partita in corso il cursore deve tornare:
        // nessuno spegnimento può lasciare il giocatore senza puntatore.
        Cursor.visible = true;
    }

    // ---------- INPUTS ----------

    /// <summary>Mette l'HUD a schermo e cede il posto al mirino.</summary>
    public void Show()
    {
        document.rootVisualElement.SetVisible(true);
        Cursor.visible = false;
    }

    /// <summary>Toglie l'HUD e restituisce il cursore di sistema.</summary>
    public void Hide()
    {
        document.rootVisualElement.SetVisible(false);
        Cursor.visible = true;
    }

    /// <summary>La fotografia da riversare a schermo. Arriva a ogni frame.</summary>
    public void Present(in HudSnapshot snapshot)
    {
        Logics.Present(snapshot);
    }
}
