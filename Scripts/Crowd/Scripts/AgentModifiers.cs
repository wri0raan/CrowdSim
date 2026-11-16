using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Zerah.CrowdSim
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentModifiers : MonoBehaviour
    {
        NavMeshAgent agent;
        float baseSpeed; bool hasBase;
        Coroutine running;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (!hasBase) { baseSpeed = agent.speed; hasBase = true; }
        }

        public void Rush(float mult, float duration)
        {
            if (!hasBase) { baseSpeed = agent.speed; hasBase = true; }
            if (running != null) StopCoroutine(running);
            agent.speed = Mathf.Max(0.1f, baseSpeed * mult);
            running = StartCoroutine(RevertSpeed(duration));
        }

        IEnumerator RevertSpeed(float d)
        {
            yield return new WaitForSeconds(d);
            agent.speed = baseSpeed;
            running = null;
        }

        public void Stumble(float duration)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(StumbleCo(duration));
        }
        IEnumerator StumbleCo(float d)
        {
            bool prev = agent.isStopped; agent.isStopped = true;
            yield return new WaitForSeconds(d);
            agent.isStopped = prev; running = null;
        }

        public void Retarget(Vector3 dest)
        {
            agent.SetDestination(dest);
        }
    }
}
