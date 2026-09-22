using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Una fotografia della mira in questo istante: dove sta il cursore, lungo che
/// raggio parte il colpo, e quale punto del mondo si sta guardando.
/// </summary>
public readonly struct AimData
{
    /// <summary>La posizione del cursore, per il mirino dell'HUD.</summary>
    public readonly Vector2 ScreenPosition;

    /// <summary>Il raggio lungo cui parte il colpo. Non ha una distanza massima.</summary>
    public readonly Ray ShotRay;

    /// <summary>Il punto verso cui puntare, preso sul raggio a distanza fissa.</summary>
    public readonly Vector3 AimPoint;

    public AimData(Vector2 screenPosition, Ray shotRay, Vector3 aimPoint)
    {
        ScreenPosition = screenPosition;
        ShotRay = shotRay;
        AimPoint = aimPoint;
    }
}

/// <summary>
/// Traduce la posizione del mouse in una mira: un punto del mondo verso cui
/// puntare, e il raggio lungo cui parte il colpo.
///
/// Non sa cosa sia una torretta né cosa sia un'arma: guarda lo schermo e dice
/// dove sta guardando il giocatore. Chi lo ascolta decide cosa farne.
/// </summary>
public class AimLookComponent : MonoBehaviour
{
    private const string Tag = "[AimLook]";

    [SerializeField] private Camera aimCamera;
    [SerializeField] private float aimDistance = 200f;

    // ---------- OUTPUTS ----------

    public event Action<AimData> OnAimUpdated;

    /// <summary>L'ultima mira emessa, per chi si sveglia a metà frame.</summary>
    public AimData Last { get; private set; }

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
            Debug.LogWarning($"{Tag} nessuna camera collegata: uso Camera.main.");
        }
    }

    // ---------- INPUTS ----------

    /// <summary>Accende o spegne la mira. Da spenta non emette più niente.</summary>
    public void SetActive(bool value)
    {
        enabled = value;
    }

    public void Tick()
    {
        if (aimCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector2 screen = Mouse.current.position.ReadValue();

        // Un cursore fuori dalla finestra darebbe una mira che il giocatore non
        // sta vedendo: lo teniamo dentro il rettangolo dello schermo.
        screen.x = Mathf.Clamp(screen.x, 0f, Screen.width);
        screen.y = Mathf.Clamp(screen.y, 0f, Screen.height);

        Ray ray = aimCamera.ScreenPointToRay(screen);

        // Il punto di mira sta sul raggio a distanza fissa, e non dove il raggio
        // incontra qualcosa: altrimenti la torretta scatterebbe avanti e indietro
        // ogni volta che un asteroide entra o esce dalla linea di tiro.
        Last = new AimData(screen, ray, ray.GetPoint(aimDistance));

        OnAimUpdated?.Invoke(Last);
    }
}
