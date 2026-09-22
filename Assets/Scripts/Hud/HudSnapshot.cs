using System.Collections.Generic;

/// <summary>
/// La fotografia che il connettore consegna all'HUD: tutto ciò che c'è da
/// scrivere a schermo, già pronto, in un colpo solo.
///
/// Due parti. I fatti della partita — tempo, punti, difficoltà — li sa soltanto
/// il connettore. Lo stato della navicella invece arriva dal Player e passa di
/// qui <b>intero</b>, senza essere smontato e rimontato: il connettore inoltra,
/// non traduce. Il giorno in cui il Player avrà qualcosa di nuovo da mostrare,
/// entra nel suo snapshot e arriva a schermo senza che il connettore cambi.
///
/// Gli avvisi d'impatto sono la terza parte, e sono l'unica che il connettore
/// costruisce davvero: nessuno dei due moduli potrebbe, perché per sapere chi
/// sta per colpire chi bisogna vederli entrambi.
/// </summary>
public readonly struct HudSnapshot
{
    /// <summary>Secondi di partita trascorsi.</summary>
    public readonly float ElapsedSeconds;

    /// <summary>I punti fatti finora.</summary>
    public readonly int Score;

    /// <summary>Il livello di difficoltà raggiunto.</summary>
    public readonly int DifficultyLevel;

    /// <summary>Quanti livelli esistono in tutto: oltre l'ultimo la pressione non sale più.</summary>
    public readonly int DifficultyLevelCount;

    /// <summary>Come sta la navicella, così come l'ha data il Player.</summary>
    public readonly PlayerSnapshot Player;

    /// <summary>
    /// Chi sta per colpire la navicella, dal più imminente in poi. Può essere
    /// vuota, e può essere <c>null</c>: chi la legge non deve darla per buona.
    ///
    /// È una lista <b>prestata</b>, non regalata: chi la produce la riusa a ogni
    /// frame per non lasciare spazzatura dietro di sé. Va letta subito e non
    /// tenuta da parte.
    /// </summary>
    public readonly IReadOnlyList<HudThreatMarker> Threats;

    public HudSnapshot(
        float elapsedSeconds,
        int score,
        int difficultyLevel,
        int difficultyLevelCount,
        in PlayerSnapshot player,
        IReadOnlyList<HudThreatMarker> threats)
    {
        ElapsedSeconds = elapsedSeconds;
        Score = score;
        DifficultyLevel = difficultyLevel;
        DifficultyLevelCount = difficultyLevelCount;
        Player = player;
        Threats = threats;
    }
}
