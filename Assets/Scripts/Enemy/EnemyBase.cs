using UnityEngine;
using UnityEngine.AI;
using Player;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float maxHp = 20f;
    float currentHp;

    [SerializeField] float expValue = 50f;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Detection")]
    public float detectionRange = 10f;

    private float lastAttackTime;

    private PlayerBase player;
    private NavMeshAgent agent;
    private Animator animator;

    private enum State
    {
        Idle,
        Chase,
        Attack
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
        if (player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        switch (currentState)
        {
            case State.Idle:

                if (distance <= detectionRange)
                {
                    currentState = State.Chase;
                }

                break;

            case State.Chase:

                agent.SetDestination(player.transform.position);
                animator.SetFloat("Speed", agent.velocity.magnitude);

                if (distance <= attackRange)
                {
                    currentState = State.Attack;
                }

                break;

            case State.Attack:

                agent.SetDestination(transform.position);
                animator.SetFloat("Speed", 0f);

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
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

       // if (animator != null)
       // {
       //     animator.SetTrigger("Attack");
       // }

        Debug.Log("Enemy attack triggered");
    }

    public void DealDamage()
    {
        if (player != null)
        {
            player.TakeDMG(attackDamage);
            Debug.Log("Enemy dealt damage!");
        }
    }

    public void TakeDamage(float dmg)
    {
        currentHp -= dmg;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (player != null)
        {
            player.AddExp(expValue);
        }

        Destroy(gameObject);
    }
}