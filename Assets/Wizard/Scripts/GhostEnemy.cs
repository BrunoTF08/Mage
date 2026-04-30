using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private float stoppingDistance = 1.9f;
    [SerializeField] private float hoverHeight = 0.05f;

    [Header("Collision")]
    [SerializeField] private Rigidbody body;
    [SerializeField] private ObjectCollisionSensor objectSensor;
    [SerializeField, Min(0f)] private float objectAvoidanceWeight = 1.85f;
    [SerializeField, Min(0.01f)] private float velocitySharpness = 18f;

    [Header("Combat")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2.05f;
    [SerializeField] private float attackCooldown = 1.35f;
    [SerializeField] private float attackHitDelay = 0.45f;
    [SerializeField, Min(0f)] private float attackAnimationLock = 0.85f;
    [SerializeField] private float attackContactPadding = 0.45f;
    [SerializeField] private float damageAnimationLock = 0.55f;
    [SerializeField] private float deathDestroyDelay = 4f;

    [Header("Audio")]
    [SerializeField] private AudioSource ghostAudioSource;
    [SerializeField] private AudioClip ghostLoopClip;
    [SerializeField] private AudioClip attackSound;
    [SerializeField, Range(0f, 1f)] private float ghostLoopVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float attackSoundVolume = 0.85f;
    [SerializeField] private Vector2 randomPitchRange = new Vector2(0.92f, 1.08f);

    [Header("Animation States")]
    [SerializeField] private string idleState = "Ghost_Idle";
    [SerializeField] private string moveState = "Ghost_Fly";
    [SerializeField] private string attackState = "Ghost_Attack";
    [SerializeField] private string damageState = "Ghost_Damage";
    [SerializeField] private string deathState = "Ghost_Death";
    [SerializeField] private float moveBlendTime = 0.12f;
    [SerializeField] private float actionBlendTime = 0.08f;

    [Header("Health Bar")]
    [SerializeField] private Canvas healthBarCanvas;
    [SerializeField] private Image healthBarFillImage;
    [SerializeField] private Image healthBarDelayImage;
    [SerializeField] private Color highHealthColor = new Color(0.35f, 1f, 0.65f, 1f);
    [SerializeField] private Color midHealthColor = new Color(1f, 0.78f, 0.24f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.2f, 0.18f, 1f);
    [SerializeField, Min(0f)] private float healthDelaySpeed = 0.65f;
    [SerializeField] private bool faceHealthBarToCamera = true;

    private Animator animator;
    private Collider hitCollider;
    private Collider playerHitCollider;
    private CharacterController playerCharacterController;
    private Camera healthBarCamera;
    private RectTransform healthBarFillRect;
    private RectTransform healthBarDelayRect;
    private float currentHealth;
    private float healthPercent = 1f;
    private float healthDelayPercent = 1f;
    private float attackCooldownTimer;
    private float attackHitTimer;
    private float actionLockTimer;
    private bool attackDamageQueued;
    private bool healthBarVisible;
    private bool isDead;
    private int currentStateHash;
    private Vector3 desiredVelocity;
    private Quaternion desiredRotation;

    public bool IsAlive => !isDead;
    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitCollider = GetComponent<Collider>();
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (objectSensor == null)
        {
            objectSensor = GetComponentInChildren<ObjectCollisionSensor>();
        }

        ConfigureRigidbody();
        ConfigureAudioSource();
        currentHealth = maxHealth;
        ResolveHealthBarReferences();
        UpdateHealthBar();
        SetHealthBarVisible(false);
        desiredRotation = transform.rotation;
    }

    private void Start()
    {
        ResolvePlayerReferences();
        PlayGhostLoop();
        CrossFadeState(idleState, 0f, true);
    }

    private void Update()
    {
        desiredVelocity = Vector3.zero;

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
            TryApplySensorSeparation(0.45f);
            return;
        }

        if (IsPlayerInAttackRange(0f))
        {
            TryApplySensorSeparation(0.6f);
            TryAttack();
            return;
        }

        MoveTowardsPlayer(toPlayer, distance);
    }

    private void FixedUpdate()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
        float blend = 1f - Mathf.Exp(-velocitySharpness * Time.fixedDeltaTime);
        Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredVelocity, blend);
        body.velocity = new Vector3(newHorizontalVelocity.x, 0f, newHorizontalVelocity.z);
        body.MoveRotation(desiredRotation);

        if (body.position.y < hoverHeight)
        {
            Vector3 correctedPosition = body.position;
            correctedPosition.y = hoverHeight;
            body.MovePosition(correctedPosition);
        }
    }

    private void LateUpdate()
    {
        UpdateHealthBarDelay();
        FaceHealthBarToCamera();
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateHealthBar();
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        actionLockTimer = damageAnimationLock;
        CrossFadeState(damageState, actionBlendTime, true);
    }

    public void SetHealthBarVisible(bool visible)
    {
        healthBarVisible = visible && !isDead;
        ResolveHealthBarReferences();

        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(healthBarVisible);
        }
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

        if (player != null)
        {
            if (playerCharacterController == null)
            {
                playerCharacterController = player.GetComponent<CharacterController>();
            }

            if (playerHitCollider == null)
            {
                playerHitCollider = playerCharacterController != null
                    ? playerCharacterController
                    : player.GetComponent<Collider>();
            }
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
            TryApplySensorSeparation(0.55f);
            return;
        }

        Vector3 direction = toPlayer / distance;
        Vector3 avoidance = objectSensor != null ? objectSensor.GetAvoidanceDirection(transform, hitCollider) : Vector3.zero;
        if (avoidance.sqrMagnitude > 0.001f)
        {
            direction = (direction + avoidance * objectAvoidanceWeight).normalized;
        }

        desiredVelocity = direction * moveSpeed;

        if (body != null && !body.isKinematic)
        {
            return;
        }

        Vector3 nextPosition = transform.position + desiredVelocity * Time.deltaTime;
        nextPosition.y = Mathf.Max(nextPosition.y, hoverHeight);
        transform.position = nextPosition;
    }

    private bool TryApplySensorSeparation(float speedMultiplier)
    {
        if (objectSensor == null)
        {
            return false;
        }

        Vector3 avoidance = objectSensor.GetAvoidanceDirection(transform, hitCollider);
        if (avoidance.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        desiredVelocity = avoidance * moveSpeed * Mathf.Max(0f, speedMultiplier);

        if (body == null || body.isKinematic)
        {
            Vector3 nextPosition = transform.position + desiredVelocity * Time.deltaTime;
            nextPosition.y = Mathf.Max(nextPosition.y, hoverHeight);
            transform.position = nextPosition;
        }

        return true;
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        desiredRotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        if (body == null || body.isKinematic)
        {
            transform.rotation = desiredRotation;
        }
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
        actionLockTimer = Mathf.Max(attackHitDelay, attackAnimationLock);
        PlayAttackSound();
        CrossFadeState(attackState, actionBlendTime, true);
    }

    private void DealAttackDamage()
    {
        if (isDead || player == null || playerHealth == null)
        {
            return;
        }

        if (!IsPlayerInAttackRange(attackContactPadding))
        {
            return;
        }

        playerHealth.TakeDamage(attackDamage);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryAttackFromContact(collision.collider);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAttackFromContact(other);
    }

    private void TryAttackFromContact(Collider other)
    {
        if (isDead || other == null || playerHealth == null || attackCooldownTimer > 0f)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        TryAttack();
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null || playerHealth == null)
        {
            return false;
        }

        PlayerHealth contactHealth = other.GetComponentInParent<PlayerHealth>();
        return contactHealth == playerHealth;
    }

    private bool IsPlayerInAttackRange(float padding)
    {
        if (player == null)
        {
            return false;
        }

        return GetDistanceToPlayerCollider() <= attackRange + padding;
    }

    private float GetDistanceToPlayerCollider()
    {
        Vector3 playerPoint = GetPlayerClosestPoint(transform.position);
        Vector3 ghostPoint = hitCollider != null && hitCollider.enabled
            ? hitCollider.ClosestPoint(playerPoint)
            : transform.position;

        playerPoint.y = 0f;
        ghostPoint.y = 0f;
        return Vector3.Distance(ghostPoint, playerPoint);
    }

    private Vector3 GetPlayerClosestPoint(Vector3 point)
    {
        if (playerCharacterController != null && playerCharacterController.enabled)
        {
            return playerCharacterController.ClosestPoint(point);
        }

        if (playerHitCollider != null && playerHitCollider.enabled)
        {
            return playerHitCollider.ClosestPoint(point);
        }

        return player != null ? player.position : point;
    }

    private void ResolveHealthBarReferences()
    {
        if (healthBarCanvas == null)
        {
            healthBarCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (healthBarFillImage == null)
        {
            Transform fillTransform = transform.Find("HealthBar/Background/Fill");
            if (fillTransform != null)
            {
                healthBarFillImage = fillTransform.GetComponent<Image>();
            }
        }

        if (healthBarDelayImage == null)
        {
            Transform delayTransform = transform.Find("HealthBar/Background/Delay");
            if (delayTransform != null)
            {
                healthBarDelayImage = delayTransform.GetComponent<Image>();
            }
        }

        if (healthBarFillImage != null)
        {
            healthBarFillImage.type = Image.Type.Simple;
            healthBarFillImage.raycastTarget = false;
            healthBarFillRect = healthBarFillImage.rectTransform;
            ConfigureHealthBarRect(healthBarFillRect);
        }

        if (healthBarDelayImage != null)
        {
            healthBarDelayImage.type = Image.Type.Simple;
            healthBarDelayImage.raycastTarget = false;
            healthBarDelayRect = healthBarDelayImage.rectTransform;
            ConfigureHealthBarRect(healthBarDelayRect);
        }
    }

    private void UpdateHealthBar()
    {
        ResolveHealthBarReferences();

        if (healthBarFillImage == null)
        {
            return;
        }

        healthPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthBarFillImage.color = GetHealthBarColor(healthPercent);
        ApplyHealthBarPercent(healthBarFillRect, healthPercent);

        if (healthDelayPercent < healthPercent)
        {
            healthDelayPercent = healthPercent;
            ApplyHealthBarPercent(healthBarDelayRect, healthDelayPercent);
        }
    }

    private void UpdateHealthBarDelay()
    {
        if (healthBarDelayRect == null)
        {
            return;
        }

        healthDelayPercent = Mathf.MoveTowards(
            healthDelayPercent,
            healthPercent,
            Time.deltaTime * healthDelaySpeed);
        ApplyHealthBarPercent(healthBarDelayRect, healthDelayPercent);
    }

    private void ConfigureHealthBarRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.offsetMin = new Vector2(3f, 3f);
        rectTransform.offsetMax = new Vector2(-3f, -3f);
    }

    private void ApplyHealthBarPercent(RectTransform rectTransform, float percent)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector3 scale = rectTransform.localScale;
        scale.x = Mathf.Clamp01(percent);
        scale.y = 1f;
        scale.z = 1f;
        rectTransform.localScale = scale;
    }

    private Color GetHealthBarColor(float healthPercent)
    {
        if (healthPercent > 0.55f)
        {
            return Color.Lerp(midHealthColor, highHealthColor, Mathf.InverseLerp(0.55f, 1f, healthPercent));
        }

        return Color.Lerp(lowHealthColor, midHealthColor, Mathf.InverseLerp(0.2f, 0.55f, healthPercent));
    }

    private void FaceHealthBarToCamera()
    {
        if (!faceHealthBarToCamera || healthBarCanvas == null || !healthBarCanvas.gameObject.activeInHierarchy)
        {
            return;
        }

        if (healthBarCamera == null || !healthBarCamera.isActiveAndEnabled)
        {
            healthBarCamera = Camera.main;
        }

        if (healthBarCamera == null)
        {
            return;
        }

        Transform barTransform = healthBarCanvas.transform;
        Vector3 directionToCamera = barTransform.position - healthBarCamera.transform.position;
        if (directionToCamera.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        barTransform.rotation = Quaternion.LookRotation(directionToCamera.normalized, healthBarCamera.transform.up);
    }

    private void Die()
    {
        isDead = true;
        attackDamageQueued = false;
        actionLockTimer = 0f;
        SetHealthBarVisible(false);

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        if (objectSensor != null)
        {
            objectSensor.enabled = false;
        }

        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        if (ghostAudioSource != null)
        {
            ghostAudioSource.Stop();
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

    private void ConfigureRigidbody()
    {
        if (body == null)
        {
            return;
        }

        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ConfigureAudioSource()
    {
        if (ghostAudioSource == null)
        {
            ghostAudioSource = GetComponent<AudioSource>();
        }

        if (ghostAudioSource == null)
        {
            return;
        }

        ghostAudioSource.playOnAwake = false;
        ghostAudioSource.loop = true;
        ghostAudioSource.spatialBlend = 1f;
        ghostAudioSource.rolloffMode = AudioRolloffMode.Linear;
        ghostAudioSource.minDistance = 1.5f;
        ghostAudioSource.maxDistance = 22f;
    }

    private void PlayGhostLoop()
    {
        if (ghostAudioSource == null || ghostLoopClip == null)
        {
            return;
        }

        ghostAudioSource.clip = ghostLoopClip;
        ghostAudioSource.volume = ghostLoopVolume;
        ghostAudioSource.pitch = Random.Range(randomPitchRange.x, randomPitchRange.y);
        ghostAudioSource.loop = true;
        ghostAudioSource.Play();
    }

    private void PlayAttackSound()
    {
        if (ghostAudioSource == null || attackSound == null || attackSoundVolume <= 0f)
        {
            return;
        }

        ghostAudioSource.PlayOneShot(attackSound, attackSoundVolume);
    }
}
