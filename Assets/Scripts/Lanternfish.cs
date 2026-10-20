using System;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lanternfish : MonoBehaviour
{

    public enum State
    {
        Wander,
        Feed,
        Flee
    }

    [SerializeField] private float energy;
    private Vector2 velocity;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private Transform targetSnow;         
    private Transform nearestPredator; 
    public State state = State.Wander;
    
    //ranges
    public float snowDetectRange = 5f;
    public float predatorDetectRange = 3f;
    public float eatRange = 0.4f;
    
    //movement stuff
    public float wanderSpeed = 1.5f;
    public float feedSpeed   = 2.5f;
    public float fleeSpeed   = 4.0f;
    public float steerSmoothing = 3f;      // higher = snappier turns
    public float separationRadius = 0.8f;
    public float separationWeight = 1.2f;
    public float wanderRetargetMin = 2f;
    public float wanderRetargetMax = 3f;
    
    //energy
    public float maxEnergy = 10f;
    public float energyDrainPerSec = 0.4f;
    public float energyPerSnow = 3f;
    public float fleeExtraDrainPerSec = 0.6f; 
    
    //clamp stuff
    [SerializeField] Vector2 minBounds;
    [SerializeField] Vector2 maxBounds;
    public float bonusPadding = 0.5f;

    void Start()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        
        minBounds = (Vector2)cam.transform.position - new Vector2(halfW, halfH) + Vector2.one * bonusPadding;
        maxBounds = (Vector2)cam.transform.position + new Vector2(halfW, halfH) - Vector2.one * bonusPadding;
        
        energy = maxEnergy * Random.Range(0.6f, 1f);
        PickNewWanderTarget();
    }
    
    // Update is called once per frame
    void Update()
    {
        SenseEnvirons();
        UpdateState();
        Vector2 desired = GetDesiredDirection();
        ApplySeparation(ref desired);
        ApplyMovement(desired);
        UpdateEnergy();
    }

    private void SenseEnvirons()
    {
        nearestPredator = FindNearestWithTag("Anglerfish", predatorDetectRange);
        targetSnow = FindNearestWithTag("Snow", snowDetectRange);
    }

    private Transform FindNearestWithTag(string tag, float range)
    {
        float smallestDistance = float.MaxValue;
        Transform nearest = null;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(tag))
            {
                float sqrDistance = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;

                if (sqrDistance < smallestDistance)
                {
                    smallestDistance = sqrDistance;
                    nearest = hit.transform;
                }
            }
        }
        return nearest;
    }
    
    private void UpdateState()
    {
        if (nearestPredator != null) state = State.Flee;
        else if (targetSnow != null) state = State.Feed;
        else state = State.Wander;
    }
    
    // TODO: Implement movement

    private Vector2 GetDesiredDirection()
    {
        switch (state)
        {
            case State.Flee:
                return FleeDirection();
            case State.Feed:
                return FeedDirection();
            default:
                return WanderDirection();
        }
    }

    private Vector2 FleeDirection()
    {
        if (nearestPredator == null) return Vector2.zero;
        return ((Vector2)transform.position - (Vector2)nearestPredator.position).normalized;
    }
    
    private Vector2 FeedDirection()
    {
        if(targetSnow == null) return Vector2.zero;
        Vector2 toSnow = (Vector2)targetSnow.position - (Vector2)transform.position;

        if (toSnow.magnitude < eatRange)
        {
            EatSnow(targetSnow);
            targetSnow = null;
            return Vector2.zero;
        }
        
        return toSnow.normalized;
    }
    
    private Vector2 WanderDirection()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0.0f) PickNewWanderTarget();
        
        return ((Vector2)wanderTarget - (Vector2)transform.position).normalized;
        
        return Vector2.zero;
    }

    private void PickNewWanderTarget()
    {
        Vector2 target = (Vector2)transform.position + Random.insideUnitCircle * 4f;
        
        target.x = Mathf.Clamp(target.x, minBounds.x, maxBounds.x);
        target.y = Mathf.Clamp(target.y, minBounds.y, maxBounds.y);
        
        wanderTarget = target;
        wanderTimer = Random.Range(wanderRetargetMin, wanderRetargetMax);
    }

    private void ApplySeparation(ref Vector2 desired)
    {
        Collider2D[] near = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        Vector2 push = Vector2.zero;

        foreach (Collider2D hit in near)
        {
            if (hit.CompareTag("Lanternfish"))
            {
                push += (Vector2)(transform.position - hit.transform.position);
            }
        }

        desired = (desired + push.normalized * separationWeight).normalized;
    }

    private void ApplyMovement(Vector2 desired)
    {
        float speed = 0.0f;
        
        switch (state)
        {
            case State.Flee:
                speed = fleeSpeed;
                break;
            case State.Feed:
                speed = feedSpeed;
                break;
            case State.Wander:
                speed = wanderSpeed;
                break;
        }

        velocity = Vector2.Lerp(velocity, desired * speed, steerSmoothing * Time.deltaTime);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;
    }

    private void UpdateEnergy()
    {
        float drain = energyDrainPerSec + (state == State.Flee ? fleeExtraDrainPerSec : 0f);
        energy -= drain *  Time.deltaTime;
        if (energy <= 0f) Destroy(gameObject);
    }

    private void EatSnow(Transform snow)
    {
        Destroy(snow.gameObject);
        energy = Mathf.Min(maxEnergy, energy + energyPerSnow);
    }
    
    public void GetEaten()
    {
        Destroy(gameObject);
    }

    /*
    switch (state)
    {
        case State.Wander:
        break;
        case State.Feed:
        break;
        case State.Flee:
        break;
    }
    */
}
