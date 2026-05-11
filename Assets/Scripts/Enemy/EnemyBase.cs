using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Player;
using SideQuests;

public class EnemyBase : MonoBehaviour
{
    public enum EnemyType
    {
        Generic,
        Skeleton,
        Spider,
        Boss
    }

    [Header("Stats")]
    public float maxHp = 20f;
    private float currentHp;

    [SerializeField] private float expValue = 50f;

    [Header("Category")]
    public EnemyType enemyType = EnemyType.Generic;
    public bool autoAssignQuestCategory = true;
    public QuestEnemyCategory questCategory = QuestEnemyCategory.Generic;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Attack Variants")]
    public bool enableJumpAttack = false;
    [Range(0f, 1f)] public float jumpAttackChance = 0f;
    public float jumpAttackRange = 6f;
    public float jumpAttackCooldown = 4f;
    public float jumpAttackDamageMultiplier = 1.3f;
    public float jumpWindup = 0.2f;
    public float jumpTravelDuration = 0.35f;
    public float jumpArcHeight = 1.6f;
    public float jumpLandingDistanceFromPlayer = 0.9f;
    public float jumpLandingSampleRadius = 2f;

    public bool useSecondaryAttack = false;
    [Range(0f, 1f)] public float secondaryAttackChance = 0.5f;

    [Header("Boss Attack Weights")]
    public bool useBossAttackWeights = true;
    [Range(0f, 1f)] public float bossJumpAttackChance = 0.3f;

    [Header("Attack Window")]
    public float hitWindowStart = 0.3f;
    public float hitWindowEnd = 0.8f;

    [Header("Detection")]
    public float detectionRange = 10f;

    [Header("Wander")]
    public bool useWander = true;

    public BoxCollider wanderArea;

    public float wanderInterval = 4f;

    private float wanderTimer;

    [Header("Stagger")]
    public bool canBeStaggered = true;

    public float staggerDuration = 1f;

    public bool useStaggerAnimation = false;

    public string staggerTrigger = "Stagger";

    [Header("Animation")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string secondaryAttackTrigger = "Attack2";
    public string jumpAttackTrigger = "JumpAttack";
    public string deathTrigger = "Death";

    [Header("Death")]
    public float destroyAfterDeath = 3f;

    private float lastAttackTime = -999f;
    private float lastJumpAttackTime = -999f;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isStaggered = false;

    private PlayerBase player;
    private NavMeshAgent agent;
    private Animator animator;
    private Coroutine activeAttackRoutine;

    public event Action<EnemyBase> Died;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => isDead;

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
        if (autoAssignQuestCategory)
        {
            questCategory = ConvertEnemyTypeToQuestCategory(enemyType);
        }

        currentHp = maxHp;

        player = FindObjectOfType<PlayerBase>();

        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();

        wanderTimer = wanderInterval;

        currentState = useWander ? State.Wander : State.Idle;
    }

    void OnValidate()
    {
        if (autoAssignQuestCategory)
        {
            questCategory = ConvertEnemyTypeToQuestCategory(enemyType);
        }
    }

    void Update()
    {
        if (isDead) return;
        if (player == null || agent == null) return;

        float distance =
            Vector3.Distance(transform.position, player.transform.position);

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
    }

    void IdleBehaviour(float distance)
    {
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

        agent.isStopped = false;

        wanderTimer += Time.deltaTime;

        SetSpeed(agent.velocity.magnitude);

        if (wanderTimer >= wanderInterval)
        {
            Bounds bounds = wanderArea.bounds;

            Vector3 randomPoint = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                randomPoint,
                out hit,
                2f,
                NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            wanderTimer = 0f;
        }
    }

    void ChaseBehaviour(float distance)
    {
        if (isStaggered) return;

        agent.isStopped = false;

        agent.SetDestination(player.transform.position);

        SetSpeed(agent.velocity.magnitude);

        if (distance <= attackRange)
        {
            currentState = State.Attack;
        }
    }

    void AttackBehaviour(float distance)
    {
        if (isStaggered) return;

        agent.isStopped = true;

        agent.velocity = Vector3.zero;

        SetSpeed(0f);

        LookAtPlayer();

        if (distance > attackRange + 1f)
        {
            currentState = State.Chase;
            return;
        }

        TryAttack();
    }

    void StaggerBehaviour()
    {
        agent.isStopped = true;

        agent.velocity = Vector3.zero;

        SetSpeed(0f);
    }

    void TryAttack()
    {
        if (isAttacking) return;
        if (isStaggered) return;
        if (player == null) return;

        if (ShouldUseJumpAttack())
        {
            StartJumpAttack();
            return;
        }

        if (!CanUseMeleeAttack())
            return;

        if (ShouldUseSecondaryAttack())
        {
            StartMeleeAttack(secondaryAttackTrigger, 1f);
            return;
        }

        StartMeleeAttack(attackTrigger, 1f);
    }

    void StartMeleeAttack(string triggerName, float damageMultiplier)
    {
        lastAttackTime = Time.time;
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }

