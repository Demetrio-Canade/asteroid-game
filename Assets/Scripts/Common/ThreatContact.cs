using UnityEngine;

/// <summary>
/// Una minaccia vista dal radar: dov'è nel mondo e fra quanti secondi arriva.
///
/// È la forma in cui una minaccia esce dal modulo Player. Non c'è dentro nessun
/// asteroide e non c'è dentro nessuno schermo: la navicella non sa cosa la stia
/// puntando, e non sa nemmeno che esista un monitor su cui disegnarlo.
///
/// Diventa un <see cref="HudThreatMarker"/> un passo più in là, nel connettore,
/// che è l'unico ad avere la camera con cui si passa dal mondo ai pixel.
/// </summary>
public readonly struct ThreatContact
{
    /// <summary>Dov'è il corpo in questo istante, in coordinate di mondo.</summary>
    public readonly Vector3 Position;

    /// <summary>Quanti secondi mancano all'impatto. Non è mai negativo.</summary>
    public readonly float Seconds;

    public ThreatContact(Vector3 position, float seconds)
    {
        Position = position;
        Seconds = seconds;
    }
}
