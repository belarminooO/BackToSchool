using UnityEngine;
using PurrNet;

public class PlanetSpawner : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] planetPrefabs;

    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float offset = 1f;

    private float _timer;

    private void Update()
    {
        if (!isServer) 
            return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnRandomPlanet();
        }
    }

    private void SpawnRandomPlanet()
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0 || spawnPoint == null)
            return;

        int randomIndex = Random.Range(0, planetPrefabs.Length);
        GameObject selectedPrefab = planetPrefabs[randomIndex];

        Vector3 spawnPos = spawnPoint.position;
        spawnPos.z += Random.Range(-offset, offset);
        Instantiate(selectedPrefab, spawnPos, spawnPoint.rotation);
    }

}
