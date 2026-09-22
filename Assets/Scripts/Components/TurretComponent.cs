using System;
using UnityEngine;

/// <summary>Le due armi della torretta (GDD §7 e §8).</summary>
public enum TurretWeapon
{
    MachineGun,
    Cannon
}

/// <summary>
/// Lo stato delle armi in questo istante. La torretta lo espone, il modulo lo
/// ricopia nel proprio State: una sola autorità, e uno specchio.
/// </summary>
public readonly struct TurretSnapshot
{
    public readonly TurretWeapon Weapon;

    /// <summary>Surriscaldamento della mitraglietta, da 0 a 1.</summary>
    public readonly float MachineGunHeat;

    /// <summary>A calore pieno la mitraglietta si ferma finché non è tornata a zero.</summary>
    public readonly bool MachineGunOverheated;

    /// <summary>Secondi che mancano al prossimo colpo di cannone.</summary>
    public readonly float CannonCooldown;

    /// <summary>
    /// Quanto manca al prossimo colpo di cannone, da 1 (appena sparato) a 0
    /// (pronto). Lo normalizza la torretta perché è l'unica a sapere quanto
    /// dura una ricarica: chi disegna una barra non deve chiederselo.
    /// </summary>
    public readonly float CannonCooldownNormalized;

    public TurretSnapshot(TurretWeapon weapon, float machineGunHeat, bool machineGunOverheated, float cannonCooldown, float cannonCooldownNormalized)
    {
        Weapon = weapon;
        MachineGunHeat = machineGunHeat;
        MachineGunOverheated = machineGunOverheated;
        CannonCooldown = cannonCooldown;
        CannonCooldownNormalized = cannonCooldownNormalized;
    }
}

/// <summary>
/// La torretta: ruota verso la mira, spara, suona, e tiene il conto di calore e
/// ricariche. È lei l'autorità sulle armi — nessun altro decide se si può sparare.
///
/// Non conosce il gioco: le si dà una mira e un tasto premuto, e restituisce
/// il nome di ciò che ha colpito.
/// </summary>
public class TurretComponent : MonoBehaviour
{
    [Header("Rotazione")]
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [Tooltip("Gradi al secondo. A 0 la torretta punta di scatto.")]
    [SerializeField] private float rotationSpeed = 720f;
    [Tooltip("La canna del modello, a riposo, guarda verso -Z. Toglila se risulta specchiata.")]
    [SerializeField] private bool cannaVersoZNegativo = true;

    [Header("Tiro")]
    [Tooltip("Cosa può essere colpito. Senza filtro il colpo prenderebbe la navicella stessa.")]
    [SerializeField] private LayerMask hitMask = 1 << 3;

    [Header("Mitraglietta")]
    [SerializeField] private float machineGunShotsPerSecond = 10f;
    [Tooltip("Il calore a cui la mitraglietta si blocca. Insieme a heatPerShot decide quanti colpi entrano in una raffica.")]
    [Min(0.01f)]
    [SerializeField] private float maxHeat = 1f;
    [Tooltip("Quanto calore aggiunge un colpo, nella stessa scala di maxHeat.")]
    [SerializeField] private float heatPerShot = 0.08f;
    [Tooltip("Quanto calore si smaltisce al secondo, nella stessa scala di maxHeat: da pieno il recupero dura maxHeat / coolPerSecond secondi.")]
    [SerializeField] private float coolPerSecond = 0.35f;
    [SerializeField] private float machineGunDamage = 1f;

    [Header("Cannone")]
    [SerializeField] private float cannonCooldownDuration = 3f;
    [SerializeField] private float cannonDamage = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource shotAudio;
    [SerializeField] private AudioClip machineGunClip;
    [SerializeField] private AudioClip cannonClip;

    // ---------- OUTPUTS ----------

    /// <summary>Ha colpito qualcosa: il nome del suo root, e quanto danno vale il colpo.</summary>
    public event Action<string, float> OnTargetHit;

    public TurretSnapshot Snapshot =>
        new TurretSnapshot(weapon, MachineGunHeatProgress, overheated, cannonCooldown, CannonCooldownProgress);

    /// <summary>
    /// Il calore riportato su 0..1. Dentro la torretta il calore vive nella
    /// scala di <c>maxHeat</c>, che è una manopola di bilanciamento e non deve
    /// uscire di qui: fuori si vede sempre "quanto manca al blocco", da 0 a 1.
    /// Così alzare o abbassare maxHeat non tocca niente altrove.
    /// </summary>
    private float MachineGunHeatProgress =>
        maxHeat <= 0f ? 0f : Mathf.Clamp01(heat / maxHeat);

    /// <summary>Il cooldown del cannone riportato su 0..1. A durata nulla è sempre pronto.</summary>
    private float CannonCooldownProgress =>
        cannonCooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(cannonCooldown / cannonCooldownDuration);

    private TurretWeapon weapon;
    private float heat;
    private bool overheated;
    private float cannonCooldown;
    private float shotTimer;
    private bool firedLastFrame;

    private AimData aim;
    private bool hasAim;
    private bool fireHeld;

    // ---------- INPUTS ----------

    /// <summary>Dove sta guardando il giocatore.</summary>
    public void Aim(in AimData sample)
    {
        aim = sample;
        hasAim = true;
    }

    /// <summary>Rimette la torretta dritta, di scatto.</summary>
    public void ResetAim()
    {
        hasAim = false;

        if (yawPivot != null)
        {
            yawPivot.localRotation = Quaternion.identity;
        }

        if (pitchPivot != null)
        {
            pitchPivot.localRotation = Quaternion.identity;
        }
    }

