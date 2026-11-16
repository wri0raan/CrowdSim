using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class CrowdAgentAnimator : MonoBehaviour
{

    [Header("Move Speeds (m/s)")]
    public float walkSpeed = 1.4f;
    public float runSpeed = 2.8f;

    [Header("Smoothing")]
    public float dampTime = 0.15f;

    Animator anim;
    NavMeshAgent agent;
    bool isRunning;

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = walkSpeed;
        agent.acceleration = 5f;
        agent.angularSpeed = 360f;

        anim.SetFloat("Speed", 1f);
    }

    void Update()
    {

        bool moving = agent.velocity.sqrMagnitude > 0.01f;

        float target = 0f;
        if (isRunning) target = 2f;
        else if (moving) target = 1f;

        anim.SetFloat("Speed", target, dampTime, Time.deltaTime);

        float denom = isRunning ? runSpeed : walkSpeed;
        float v = agent.velocity.magnitude;
        anim.speed = Mathf.Clamp01(denom > 0 ? v / denom : 1f) * 1.05f;
    }

    public void SetRunning(bool run)
    {
        isRunning = run;
        agent.speed = run ? runSpeed : walkSpeed;
        agent.acceleration = run ? 8f : 5f;
        agent.angularSpeed = run ? 420f : 360f;
    }
}
