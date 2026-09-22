using System;
using UnityEngine;

/// <summary>
/// Unica porta d'ingresso del modulo Player: espone gli Inputs come metodi
/// pubblici e smista a Logics. Non contiene logica di gioco.
/// Va montato sull'oggetto root del prefab Player.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    private const string Tag = "[PlayerManager]";

    [SerializeField] private PlayerState state = new PlayerState();

    [Header("Componenti")]
    [SerializeField] private AimLookComponent aimLook;
    [SerializeField] private TurretComponent turret;
    [SerializeField] private PlayerInputComponent input;
    [SerializeField] private HealthComponent health;
    [SerializeField] private RadarComponent radar;

    private PlayerLogics logics;

    private void Awake()
    {
        if (aimLook == null)
        {
            Debug.LogError($"{Tag} il campo 'aimLook' non è collegato: la torretta non seguirà il mouse.");
        }

        if (turret == null)
        {
            Debug.LogError($"{Tag} il campo 'turret' non è collegato: niente rotazione e niente colpi.");
        }

        if (input == null)
        {
            Debug.LogError($"{Tag} il campo 'input' non è collegato: fuoco e cambio arma non risponderanno.");
        }

        if (health == null)
        {
            Debug.LogError($"{Tag} il campo 'health' non è collegato: la navicella non potrà essere distrutta.");
        }

        if (radar == null)
        {
            Debug.LogWarning($"{Tag} il campo 'radar' non è collegato: la navicella non vedrà arrivare niente.");
        }

        logics = new PlayerLogics(state, aimLook, turret, input, health, radar);
        logics.OnDied += HandleDied;
        logics.OnAsteroidHit += HandleAsteroidHit;

        // Prima dello Spawn non si mira e non si spara.
        if (aimLook != null)
        {
            aimLook.SetActive(false);
        }

        if (input != null)
        {
            input.SetActive(false);
        }
    }

    private void OnEnable()
    {
        logics?.Attach();
    }

    private void OnDisable()
    {
        logics?.Detach();
    }

    private void OnDestroy()
    {
        if (logics == null)
        {
            return;
        }

        logics.OnDied -= HandleDied;
        logics.OnAsteroidHit -= HandleAsteroidHit;
    }

    private void Update()
    {
        logics?.Tick(Time.deltaTime);
    }

    // ---------- INPUTS ----------

    public void Spawn()
    {
        logics.Spawn();
    }

    public void Despawn()
    {
        logics.Despawn();
    }

    public void ApplyDamage(float damage)
    {
        logics.ApplyDamage(damage);
    }

    // ---------- OUTPUTS ----------

    /// <summary>La navicella è stata distrutta.</summary>
    public event Action OnDied;

    /// <summary>La torretta ha colpito il root indicato per la quantità di danno indicata.</summary>
    public event Action<string, float> OnAsteroidHit;

    /// <summary>
    /// Come sta la navicella adesso, in sola lettura. Non è un evento perché non
    /// è un fatto: è una condizione, e chi la disegna la ridisegna a ogni frame.
    /// Lo State resta dentro il modulo — di qui esce solo una copia.
    /// </summary>
    public PlayerSnapshot Snapshot => new PlayerSnapshot(
        state.Ready,
        state.IsAlive,
        state.Health,
        state.MaxHealth,
        state.SelectedWeapon,
        state.MachineGunHeat,
        state.MachineGunOverheated,
        state.CannonCooldown,
        state.CannonCooldownNormalized,
        state.AimScreenPosition,
        state.Threats);

    private void HandleDied()
    {
        OnDied?.Invoke();
    }

    private void HandleAsteroidHit(string asteroidId, float damage)
    {
        OnAsteroidHit?.Invoke(asteroidId, damage);
    }
}
