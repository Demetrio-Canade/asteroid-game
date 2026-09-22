using System;
using UnityEngine;

/// <summary>
/// Rileva il primo trigger appartenente ai layer configurati. Non conosce cosa
/// rappresenti l'oggetto toccato: emette soltanto un impatto.
/// </summary>
public class AsteroidImpactComponent : MonoBehaviour
{
    [SerializeField] private LayerMask impactMask = 1 << 6;

    private bool armed;

    public event Action OnImpact;

    public void Arm()
    {
        armed = true;
    }

    public void Disarm()
    {
        armed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || (impactMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        // Disarmare prima dell'evento impedisce che più collider della stessa
        // DamageZone producano danni multipli nello stesso passo fisico.
        armed = false;
        OnImpact?.Invoke();
    }
}
