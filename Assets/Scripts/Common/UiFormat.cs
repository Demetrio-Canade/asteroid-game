using UnityEngine;

/// <summary>
/// Come si scrivono a schermo i numeri del gioco.
///
/// Non è di nessuna schermata in particolare: decide per tutte. Un tempo scritto
/// in un modo nell'HUD e in un altro nel Game Over sarebbe lo stesso dato con due
/// facce, e nessuno saprebbe quale delle due è quella giusta.
/// </summary>
public static class UiFormat
{
    /// <summary>Secondi in <c>mm:ss</c>. I decimi si buttano: un HUD non è un cronometro.</summary>
    public static string Time(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    /// <summary>Un punteggio. Passa di qui perché il giorno in cui vorrà i separatori, li avrà ovunque.</summary>
    public static string Score(int score)
    {
        return score.ToString();
    }
}
