using System;
using UnityEngine;

/// <summary>Dati del modulo Asteroid.</summary>
[Serializable]
public class AsteroidState
{
    /// <summary>L'asteroide è attualmente in gioco e può reagire agli eventi.</summary>
    public bool IsAlive;

    /// <summary>
    /// La rotta corrente: unità al secondo, direzione compresa. Zero da fermo.
    ///
    /// È un dato della situazione dell'asteroide, non un dettaglio del suo
    /// movimento — ed è per questo che sta qui e non solo dentro il componente.
    /// Serve a chi da fuori deve prevedere dove passa: leggerla è l'unico modo
    /// per non doverla ricostruire frame dopo frame, e una rotta ricostruita da
    /// fuori sarebbe una seconda verità.
    /// </summary>
    public Vector3 Velocity;
}
