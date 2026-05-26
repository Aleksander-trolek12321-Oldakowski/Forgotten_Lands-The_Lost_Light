using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Item;
using Player;
using SideQuests;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float maxHp = 20f;
    public float currentHp;

    [SerializeField] private float expValue = 50f;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Combat Brain")]
    public bool useAdvancedCombat = true;
    [Range(0f, 1f)] public float attackCommitChance = 0.65f;
    [Range(0f, 1f)] public float dodgeChanceOnPlayerSwing = 0.55f;
    public float strafeStartDistance = 4f;
    public float strafeOrbitMinDistance = 1.8f;
    public float strafeOrbitMaxDistance = 3.4f;
    public float strafeMoveSpeed = 3.2f;
    public Vector2 strafeDurationRange = new Vector2(0.5f, 1.25f);
    public float dodgeDistance = 2f;
    public float dodgeMoveSpeed = 7f;
    public float dodgeCooldown = 1.5f;

    [Header("Attack Window")]
    public float hitWindowStart = 0.3f;
    public float hitWindowEnd = 0.8f;

    [Header("Detection")]
    public float detectionRange = 10f;
    [Tooltip("Fallback chase speed used when NavMeshAgent is unavailable in build/runtime.")]
    public float fallbackChaseSpeed = 10f;
    [Tooltip("Fallback wander speed used when NavMeshAgent is unavailable in build/runtime.")]
    public float fallbackWanderSpeed = 4f;
    public float fallbackWanderReachDistance = 0.25f;

    [Header("Spacing / Collision")]
    [Tooltip("Minimum separation between enemies.")]
    public float minimumEnemySeparation = 1.1f;
    [Tooltip("Minimum separation between enemy and player body.")]
    public float minimumPlayerSeparation = 1.0f;
    [Tooltip("Maximum separation correction per second.")]
    public float separationResolveSpeed = 4f;
    [Tooltip("Force main enemy collider to be solid (not trigger) so player cannot pass through.")]
    public bool forceSolidBodyCollider = true;
    [Tooltip("Lock enemy movement to Y value captured at start.")]
    public bool lockYToStartHeight = true;

    [Header("Wander")]
    public bool useWander = true;
    public BoxCollider wanderArea;
    public float wanderInterval = 4f;

    private float wanderTimer;
    private Vector3 fallbackWanderTarget;
    private bool hasFallbackWanderTarget;

    [Header("Stagger")]
    public bool canBeStaggered = true;
    public float staggerDuration = 1f;

    public bool useStaggerAnimation = false;
    public string staggerTrigger = "Stagger";

    [Header("Boss")]
    public bool isBoss = false;

    [Tooltip("Ilość normalnych animacji ataku")]
    public int normalAttackAnimations = 1;

    [Tooltip("Ilość bossowych animacji ataku")]
    public int bossAttackAnimations = 1;

    [Header("Model Fix")]
    public bool invertModelForward = false;

    [Header("Animation")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string deathTrigger = "Die";

    [Header("Death")]
    public float destroyAfterDeath = 3f;

    [Header("Loot Drop")]
    [Range(0f, 1f)]
    public float lootDropChance = 0.25f;

    public GameObject lootBagPrefab;

    public List<ItemData> lootDropTable =
        new List<ItemData>();

    public float lootSpawnHeightOffset = 0.2f;

    private float lastAttackTime = -999f;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isStaggered = false;
    private bool isDodging = false;
    private float dodgeUntilTime = -999f;
    private float nextAllowedDodgeTime = -999f;
    private Vector3 dodgeTarget;
    private bool isStrafing = false;
    private float strafeUntilTime = -999f;
    private int strafeDirection = 1;
    private float nextCombatDecisionTime = -999f;
    private float lastProcessedPlayerSwingTime = -999f;

    private PlayerBase player;
    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody rb;
    private bool rigidbodyConfiguredForNavMesh;
    private Collider bodyCollider;
    private bool bodyColliderConfigured;
    private float lockedY;
    private bool hasLockedY;
    [SerializeField] private GameObject Target;
    private float nextPlayerLookupTime = 0f;

    public event Action<EnemyBase> Died;

    public bool IsDead => isDead;
    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;

    public QuestEnemyCategory questCategory =
        QuestEnemyCategory.Generic;

    private enum State
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Stagger,
        Dead
    }

    private State currentState;

    void Start()
    {
        currentHp = maxHp;
        lockedY = transform.position.y;
        hasLockedY = true;

        ResolveRuntimeReferences(forcePlayerLookup: true);

        if (agent != null)
        {
            agent.updateRotation = false;
        }

        wanderTimer = wanderInterval;

        currentState =
            useWander ? State.Wander : State.Idle;
    }

    void Update()
    {
        if (isDead) return;
        ResolveRuntimeReferences(forcePlayerLookup: false);

        float distance = float.PositiveInfinity;
        if (player != null)
        {
            distance =
                Vector3.Distance(
                    transform.position,
                    player.transform.position);
        }

        switch (currentState)
        {
            case State.Idle:

                IdleBehaviour(distance);

                break;

            case State.Wander:

                WanderBehaviour(distance);

                break;

            case State.Chase:

                ChaseBehaviour(distance);

                break;

            case State.Attack:

                AttackBehaviour(distance);

                break;

            case State.Stagger:

                StaggerBehaviour();

                break;
        }

        ResolvePersonalSpace();
        EnforceLockedY();
    }

    void IdleBehaviour(float distance)
    {
        if (CanUseNavAgent())
            agent.isStopped = true;

        SetSpeed(0f);

        if (distance <= detectionRange)
        {
            currentState = State.Chase;
        }
    }

    void WanderBehaviour(float distance)
    {
        if (distance <= detectionRange)
        {
            currentState = State.Chase;
            return;
        }

        if (wanderArea == null)
            return;

        wanderTimer += Time.deltaTime;

        if (CanUseNavAgent())
        {
            agent.isStopped = false;
            LookAtMovementDirection();
            SetSpeed(agent.desiredVelocity.magnitude);

            if (wanderTimer >= wanderInterval || !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                PickNewWanderTarget(useNavMesh: true);
                wanderTimer = 0f;
            }

            return;
        }

        if (wanderTimer >= wanderInterval || !hasFallbackWanderTarget || IsNearPosition(transform.position, fallbackWanderTarget, fallbackWanderReachDistance))
        {
            PickNewWanderTarget(useNavMesh: false);
            wanderTimer = 0f;
        }

        if (!hasFallbackWanderTarget)
        {
            SetSpeed(0f);
            return;
        }

        Vector3 move = MoveTowardsPointFallback(fallbackWanderTarget, fallbackWanderSpeed);
        LookAtDirection(move);

        if (move.sqrMagnitude > 0.0001f) SetSpeed(fallbackWanderSpeed);
        else SetSpeed(0f);
    }

    void ChaseBehaviour(float distance)
    {
        if (isStaggered) return;
        if (player == null)
        {
            currentState = useWander ? State.Wander : State.Idle;
            return;
        }

        if (useAdvancedCombat && TryHandleCombatBrain(distance))
            return;

        if (CanUseNavAgent())
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
            SetSpeed(agent.velocity.magnitude);
        }
        else
        {
            MoveTowardsPlayerFallback();
            SetSpeed(fallbackChaseSpeed);
        }

        LookAtPlayer();

        if (distance <= attackRange)
        {
            currentState = State.Attack;
        }
    }

    void AttackBehaviour(float distance)
    {
        if (isStaggered) return;
        if (player == null)
        {
            currentState = useWander ? State.Wander : State.Idle;
            return;
        }

        if (useAdvancedCombat && TryHandleCombatBrain(distance))
            return;

        if (CanUseNavAgent())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        LookAtPlayer();

        SetSpeed(0f);

        if (distance > attackRange + 1f)
        {
            currentState = State.Chase;
            return;
        }

        if (useAdvancedCombat && Time.time < lastAttackTime + attackCooldown)
            return;

        if (!useAdvancedCombat || UnityEngine.Random.value <= attackCommitChance)
            TryAttack();
        else
            StartStrafe();
    }

    void StaggerBehaviour()
    {
        isDodging = false;
        isStrafing = false;

        if (CanUseNavAgent())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        SetSpeed(0f);
    }

    void TryAttack()
    {
        if (isAttacking) return;
        if (isStaggered) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        isAttacking = true;

        PlayAttackAnimation();

        StartCoroutine(AttackRoutine());
    }

    void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        int attackIndex;

        if (isBoss)
        {
            int totalAnimations =
                normalAttackAnimations +
                bossAttackAnimations;

            int random =
                UnityEngine.Random.Range(
                    0,
                    totalAnimations);

            if (random < normalAttackAnimations)
            {
                attackIndex = random + 1;
            }
            else
            {
                attackIndex = 100;
            }
        }
        else
        {
            attackIndex =
                UnityEngine.Random.Range(
                    1,
                    normalAttackAnimations + 1
                );
        }

        animator.SetInteger(
            "AttackIndex",
            attackIndex);

        animator.SetTrigger(attackTrigger);
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(
            hitWindowStart);

        bool hitDone = false;

        float timer = 0f;

        float duration =
            hitWindowEnd - hitWindowStart;

        while (timer < duration)
        {
            if (isDead || isStaggered)
            {
                isAttacking = false;
                yield break;
            }

            LookAtPlayer();

            if (!hitDone && player != null)
            {
                float distance =
                    Vector3.Distance(
                        transform.position,
                        player.transform.position);

                if (distance <= attackRange)
                {
                    DealDamage();

                    hitDone = true;
                }
            }

            timer += Time.deltaTime;

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
    }

    public void DealDamage()
    {
        if (isDead) return;
        if (isStaggered) return;
        if (player == null) return;

        player.TakeDMG(attackDamage);

        Debug.Log(name + " attacked player");
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHp -= dmg;

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        if (canBeStaggered)
        {
            StartCoroutine(StaggerRoutine());
        }
    }

    IEnumerator StaggerRoutine()
    {
        if (isStaggered)
            yield break;

        isStaggered = true;
        isDodging = false;
        isStrafing = false;

        currentState = State.Stagger;

        isAttacking = false;

        StopCoroutine(nameof(AttackRoutine));

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        SetSpeed(0f);

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);

            if (useStaggerAnimation)
            {
                animator.SetTrigger(staggerTrigger);
            }
        }

        yield return new WaitForSeconds(
            staggerDuration);

        if (isDead)
            yield break;

        isStaggered = false;

        currentState = State.Chase;

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        isDodging = false;
        isStrafing = false;

        currentState = State.Dead;

        StopAllCoroutines();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.enabled = false;
        }

        SideQuestManager.Instance
            ?.ReportEnemyKilled(questCategory);

        if (player != null)
        {
            player.AddExp(expValue);
        }

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);

            if (useStaggerAnimation)
            {
                animator.ResetTrigger(
                    staggerTrigger);
            }

            animator.SetTrigger(deathTrigger);
        }

        TryDropLootBag();

        Died?.Invoke(this);

        Destroy(gameObject, destroyAfterDeath);
    }

    void TryDropLootBag()
    {
        if (lootBagPrefab == null) return;

        if (lootDropChance <= 0f) return;

        if (UnityEngine.Random.value >
            lootDropChance)
            return;

        Vector3 spawnPos =
            transform.position +
            Vector3.up * lootSpawnHeightOffset;

        GameObject bag =
            Instantiate(
                lootBagPrefab,
                spawnPos,
                Quaternion.identity);

        LootBag lootBag =
            bag.GetComponent<LootBag>();

        if (lootBag != null &&
            lootDropTable != null &&
            lootDropTable.Count > 0)
        {
            lootBag.lootTable =
                new List<ItemData>(
                    lootDropTable);
        }
    }

    void SetSpeed(float value)
    {
        if (animator != null)
        {
            animator.SetFloat(
                speedParam,
                value);
        }
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir =
            player.transform.position -
            transform.position;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(dir);

        if (invertModelForward)
        {
            targetRot *=
                Quaternion.Euler(0f, 180f, 0f);
        }

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRot,
                8f * Time.deltaTime
            );
    }

    void LookAtMovementDirection()
    {
        Vector3 dir = agent.desiredVelocity;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(dir);

        if (invertModelForward)
        {
            targetRot *=
                Quaternion.Euler(0f, 180f, 0f);
        }

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRot,
                8f * Time.deltaTime
            );
    }

    bool TryHandleCombatBrain(float distanceToPlayer)
    {
        if (player == null)
            return false;
        if (isAttacking)
            return false;

        if (Time.time >= nextCombatDecisionTime)
        {
            nextCombatDecisionTime = Time.time + UnityEngine.Random.Range(0.18f, 0.45f);
            ProcessIncomingPlayerSwing(distanceToPlayer);
        }

        if (isDodging)
        {
            ExecuteDodge();
            return true;
        }

        if (distanceToPlayer <= strafeStartDistance)
        {
            if (!isStrafing && UnityEngine.Random.value < 0.38f)
                StartStrafe();

            if (isStrafing)
            {
                ExecuteStrafe(distanceToPlayer);
                return true;
            }
        }

        return false;
    }

    void ProcessIncomingPlayerSwing(float distanceToPlayer)
    {
        if (isAttacking)
            return;

        if (distanceToPlayer > strafeStartDistance + 1.5f)
            return;

        if (Time.time < nextAllowedDodgeTime)
            return;

        if (AttackHitbox.LastAttackWindowOpenTime <= lastProcessedPlayerSwingTime)
            return;

        lastProcessedPlayerSwingTime = AttackHitbox.LastAttackWindowOpenTime;

        Vector3 fromAttackToEnemy = transform.position - AttackHitbox.LastAttackOrigin;
        fromAttackToEnemy.y = 0f;
        if (fromAttackToEnemy.sqrMagnitude < 0.0001f)
            return;

        Vector3 attackForward = AttackHitbox.LastAttackForward;
        attackForward.y = 0f;
        if (attackForward.sqrMagnitude < 0.0001f)
            return;

        float facingDot = Vector3.Dot(attackForward.normalized, fromAttackToEnemy.normalized);
        if (facingDot < 0.25f)
            return;

        if (UnityEngine.Random.value <= dodgeChanceOnPlayerSwing)
            StartDodge();
    }

    void StartStrafe()
    {
        isStrafing = true;
        strafeDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
        float min = Mathf.Min(strafeDurationRange.x, strafeDurationRange.y);
        float max = Mathf.Max(strafeDurationRange.x, strafeDurationRange.y);
        strafeUntilTime = Time.time + UnityEngine.Random.Range(min, max);
    }

    void ExecuteStrafe(float distanceToPlayer)
    {
        if (player == null)
        {
            isStrafing = false;
            return;
        }

        if (Time.time >= strafeUntilTime || distanceToPlayer > strafeStartDistance + 0.9f)
        {
            isStrafing = false;
            return;
        }

        Vector3 playerPos = player.transform.position;
        Vector3 toEnemy = transform.position - playerPos;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.0001f)
            toEnemy = -player.transform.forward;

        Vector3 radial = toEnemy.normalized;
        float orbitRadius = Mathf.Clamp(distanceToPlayer, strafeOrbitMinDistance, strafeOrbitMaxDistance);
        Vector3 tangent = Vector3.Cross(Vector3.up, radial) * strafeDirection;

        Vector3 target =
            playerPos +
            radial * orbitRadius +
            tangent.normalized * Mathf.Max(0.6f, strafeMoveSpeed * 0.45f);

        MoveTacticallyTowards(target, strafeMoveSpeed);
        LookAtPlayer();
    }

    void StartDodge()
    {
        if (player == null)
            return;

        isDodging = true;
        isStrafing = false;
        nextAllowedDodgeTime = Time.time + Mathf.Max(0.1f, dodgeCooldown);
        dodgeUntilTime = Time.time + 0.28f;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
            toPlayer = transform.forward;

        Vector3 away = -toPlayer.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
        Vector3 dodgeDir = (side * 0.8f + away * 0.6f).normalized;

        dodgeTarget = transform.position + dodgeDir * Mathf.Max(0.4f, dodgeDistance);
    }

    void ExecuteDodge()
    {
        if (!isDodging)
            return;

        MoveTacticallyTowards(dodgeTarget, dodgeMoveSpeed);
        LookAtPlayer();

        if (Time.time >= dodgeUntilTime || IsNearPosition(transform.position, dodgeTarget, 0.25f))
            isDodging = false;
    }

    void MoveTacticallyTowards(Vector3 worldTarget, float speed)
    {
        if (CanUseNavAgent())
        {
            agent.isStopped = false;
            agent.speed = Mathf.Max(0.1f, speed);
            agent.SetDestination(worldTarget);
            SetSpeed(agent.velocity.magnitude);
            return;
        }

        Vector3 move = MoveTowardsPointFallback(worldTarget, speed);
        if (move.sqrMagnitude > 0.0001f)
            SetSpeed(speed);
        else
            SetSpeed(0f);
    }

    void ResolveRuntimeReferences(bool forcePlayerLookup)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            if (minimumEnemySeparation > 0.01f)
                agent.radius = Mathf.Max(agent.radius, minimumEnemySeparation * 0.5f);
            if (minimumPlayerSeparation > 0.01f)
                agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, minimumPlayerSeparation);
        }
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        ConfigureRigidbodyForNavMesh();
        ConfigureBodyCollider();

        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        bool shouldLookupPlayer =
            forcePlayerLookup ||
            player == null ||
            !player.gameObject.activeInHierarchy;

        if (shouldLookupPlayer && Time.unscaledTime >= nextPlayerLookupTime)
        {
            player = FindObjectOfType<PlayerBase>();
            nextPlayerLookupTime = Time.unscaledTime + 0.5f;
        }

        if (agent != null && agent.enabled && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    void ConfigureRigidbodyForNavMesh()
    {
        if (rigidbodyConfiguredForNavMesh)
            return;

        if (agent == null || rb == null)
            return;

        //rb.isKinematic = true;
        rb.useGravity = false;
        rigidbodyConfiguredForNavMesh = true;
    }

    void ConfigureBodyCollider()
    {
        if (bodyColliderConfigured)
            return;

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider>();
        if (bodyCollider == null)
            bodyCollider = GetComponentInChildren<Collider>();

        if (forceSolidBodyCollider && bodyCollider != null)
            bodyCollider.isTrigger = false;

        bodyColliderConfigured = true;
    }

    bool CanUseNavAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    void ResolvePersonalSpace()
    {
        Vector3 totalPush = Vector3.zero;

        if (minimumEnemySeparation > 0.01f)
        {
            Collider[] overlaps = Physics.OverlapSphere(
                transform.position,
                minimumEnemySeparation,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider c = overlaps[i];
                if (c == null) continue;

                EnemyBase other = c.GetComponentInParent<EnemyBase>();
                if (other == null || other == this || other.IsDead) continue;

                Vector3 diff = transform.position - other.transform.position;
                diff.y = 0f;
                float dist = diff.magnitude;
                if (dist < 0.001f)
                {
                    diff = UnityEngine.Random.insideUnitSphere;
                    diff.y = 0f;
                    dist = Mathf.Max(0.001f, diff.magnitude);
                }

                float overlap = minimumEnemySeparation - dist;
                if (overlap > 0f)
                {
                    totalPush += diff.normalized * overlap;
                }
            }
        }

        if (player != null && minimumPlayerSeparation > 0.01f)
        {
            Vector3 diff = transform.position - player.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < 0.001f)
            {
                diff = transform.forward;
                diff.y = 0f;
                dist = Mathf.Max(0.001f, diff.magnitude);
            }

            float overlap = minimumPlayerSeparation - dist;
            if (overlap > 0f)
            {
                totalPush += diff.normalized * overlap;
            }
        }

        if (totalPush.sqrMagnitude < 0.0001f)
            return;

        float maxStep = Mathf.Max(0.01f, separationResolveSpeed) * Time.deltaTime;
        Vector3 step = Vector3.ClampMagnitude(totalPush, maxStep);
        Vector3 target = transform.position + step;

        ForceRelocate(target, keepCurrentY: true);
    }

    void MoveTowardsPlayerFallback()
    {
        if (player == null)
            return;

        Vector3 from = transform.position;
        Vector3 to = player.transform.position;
        to.y = from.y;

        Vector3 dir = to - from;
        float sqr = dir.sqrMagnitude;
        if (sqr < 0.0001f)
            return;

        float speed = Mathf.Max(0.1f, fallbackChaseSpeed);
        Vector3 next = from + dir.normalized * speed * Time.deltaTime;
        ForceRelocate(next, keepCurrentY: true);
    }

    bool PickNewWanderTarget(bool useNavMesh)
    {
        if (wanderArea == null)
        {
            hasFallbackWanderTarget = false;
            return false;
        }

        Bounds bounds = wanderArea.bounds;
        Vector3 randomPoint =
            new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

        if (useNavMesh && CanUseNavAgent())
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }
        }

        fallbackWanderTarget = randomPoint;
        hasFallbackWanderTarget = true;
        return true;
    }

    Vector3 MoveTowardsPointFallback(Vector3 target, float speed)
    {
        Vector3 from = transform.position;
        Vector3 to = target;
        to.y = from.y;

        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        float step = Mathf.Max(0.1f, speed) * Time.deltaTime;
        if (dir.magnitude <= step)
        {
            ForceRelocate(to, keepCurrentY: true);
            return dir;
        }

        Vector3 move = dir.normalized * step;
        ForceRelocate(from + move, keepCurrentY: true);
        return move;
    }

    bool IsNearPosition(Vector3 a, Vector3 b, float distance)
    {
        Vector3 da = a;
        Vector3 db = b;
        da.y = 0f;
        db.y = 0f;
        return (da - db).sqrMagnitude <= distance * distance;
    }

    void LookAtDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        if (invertModelForward)
            targetRot *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
    }

    public void ForceRelocate(Vector3 worldPosition, bool keepCurrentY = false)
    {
        Vector3 target = worldPosition;
        if (lockYToStartHeight && hasLockedY)
            target.y = lockedY;
        else if (keepCurrentY)
            target.y = transform.position.y;

        if (CanUseNavAgent())
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
            {
                Vector3 warpPos = hit.position;
                if (lockYToStartHeight && hasLockedY)
                    warpPos.y = lockedY;
                agent.Warp(warpPos);
                return;
            }
        }

        transform.position = target;
    }

    void EnforceLockedY()
    {
        if (!lockYToStartHeight || !hasLockedY)
            return;

        Vector3 p = transform.position;
        if (Mathf.Abs(p.y - lockedY) <= 0.0001f)
            return;

        p.y = lockedY;
        transform.position = p;

        if (agent != null && agent.enabled)
            agent.nextPosition = p;
    }
}
