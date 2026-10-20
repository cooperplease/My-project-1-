using UnityEngine;

public class Anglerfish : MonoBehaviour
{

    public enum State
    {
        Lurk,
        Lunge,
        Digest
    }
    
    //ranges
    public float huntRange = 4f;       
    public float catchRange = 0.6f;

    //lurk
    public float bobAmount = 0.15f;
    public float bobSpeed = 1.5f;
    
    //lunge
    public float lungeSpeed = 12f;
    public float lungeEndSpeed = 4.5f;    
    public float lungeDuration = 0.6f;

    //digest
    public float digestDurationAfterCatch = 5f;
    public float digestDurationAfterMiss = 2f;
    public float digestDriftSpeed = 0.1f;

    //runtime
    public State state = State.Lurk;
    Vector2 lungeDirection;
    float stateTimer;
    Vector3 lurkAnchor;                
    bool leaving;
    float leaveTimer;
    Vector2 minBounds, maxBounds;
    float lungeElapsed;
    Transform lungeTarget;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        
        minBounds = (Vector2)cam.transform.position - new Vector2(halfW, halfH);
        maxBounds = (Vector2)cam.transform.position + new Vector2(halfW, halfH);

        lurkAnchor = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.Lurk:
                UpdateLurk();
                break;
            case State.Lunge:
                UpdateLunge();
                break;
            case State.Digest:
                UpdateDigest();
                break;
        }
        
        ClampToBounds();
    }

    void UpdateLurk()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = lurkAnchor + Vector3.up * bob;

        Transform prey = FindNearestWithTag("Lanternfish", huntRange);
        if (prey != null) StartLunge(prey);
    }
    
    void StartLunge(Transform prey)
    {
        lungeTarget = prey;
        lungeDirection = ((Vector2)prey.position - (Vector2)transform.position).normalized;
        lungeElapsed = 0f;
        state = State.Lunge; 
    }

    void UpdateLunge()
    {

        lungeElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(lungeElapsed / lungeDuration);
        
        float speed = Mathf.Lerp(lungeSpeed, lungeEndSpeed, t);
        transform.position += (Vector3)(lungeDirection * speed * Time.deltaTime);

        Transform caught = FindNearestWithTag("Lanternfish", catchRange);
        if (caught != null)
        {
            Eat(caught);
            StartDigest(digestDurationAfterCatch);
            return;
        }

        if (t >= 1f) StartDigest(digestDurationAfterMiss);
    }
    
    void Eat(Transform prey)
    {
        Lanternfish fish = prey.GetComponent<Lanternfish>();
        if (fish != null) fish.GetEaten();
    }
    
    void StartDigest(float duration)
    {
        stateTimer = duration;
        state = State.Digest;
    }

    void UpdateDigest()
    {
        transform.position += Vector3.down * digestDriftSpeed * Time.deltaTime;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            lurkAnchor = transform.position;  
            state = State.Lurk;
        }
    }
    
    public void Leave()
    {
        Destroy(gameObject);
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
    
    void ClampToBounds()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;

        lurkAnchor.x = Mathf.Clamp(lurkAnchor.x, minBounds.x, maxBounds.x);
        lurkAnchor.y = Mathf.Clamp(lurkAnchor.y, minBounds.y, maxBounds.y);
    }
    
    void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    
}
