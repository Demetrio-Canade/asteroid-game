using UnityEngine;

/// <summary>Quali record sono caduti con l'ultima partita.</summary>
public readonly struct BestRunResult
{
    public readonly bool IsNewBestScore;
    public readonly bool IsNewBestTime;

    public BestRunResult(bool isNewBestScore, bool isNewBestTime)
    {
        IsNewBestScore = isNewBestScore;
        IsNewBestTime = isNewBestTime;
    }
}

/// <summary>
/// I record che sopravvivono alla sessione: il punteggio più alto e la
/// sopravvivenza più lunga.
///
/// Non è un modulo e non è un componente: non ha presenza in scena e non ha
/// comportamento. È l'unico posto del progetto che conosce le chiavi dei
/// PlayerPrefs, e chi lo usa non deve sapere che esistono.
///
/// I due record sono <b>indipendenti</b>: si può battere il punteggio senza
/// battere il tempo, e viceversa. Sono due modi diversi di giocare bene — fare
/// molti punti in fretta, o resistere a lungo — e uno non deve cancellare l'altro.
/// </summary>
public static class BestRun
{
    private const string ScoreKey = "BestScore";
    private const string TimeKey = "BestTime";

    /// <summary>Il punteggio più alto mai fatto.</summary>
    public static int Score => PlayerPrefs.GetInt(ScoreKey, 0);

    /// <summary>La sopravvivenza più lunga mai raggiunta, in secondi.</summary>
    public static float Time => PlayerPrefs.GetFloat(TimeKey, 0f);

    /// <summary>
    /// Registra la partita appena chiusa e dice quali record ha battuto. Salva
    /// solo ciò che è davvero migliorato.
    /// </summary>
    public static BestRunResult Submit(int score, float time)
    {
        bool isNewBestScore = score > Score;
        bool isNewBestTime = time > Time;

        if (isNewBestScore)
        {
            PlayerPrefs.SetInt(ScoreKey, score);
        }

        if (isNewBestTime)
        {
            PlayerPrefs.SetFloat(TimeKey, time);
        }

        if (isNewBestScore || isNewBestTime)
        {
            // Salvataggio esplicito: senza, i record finirebbero su disco solo a
            // un'uscita pulita, e una chiusura brusca li perderebbe.
            PlayerPrefs.Save();
        }

        return new BestRunResult(isNewBestScore, isNewBestTime);
    }
}
