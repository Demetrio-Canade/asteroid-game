/// <summary>
/// I nomi delle scene, in un posto solo.
///
/// La navigazione non passa dal connettore (Solutioning §7.2): sono i moduli
/// delle schermate a caricare la scena successiva. Il prezzo è che conoscono i
/// nomi — e li conoscono da qui, non come stringhe sparse nel codice, così
/// rinominare una scena è una modifica sola.
/// </summary>
public static class GameScenes
{
    public const string MainMenu = "MainMenu";
    public const string CoreLoop = "CoreLoop";
}
