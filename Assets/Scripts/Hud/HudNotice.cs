/// <summary>
/// Un fatto da annunciare a schermo, una volta sola.
///
/// Il connettore dice <b>cosa</b> è successo, l'HUD decide <b>come</b> si scrive
/// e come compare: la stessa divisione di <see cref="UiFormat"/>. Un annuncio
/// nuovo è un valore qui e una riga in HudLogics, non un Input in più.
/// </summary>
public enum HudNotice
{
    /// <summary>Il cannone ha finito di ricaricare.</summary>
    CannonReady
}
