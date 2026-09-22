using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dati del modulo Player. Non elabora niente: descrive soltanto
/// la situazione corrente della navicella.
/// </summary>
[Serializable]
public class PlayerState
{
    /// <summary>La navicella è in campo e pronta a giocare.</summary>
    public bool Ready;

    /// <summary>Gli HP che restano alla navicella (GDD §11: ne parte con 5).</summary>
    public float Health;

    /// <summary>Gli HP che la navicella ha da piena, per chi deve disegnarne la frazione.</summary>
    public float MaxHealth;

    /// <summary>La navicella non è ancora stata distrutta.</summary>
    public bool IsAlive;

    /// <summary>L'arma in mano al giocatore. A inizio partita è la mitraglietta.</summary>
    public TurretWeapon SelectedWeapon;

    /// <summary>Surriscaldamento della mitraglietta, da 0 a 1.</summary>
    public float MachineGunHeat;

    /// <summary>A calore pieno la mitraglietta si ferma finché non è tornata a zero.</summary>
    public bool MachineGunOverheated;

    /// <summary>Secondi che mancano al prossimo colpo di cannone.</summary>
    public float CannonCooldown;

    /// <summary>Lo stesso cooldown su 0..1, da 1 (appena sparato) a 0 (pronto).</summary>
    public float CannonCooldownNormalized;

    /// <summary>Dove sta mirando il giocatore sullo schermo, per il mirino dell'HUD.</summary>
    public Vector2 AimScreenPosition;

    /// <summary>
    /// Chi sta per colpire la navicella, dal più imminente in poi, in coordinate
    /// di mondo. Può essere <c>null</c> fuori partita.
    ///
    /// È una lista <b>prestata</b> dal radar, non una copia: quel buffer si
    /// riusa a ogni frame per non lasciare spazzatura dietro di sé. Va letta
    /// subito e non tenuta da parte.
    ///
    /// Che esista uno schermo su cui disegnarle non è affare della navicella:
    /// qui ci sono punti nello spazio e secondi, e basta.
    /// </summary>
    [NonSerialized] public IReadOnlyList<ThreatContact> Threats;
}
