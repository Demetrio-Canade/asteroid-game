using UnityEngine;

/// <summary>
/// Ciò che il radar della navicella ha bisogno di sapere su un corpo in volo:
/// chi è, dov'è adesso, dove sta andando, quanto è grosso e se è ancora in
/// campo.
///
/// Esiste per una ragione sola: <b>il radar non deve sapere cosa sia un
/// asteroide.</b> Guarda fuori, trova dei corpi e chiede loro la propria rotta.
/// Il giorno in cui a volare verso la navicella fossero detriti, missili o navi
/// nemiche, il radar li vedrebbe già senza che nessuno lo tocchi — basta che
/// implementino questa interfaccia.
///
/// È la stessa figura che <see cref="AsteroidMeshVariantComponent.ActiveRadius"/>
/// ha introdotto nel progetto, portata a termine: il modulo <b>dichiara</b> ciò
/// che solo lui può sapere, e chi deve prevedere lo legge invece di stimarlo.
/// </summary>
public interface IRadarContact
{
    /// <summary>Il nome con cui il resto del gioco lo chiama. È la chiave del registro.</summary>
    string Id { get; }

    /// <summary>Dov'è <b>adesso</b>. Si rilegge a ogni frame e non si mette mai da parte.</summary>
    Vector3 Position { get; }

    /// <summary>
    /// Unità al secondo, direzione compresa. È un vettore solo e non una coppia
    /// direzione + velocità di proposito: due campi separati possono
    /// disallinearsi, e una rotta mezza aggiornata è peggio di nessuna rotta.
    ///
    /// Zero vuol dire fermo, e un corpo fermo non arriva addosso a nessuno.
    /// </summary>
    Vector3 Velocity { get; }

    /// <summary>
    /// Il raggio della sfera che lo contiene. Un impatto non lo fa il centro, lo
    /// fa il corpo: prevederlo su un punto vorrebbe dire non vedere quasi
    /// nessuno dei sassi grossi, che sono quelli che contano.
    /// </summary>
    float Radius { get; }

    /// <summary>
    /// È ancora in campo. <b>Il radar non viene avvisato quando un contatto
    /// sparisce: lo chiede.</b> Un radar guarda, non riceve comunicati — e così
    /// non serve che qualcuno gli racconti di abbattimenti, fuoricampo e
    /// impatti, che sono fatti di cui la navicella non sa niente.
    /// </summary>
    bool IsActive { get; }
}
