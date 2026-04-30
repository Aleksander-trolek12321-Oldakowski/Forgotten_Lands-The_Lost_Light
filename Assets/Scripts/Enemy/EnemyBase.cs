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

    [Header("Detection")]
    public float detectionRange = 10f;

    [Header("Animation Parameters")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string deathTrigger = "Die";

    [Header("Death")]
    public float destroyAfterDeath = 3f;

    private float lastAttackTime = -999f;
    private bool isDead = false;

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
                agent.SetDestination(transform.position);

                SetSpeed(0f);

                LookAtPlayer();

                if (distance > attackRange)
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
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        Debug.Log(name + " attack triggered");
    }

    public void DealDamage()
    {
        if (isDead) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= attackRange + 0.5f)
        {
            player.TakeDMG(attackDamage);
            Debug.Log(name + " attacked");
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHp -= dmg;

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = State.Dead;

        if (agent != null)
        {
            agent.isStopped = true;
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

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                8f * Time.deltaTime
            );
        }
    }
}