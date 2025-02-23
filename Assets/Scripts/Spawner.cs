using System;
using UnityEngine;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using UnityEngine.UIElements;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> demonPrefabs;
    public GameObject bossPrefabs;
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public Vector2 spawnAreaSize = new Vector2(2f, 2f);
    public float spawnInterval = 1f;
    public float spawnRateAdjustment = 0.1f; // Decrease in spawn interval per round
    public float minSpawnInterval = 0.2f;   // Minimum spawn interval to prevent overly fast spawns

    public event Action RoundCompleted;
    public int Round => round;

    private int round = 1;
    private int objectsToSpawn;
    private int spawnedObjects;
    private int spawnBossAmounts = 1;
    private float timer;

    private int availableDemons = 1; // Limits demon types based on round progression

    private void Start()
    {
        ValidateSetup();
        StartNewRound();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval && spawnedObjects < objectsToSpawn)
        {
            SpawnObject();
            timer -= spawnInterval;
        }

        CheckEndOfRound();
    }

    private void CheckEndOfRound()
    {
        // Proceed to the next round only when all demons are spawned and defeated
        if (spawnedObjects >= objectsToSpawn && CountActiveDemons() == 0)
        {
            ProgressToNextRound();
        }
    }

    private int CountActiveDemons()
    {
        int count = 0;
        int demonLayer = LayerMask.NameToLayer("Demon");

        foreach (GameObject demon in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (demon.layer == demonLayer)
            {
                count++;
            }
        }

        return count;
    }

    private void StartNewRound()
    {
        spawnedObjects = 0;
        objectsToSpawn = round * 5; // Adjust demon count per round
        Debug.Log($"Starting Round {round}. Demons to Spawn: {objectsToSpawn}");
    }

    private void ProgressToNextRound()
    {
        round++;
        RoundCompleted?.Invoke();
        Debug.Log($"Round {round - 1} completed! Starting Round {round}. Adjusting spawn settings...");

        // Adjust spawn interval
        spawnInterval = Mathf.Max(spawnInterval - spawnRateAdjustment, minSpawnInterval);

        // Unlock new demons every 3 rounds
        if (round % 2 == 0 && availableDemons < demonPrefabs.Count)
        {
            availableDemons++;
            Debug.Log($"Unlocked new demon type! Available Demons: {availableDemons}");
        }

        // Spawn boss
        if (round % 5 == 0){
            for (int i = 0; i < spawnBossAmounts; i++)
            {
            Instantiate(bossPrefabs, new Vector2 (Random.Range(-2f, 2f), 16f), Quaternion.identity);
            }
            spawnBossAmounts++;
        }

        StartNewRound();
    }

    private void SpawnObject()
    {
        bool spawnFromLeft = Random.value < 0.5f;
        Transform spawnPoint = spawnFromLeft ? leftSpawnPoint : rightSpawnPoint;

        GameObject demonToSpawn = demonPrefabs[Random.Range(0, availableDemons)];
        bool isFlyingEnemy = demonToSpawn.CompareTag("FlyingDemon");
        bool isTopEnemy = demonToSpawn.CompareTag("TopDemon");

        Vector3 spawnPosition = spawnPoint.position;

        if (isFlyingEnemy)
        {
            spawnPosition.y += Random.Range(8f, 10f);
        }
        else if (isTopEnemy)
        {
            spawnPosition.x = Random.Range(-2f, 2f);
            spawnPosition.y = 16f;
        }
        else
        {
            spawnPosition.y += Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
        }

        GameObject spawnedDemon = Instantiate(demonToSpawn, spawnPosition, Quaternion.identity);

        EnemyMovement enemyMovement = spawnedDemon.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.IsSpawnedFromLeft = spawnFromLeft;
        }

        spawnedObjects++;
    }

    private void ValidateSetup()
    {
        if (leftSpawnPoint == null || rightSpawnPoint == null)
            Debug.LogError("Spawn points are not assigned!");

        if (demonPrefabs == null || demonPrefabs.Count == 0)
            Debug.LogError("No demon prefabs assigned!");
    }

    private void OnDrawGizmos()
    {
        if (leftSpawnPoint != null && rightSpawnPoint != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(leftSpawnPoint.position, new Vector3(0.1f, spawnAreaSize.y, 0));
            Gizmos.DrawWireCube(rightSpawnPoint.position, new Vector3(0.1f, spawnAreaSize.y, 0));

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(leftSpawnPoint.position + Vector3.up * 9f, new Vector3(0.1f, 2f, 0));
            Gizmos.DrawWireCube(rightSpawnPoint.position + Vector3.up * 9f, new Vector3(0.1f, 2f, 0));
        }
    }
}
