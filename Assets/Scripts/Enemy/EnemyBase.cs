using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Player;
using SideQuests;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float maxHp = 20f;
    private float currentHp;

    [SerializeField] private float expValue = 50f;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

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
    public string deathTrigger = "Death";

    [Header("Death")]
    public float destroyAfterDeath = 3f;

    private float lastAttackTime = -999f;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isStaggered = false;

    private PlayerBase player;
    private NavMeshAgent agent;
    private Animator animator;

    public QuestEnemyCategory questCategory = QuestEnemyCategory.Generic;

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

        player = FindObjectOfType<PlayerBase>();

        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();

        wanderTimer = wanderInterval;

        currentState = useWander ? State.Wander : State.Idle;
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
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
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

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
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

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
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

            if (useStaggerAnimation)
            {
                animator.ResetTrigger(staggerTrigger);
            }

            animator.SetTrigger(deathTrigger);
        }

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
}