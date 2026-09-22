using System;
using UnityEngine;

/// <summary>Elaborazione del modulo Asteroid: orchestra i componenti e lo State.</summary>
public class AsteroidLogics
{
    private readonly AsteroidState state;
    private readonly AsteroidMovementComponent movement;
    private readonly AsteroidMeshVariantComponent meshVariant;
    private readonly AsteroidImpactComponent impact;
    private readonly AsteroidEffects effects;
    private readonly HealthComponent health;

    private AsteroidDeathCause pendingDeathCause;
    private Vector3 deathEffectPosition;

    public event Action<AsteroidDeathCause> OnDied;
    public event Action OnImpact;
    public event Action OnOutOfBounds;

    public AsteroidLogics(
        AsteroidState state,
        AsteroidMovementComponent movement,
        AsteroidMeshVariantComponent meshVariant,
        AsteroidImpactComponent impact,
        AsteroidEffects effects,
        HealthComponent health)
    {
        this.state = state;
        this.movement = movement;
        this.meshVariant = meshVariant;
        this.impact = impact;
        this.effects = effects;
        this.health = health;
    }

    public void Attach()
    {
        if (movement != null)
        {
            movement.OnLifetimeExpired += HandleLifetimeExpired;
        }

        if (impact != null)
        {
            impact.OnImpact += HandleImpact;
        }

        if (health != null)
        {
            health.OnDie += HandleDie;
        }
    }

    public void Detach()
    {
        if (movement != null)
        {
            movement.OnLifetimeExpired -= HandleLifetimeExpired;
        }

        if (impact != null)
        {
            impact.OnImpact -= HandleImpact;
        }

        if (health != null)
        {
            health.OnDie -= HandleDie;
        }
    }

    public void Spawn(Vector3 position, Vector3 target, float speed, float lifetime)
    {
        // state.IsAlive = false;
        // movement?.Stop();
        // impact?.Disarm();

        health?.Restore();
        meshVariant?.SelectRandom();

        state.IsAlive = health != null && health.IsAlive;
        if (!state.IsAlive)
        {
            return;
        }

        pendingDeathCause = AsteroidDeathCause.Killed;
        impact?.Arm();
        movement?.Begin(position, target, speed, lifetime);

        // La rotta si ricopia da chi l'ha appena fissata, e non si ricalcola da
        // partenza e destinazione: se il volo non è partito — succede quando i
        // due punti coincidono — quella qui resta zero, e l'asteroide non
        // dichiara una rotta che non ha.
        state.Velocity = movement != null ? movement.Velocity : Vector3.zero;
    }

    public void Despawn()
    {
        state.IsAlive = false;
        state.Velocity = Vector3.zero;
        movement?.Stop();
        impact?.Disarm();
        meshVariant?.HideAll();
    }

    public void ApplyDamage(float damage, Vector3 effectPosition)
    {
        if (!state.IsAlive || health == null || damage <= 0f)
        {
            return;
        }

        effects?.PlayHit(effectPosition);
        deathEffectPosition = effectPosition;
        pendingDeathCause = AsteroidDeathCause.DestroyedByDamage;
        health.ApplyDamage(damage);
        state.IsAlive = health.IsAlive;
    }

    public void Kill(Vector3 effectPosition)
    {
        if (!state.IsAlive || health == null)
        {
            return;
        }

        pendingDeathCause = AsteroidDeathCause.Killed;
        deathEffectPosition = effectPosition;
        health.Kill();
        state.IsAlive = health.IsAlive;
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        if (state.IsAlive)
        {
            movement?.Tick(fixedDeltaTime);
        }
    }

    private void HandleDie()
    {
        if (!state.IsAlive)
        {
            return;
        }

        state.IsAlive = false;
        state.Velocity = Vector3.zero;
        movement?.Stop();
        impact?.Disarm();
        effects?.PlayDestruction(deathEffectPosition);
        OnDied?.Invoke(pendingDeathCause);
    }

    private void HandleImpact()
    {
        if (state.IsAlive)
        {
            OnImpact?.Invoke();
        }
    }

    private void HandleLifetimeExpired()
    {
        if (!state.IsAlive)
        {
            return;
        }

        state.IsAlive = false;
        state.Velocity = Vector3.zero;
        impact?.Disarm();
        OnOutOfBounds?.Invoke();
    }
}
