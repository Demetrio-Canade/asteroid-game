using System;

/// <summary>
/// I dati della partita in corso. Non elabora niente: descrive soltanto a che
/// punto è il gioco.
///
/// Sono fatti della singola partita, non del progetto: <c>StartGame</c> li
/// riazzera tutti, e fra un Game Over e un Retry non sopravvive nessuno di
/// questi numeri.
/// </summary>
[Serializable]
public class GameplayState
{
    /// <summary>Una partita è cominciata e non è ancora finita.</summary>
    public bool IsRunning;

    /// <summary>Secondi trascorsi da quando è cominciata.</summary>
    public float Elapsed;

    /// <summary>I punti fatti finora.</summary>
    public int Score;

    /// <summary>
    /// Quanto è dura adesso, da 0 a 1. È il valore che guida davvero il gioco:
    /// da qui nascono il ritmo di spawn e la banda di velocità.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// Il livello di difficoltà raggiunto, da 1 in su. È una <b>lettura</b>
    /// dell'intensità, tenuta qui per poterne annunciare i cambi: non guida
    /// niente, la mostra soltanto l'HUD.
    /// </summary>
    public int DifficultyLevel;
}
