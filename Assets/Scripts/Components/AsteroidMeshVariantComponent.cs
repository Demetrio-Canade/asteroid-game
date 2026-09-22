using UnityEngine;

/// <summary>
/// Attiva una sola variante visiva scelta casualmente a ogni spawn.
///
/// Sa anche <b>quanto è grosso</b> ciò che ha acceso. Non è un dettaglio di
/// grafica: le varianti vanno da un sasso di 0.2 a uno di quasi 1, e chi deve
/// prevedere dove passa l'asteroide non può indovinarlo da fuori. Meglio che sia
/// il modulo a dichiarare il proprio corpo, che vederlo stimato altrove.
/// </summary>
public class AsteroidMeshVariantComponent : MonoBehaviour
{
    private const string Tag = "[AsteroidMeshVariant]";

    [SerializeField] private GameObject[] variants;

    private float activeRadius;

    /// <summary>
    /// Il raggio della sfera che contiene la variante accesa, in scala di mondo.
    /// Zero quando non è acceso niente.
    ///
    /// È la sfera <b>circoscritta</b> e non una misura più stretta, perché
    /// l'asteroide ruota per tutto il volo: quale faccia si presenti al momento
    /// del passaggio non lo sa nessuno, e l'unico volume che tiene comunque è
    /// quello che contiene il sasso in ogni sua rotazione.
    /// </summary>
    public float ActiveRadius => activeRadius;

    public void SelectRandom()
    {
        if (variants == null || variants.Length == 0)
        {
            Debug.LogWarning($"{Tag} nessuna variante collegata.");
            return;
        }

        int selectedIndex = UnityEngine.Random.Range(0, variants.Length);

        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] != null)
            {
                variants[i].SetActive(i == selectedIndex);
            }
        }

        activeRadius = RadiusOf(variants[selectedIndex]);
    }

    public void HideAll()
    {
        activeRadius = 0f;

        if (variants == null)
        {
            return;
        }

        foreach (GameObject variant in variants)
        {
            if (variant != null)
            {
                variant.SetActive(false);
            }
        }
    }

    /// <summary>
    /// La sfera che contiene una variante. Si misura dalla forma dichiarata del
    /// collider e non da <c>bounds</c>, che è il riquadro allineato agli assi e
    /// quindi cambia con la rotazione: qui serve un numero che valga per tutto
    /// il volo, non per l'istante in cui lo si chiede.
    /// </summary>
    private static float RadiusOf(GameObject variant)
    {
        if (variant == null)
        {
            return 0f;
        }

        Collider collider = variant.GetComponentInChildren<Collider>(true);

        switch (collider)
        {
            case BoxCollider box:
                return 0.5f * Vector3.Scale(box.size, box.transform.lossyScale).magnitude;

            case SphereCollider sphere:
                Vector3 scale = sphere.transform.lossyScale;
                return sphere.radius * Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

            case null:
                return 0f;

            // Una forma che non conosciamo: il riquadro allineato agli assi è
            // una sovrastima, e per un avviso sovrastimare è il verso giusto.
            default:
                return collider.bounds.extents.magnitude;
        }
    }
}
