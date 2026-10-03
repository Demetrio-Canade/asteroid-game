using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Costruisce gli effetti dei colpi come asset — materiali, texture e prefab —
/// con tutte le impostazioni scritte qui invece che cliccate nell'Inspector.
///
/// Si lancia dal menu Tools/VFX e si può rilanciare quando si vuole: gli asset
/// vengono aggiornati sul posto e tengono lo stesso GUID, quindi i riferimenti
/// già collegati nei prefab restano validi. Chi ritocca a mano un asset
/// generato sappia che il prossimo lancio lo riscrive: le modifiche che devono
/// restare vanno portate qui.
/// </summary>
public static class VfxBuilder
{
    private const string Folder = "Assets/VFX";
    private const string TracerMaterialPath = Folder + "/Tracer.mat";
    private const string MachineGunTracerPath = Folder + "/MachineGunTracer.prefab";
    private const string BeamMaterialPath = Folder + "/Beam.mat";
    private const string BeamTexturePath = Folder + "/BeamProfile.asset";
    private const string CannonBeamPath = Folder + "/CannonBeam.prefab";
    private const string DamageProfilePath = Folder + "/DamageProfile.asset";
    private const string DamageVolumePath = Folder + "/DamageVolume.prefab";

    /// <summary>
    /// L'intensità del Vignette a danno massimo. La scena lo tiene a 0, quindi
    /// con 1 qui il peso del Volume è l'intensità stessa.
    /// </summary>
    private const float DamageVignetteIntensity = 1f;

    /// <summary>La macchia morbida già presente nel progetto: stirata diventa una scia.</summary>
    private const string ParticleTexturePath = "Assets/Resources/particle-unit.png";

    private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

    [MenuItem("Tools/VFX/Build Machine Gun Tracer")]
    public static void BuildMachineGunTracer()
    {
        EnsureFolder();

        Material material = BuildAdditiveMaterial(
            TracerMaterialPath,
            AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleTexturePath),
            new Color(3f, 2.2f, 1.1f, 1f));

        if (material == null)
        {
            return;
        }

        GameObject root = new GameObject("MachineGunTracer");

        try
        {
            ParticleSystem system = root.AddComponent<ParticleSystem>();
            ConfigureMachineGunTracer(system, root.GetComponent<ParticleSystemRenderer>(), material);

            PrefabUtility.SaveAsPrefabAsset(root, MachineGunTracerPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(MachineGunTracerPath));

        Debug.Log($"[VfxBuilder] tracciante della mitraglietta pronto in {MachineGunTracerPath}.");
    }

