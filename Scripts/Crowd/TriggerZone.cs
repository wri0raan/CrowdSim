using UnityEngine;

public enum CrowdTriggerKind { None, Spawn_ToB1, Spawn_DownB1, Exit_To1, Exit_ToB2 }

[DisallowMultipleComponent]
public class TriggerZone : MonoBehaviour
{
    public CrowdTriggerKind kind = CrowdTriggerKind.None;

    void Reset() { AutoDetectKindByName(); EnsureCollider(); EnsureRigidbody(); }
    void Awake() { AutoDetectKindByName(); EnsureCollider(); EnsureRigidbody(); }

    void AutoDetectKindByName()
    {
        var n = name.ToLower();
        if (n.StartsWith("spawn_tob1")) kind = CrowdTriggerKind.Spawn_ToB1;
        else if (n.StartsWith("spawn_downb1")) kind = CrowdTriggerKind.Spawn_DownB1;
        else if (n.StartsWith("exit_to1") || n.StartsWith("exit_1")) kind = CrowdTriggerKind.Exit_To1;
        else if (n.StartsWith("exit_tob2") || n.StartsWith("exit_b2")) kind = CrowdTriggerKind.Exit_ToB2;
        else kind = CrowdTriggerKind.None;
    }

    void EnsureCollider()
    {
        var bc = GetComponent<BoxCollider>();
        if (!bc) bc = gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        if (bc.size == Vector3.zero || bc.size.magnitude < 0.01f)
            bc.size = new Vector3(3.0f, 2.0f, 1.0f);
        if (bc.center == Vector3.zero)
            bc.center = new Vector3(0, 1.0f, 0);
    }

    void EnsureRigidbody()
    {
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (kind != CrowdTriggerKind.Exit_To1 && kind != CrowdTriggerKind.Exit_ToB2) return;

        var agent = other.GetComponentInParent<AgentController>();
        if (!agent) agent = other.GetComponent<AgentController>();
        if (!agent) return;

        agent.DespawnImmediate();
    }
    void OnTriggerStay(Collider other) => OnTriggerEnter(other);

}
