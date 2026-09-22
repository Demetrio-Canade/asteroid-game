using UnityEngine;

/// <summary>
/// Un avviso d'impatto già pronto da disegnare: dove metterlo e che numero
/// scriverci.
///
/// Non c'è dentro nessun asteroide, ed è il punto: l'HUD riceve un punto sullo
/// schermo e dei secondi, e non ha bisogno di sapere cosa li abbia prodotti. Il
/// giorno in cui a minacciare la navicella fosse altro, questa struct non
/// cambia.
/// </summary>
public readonly struct HudThreatMarker
{
    /// <summary>
    /// Dove si vede la minaccia in questo istante, in pixel e con l'origine in
    /// basso a sinistra: la stessa convenzione di
    /// <see cref="PlayerSnapshot.AimScreenPosition"/>.
    /// </summary>
    public readonly Vector2 ScreenPosition;

    /// <summary>Quanti secondi mancano all'impatto. Non scende sotto zero.</summary>
    public readonly float Seconds;

    public HudThreatMarker(Vector2 screenPosition, float seconds)
    {
        ScreenPosition = screenPosition;
        Seconds = seconds;
    }
}
