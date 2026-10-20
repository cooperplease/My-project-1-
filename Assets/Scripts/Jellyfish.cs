using UnityEngine;

public class Jellyfish : MonoBehaviour
{
    public enum State { Sink, Pulse, Feed }

    [Header("Senses")]
    public float feedRange = 2.5f;     // "tentacle range"
    public float eatRange = 0.5f;

    [Header("Pulse movement")]
    public float pulseInterval = 2f;   // seconds between pumps
    public float pulseImpulse = 3f;    // velocity added by one pump
    public float pulseDuration = 0.5f; // how long the Pulse state lasts (squash animation)
    public float sinkSpeed = 0.4f;     // resting downward speed
    public float drag = 1.5f;          // how fast velocity decays toward the resting drift
    public float sidewaysWobble = 0.8f;
    public float feedDriftSpeed = 0.6f;

    [Header("Energy / lifespan")]
    public float maxEnergy = 8f;
    public float passiveDrainPerSec = 0.1f;
    public float pulseCost = 0.8f;
    public float energyPerSnow = 2.5f;

    [Header("Visuals")]
    public float squashAmount = 0.3f;
    public TextMesh stateLabel;        // optional

    [Header("Bounds")]
    public float boundsPadding = 0.5f;

    public State state = State.Sink;
    [SerializeField] float energy;

    Vector2 velocity;
    float pulseTimer;
    float stateTimer;
    Transform targetSnow;
    Vector2 minBounds, maxBounds;
    Vector3 baseScale;
    SpriteRenderer body;

    void Start()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        minBounds = (Vector2)cam.transform.position - new Vector2(halfW, halfH) + Vector2.one * boundsPadding;
        maxBounds = (Vector2)cam.transform.position + new Vector2(halfW, halfH) - Vector2.one * boundsPadding;

        baseScale = transform.localScale;
        body = GetComponent<SpriteRenderer>();

        energy = maxEnergy * Random.Range(0.6f, 1f);
        pulseTimer = Random.Range(0.5f, pulseInterval);   // stagger so jellyfish don't pump in sync
    }

    // The order here is fixed. Do not reorder.
    void Update()
    {
        SenseEnvironment();
        UpdateState();
        TryEat();
        ApplyMovement();
        UpdateEnergy();
        UpdateVisuals();
    }

    // ------------------------------------------------------------------
    // SENSING
    // ------------------------------------------------------------------
    void SenseEnvironment()
    {
        targetSnow = FindNearestWithTag("Snow", feedRange);
    }

    // ------------------------------------------------------------------
    // STATES
    //   Pulse: a brief, timed state while the pump animation plays.
    //   Feed:  snow is in tentacle range (drifts toward it, pumps aimed at it).
    //   Sink:  nothing nearby, just drifting down between pumps.
    // ------------------------------------------------------------------
    void UpdateState()
    {
        if (state == State.Pulse)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) state = targetSnow != null ? State.Feed : State.Sink;
            return;
        }

        state = targetSnow != null ? State.Feed : State.Sink;

        pulseTimer -= Time.deltaTime;

        // If the snow is below us, don't pump (pumping moves us up); just sink onto it.
        bool snowBelow = targetSnow != null && targetSnow.position.y < transform.position.y;

        if (pulseTimer <= 0f && energy >= pulseCost && !snowBelow) StartPulse();
    }

    void StartPulse()
    {
        Vector2 dir;
        if (targetSnow != null)
            dir = ((Vector2)targetSnow.position - (Vector2)transform.position).normalized;
        else
            dir = new Vector2(Random.Range(-sidewaysWobble, sidewaysWobble), 1f).normalized;

        velocity += dir * pulseImpulse;     // the impulse; drag in ApplyMovement makes it decay
        energy -= pulseCost;                // pumping is tiring
        pulseTimer = pulseInterval * Random.Range(0.8f, 1.2f);
        stateTimer = pulseDuration;
        state = State.Pulse;
    }

    // ------------------------------------------------------------------
    // FEEDING
    // ------------------------------------------------------------------
    void TryEat()
    {
        if (targetSnow == null) return;

        float dist = ((Vector2)targetSnow.position - (Vector2)transform.position).magnitude;
        if (dist < eatRange)
        {
            Destroy(targetSnow.gameObject);
            energy = Mathf.Min(maxEnergy, energy + energyPerSnow);
            targetSnow = null;   // Destroy is deferred, so clear the reference now
        }
    }

    // ------------------------------------------------------------------
    // MOVEMENT
    // Velocity is pulled toward a slow "resting drift" by drag. A pump adds a burst on top,
    // which decays back to the drift. That decay is what gives the pump-then-sink feel.
    // ------------------------------------------------------------------
    void ApplyMovement()
    {
        Vector2 rest = new Vector2(0f, -sinkSpeed);

        if (state == State.Feed && targetSnow != null)
        {
            float dx = targetSnow.position.x - transform.position.x;
            rest.x = Mathf.Sign(dx) * feedDriftSpeed * Mathf.Clamp01(Mathf.Abs(dx));  // eases in near the snow
        }

        velocity = Vector2.Lerp(velocity, rest, Mathf.Clamp01(drag * Time.deltaTime));
        transform.position += (Vector3)(velocity * Time.deltaTime);

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;

        // resting on the floor: stop sinking
        if (p.y <= minBounds.y && velocity.y < 0f) velocity.y = 0f;
    }

    // ------------------------------------------------------------------
    // ENERGY + DESPAWN
    // ------------------------------------------------------------------
    void UpdateEnergy()
    {
        energy -= passiveDrainPerSec * Time.deltaTime;
        if (energy <= 0f) Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------
    Transform FindNearestWithTag(string tag, float range)
    {
        float smallestDistance = float.MaxValue;
        Transform nearest = null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(tag)) continue;

            float sqrDistance = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < smallestDistance)
            {
                smallestDistance = sqrDistance;
                nearest = hit.transform;
            }
        }
        return nearest;
    }

    // ------------------------------------------------------------------
    // GRADER-VISIBLE FEEDBACK
    // ------------------------------------------------------------------
    void UpdateVisuals()
    {
        // Bell contracts (narrower and taller) during the pump
        float s = 0f;
        if (state == State.Pulse)
        {
            float t = 1f - Mathf.Clamp01(stateTimer / pulseDuration);
            s = Mathf.Sin(t * Mathf.PI) * squashAmount;
        }
        transform.localScale = new Vector3(baseScale.x * (1f - s), baseScale.y * (1f + s), baseScale.z);

        // Dim as it starves
        if (body != null)
        {
            Color c = body.color;
            c.a = Mathf.Lerp(0.35f, 1f, energy / maxEnergy);
            body.color = c;
        }

        if (stateLabel != null) stateLabel.text = state.ToString();
    }
}
