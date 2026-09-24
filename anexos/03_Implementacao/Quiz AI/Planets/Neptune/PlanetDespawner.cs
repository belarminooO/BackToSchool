using UnityEngine;
using PurrNet;

public class PlanetDespawner : NetworkBehaviour
{
    [Header("Despawn Settings")]
    [SerializeField] private float despawnYLimit = -50f;

    private void Update()
    {
        if (!isServer) 
            return;

        if (transform.position.y < despawnYLimit)
        {
            Destroy(gameObject);
        }
    }
}
