using UnityEngine;

[DisallowMultipleComponent]
public class GhostEnemy : MonoBehaviour, IDamageable
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform aimPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float stoppingDistance = 1.45f;
    [SerializeField] private float hoverHeight = 0.05f;

    [Header("Combat")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.65f;
    [SerializeField] private float attackCooldown = 1.35f;
    [SerializeField] private float attackHitDelay = 0.45f;
    [SerializeField] private float damageAnimationLock = 0.35f;
    [SerializeField] private float deathDestroyDelay = 4f;

    [Header("Animation States")]
    [SerializeField] private string idleState = "Ghost_Idle";
    [SerializeField] private string moveState = "Ghost_Fly";
    [SerializeField] private string attackState = "Ghost_Attack";
    [SerializeField] private string damageState = "Ghost_Damage";
    [SerializeField] private string deathState = "Ghost_Death";
    [SerializeField] private float moveBlendTime = 0.12f;
    [SerializeField] private float actionBlendTime = 0.08f;

    private Animator animator;
    private Collider hitCollider;
    private float currentHealth;
    private float attackCooldownTimer;
    private float attackHitTimer;
    private float actionLockTimer;
    private bool attackDamageQueued;
    private bool isDead;
    private int currentStateHash;

    public bool IsAlive => !isDead;
    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitCollider = GetComponent<Collider>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        ResolvePlayerReferences();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        ResolvePlayerReferences();
        UpdateTimers();

        if (player == null)
        {
            CrossFadeState(idleState, moveBlendTime);
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            RotateTowards(toPlayer);
        }

        if (actionLockTimer > 0f)
        {
            return;
        }

        if (distance <= attackRange)
        {
            TryAttack();
            return;
        }

        MoveTowardsPlayer(toPlayer, distance);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        actionLockTimer = damageAnimationLock;
        CrossFadeState(damageState, actionBlendTime, true);
    }

    private void ResolvePlayerReferences()
    {
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.transform;
            }
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void UpdateTimers()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (actionLockTimer > 0f)
        {
            actionLockTimer -= Time.deltaTime;
        }

        if (!attackDamageQueued)
        {
            return;
        }

        attackHitTimer -= Time.deltaTime;
        if (attackHitTimer <= 0f)
        {
            attackDamageQueued = false;
            DealAttackDamage();
        }
    }

    private void MoveTowardsPlayer(Vector3 toPlayer, float distance)
    {
        CrossFadeState(moveState, moveBlendTime);

        if (distance <= stoppingDistance || toPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 direction = toPlayer / distance;
        Vector3 nextPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        nextPosition.y = Mathf.Max(nextPosition.y, hoverHeight);
        transform.position = nextPosition;
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        CrossFadeState(idleState, moveBlendTime);

        if (attackCooldownTimer > 0f)
        {
            return;
        }

        attackCooldownTimer = attackCooldown;
        attackHitTimer = attackHitDelay;
        attackDamageQueued = true;
        actionLockTimer = attackHitDelay;
        CrossFadeState(attackState, actionBlendTime, true);
    }

    private void DealAttackDamage()
    {
        if (isDead || player == null || playerHealth == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange + 0.35f)
        {
            return;
        }

        playerHealth.TakeDamage(attackDamage);
    }

    private void Die()
    {
        isDead = true;
        attackDamageQueued = false;
        actionLockTimer = 0f;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        CrossFadeState(deathState, actionBlendTime, true);

        if (deathDestroyDelay > 0f)
        {
            Destroy(gameObject, deathDestroyDelay);
        }
    }

    private void CrossFadeState(string stateName, float blendTime, bool force = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        if (!force && currentStateHash == stateHash)
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateHash, blendTime, 0);
        currentStateHash = stateHash;
    }
}
