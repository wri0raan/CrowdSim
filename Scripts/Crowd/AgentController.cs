using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    public Transform target;
    NavMeshAgent _agent;

    Vector3[] _route;
    int _routeIdx = -1;
    Transform _finalGoal;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        EnsureColliderForTriggers();
        SetupAgentDefaults();
    }

    void SetupAgentDefaults()
    {
        _agent.angularSpeed = 720f;
        _agent.acceleration = 12f;
        if (_agent.radius < 0.15f) _agent.radius = 0.25f;
        if (_agent.speed < 0.1f) _agent.speed = 1.35f;
        _agent.stoppingDistance = 0.30f;
        _agent.autoBraking = true;
    }

    void EnsureColliderForTriggers()
    {
        var cc = GetComponent<CapsuleCollider>();
        if (!cc) cc = gameObject.AddComponent<CapsuleCollider>();
        if (cc.height < 1f) { cc.height = 1.7f; cc.center = new Vector3(0, 0.9f, 0); }
        if (cc.radius < 0.15f) cc.radius = 0.25f;

        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Init(Transform goal)
    {
        _finalGoal = null;
        _route = null;
        _routeIdx = -1;
        target = goal;
        if (target)
        {
            _agent.isStopped = false;
            SafeSetDestination(target.position);

        }
    }

    public void InitWithRoute(Vector3[] route, Transform finalGoal)
    {
        _finalGoal = finalGoal;
        _route = route;
        _routeIdx = 0;
        target = null;
        if (_route != null && _route.Length > 0)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_route[_routeIdx]);
        }
        else if (_finalGoal)
        {
            _agent.isStopped = false;
            SafeSetDestination(_finalGoal.position);

        }
    }

    void Update()
    {
        if (_route != null && _routeIdx >= 0)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                _routeIdx++;
                if (_routeIdx < _route.Length)
                {
                    _agent.SetDestination(_route[_routeIdx]);
                }
                else if (_finalGoal)
                {
                    _agent.SetDestination(_finalGoal.position);
                    _route = null; _routeIdx = -1;
                }
                else
                {
                    _agent.isStopped = true;
                    _route = null; _routeIdx = -1;
                }
            }
            return;
        }

        if (target)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
                _agent.isStopped = true;
        }
    }

    public void DespawnImmediate()
    {
        Destroy(gameObject);
    }

    void SafeSetDestination(Vector3 p)
    {
        if (NavMesh.SamplePosition(p, out var hit, 1.0f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
        else
            _agent.SetDestination(p);
    }

}
