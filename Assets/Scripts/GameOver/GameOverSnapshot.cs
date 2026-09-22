/// <summary>
/// Il bilancio della partita appena chiusa, già fatto.
///
/// Arriva insieme all'ordine di mostrarsi, non dopo: questa schermata si riempie
/// una volta sola, e un momento in cui fosse a video senza niente da dire sarebbe
/// uno stato che non deve esistere.
///
/// I due <c>IsNew…</c> non sono deducibili dai numeri: un pareggio col vecchio
/// record ha esattamente lo stesso aspetto di un sorpasso, e senza qualcuno che
/// lo dica la schermata non saprebbe quale dei due sta mostrando.
/// </summary>
public readonly struct GameOverSnapshot
{
    /// <summary>I punti fatti in questa partita.</summary>
    public readonly int Score;

    /// <summary>Il record di punti, già aggiornato se è appena caduto.</summary>
    public readonly int BestScore;

    /// <summary>Questa partita ha battuto il record di punti.</summary>
    public readonly bool IsNewBestScore;

    /// <summary>Quanto è durata questa partita, in secondi.</summary>
    public readonly float Time;

    /// <summary>Il record di sopravvivenza, già aggiornato se è appena caduto.</summary>
    public readonly float BestTime;

    /// <summary>Questa partita ha battuto il record di sopravvivenza.</summary>
    public readonly bool IsNewBestTime;

    public GameOverSnapshot(
        int score,
        int bestScore,
        bool isNewBestScore,
        float time,
        float bestTime,
        bool isNewBestTime)
    {
        Score = score;
        BestScore = bestScore;
        IsNewBestScore = isNewBestScore;
        Time = time;
        BestTime = bestTime;
        IsNewBestTime = isNewBestTime;
    }
}