    [MenuItem("Tools/VFX/Build Cannon Beam")]
    public static void BuildCannonBeam()
    {
        EnsureFolder();

        Material material = BuildAdditiveMaterial(
            BeamMaterialPath,
            BuildBeamTexture(),
            new Color(2.4f, 3.2f, 4.5f, 1f));

        if (material == null)
        {
            return;
        }

        GameObject root = new GameObject("CannonBeam");

        try
        {
            ConfigureCannonBeam(root.AddComponent<LineRenderer>(), material);

            PrefabUtility.SaveAsPrefabAsset(root, CannonBeamPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(CannonBeamPath));

        Debug.Log($"[VfxBuilder] raggio del cannone pronto in {CannonBeamPath}.");
    }

    [MenuItem("Tools/VFX/Build Damage Volume")]
    public static void BuildDamageVolume()
    {
        EnsureFolder();

        VolumeProfile profile = BuildDamageProfile();
        GameObject root = new GameObject("DamageVolume");

        try
        {
            Volume volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            // Sopra la scena (0) e sopra il Volume dello sparo (1).
            volume.priority = 2f;
            volume.weight = 0f;
            volume.sharedProfile = profile;

            PrefabUtility.SaveAsPrefabAsset(root, DamageVolumePath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(DamageVolumePath));

        Debug.Log($"[VfxBuilder] Volume del danno pronto in {DamageVolumePath}.");
    }

    // ---------- IL TRACCIANTE DELLA MITRAGLIETTA ----------

    /// <summary>
    /// Un sistema che non emette da solo: gira sempre, e le particelle gliele
    /// dà il FeedbackComponent, una per colpo, con posizione, velocità e vita
    /// già decise. Qui si decide solo come appaiono.
    /// </summary>
    private static void ConfigureMachineGunTracer(ParticleSystem system, ParticleSystemRenderer renderer, Material material)
    {
        // Le proprietà si toccano a sistema fermo: alcune non si possono
        // cambiare mentre simula.
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.85f, 0.55f, 1f);
        main.gravityModifier = 0f;
        // In World le particelle non seguono la torretta che ruota: un colpo
        // partito resta sulla sua traiettoria.
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        // Pieno per quasi tutto il tragitto, poi si spegne sull'arrivo.
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        // Stirata sulla velocità: a 250 unità al secondo è lunga circa 2,5.
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.01f;
        renderer.lengthScale = 2f;
        renderer.cameraVelocityScale = 0f;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // ---------- IL RAGGIO DEL CANNONE ----------

    /// <summary>
    /// Due punti e basta, che il FeedbackComponent sposta a ogni colpo. Nasce
    /// spento: si accende quando si spara. I colori qui sono quelli a raggio
    /// pieno; la dissolvenza ne abbassa solo l'alpha.
    /// </summary>
    private static void ConfigureCannonBeam(LineRenderer line, Material material)
    {
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, Vector3.forward);
        line.widthMultiplier = 0.35f;
        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.startColor = Color.white;
        line.endColor = new Color(0.85f, 0.95f, 1f, 1f);
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = material;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
    }

    /// <summary>
    /// Il profilo del raggio in sezione: pieno al centro e morbido sui bordi,
    /// uguale per tutta la lunghezza. La LineRenderer stira la U lungo il
    /// raggio e la V attraverso, quindi basta una colonna che vari in V.
    /// </summary>
    private static Texture2D BuildBeamTexture()
    {
        const int height = 64;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BeamTexturePath);
        bool isNew = texture == null;

        if (isNew)
        {
            texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[4 * height];

        for (int y = 0; y < height; y++)
        {
            // Da -1 a 1 attraverso il raggio: un nucleo stretto quasi bianco
            // e un alone che cala più piano.
            float across = (y + 0.5f) / height * 2f - 1f;
            float core = Mathf.Exp(-across * across * 40f);
            float glow = Mathf.Exp(-across * across * 5f) * 0.55f;
            float alpha = Mathf.Clamp01(core + glow);

            for (int x = 0; x < 4; x++)
            {
                pixels[y * 4 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        if (isNew)
        {
            AssetDatabase.CreateAsset(texture, BeamTexturePath);
        }
        else
        {
            EditorUtility.SetDirty(texture);
        }

        return texture;
    }

    // ---------- IL DANNO ----------

    /// <summary>
    /// Il profilo del danno sovrascrive una cosa sola: l'intensità del
    /// Vignette. Colore e morbidezza restano quelli della scena, così il rosso
    /// si decide in un posto solo. Altri effetti si possono aggiungere a mano:
    /// il prossimo lancio tocca solo il Vignette.
    ///
    /// Gli override di un profilo sono sotto-asset del profilo stesso: vanno
    /// agganciati al file, o spariscono alla chiusura dell'Editor.
    /// </summary>
    private static VolumeProfile BuildDamageProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DamageProfilePath);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, DamageProfilePath);
        }

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>();
            vignette.name = nameof(Vignette);
            vignette.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }

        vignette.intensity.Override(DamageVignetteIntensity);

        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(profile);

        return profile;
    }

    // ---------- I MATERIALI ----------

    /// <summary>
    /// Additivo e con un colore HDR sopra 1: è quello che lo fa passare la
    /// soglia del bloom, così l'effetto brilla invece di essere una riga
    /// colorata. Il colore delle particelle o della linea poi lo tinge, ma la
    /// luce viene da qui.
    ///
    /// Le proprietà di fusione si scrivono a mano perché fuori dall'Inspector
    /// nessuno le ricava dalla scelta "Transparent / Additive".
    /// </summary>
    private static Material BuildAdditiveMaterial(string path, Texture2D texture, Color hdrColor)
    {
        Shader shader = Shader.Find(ParticleShader);

        if (shader == null)
        {
            Debug.LogError($"[VfxBuilder] shader '{ParticleShader}' non trovato: serve URP.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;

        if (isNew)
        {
            material = new Material(shader);
        }
        else
        {
            material.shader = shader;
        }

        if (texture == null)
        {
            Debug.LogWarning($"[VfxBuilder] nessuna texture per '{path}': l'effetto sarà un rettangolo pieno.");
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", hdrColor);

        // Trasparente, additivo, senza scrittura di profondità.
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;

        if (isNew)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder), Path.GetFileName(Folder));
        }
    }
}
