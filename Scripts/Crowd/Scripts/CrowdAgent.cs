using UnityEngine;
using UnityEngine.AI;

namespace Zerah.CrowdSim
{

    [RequireComponent(typeof(NavMeshAgent))]
    public class CrowdAgent : MonoBehaviour
    {
        NavMeshAgent agent;
        System.Random rng;

        Transform[] pois;

        Transform[] route;
        int routeIdx = -1;
        bool useRoute;

        public AgentRole role;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (agent.radius <= 0f) agent.radius = 0.3f;
            if (agent.height <= 0f) agent.height = 1.7f;
            if (agent.speed <= 0f) agent.speed = 1.3f;
            if (agent.acceleration <= 0f) agent.acceleration = 4f;
            if (agent.angularSpeed <= 0f) agent.angularSpeed = 720f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public void Init(Transform[] pois, System.Random rng)
        {
            this.pois = pois;
            this.rng = rng ?? new System.Random();

            NextDestination();
        }

        public void SetRoute(Transform[] route)
        {
            this.route = route;
            useRoute = (route != null && route.Length > 0);
            routeIdx = -1;
            NextDestination();
        }

        void Update()
        {
            if (!agent || !agent.enabled) return;

            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                else
                    return;
            }

            if (agent.pathPending) return;

            if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                NextDestination();
            }
        }

        void NextDestination()
        {
            if (!agent || !agent.enabled) return;

            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                else
                    return;
            }

            if (useRoute && route != null && route.Length > 0)
            {
                routeIdx = (routeIdx + 1) % route.Length;
                var p = route[routeIdx].position + Jitter(0.6f);
                agent.SetDestination(p);
            }
            else if (pois != null && pois.Length > 0)
            {
                int i = rng != null ? rng.Next(0, pois.Length) : Random.Range(0, pois.Length);
                var p = pois[i].position + Jitter(0.6f);
                agent.SetDestination(p);
            }

        }

        Vector3 Jitter(float r)
        {
            if (rng == null) return Random.insideUnitCircle * r;
            double a = 2 * System.Math.PI * rng.NextDouble();
            double d = r * System.Math.Sqrt(rng.NextDouble());
            return new Vector3((float)(System.Math.Cos(a) * d), 0f, (float)(System.Math.Sin(a) * d));
        }
    }
}
