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

    [Header("Stagger")]
    public bool canBeStaggered = true;

    [Tooltip("Jak długo enemy jest zatrzymany po trafieniu")]
    public float staggerDuration = 1f;

    [Tooltip("Czy enemy ma animację staggera")]
    public bool useStaggerAnimation = false;

    public string staggerTrigger = "Stagger";

    [Header("Animation Parameters")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string deathTrigger = "Die";

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
        Chase,
        Attack,
        Dead
    }

    private State currentState;

    void Start()
    {
        currentHp = maxHp;

        player = FindObjectOfType<PlayerBase>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        currentState = State.Idle;
    }

    void Update()
    {
        if (isDead) return;
        if (isStaggered) return;
        if (player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        switch (currentState)
        {
            case State.Idle:

                SetSpeed(0f);

                if (distance <= detectionRange)
                {
                    currentState = State.Chase;
                }

                break;

            case State.Chase:

                agent.isStopped = false;

                agent.SetDestination(player.transform.position);

                SetSpeed(agent.velocity.magnitude);

                if (distance <= attackRange)
                {
                    currentState = State.Attack;
                }

                break;

            case State.Attack:

                agent.isStopped = true;

                SetSpeed(0f);

                LookAtPlayer();

                if (distance > attackRange + 1f)
                {
                    currentState = State.Chase;
                    break;
                }

                TryAttack();

                break;
        }
    }

    void TryAttack()
    {
        if (isAttacking) return;
        if (isStaggered) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

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

            LookAtPlayer();

            if (!hitDone && player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance <= attackRange)
                {
                    DealDamage();
                    hitDone = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

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

        // 🔥 śmierć ma priorytet nad staggerem
        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        // 🔥 stagger tylko jeśli enemy przeżył
        if (canBeStaggered)
        {
            StartCoroutine(StaggerRoutine());
        }
    }

    IEnumerator StaggerRoutine()
    {
        // 🔥 zabezpieczenie przed spamem
        if (isStaggered) yield break;

        isStaggered = true;

        // 🔥 przerywa atak
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        SetSpeed(0f);

        // 🔥 opcjonalna animacja
        if (useStaggerAnimation && animator != null)
        {
            animator.SetTrigger(staggerTrigger);
        }

        yield return new WaitForSeconds(staggerDuration);

        if (isDead) yield break;

        isStaggered = false;

        // 🔥 reset AI po staggerze
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
            agent.enabled = false;
        }

        SideQuestManager.Instance?.ReportEnemyKilled(questCategory);

        if (player != null)
        {
            player.AddExp(expValue);
        }

        if (animator != null)
        {
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
        if (isStaggered) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            8f * Time.deltaTime
        );
    }
}