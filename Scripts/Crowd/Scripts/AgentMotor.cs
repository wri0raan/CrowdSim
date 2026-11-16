using UnityEngine;
using UnityEngine.AI;

public class AgentMotor : MonoBehaviour
{
    public Vector2 areaX = new(-30, 30);
    public Vector2 areaZ = new(-30, 30);
    public float minWait = 0.5f;
    public float maxWait = 2.0f;

    NavMeshAgent agent;
    float waitUntil;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Random.Range(1.2f, 2.8f);
        Next();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (Time.time >= waitUntil) Next();
        }
    }

    void Next()
    {
        var p = new Vector3(Random.Range(areaX.x, areaX.y), 0, Random.Range(areaZ.x, areaZ.y));
        if (NavMesh.SamplePosition(p, out var hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            waitUntil = Time.time + Random.Range(minWait, maxWait);
        }
    }
}
