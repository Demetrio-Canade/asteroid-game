using System;
using UnityEngine;

/// <summary>
/// Elaborazione del modulo Player: pilota i componenti e tiene aggiornato lo State.
///
/// La mira nasce da AimLook, i comandi da PlayerInput, e torretta e salute sono
/// le uniche che sanno com'è messo il Player: qui non si duplicano quei numeri,
/// si ricopiano nello State perché il resto del gioco possa leggerli.
///
/// Il radar segue la stessa regola, con una differenza: la sua risposta non si
/// ricopia, si <b>presta</b>. Il buffer resta suo e nello State finisce solo il
/// riferimento — copiarlo a ogni frame vorrebbe dire allocare a ogni frame.
/// </summary>
public class PlayerLogics
{
    private readonly PlayerState state;
    private readonly AimLookComponent aimLook;
    private readonly TurretComponent turret;
    private readonly PlayerInputComponent input;
    private readonly HealthComponent health;
    private readonly RadarComponent radar;
    private readonly FeedbackComponent feedback;

    /// <summary>La navicella è stata distrutta. Sale al Manager, che lo dice al connettore.</summary>
    public event Action OnDied;

    /// <summary>La torretta ha colpito un bersaglio, identificato dal nome del root.</summary>
    public event Action<string, float> OnAsteroidHit;

    /// <summary>Il cannone ha finito di ricaricare.</summary>
    public event Action OnCannonReady;

    public PlayerLogics(
        PlayerState state,
        AimLookComponent aimLook,
        TurretComponent turret,
        PlayerInputComponent input,
        HealthComponent health,
        RadarComponent radar,
        FeedbackComponent feedback)
    {
        this.state = state;
        this.aimLook = aimLook;
        this.turret = turret;
        this.input = input;
        this.health = health;
        this.radar = radar;
        this.feedback = feedback;
    }

    public void Attach()
    {
        if (radar != null)
        {
            radar.OnContactsAcquired += HandleContactsAcquired;
        }

        if (health != null)
        {
            health.OnDie += HandleDie;
        }

        if (aimLook != null)
        {
            aimLook.OnAimUpdated += HandleAimUpdated;
        }

        if (input != null)
        {
            input.OnWeaponToggleRequested += HandleWeaponToggle;
        }

        if (turret != null)
        {
            turret.OnTargetHit += HandleTargetHit;
            turret.OnShot += HandleShot;
            turret.OnWeaponSelected += HandleWeaponSelected;
            turret.OnCannonReady += HandleCannonReady;
            turret.OnOverheated += HandleOverheated;
        }
    }

    public void Detach()
    {
        if (radar != null)
        {
            radar.OnContactsAcquired -= HandleContactsAcquired;
        }

        if (health != null)
        {
            health.OnDie -= HandleDie;
        }

        if (aimLook != null)
        {
            aimLook.OnAimUpdated -= HandleAimUpdated;
        }

        if (input != null)
        {
            input.OnWeaponToggleRequested -= HandleWeaponToggle;
        }

        if (turret != null)
        {
            turret.OnTargetHit -= HandleTargetHit;
            turret.OnShot -= HandleShot;
            turret.OnWeaponSelected -= HandleWeaponSelected;
            turret.OnCannonReady -= HandleCannonReady;
            turret.OnOverheated -= HandleOverheated;
        }
    }

    public void Spawn()
    {
        if (turret != null)
        {
            turret.ResetAim();
            turret.ResetWeapons();
        }

        if (health != null)
        {
            health.Restore();
        }

        if (aimLook != null)
        {
            aimLook.SetActive(true);
        }

        if (input != null)
        {
            input.SetActive(true);
        }

        radar?.SetActive(true);

        state.Ready = true;
        StateUpdate();

        // Vita piena: il danno di una partita non deve sopravvivere al Retry.
        feedback?.UpdateHealth(HealthRatio);
    }

    public void Despawn()
    {
        state.Ready = false;

        // Spegnere il radar ne svuota anche il registro. Serve: le minacce di
        // questa partita non devono sopravvivere al Retry di quella dopo.
        radar?.SetActive(false);
        state.Threats = null;

        if (input != null)
        {
            input.SetActive(false);
        }

        if (aimLook != null)
        {
            aimLook.SetActive(false);
        }

        // Le armi si azzerano, la rotazione no: la torretta resta puntata dove
        // l'aveva lasciata il giocatore.
        if (turret != null)
        {
            turret.ResetWeapons();
        }

        StateUpdate();
    }

