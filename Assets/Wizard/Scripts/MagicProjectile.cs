using UnityEngine;

[DisallowMultipleComponent]
public class MagicProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float aimFollowSharpness = 35f;
    [SerializeField] private bool followAimTarget = true;
    [SerializeField] private bool destroyOnTrigger = true;
    [SerializeField] private bool destroyOnCollision = true;

    private Rigidbody rb;
    private Transform aimTarget;
    private Vector3 currentDirection = Vector3.forward;
    private float lifeTimer;
    private bool launched;
    private Transform ownerRoot;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }

    private void OnEnable()
    {
        lifeTimer = lifeTime;
    }

    private void Update()
    {
        if (!launched)
        {
            return;
        }

        UpdateLifeTimer(Time.deltaTime);
        UpdateAimDirection(Time.deltaTime);

        if (rb == null)
        {
            transform.position += currentDirection * speed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (!launched || rb == null)
        {
            return;
        }

        UpdateAimDirection(Time.fixedDeltaTime);
        rb.velocity = currentDirection * speed;
    }

    public void Configure(float projectileSpeed, float projectileLifeTime)
    {
        Configure(projectileSpeed, projectileLifeTime, damage);
    }

    public void Configure(float projectileSpeed, float projectileLifeTime, float projectileDamage)
    {
        speed = projectileSpeed;
        lifeTime = projectileLifeTime;
        damage = projectileDamage;
        lifeTimer = lifeTime;
    }

    public void SetOwner(GameObject owner)
    {
        ownerRoot = owner != null ? owner.transform.root : null;
    }

    public void Launch(Vector3 direction, Transform targetToFollow)
    {
        aimTarget = targetToFollow;
        launched = true;
        SetDirection(direction);
    }

    public void SetDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        launched = true;
        currentDirection = direction.normalized;
        transform.forward = currentDirection;

        if (rb != null)
        {
            rb.velocity = currentDirection * speed;
        }
    }

    private void UpdateAimDirection(float deltaTime)
    {
        if (!followAimTarget || aimTarget == null)
        {
            return;
        }

        Vector3 targetDirection = aimTarget.position - transform.position;
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        targetDirection.Normalize();
        float blend = 1f - Mathf.Exp(-aimFollowSharpness * deltaTime);
        currentDirection = Vector3.Slerp(currentDirection, targetDirection, blend).normalized;
        transform.forward = currentDirection;
    }

    private void UpdateLifeTimer(float deltaTime)
    {
        if (lifeTime <= 0f)
        {
            return;
        }

        lifeTimer -= deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other);

        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null)
        {
            TryApplyDamage(collision.collider);
        }

        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
    }

    private void TryApplyDamage(Collider other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.IsAlive)
        {
            return;
        }

        hasHit = true;
        damageable.TakeDamage(damage, ownerRoot != null ? ownerRoot.gameObject : gameObject);
    }
}
