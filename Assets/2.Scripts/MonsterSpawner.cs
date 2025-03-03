using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Threading;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public Transform spawnPoint;
    public Transform tower;
    public float spawnInterval = 1.5f;
    private List<GameObject> monsters = new List<GameObject>();
    private int maxStack = 6;

    void Start()
    {
        StartCoroutine(SpawnMonsters());
    }

    IEnumerator SpawnMonsters()
    {
        while(true)
        {
            GameObject monster = Instantiate(monsterPrefab, spawnPoint.position, Quaternion.identity);
            monsters.Add(monster);
            monster.AddComponent<Monster>();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}