    /// <summary>Armi come a inizio partita: mitraglietta fredda e cannone carico.</summary>
    public void ResetWeapons()
    {
        weapon = TurretWeapon.MachineGun;
        heat = 0f;
        overheated = false;
        cannonCooldown = 0f;
        shotTimer = 0f;
        firedLastFrame = false;
    }

    public void SelectWeapon(TurretWeapon value)
    {
        weapon = value;
    }

    public void ToggleWeapon()
    {
        SelectWeapon(weapon == TurretWeapon.MachineGun ? TurretWeapon.Cannon : TurretWeapon.MachineGun);
    }

    /// <summary>Un frame di torretta: recupero, rotazione, fuoco.</summary>
    public void Tick(float deltaTime, bool fireHeld)
    {
        Recover(deltaTime);
        RotateTowardsAim(deltaTime);
        HandleFire(fireHeld);

        firedLastFrame = fireHeld;
    }

    // ---------- ROTAZIONE ----------

    private void RotateTowardsAim(float deltaTime)
    {
        if (!hasAim || yawPivot == null || pitchPivot == null)
        {
            return;
        }

        // LA BASE guarda il bersaglio con la direzione schiacciata sul piano
        // orizzontale: così le resta soltanto l'imbardata.
        Vector3 orizzontale = aim.AimPoint - yawPivot.position;
        orizzontale.y = 0f;

        if (orizzontale.sqrMagnitude > 0.0001f)
        {
            ApplyRotation(yawPivot, Quaternion.LookRotation(Avanti(orizzontale), Vector3.up), deltaTime);
        }

        // LA CANNA guarda il bersaglio per intero. L'imbardata gliel'ha già data
        // la base, quindi quello che le resta addosso è esattamente l'alzo.
        // Misura dal proprio centro, non da quello della base: sta più in alto.
        Vector3 dallaCanna = aim.AimPoint - pitchPivot.position;

        if (dallaCanna.sqrMagnitude > 0.0001f)
        {
            ApplyRotation(pitchPivot, Quaternion.LookRotation(Avanti(dallaCanna), Vector3.up), deltaTime);
        }
    }

    /// <summary>
    /// LookRotation punta il +Z verso la direzione che le si dà, ma il davanti
    /// della navicella è -Z (la camera è ruotata di 180°): gliela passiamo al contrario.
    /// </summary>
    private Vector3 Avanti(Vector3 direction)
    {
        return cannaVersoZNegativo ? -direction : direction;
    }

    private void ApplyRotation(Transform pivot, Quaternion target, float deltaTime)
    {
        pivot.rotation = rotationSpeed <= 0f
            ? target
            : Quaternion.RotateTowards(pivot.rotation, target, rotationSpeed * deltaTime);
    }

    // ---------- ARMI ----------

    /// <summary>
    /// Calore e ricariche scendono sempre, anche mentre è selezionata l'altra
    /// arma: è il recupero in background del GDD §9.
    /// </summary>
    private void Recover(float deltaTime)
    {
        if (heat > 0f && (!fireHeld || overheated))
        {
            heat = Mathf.Max(0f, heat - coolPerSecond * deltaTime);

            if (overheated && heat <= 0f)
            {
                overheated = false;
            }
        }

        if (cannonCooldown > 0f)
        {
            cannonCooldown = Mathf.Max(0f, cannonCooldown - deltaTime);
        }

        if (shotTimer > 0f)
        {
            shotTimer -= deltaTime;
        }
    }

    private void HandleFire(bool fireHeld)
    {
        if (!hasAim)
        {
            return;
        }

        this.fireHeld = fireHeld;

        if (weapon == TurretWeapon.MachineGun)
        {
            // Fuoco tenuto premuto, un colpo per volta secondo il rateo.
            if (!fireHeld || overheated || shotTimer > 0f)
            {
                return;
            }

            shotTimer = 1f / Mathf.Max(0.01f, machineGunShotsPerSecond);
            heat = Mathf.Min(maxHeat, heat + heatPerShot);

            if (heat >= maxHeat)
            {
                overheated = true;
            }

            Shoot(machineGunClip, machineGunDamage);
            return;
        }

        // Il cannone spara sul fronte di salita: un click, un colpo.
        bool pressedNow = fireHeld && !firedLastFrame;

        if (!pressedNow || cannonCooldown > 0f)
        {
            return;
        }

        cannonCooldown = cannonCooldownDuration;
        Shoot(cannonClip, cannonDamage);
    }

    private void Shoot(AudioClip clip, float damage)
    {
        if (shotAudio != null && clip != null)
        {
            shotAudio.PlayOneShot(clip);
        }

        // Le varianti degli asteroidi usano trigger per poter rilevare anche
        // l'impatto sulla navicella: devono quindi partecipare al raycast.
        if (!Physics.Raycast(aim.ShotRay, out RaycastHit hit, Mathf.Infinity, hitMask, QueryTriggerInteraction.Collide))
        {
            Debug.DrawRay(aim.ShotRay.origin, aim.ShotRay.direction * 100f, Color.yellow, 0.2f);
            return;
        }

        Debug.DrawLine(aim.ShotRay.origin, hit.point, Color.red, 0.2f);

        // Il collider colpito è una mesh figlia: il nome che conta è quello del
        // root, che il Rigidbody raccoglie. È la chiave con cui il connettore
        // ritroverà il bersaglio nel registro.
        string target = hit.rigidbody != null
            ? hit.rigidbody.gameObject.name
            : hit.transform.root.name;

        OnTargetHit?.Invoke(target, damage);
    }
}
