using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

/// <summary>Pool e registro degli asteroidi attualmente in gioco.</summary>
public sealed class AsteroidPool : IDisposable
{
    private readonly AsteroidManager prefab;
    private readonly Transform parent;
    private readonly Action<AsteroidManager> onCreated;
    private readonly ObjectPool<AsteroidManager> pool;
    private readonly Dictionary<string, AsteroidManager> activeById =
        new Dictionary<string, AsteroidManager>();

    private int nextId;
    private bool disposed;

    public int ActiveCount => activeById.Count;

    public AsteroidPool(
        AsteroidManager prefab,
        Transform parent,
        Action<AsteroidManager> onCreated,
        int defaultCapacity = 10,
        int maxSize = 100)
    {
        this.prefab = prefab != null
            ? prefab
            : throw new ArgumentNullException(nameof(prefab));
        this.parent = parent;
        this.onCreated = onCreated;

        pool = new ObjectPool<AsteroidManager>(
            Create,
            OnGet,
            OnRelease,
            OnDestroy,
            true,
            defaultCapacity,
            maxSize);
    }

    public AsteroidManager Get()
    {
        AsteroidManager asteroid = pool.Get();
        activeById.Add(asteroid.Id, asteroid);
        return asteroid;
    }

    public bool TryGet(string id, out AsteroidManager asteroid)
    {
        if (disposed || string.IsNullOrEmpty(id))
        {
            asteroid = null;
            return false;
        }

        return activeById.TryGetValue(id, out asteroid);
    }

    public bool Release(AsteroidManager asteroid)
    {
        if (disposed || asteroid == null ||
            !activeById.TryGetValue(asteroid.Id, out AsteroidManager registered) ||
            registered != asteroid)
        {
            return false;
        }

        activeById.Remove(asteroid.Id);
        pool.Release(asteroid);
        return true;
    }

    public void ReleaseAll()
    {
        if (disposed || activeById.Count == 0)
        {
            return;
        }

        var snapshot = new List<AsteroidManager>(activeById.Values);
        foreach (AsteroidManager asteroid in snapshot)
        {
            Release(asteroid);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ReleaseAll();
        disposed = true;
        activeById.Clear();
        pool.Clear();
    }

    private AsteroidManager Create()
    {
        AsteroidManager asteroid = Object.Instantiate(prefab, parent);
        asteroid.gameObject.name = $"Asteroid{++nextId}";
        asteroid.gameObject.SetActive(false);
        asteroid.transform.SetParent(parent, false);
        onCreated?.Invoke(asteroid);
        return asteroid;
    }

    private void OnGet(AsteroidManager asteroid)
    {
        asteroid.gameObject.SetActive(true);
    }

    private void OnRelease(AsteroidManager asteroid)
    {
        asteroid.Despawn();
        asteroid.gameObject.SetActive(false);
    }

    private static void OnDestroy(AsteroidManager asteroid)
    {
        if (asteroid != null)
        {
            Object.Destroy(asteroid.gameObject);
        }
    }
}
