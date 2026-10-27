using UnityEngine;

public class GlassShard : MonoBehaviour
{
    private Vector2 velocity;
    private float angularVelocity;
    private float gravity;
    private float lifetime;
    private float age;

    private SpriteRenderer sr;
    private float startAlpha;

    public void Init(Vector2 vel, float angVel, float grav, float life)
    {
        velocity = vel;
        angularVelocity = angVel;
        gravity = grav;
        lifetime = Mathf.Max(0.1f, life);

        sr = GetComponent<SpriteRenderer>();
        startAlpha = sr != null ? sr.color.a : 1f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        age += dt;

        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, angularVelocity * dt);

        // 40% สุดท้ายของอายุ ให้ค่อยๆ จาง
        float fadeStart = lifetime * 0.6f;
        if (sr != null && age > fadeStart)
        {
            float t = (age - fadeStart) / (lifetime - fadeStart);
            Color c = sr.color;
            c.a = startAlpha * (1f - Mathf.Clamp01(t));
            sr.color = c;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }
}
