using UnityEngine;
using UnityEngine.AI;

public class EnemyRotateFix : MonoBehaviour
{
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent == null || !agent.enabled)
            return;

        Vector3 dir = agent.velocity;

        dir.y = 0f;

        if (dir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(-dir);
        }
    }
}