        activeAttackRoutine = StartCoroutine(MeleeAttackRoutine(damageMultiplier));
    }

    void StartJumpAttack()
    {
        lastJumpAttackTime = Time.time;
        lastAttackTime = Time.time;
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(jumpAttackTrigger);
        }

        activeAttackRoutine = StartCoroutine(JumpAttackRoutine());
    }

    bool CanUseMeleeAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    bool CanUseJumpAttack()
    {
        if (!enableJumpAttack) return false;
        return Time.time >= lastJumpAttackTime + jumpAttackCooldown;
    }

    bool ShouldUseJumpAttack()
    {
        if (!CanUseJumpAttack()) return false;
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > jumpAttackRange) return false;
        if (distance < attackRange * 0.7f) return false;

        float chance = jumpAttackChance;

        if (enemyType == EnemyType.Boss && useBossAttackWeights)
        {
            chance = bossJumpAttackChance;
        }

        chance = Mathf.Clamp01(chance);
        if (chance <= 0f) return false;
        if (chance >= 1f) return true;

        return UnityEngine.Random.value <= chance;
    }

    bool ShouldUseSecondaryAttack()
    {
        if (!useSecondaryAttack) return false;

        if (enemyType == EnemyType.Boss && useBossAttackWeights)
        {
            return true;
        }

        float chance = Mathf.Clamp01(secondaryAttackChance);
        if (chance <= 0f) return false;
        if (chance >= 1f) return true;

        return UnityEngine.Random.value <= chance;
    }

    IEnumerator MeleeAttackRoutine(float damageMultiplier)
    {
        yield return new WaitForSeconds(hitWindowStart);

        bool hitDone = false;

        float timer = 0f;

        float duration = hitWindowEnd - hitWindowStart;

        while (timer < duration)
        {
            if (isDead || isStaggered)
            {
                isAttacking = false;
                yield break;
            }

            if (!hitDone && player != null)
            {
                float distance =
                    Vector3.Distance(
                        transform.position,
                        player.transform.position);

                if (distance <= attackRange)
                {
                    DealDamage(damageMultiplier);
                    hitDone = true;
                }
            }

            timer += Time.deltaTime;

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        activeAttackRoutine = null;
    }

    IEnumerator JumpAttackRoutine()
    {
        yield return new WaitForSeconds(jumpWindup);

        if (isDead || isStaggered || player == null)
        {
            isAttacking = false;
            activeAttackRoutine = null;
            yield break;
        }

        if (!TryGetJumpLandingPoint(out Vector3 landingPoint))
        {
            isAttacking = false;
            activeAttackRoutine = null;
            yield break;
        }

        yield return JumpToPointRoutine(landingPoint);

        if (!isDead && !isStaggered && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange + 1f)
            {
                DealDamage(jumpAttackDamageMultiplier);
            }
        }

        yield return new WaitForSeconds(0.15f);

        isAttacking = false;
        activeAttackRoutine = null;
    }

    bool TryGetJumpLandingPoint(out Vector3 landingPoint)
    {
        landingPoint = transform.position;
        if (player == null) return false;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;

        Vector3 direction = toPlayer.sqrMagnitude > 0.01f
            ? toPlayer.normalized
            : transform.forward;

        Vector3 desired = player.transform.position - direction * jumpLandingDistanceFromPlayer;
        desired.y = transform.position.y;

        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, jumpLandingSampleRadius, areaMask))
        {
            landingPoint = hit.position;
            return true;
        }

        if (NavMesh.SamplePosition(player.transform.position, out hit, jumpLandingSampleRadius, areaMask))
        {
            landingPoint = hit.position;
            return true;
        }

        return false;
    }

    IEnumerator JumpToPointRoutine(Vector3 landingPoint)
    {
        Vector3 startPoint = transform.position;
        float duration = Mathf.Max(0.05f, jumpTravelDuration);

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.updatePosition = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isDead || isStaggered)
                break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 horizontal = Vector3.Lerp(startPoint, landingPoint, t);
            float vertical = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
            transform.position = horizontal + Vector3.up * vertical;

            Vector3 lookDir = landingPoint - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    12f * Time.deltaTime
                );
            }

            yield return null;
        }

        transform.position = landingPoint;

        if (agent != null)
        {
            agent.Warp(landingPoint);
            agent.updatePosition = true;
            agent.isStopped = false;
        }
    }

    public void DealDamage(float damageMultiplier = 1f)
    {
        if (isDead) return;
        if (isStaggered) return;
        if (player == null) return;

        player.TakeDMG(attackDamage * damageMultiplier);

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

        currentState = State.Stagger;

        isAttacking = false;

        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = null;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.updatePosition = true;
        }

        SetSpeed(0f);

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.ResetTrigger(secondaryAttackTrigger);
            animator.ResetTrigger(jumpAttackTrigger);

            if (useStaggerAnimation)
            {
                animator.SetTrigger(staggerTrigger);
            }
        }

        yield return new WaitForSeconds(staggerDuration);

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

        currentState = State.Dead;

        StopAllCoroutines();
        activeAttackRoutine = null;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.updatePosition = true;
            agent.enabled = false;
        }

        SideQuestManager.Instance?.ReportEnemyKilled(questCategory);

        if (player != null)
        {
            player.AddExp(expValue);
        }

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.ResetTrigger(secondaryAttackTrigger);
            animator.ResetTrigger(jumpAttackTrigger);

            if (useStaggerAnimation)
            {
                animator.ResetTrigger(staggerTrigger);
            }

            animator.SetTrigger(deathTrigger);
        }

        Died?.Invoke(this);
        Destroy(gameObject, destroyAfterDeath);
    }

    void SetSpeed(float value)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParam, value);
        }
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir =
            player.transform.position - transform.position;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            8f * Time.deltaTime
        );
    }

    QuestEnemyCategory ConvertEnemyTypeToQuestCategory(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Spider:
                return QuestEnemyCategory.Spider;
            case EnemyType.Skeleton:
                return QuestEnemyCategory.Skeleton;
            case EnemyType.Boss:
                return QuestEnemyCategory.Boss;
            default:
                return QuestEnemyCategory.Generic;
        }
    }
}
