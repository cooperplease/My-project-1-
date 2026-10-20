using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //objs
    public GameObject lanternfishPrefab;
    public GameObject jellyfishPrefab;
    public GameObject anglerfishPrefab;

    //timing
    public float evaluateInterval = 1f;

    //lanternfish
    public int snowNeededToSpawnFish = 3;
    public int maxLanternfish = 30;
    public float fishSpawnCooldown = 1.5f;

    //jellyfish
    public int floorSnowNeededForJellyfish = 8;
    public int maxJellyfish = 6;
    public float jellySpawnCooldown = 4f;
    public float floorY = -5f;

    //angler
    public int fishNeededToSpawnAngler = 15;
    public int fishBelowAnglerLeaves = 5;
    public int maxAnglerfish = 2;
    public float anglerSpawnCooldown = 8f;

    //counts
    public int snowCount;
    public int floorSnowCount;
    public int fishCount;
    public int jellyCount;
    public int anglerCount;

    Vector2 minBounds, maxBounds;
    float nextFishTime, nextJellyTime, nextAnglerTime;
    public float evaluateTimer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        evaluateTimer = evaluateInterval;
    }

    // Update is called once per frame
    void Update()
    {
        evaluateTimer -= Time.deltaTime;
        if (evaluateTimer <= 0f)
        {
            Evaluate();
            evaluateTimer = evaluateInterval;
        }
    }
    
    private void Evaluate()
    {
        CountPopulations();
        EvaluateLanternfish();
        EvaluateJellyfish();
        EvaluateAnglerfish();
    }

    private void CountPopulations()
    {
        GameObject[] snow = GameObject.FindGameObjectsWithTag("Snow");
        snowCount = snow.Length;

        floorSnowCount = 0;
        foreach (GameObject s in snow)
        {
            if(s.transform.position.y <= floorY + 0.2f) floorSnowCount++;
        }

        fishCount = GameObject.FindGameObjectsWithTag("Lanternfish").Length;
        jellyCount = GameObject.FindGameObjectsWithTag("Jellyfish").Length;
        anglerCount = GameObject.FindGameObjectsWithTag("Anglerfish").Length;
    }

    private void EvaluateLanternfish()
    {
        if (snowCount >= snowNeededToSpawnFish && fishCount < maxLanternfish && Time.time >= nextFishTime)
        {
            Spawn(lanternfishPrefab, RandomSidePoint());
            nextFishTime = Time.time + fishSpawnCooldown;
        }
    }

    private void EvaluateJellyfish()
    {
        if (floorSnowCount >= floorSnowNeededForJellyfish && jellyCount < maxJellyfish && Time.time >= nextJellyTime)
        {
            Spawn(jellyfishPrefab, RandomTopPoint());
            nextJellyTime = Time.time + jellySpawnCooldown;
        }
    }

    private void EvaluateAnglerfish()
    {
        if (fishCount >= fishNeededToSpawnAngler && anglerCount < maxAnglerfish && Time.time >= nextAnglerTime)
        {
            Spawn(anglerfishPrefab, RandomBottomPoint());
            nextAnglerTime = Time.time + anglerSpawnCooldown;
        }
        
        if (fishCount <= fishBelowAnglerLeaves && anglerCount > 0)
        {
            GameObject[] anglers = GameObject.FindGameObjectsWithTag("Anglerfish");
            anglers[0].GetComponent<Anglerfish>().Leave();
        }
    }

    private void Spawn(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
    }

    private Vector3 RandomSidePoint()
    {
        float x = Random.value < 0.5f ? minBounds.x + 0.5f : maxBounds.x - 0.5f;
        float y = Random.Range(minBounds.y + 1f, maxBounds.y - 1f);
        return new Vector2(x, y);
    }

    private Vector3 RandomTopPoint()
    {
        return new Vector2(Random.Range(minBounds.x + 1f, maxBounds.x - 1f), maxBounds.y - 0.5f);
    }

    private Vector3 RandomBottomPoint()
    {
        return new Vector2(Random.Range(minBounds.x + 1f, maxBounds.x - 1f), minBounds.y + 1f);
    }
}