    /// <summary>
    /// Il danno che la navicella incassa. Fuori partita cade nel vuoto: se non
    /// si sta giocando, non c'è niente da rovinare.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (!state.Ready || health == null)
        {
            return;
        }

        // Il danno arriva su evento, non a ogni frame: lo State va aggiornato
        // qui, altrimenti un colpo incassato fra un Tick e l'altro non si vedrebbe.
        health.ApplyDamage(damage);
        StateUpdate();

        feedback?.UpdateHealth(HealthRatio);
        feedback?.Damaged();
    }

    /// <summary>Quanta vita resta, da 0 a 1.</summary>
    private float HealthRatio => state.MaxHealth <= 0f ? 0f : Mathf.Clamp01(state.Health / state.MaxHealth);

    /// <summary>
    /// Fuori partita non gira niente: è questo che rende vero il "si bloccano
    /// solo gli input" — non c'è nessuno che muova la torretta.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!state.Ready)
        {
            return;
        }

        // Il radar gira prima e per conto suo: spazza a cadenza propria, e le
        // minacce vanno raccolte anche se la torretta non c'è.
        ScanForThreats(deltaTime);

        if (turret == null || aimLook == null)
        {
            return;
        }

        turret.Tick(deltaTime, input != null && input.FireHeld);
        aimLook.Tick();
        StateUpdate();
    }

    /// <summary>
    /// Un frame di radar. <see cref="RadarComponent.Tick"/> decide se è il
    /// momento di spazzare; <see cref="RadarComponent.Collect"/> invece va
    /// chiamata <b>a ogni frame</b>, perché è lì che le posizioni si rileggono
    /// vive. È quello che tiene i marcatori incollati anche fra uno sweep e
    /// l'altro.
    /// </summary>
    private void ScanForThreats(float deltaTime)
    {
        if (radar == null)
        {
            return;
        }

        radar.Tick(deltaTime);
        state.Threats = radar.Collect();
    }

    private void HandleAimUpdated(AimData sample)
    {
        state.AimScreenPosition = sample.ScreenPosition;

        if (turret != null)
        {
            turret.Aim(sample);
        }
    }

    private void HandleWeaponToggle()
    {
        if (turret != null)
        {
            turret.ToggleWeapon();
        }
    }

    private void HandleTargetHit(string target, float damage)
    {
        OnAsteroidHit?.Invoke(target, damage);
    }

    /// <summary>
    /// Il calore si legge dalla torretta e non dallo State: il colpo è partito
    /// adesso, e lo State lo saprà solo alla fine del Tick.
    /// </summary>
    private void HandleShot(ShotInfo shot)
    {
        feedback?.Shot(shot, turret.Snapshot.MachineGunHeat);
    }

    private void HandleWeaponSelected(TurretWeapon weapon)
    {
        feedback?.WeaponSelected(weapon);
    }

    private void HandleCannonReady()
    {
        feedback?.CannonReady();
        OnCannonReady?.Invoke();
    }

    private void HandleOverheated()
    {
        feedback?.Overheated();
    }

    private void HandleContactsAcquired(int count)
    {
        feedback?.ContactAcquired();
    }

    /// <summary>
    /// La navicella è stata distrutta. Qui non si spegne niente: si aggiorna lo
    /// State e si dice al mondo. A spegnere i componenti sarà il Despawn che
    /// tornerà indietro dal connettore — una sola strada, non due.
    /// </summary>
    private void HandleDie()
    {
        StateUpdate();
        OnDied?.Invoke();
    }

    /// <summary>Ricopia nello State ciò che torretta e salute sanno di sé.</summary>
    private void StateUpdate()
    {
        if (turret != null)
        {
            TurretSnapshot snapshot = turret.Snapshot;

            state.SelectedWeapon = snapshot.Weapon;
            state.MachineGunHeat = snapshot.MachineGunHeat;
            state.MachineGunOverheated = snapshot.MachineGunOverheated;
            state.CannonCooldown = snapshot.CannonCooldown;
            state.CannonCooldownNormalized = snapshot.CannonCooldownNormalized;
        }

        if (health != null)
        {
            state.Health = health.Health;
            state.MaxHealth = health.MaxHealth;
            state.IsAlive = health.IsAlive;
        }
    }
}
