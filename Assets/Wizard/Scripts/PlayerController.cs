using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    private class MagicSpell
    {
        [SerializeField] private string displayName = "Magia";
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 25f;
        [SerializeField] private float projectileLifeTime = 3f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private AudioClip castSound;
        [SerializeField, Range(0f, 1f)] private float castVolume = 1f;

        public string DisplayName => displayName;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifeTime => projectileLifeTime;
        public float Damage => damage;
        public AudioClip CastSound => castSound;
        public float CastVolume => castVolume;
    }

    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int WalkBackHash = Animator.StringToHash("WalkBack");
    private static readonly int WalkingRightHash = Animator.StringToHash("IsWalkingRight");
    private static readonly int WalkingLeftHash = Animator.StringToHash("IsWalkingLeft");
    private static readonly int RunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsAttackHash = Animator.StringToHash("IsAttack");

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool moveRelativeToCamera = true;

    [Header("Animation States")]
    [SerializeField] private string idleState = "Idle03";
    [SerializeField] private string walkForwardState = "BattleWalkForward";
    [SerializeField] private string walkBackState = "BattleWalkBack";
    [SerializeField] private string walkRightState = "BattleWalkRight";
    [SerializeField] private string walkLeftState = "BattleWalkLeft";
    [SerializeField] private string runForwardState = "BattleRunForward";
    [SerializeField] private string attackState = "Attack04";
    [SerializeField] private string hitState = "GetHit";
    [SerializeField] private float locomotionBlendTime = 0.12f;
    [SerializeField] private float actionBlendTime = 0.08f;
    [SerializeField] private float attackResetTimeout = 1.2f;
    [SerializeField] private float hitAnimationLock = 0.45f;

    [Header("Magic Attack")]
    [SerializeField] private MagicSpell[] magicSpells;
    [SerializeField] private int currentMagicIndex;
    [SerializeField] private KeyCode previousMagicKey = KeyCode.Q;
    [SerializeField] private KeyCode nextMagicKey = KeyCode.E;
    [SerializeField] private Transform spellSpawnPoint;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private float aimRayDistance = 80f;
    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private float attackCastDelay = 0.18f;
    [SerializeField] private bool useCameraForwardWhenNoHit = true;
    [SerializeField] private Text currentMagicText;
    [SerializeField] private string magicHudPrefix = "Magia: ";

    [Header("Target Lock")]
    [SerializeField] private KeyCode targetLockKey = KeyCode.Mouse1;
    [SerializeField] private float targetLockRange = 35f;
    [SerializeField] private float targetLockScreenRadius = 0.35f;
    [SerializeField] private Text targetLockText;
    [SerializeField] private string targetLockHudText = "Mira: Ghost";

    private readonly HashSet<int> boolParameters = new HashSet<int>();
    private readonly HashSet<int> triggerParameters = new HashSet<int>();

    private CharacterController controller;
    private Animator animator;
    private Vector2 moveInput;
    private float yVelocity;
    private float attackTimer;
    private float attackCastTimer;
    private float actionLockTimer;
    private int currentStateHash;
    private bool attackCastQueued;
    private bool attackCastFinished;
    private bool isAttacking;
    private bool isGrounded;
    private bool warnedMissingProjectile;
    private GhostEnemy lockedTarget;
    private AimTarget aimTargetController;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (aimTarget == null)
        {
            AimTarget sceneAimTarget = FindObjectOfType<AimTarget>();
            if (sceneAimTarget != null)
            {
                aimTarget = sceneAimTarget.transform;
            }
        }

        if (aimTarget != null)
        {
            aimTargetController = aimTarget.GetComponent<AimTarget>();
        }

        CacheAnimatorParameters();
        ClampCurrentMagicIndex();
        UpdateMagicHud();
        UpdateTargetLockHud();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        ReadMovementInput();
        Move();
        HandleMagicSelection();
        HandleTargetLockInput();
        HandleAttack();
        UpdateAttackCast();
        UpdateActionLock();
        UpdateAnimation();
        UpdateAttackTimeout();
    }

    private void ReadMovementInput()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void Move()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && yVelocity < 0f)
        {
            yVelocity = -2f;
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 horizontalMove = GetHorizontalMoveDirection();
        float currentSpeed = IsRunning() ? runSpeed : speed;
        Vector3 verticalMove = Vector3.up * yVelocity;

        controller.Move((horizontalMove * currentSpeed + verticalMove) * Time.deltaTime);

        isGrounded = controller.isGrounded;
    }

    private Vector3 GetHorizontalMoveDirection()
    {
        if (!moveRelativeToCamera || cameraTransform == null)
        {
            return transform.right * moveInput.x + transform.forward * moveInput.y;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f || cameraRight.sqrMagnitude < 0.001f)
        {
            return transform.right * moveInput.x + transform.forward * moveInput.y;
        }

        cameraForward.Normalize();
        cameraRight.Normalize();

        return cameraRight * moveInput.x + cameraForward * moveInput.y;
    }

    private void HandleMagicSelection()
    {
        if (Input.GetKeyDown(previousMagicKey))
        {
            SelectMagic(currentMagicIndex - 1);
        }

        if (Input.GetKeyDown(nextMagicKey))
        {
            SelectMagic(currentMagicIndex + 1);
        }
    }

    public void SelectMagic(int index)
    {
        if (magicSpells == null || magicSpells.Length == 0)
        {
            currentMagicIndex = 0;
            UpdateMagicHud();
            return;
        }

        currentMagicIndex = index % magicSpells.Length;
        if (currentMagicIndex < 0)
        {
            currentMagicIndex += magicSpells.Length;
        }

        UpdateMagicHud();
    }

    private void HandleTargetLockInput()
    {
        if (lockedTarget != null && !lockedTarget.IsAlive)
        {
            ClearTargetLock();
        }

        if (!Input.GetKeyDown(targetLockKey))
        {
            return;
        }

        if (lockedTarget != null)
        {
            ClearTargetLock();
            return;
        }

        LockNearestGhost();
    }

    private void LockNearestGhost()
    {
        GhostEnemy bestTarget = null;
        float bestScore = float.PositiveInfinity;
        Camera aimCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;

        GhostEnemy[] enemies = FindObjectsOfType<GhostEnemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            GhostEnemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector3 targetPosition = enemy.AimPoint.position;
            float worldDistance = Vector3.Distance(transform.position, targetPosition);
            if (worldDistance > targetLockRange)
            {
                continue;
            }

            float score = worldDistance;
            if (aimCamera != null)
            {
                Vector3 viewportPoint = aimCamera.WorldToViewportPoint(targetPosition);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
                if (viewportOffset.magnitude > targetLockScreenRadius)
                {
                    continue;
                }

                score = viewportOffset.sqrMagnitude * 100f + worldDistance * 0.01f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }

        if (bestTarget == null)
        {
            return;
        }

        lockedTarget = bestTarget;
        if (aimTargetController != null)
        {
            aimTargetController.SetLockTarget(lockedTarget.AimPoint);
        }

        UpdateTargetLockHud();
    }

    private void ClearTargetLock()
    {
        lockedTarget = null;
        if (aimTargetController != null)
        {
            aimTargetController.ClearLockTarget();
        }

        UpdateTargetLockHud();
    }

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            SetAnimatorTrigger(AttackHash);
            SetAnimatorTrigger(IsAttackHash);
            CrossFadeState(attackState, actionBlendTime, true);

            isAttacking = true;
            attackTimer = attackResetTimeout;
            attackCastFinished = false;
            QueueMagicAttack();
        }
    }

    public void CastMagicAttack()
    {
        if (attackCastFinished)
        {
            return;
        }

        attackCastQueued = false;
        attackCastFinished = true;
        SpawnMagicProjectile();
    }

    public void EndAttack()
    {
        isAttacking = false;
        attackTimer = 0f;
        attackCastQueued = false;
        attackCastFinished = false;
    }

    public void PlayHitReaction()
    {
        EndAttack();
        actionLockTimer = Mathf.Max(actionLockTimer, hitAnimationLock);
        CrossFadeState(hitState, actionBlendTime, true);
    }

    private void QueueMagicAttack()
    {
        attackCastQueued = true;
        attackCastTimer = Mathf.Max(0f, attackCastDelay);

        if (attackCastTimer <= 0f)
        {
            CastMagicAttack();
        }
    }

    private void UpdateAttackCast()
    {
        if (!attackCastQueued)
        {
            return;
        }

        attackCastTimer -= Time.deltaTime;
        if (attackCastTimer <= 0f)
        {
            CastMagicAttack();
        }
    }

    private void UpdateActionLock()
    {
        if (actionLockTimer > 0f)
        {
            actionLockTimer -= Time.deltaTime;
        }
    }

    private void SpawnMagicProjectile()
    {
        MagicSpell currentSpell = GetCurrentMagicSpell();
        if (currentSpell == null || currentSpell.ProjectilePrefab == null)
        {
            if (!warnedMissingProjectile)
            {
                Debug.LogWarning("Nenhuma magia valida esta atribuida no PlayerController.");
                warnedMissingProjectile = true;
            }

            return;
        }

        Vector3 spawnPosition = GetSpellSpawnPosition();
        Vector3 aimDirection = GetAimDirection(spawnPosition);
        Quaternion spawnRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        GameObject projectileObject = Instantiate(currentSpell.ProjectilePrefab, spawnPosition, spawnRotation);

        MagicProjectile projectile = projectileObject.GetComponent<MagicProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<MagicProjectile>();
        }

        projectile.Configure(currentSpell.ProjectileSpeed, currentSpell.ProjectileLifeTime, currentSpell.Damage);
        projectile.SetOwner(gameObject);
        projectile.Launch(aimDirection, GetProjectileFollowTarget());
        PlayMagicCastSound(currentSpell, spawnPosition);
    }

    private void PlayMagicCastSound(MagicSpell spell, Vector3 position)
    {
        if (spell == null || spell.CastSound == null || spell.CastVolume <= 0f)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(spell.CastSound, position, spell.CastVolume);
    }

    private Vector3 GetSpellSpawnPosition()
    {
        if (spellSpawnPoint != null)
        {
            return spellSpawnPoint.position;
        }

        Vector3 aimForward = GetAimForward();
        return transform.position + Vector3.up * 1.25f + aimForward * 0.75f;
    }

    private Vector3 GetAimDirection(Vector3 origin)
    {
        Transform lockedAimTarget = GetLockedAimTarget();
        if (lockedAimTarget != null)
        {
            Vector3 lockedDirection = lockedAimTarget.position - origin;
            if (lockedDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return lockedDirection.normalized;
            }
        }

        if (cameraTransform != null)
        {
            Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
            if (TryGetAimHitPoint(aimRay, out Vector3 hitPoint))
            {
                Vector3 hitDirection = hitPoint - origin;
                if (hitDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    return hitDirection.normalized;
                }
            }

            if (useCameraForwardWhenNoHit)
            {
                return aimRay.direction.normalized;
            }
        }

        Vector3 aimPoint = GetAimPoint(origin);
        Vector3 direction = aimPoint - origin;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return GetAimForward();
        }

        return direction.normalized;
    }

    private Vector3 GetAimPoint(Vector3 origin)
    {
        Transform lockedAimTarget = GetLockedAimTarget();
        if (lockedAimTarget != null)
        {
            return lockedAimTarget.position;
        }

        if (cameraTransform != null)
        {
            Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
            if (TryGetAimHitPoint(aimRay, out Vector3 hitPoint))
            {
                return hitPoint;
            }

            return aimRay.origin + aimRay.direction * aimRayDistance;
        }

        if (aimTarget != null)
        {
            return aimTarget.position;
        }

        return origin + transform.forward * aimRayDistance;
    }

    private Transform GetProjectileFollowTarget()
    {
        Transform lockedAimTarget = GetLockedAimTarget();
        if (lockedAimTarget != null)
        {
            return lockedAimTarget;
        }

        return aimTarget;
    }

    private Transform GetLockedAimTarget()
    {
        if (lockedTarget == null || !lockedTarget.IsAlive)
        {
            return null;
        }

        return lockedTarget.AimPoint;
    }

    private bool TryGetAimHitPoint(Ray aimRay, out Vector3 hitPoint)
    {
        RaycastHit[] hits = Physics.RaycastAll(aimRay, aimRayDistance, aimHitMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.PositiveInfinity;
        hitPoint = aimRay.origin + aimRay.direction * aimRayDistance;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform != null && hit.transform.root == transform.root)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                hitPoint = hit.point;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private Vector3 GetAimForward()
    {
        if (cameraTransform != null)
        {
            return cameraTransform.forward;
        }

        if (aimTarget != null)
        {
            Vector3 targetDirection = aimTarget.position - transform.position;
            if (targetDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return targetDirection.normalized;
            }
        }

        return transform.forward;
    }

    private MagicSpell GetCurrentMagicSpell()
    {
        if (magicSpells == null || magicSpells.Length == 0)
        {
            return null;
        }

        ClampCurrentMagicIndex();
        return magicSpells[currentMagicIndex];
    }

    private void ClampCurrentMagicIndex()
    {
        if (magicSpells == null || magicSpells.Length == 0)
        {
            currentMagicIndex = 0;
            return;
        }

        currentMagicIndex = Mathf.Clamp(currentMagicIndex, 0, magicSpells.Length - 1);
    }

    private void UpdateMagicHud()
    {
        if (currentMagicText == null)
        {
            return;
        }

        MagicSpell spell = GetCurrentMagicSpell();
        string magicName = spell != null && !string.IsNullOrWhiteSpace(spell.DisplayName)
            ? spell.DisplayName
            : "Nenhuma";
        currentMagicText.text = magicHudPrefix + magicName;
    }

    private void UpdateTargetLockHud()
    {
        if (targetLockText == null)
        {
            return;
        }

        bool hasLockedTarget = lockedTarget != null && lockedTarget.IsAlive;
        targetLockText.gameObject.SetActive(hasLockedTarget);
        targetLockText.text = hasLockedTarget ? targetLockHudText : string.Empty;
    }

    private void UpdateAttackTimeout()
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            EndAttack();
        }
    }

    private void UpdateAnimation()
    {
        bool movingForward = moveInput.y > 0.1f;
        bool movingBack = moveInput.y < -0.1f;
        bool movingRight = moveInput.x > 0.1f;
        bool movingLeft = moveInput.x < -0.1f;
        bool running = IsRunning();

        SetAnimatorBool(WalkHash, movingForward && !running);
        SetAnimatorBool(WalkBackHash, movingBack);
        SetAnimatorBool(WalkingRightHash, movingRight);
        SetAnimatorBool(WalkingLeftHash, movingLeft);
        SetAnimatorBool(RunningHash, running);

        if (actionLockTimer > 0f || isAttacking)
        {
            return;
        }

        if (running)
        {
            CrossFadeState(runForwardState, locomotionBlendTime);
        }
        else if (movingForward)
        {
            CrossFadeState(walkForwardState, locomotionBlendTime);
        }
        else if (movingBack)
        {
            CrossFadeState(walkBackState, locomotionBlendTime);
        }
        else if (movingRight)
        {
            CrossFadeState(walkRightState, locomotionBlendTime);
        }
        else if (movingLeft)
        {
            CrossFadeState(walkLeftState, locomotionBlendTime);
        }
        else
        {
            CrossFadeState(idleState, locomotionBlendTime);
        }
    }

    private bool IsRunning()
    {
        bool wantsSprint = Input.GetKey(sprintKey) || Input.GetKey(KeyCode.RightShift);
        return wantsSprint && moveInput.sqrMagnitude > 0.01f;
    }

    private void CacheAnimatorParameters()
    {
        boolParameters.Clear();
        triggerParameters.Clear();

        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                boolParameters.Add(parameter.nameHash);
            }
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                triggerParameters.Add(parameter.nameHash);
            }
        }
    }

    private void SetAnimatorBool(int parameterHash, bool value)
    {
        if (animator != null && boolParameters.Contains(parameterHash))
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void SetAnimatorTrigger(int parameterHash)
    {
        if (animator != null && triggerParameters.Contains(parameterHash))
        {
            animator.SetTrigger(parameterHash);
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
