using System;
using UnityEngine;

/// <summary>
/// La salute di qualcosa: quanta ne ha, quanta può averne, se è ancora vivo.
///
/// Non sa di chi è. Lo stesso componente sta sulla navicella e starà
/// sull'asteroide: gestisce tre numeri e dice quando sono finiti, e chi lo
/// ascolta decide cosa vuol dire.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float maxHealth = 5f;

    // ---------- OUTPUTS ----------

    /// <summary>La salute è finita. Parte una volta sola per ogni vita.</summary>
    public event Action OnDie;

    public float Health { get; private set; }

    public float MaxHealth => maxHealth;

    /// <summary>
    /// Non è "Health > 0", ed è voluto: è questo campo a garantire che OnDie
    /// parta una volta sola, e a rendere Kill() diverso da un danno qualsiasi
    /// pur finendo sullo stesso numero.
    /// </summary>
    public bool IsAlive { get; private set; }

    // ---------- INPUTS ----------

    public void ApplyDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
        {
            return;
        }

        Health = Mathf.Max(0f, Health - damage);

        if (Health <= 0f)
        {
            Die();
        }
    }

    /// <summary>Ammazza in un colpo solo, qualunque salute fosse rimasta.</summary>
    public void Kill()
    {
        if (!IsAlive)
        {
            return;
        }

        Health = 0f;
        Die();
    }

    /// <summary>Rimette in piedi con la salute piena.</summary>
    public void Restore()
    {
        Health = maxHealth;
        IsAlive = true;
    }

    private void Die()
    {
        IsAlive = false;

        OnDie?.Invoke();
    }
}
