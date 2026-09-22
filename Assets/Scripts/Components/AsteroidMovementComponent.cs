using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Muove un Rigidbody cinematico lungo una direzione fissata all'avvio e gli
/// applica una rotazione casuale. Non decide né partenza né destinazione.
/// </summary>
public class AsteroidMovementComponent : MonoBehaviour
{
    private const string Tag = "[AsteroidMovement]";

    [SerializeField] private Rigidbody body;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(45f, 120f);

    private Vector3 direction;
    private Vector3 rotationAxis;
    private float speed;
    private float rotationSpeed;
    private float lifetimeRemaining;
    private bool isMoving;

    /// <summary>
    /// Dove sta andando e a che andatura, in un vettore solo: unità al secondo,
    /// direzione compresa. Zero da fermo.
    ///
    /// È un vettore e non una coppia direzione + velocità di proposito. Sono
    /// fissati insieme nel <see cref="Begin"/> e non cambiano più: tenerli
    /// separati vorrebbe dire poterne aggiornare uno solo, e una rotta mezza
    /// aggiornata è peggio di nessuna rotta.
    /// </summary>
    public Vector3 Velocity => isMoving ? direction * speed : Vector3.zero;

    /// <summary>Il tempo massimo di volo è terminato.</summary>
    public event Action OnLifetimeExpired;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponentInParent<Rigidbody>();
        }

        if (body == null)
        {
            Debug.LogError($"{Tag} nessun Rigidbody collegato.");
        }
    }

    /// <summary>Inizia un nuovo volo con una traiettoria rettilinea.</summary>
    public void Begin(Vector3 startPosition, Vector3 targetPosition, float movementSpeed, float lifetime)
    {
        if (body == null)
        {
            return;
        }

        Vector3 path = targetPosition - startPosition;
        if (path.sqrMagnitude <= Mathf.Epsilon)
        {
            Debug.LogWarning($"{Tag} partenza e destinazione coincidono: il volo non viene avviato.");
            return;
        }

        float minRotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        float maxRotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);

        direction = path.normalized;
        speed = movementSpeed;
        lifetimeRemaining = lifetime;
        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        isMoving = true;

        body.position = startPosition;
        body.rotation = Random.rotationUniform;
    }

    /// <summary>Avanza di un passo fisico.</summary>
    public void Tick(float fixedDeltaTime)
    {
        if (!isMoving || body == null)
        {
            return;
        }

        body.MovePosition(body.position + direction * speed * fixedDeltaTime);

        Quaternion rotationStep = Quaternion.AngleAxis(rotationSpeed * fixedDeltaTime, rotationAxis);
        body.MoveRotation(rotationStep * body.rotation);

        lifetimeRemaining -= fixedDeltaTime;
        if (lifetimeRemaining > 0f)
        {
            return;
        }

        isMoving = false;
        OnLifetimeExpired?.Invoke();
    }

    public void Stop()
    {
        isMoving = false;
        lifetimeRemaining = 0f;
    }
}
