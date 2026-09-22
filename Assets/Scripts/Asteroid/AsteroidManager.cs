using System;
using UnityEngine;

/// <summary>
/// Unica porta d'ingresso del modulo Asteroid.
///
/// È anche un <see cref="IRadarContact"/>: da fuori si può chiedere dove va e
/// quanto è grosso, senza che quel «fuori» debba sapere che è un asteroide. Le
/// tre risposte che nascono dallo State sono implementate <b>esplicitamente</b>,
/// così restano visibili solo attraverso l'interfaccia e non allargano la porta
/// del modulo con dei doppioni di ciò che c'è già.
/// </summary>
public class AsteroidManager : MonoBehaviour, IRadarContact
{
    private const string Tag = "[AsteroidManager]";

    [SerializeField] private AsteroidState state = new AsteroidState();

    [Header("Componenti")]
    [SerializeField] private AsteroidMovementComponent movement;
    [SerializeField] private AsteroidMeshVariantComponent meshVariant;
    [SerializeField] private AsteroidImpactComponent impact;
    [SerializeField] private AsteroidEffects effects;
    [SerializeField] private HealthComponent health;

    private AsteroidLogics logics;

    public string Id => gameObject.name;
    public bool IsAlive => state.IsAlive;

    /// <summary>
    /// Quanto è grosso l'asteroide adesso: il raggio della sfera che lo
    /// contiene, valido dopo lo <see cref="Spawn"/> che ha scelto la variante.
    ///
    /// Serve a chi deve prevedere dove passa. Un impatto non lo fa il centro,
    /// lo fa il corpo: prevederlo su un punto vorrebbe dire non vedere quasi
    /// nessuno degli asteroidi grossi, che sono quelli che contano.
    /// </summary>
    public float ImpactRadius => meshVariant != null ? meshVariant.ActiveRadius : 0f;

    // ---------- COME LO VEDE UN RADAR ----------

    Vector3 IRadarContact.Position => transform.position;

    Vector3 IRadarContact.Velocity => state.Velocity;

    float IRadarContact.Radius => ImpactRadius;

    bool IRadarContact.IsActive => state.IsAlive;

    public event Action<string, AsteroidDeathCause> OnDied;
    public event Action<string> OnImpact;
    public event Action<string> OnOutOfBounds;

    private void Awake()
    {
        ValidateReferences();

        logics = new AsteroidLogics(state, movement, meshVariant, impact, effects, health);
        logics.OnDied += HandleDied;
        logics.OnImpact += HandleImpact;
        logics.OnOutOfBounds += HandleOutOfBounds;
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
        logics.OnImpact -= HandleImpact;
        logics.OnOutOfBounds -= HandleOutOfBounds;
    }

    private void FixedUpdate()
    {
        logics?.PhysicsTick(Time.fixedDeltaTime);
    }

    // ---------- INPUTS ----------

    public void Spawn(Vector3 position, Vector3 target, float speed, float lifetime)
    {
        logics?.Spawn(position, target, speed, lifetime);
    }

    public void ApplyDamage(float damage)
    {
        logics?.ApplyDamage(damage, transform.position);
    }

    public void Kill()
    {
        logics?.Kill(transform.position);
    }

    public void Despawn()
    {
        logics?.Despawn();
    }

    private void HandleDied(AsteroidDeathCause cause)
    {
        OnDied?.Invoke(Id, cause);
    }

    private void HandleImpact()
    {
        OnImpact?.Invoke(Id);
    }

    private void HandleOutOfBounds()
    {
        OnOutOfBounds?.Invoke(Id);
    }

    private void ValidateReferences()
    {
        if (movement == null)
        {
            Debug.LogError($"{Tag} il campo 'movement' non è collegato.");
        }

        if (meshVariant == null)
        {
            Debug.LogError($"{Tag} il campo 'meshVariant' non è collegato.");
        }

        if (impact == null)
        {
            Debug.LogError($"{Tag} il campo 'impact' non è collegato.");
        }

        if (effects == null)
        {
            Debug.LogError($"{Tag} il campo 'effects' non è collegato.");
        }

        if (health == null)
        {
            Debug.LogError($"{Tag} il campo 'health' non è collegato.");
        }
    }
}
