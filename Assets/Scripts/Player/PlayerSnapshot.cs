using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Come sta la navicella in questo istante, in sola lettura.
///
/// È l'unica cosa del modulo Player che si veda da fuori oltre agli eventi: lo
/// State resta dentro, e chi guarda riceve una copia che non può cambiare.
/// Stessa forma di <see cref="TurretSnapshot"/> — una fotografia, non una porta.
/// </summary>
public readonly struct PlayerSnapshot
{
    /// <summary>La navicella è in campo e pronta a giocare.</summary>
    public readonly bool Ready;

    /// <summary>La navicella non è ancora stata distrutta.</summary>
    public readonly bool IsAlive;

    /// <summary>Gli HP che restano.</summary>
    public readonly float Health;

    /// <summary>Gli HP da piena.</summary>
    public readonly float MaxHealth;

    /// <summary>L'arma in mano al giocatore.</summary>
    public readonly TurretWeapon Weapon;

    /// <summary>Surriscaldamento della mitraglietta, da 0 a 1.</summary>
    public readonly float MachineGunHeat;

    /// <summary>A calore pieno la mitraglietta si ferma finché non è tornata a zero.</summary>
    public readonly bool MachineGunOverheated;

    /// <summary>Secondi che mancano al prossimo colpo di cannone.</summary>
    public readonly float CannonCooldown;

    /// <summary>Lo stesso cooldown su 0..1, da 1 (appena sparato) a 0 (pronto).</summary>
    public readonly float CannonCooldownNormalized;

    /// <summary>
    /// Dove sta mirando il giocatore sullo schermo, in pixel e con l'origine in
    /// basso a sinistra. È la stessa sorgente da cui parte il colpo: chi disegna
    /// un mirino con questo valore non può disegnare una bugia.
    /// </summary>
    public readonly Vector2 AimScreenPosition;

    /// <summary>
    /// Chi sta per colpire la navicella secondo il suo radar, dal più imminente
    /// in poi, in coordinate di mondo. Può essere vuota e può essere
    /// <c>null</c>: chi la legge non deve darla per buona.
    ///
    /// È una lista <b>prestata</b>, non regalata: chi la produce la riusa a ogni
    /// frame. Va letta subito e non tenuta da parte.
    /// </summary>
    public readonly IReadOnlyList<ThreatContact> Threats;

    public PlayerSnapshot(
        bool ready,
        bool isAlive,
        float health,
        float maxHealth,
        TurretWeapon weapon,
        float machineGunHeat,
        bool machineGunOverheated,
        float cannonCooldown,
        float cannonCooldownNormalized,
        Vector2 aimScreenPosition,
        IReadOnlyList<ThreatContact> threats)
    {
        Ready = ready;
        IsAlive = isAlive;
        Health = health;
        MaxHealth = maxHealth;
        Weapon = weapon;
        MachineGunHeat = machineGunHeat;
        MachineGunOverheated = machineGunOverheated;
        CannonCooldown = cannonCooldown;
        CannonCooldownNormalized = cannonCooldownNormalized;
        AimScreenPosition = aimScreenPosition;
        Threats = threats;
    }
}
