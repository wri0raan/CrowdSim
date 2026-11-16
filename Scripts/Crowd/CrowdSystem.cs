using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CrowdSystem : MonoBehaviour
{
    [Header("Prefabs (Resources)")]
    [Tooltip("Resources/Crowd/RPM 폴더 사용 권장")]
    public string resourcesFolder = "Crowd/RPM";

    [Header("Spawn Options")]
    public float snapRadius = 0.6f;
    public float spawnLiftY = 0.02f;
    public int populationLimit = 1000;

    [Header("DownStair Lanes")]
    [Tooltip("내려가는 계단 줄 수")]
    public int laneCount = 3;
    [Tooltip("줄 간격(m)")]
    public float laneSpacing = 0.6f;
    [Tooltip("계단 방향으로 중간 경유점 개수")]
    public int laneSegmentCount = 6;
    [Tooltip("세그먼트 간 거리(m)")]
    public float laneSegmentStep = 0.9f;

    [Header("Debug")]
    public bool autoSpawnOnPlay = false;
    public int debugPopulation = 120;
    public int debugSeed = 0;
    public bool drawTriggerGizmos = true;

    readonly List<Transform> _spawn_ToB1 = new();
    readonly List<Transform> _spawn_DownB1 = new();
    readonly List<Transform> _exit_To1 = new();
    readonly List<Transform> _exit_ToB2 = new();

    GameObject[] _prefabs;
    readonly List<GameObject> _spawned = new();

    void Awake()
    {
        _prefabs = Resources.LoadAll<GameObject>(resourcesFolder);
        RefreshTriggerMap(addMissingComponents: true);
    }

    void Start()
    {
        if (autoSpawnOnPlay) SpawnPopulation(debugPopulation, debugSeed);
    }

    public void RefreshTriggerMap(bool addMissingComponents)
    {
        _spawn_ToB1.Clear();
        _spawn_DownB1.Clear();
        _exit_To1.Clear();
        _exit_ToB2.Clear();

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))) continue;
#endif
            if (!go.scene.IsValid()) continue;

            var n = go.name.ToLower();
            if (!(n.StartsWith("spawn_tob1") || n.StartsWith("spawn_downb1")
                || n.StartsWith("exit_to1") || n.StartsWith("exit_1")
                || n.StartsWith("exit_tob2") || n.StartsWith("exit_b2")))
                continue;

            var tz = go.GetComponent<TriggerZone>();
            if (!tz && addMissingComponents) tz = go.AddComponent<TriggerZone>();
            if (!tz) continue;

            switch (tz.kind)
            {
                case CrowdTriggerKind.Spawn_ToB1: _spawn_ToB1.Add(go.transform); break;
                case CrowdTriggerKind.Spawn_DownB1: _spawn_DownB1.Add(go.transform); break;
                case CrowdTriggerKind.Exit_To1: _exit_To1.Add(go.transform); break;
                case CrowdTriggerKind.Exit_ToB2: _exit_ToB2.Add(go.transform); break;
            }
        }

        _spawn_ToB1.Sort((a, b) => a.name.CompareTo(b.name));
        _spawn_DownB1.Sort((a, b) => a.name.CompareTo(b.name));
        _exit_To1.Sort((a, b) => a.name.CompareTo(b.name));
        _exit_ToB2.Sort((a, b) => a.name.CompareTo(b.name));
    }

    public void ClearAll()
    {
        for (int i = _spawned.Count - 1; i >= 0; --i)
            if (_spawned[i]) Destroy(_spawned[i]);
        _spawned.Clear();
    }

    public void SpawnPopulation(int total, int seed)
    {
        RefreshTriggerMap(addMissingComponents: true);

        total = Mathf.Clamp(total, 0, populationLimit);
        if (total <= 0) return;

        if ((_spawn_ToB1.Count + _spawn_DownB1.Count) == 0)
        {
            Debug.LogWarning("[CrowdSystem] Spawn 포인트가 없습니다. Spawn_ToB1_(n) 또는 Spawn_DownB1_(n)을 배치하세요.");
            return;
        }

        var rng = (seed == 0) ? new System.Random(System.Environment.TickCount) : new System.Random(seed);

        var sources = new List<(CrowdTriggerKind kind, Transform tr)>();
        foreach (var t in _spawn_ToB1) sources.Add((CrowdTriggerKind.Spawn_ToB1, t));
        foreach (var t in _spawn_DownB1) sources.Add((CrowdTriggerKind.Spawn_DownB1, t));

        int idx = 0;
        var laneCursor = new Dictionary<Transform, int>();

        for (int i = 0; i < total; i++)
        {
            var (kind, tr) = sources[idx % sources.Count];
            idx++;

            switch (kind)
            {
                case CrowdTriggerKind.Spawn_ToB1:
                    SpawnFrom_ToB1(tr, rng);
                    break;
                case CrowdTriggerKind.Spawn_DownB1:
                    if (!laneCursor.TryGetValue(tr, out var lane)) lane = 0;
                    SpawnFrom_DownB1(tr, lane, rng);
                    lane = (lane + 1) % Mathf.Max(1, laneCount);
                    laneCursor[tr] = lane;
                    break;
            }
        }
    }

    void SpawnFrom_ToB1(Transform tr, System.Random rng)
    {
        if (_exit_To1 == null || _exit_To1.Count == 0)
        {
            Debug.LogWarning("[CrowdSystem] Exit_To1/Exit_1 이 없습니다.");
            return;
        }

        var prefab = PickPrefab(rng); if (!prefab) return;

        var pos = SnapToNavmesh(tr.position + Vector3.up * spawnLiftY, snapRadius, out var ok);
        if (!ok) pos = tr.position + Vector3.up * spawnLiftY;

        var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
        var go = Instantiate(prefab, pos, rot, transform);
        _spawned.Add(go);

        var ctrl = go.GetComponent<AgentController>() ?? go.AddComponent<AgentController>();

        var goal = NearestTransform(pos, _exit_To1);
        ctrl.Init(goal);
    }

    void SpawnFrom_DownB1(Transform anchor, int laneIndex, System.Random rng)
    {
        if (_exit_ToB2 == null || _exit_ToB2.Count == 0)
        {
            Debug.LogWarning("[CrowdSystem] Exit_ToB2/Exit_B2 가 없습니다.");
            return;
        }

        var prefab = PickPrefab(rng); if (!prefab) return;

        var right = anchor.right;
        var fwd = anchor.forward;

        float centerOffset = (laneCount - 1) * 0.5f * laneSpacing;
        float lateral = laneIndex * laneSpacing - centerOffset;

        var spawnPos = anchor.position + right * lateral;
        spawnPos = SnapToNavmesh(spawnPos + Vector3.up * spawnLiftY, snapRadius, out var ok);
        if (!ok) spawnPos = anchor.position + right * lateral + Vector3.up * spawnLiftY;

        var go = Instantiate(prefab, spawnPos, Quaternion.LookRotation(fwd, Vector3.up), transform);
        _spawned.Add(go);

        var points = new List<Vector3>();
        for (int s = 1; s <= laneSegmentCount; s++)
        {
            var p = anchor.position + right * lateral + fwd * (s * laneSegmentStep);
            p = SnapToNavmesh(p, snapRadius, out ok);
            if (!ok) p += Vector3.up * spawnLiftY;
            points.Add(p);
        }

        var ctrl = go.GetComponent<AgentController>() ?? go.AddComponent<AgentController>();

        var final = NearestTransform(points.Count > 0 ? points[^1] : spawnPos, _exit_ToB2);
        ctrl.InitWithRoute(points.ToArray(), final);
    }

    GameObject PickPrefab(System.Random rng)
    {
        if (_prefabs == null || _prefabs.Length == 0) return null;
        if (_prefabs.Length == 1) return _prefabs[0];
        return _prefabs[rng.Next(0, _prefabs.Length)];
    }

    Vector3 SnapToNavmesh(Vector3 src, float radius, out bool ok)
    {
        ok = NavMesh.SamplePosition(src, out var hit, radius, NavMesh.AllAreas);
        return ok ? hit.position + Vector3.up * spawnLiftY : src;
    }

    Transform NearestTransform(Vector3 from, List<Transform> list)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (var t in list)
        {
            var s = (t.position - from).sqrMagnitude;
            if (s < bestSqr) { bestSqr = s; best = t; }
        }
        return best;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawTriggerGizmos) return;
        DrawFor("spawn_tob1", new Color(1f, 0f, 0f, 0.25f), new Color(1f, 0f, 0f, 1f));
        DrawFor("spawn_downb1", new Color(0f, 0.6f, 1f, 0.25f), new Color(0f, 0.6f, 1f, 1f));
        DrawFor("exit_to1", new Color(0f, 1f, 0f, 0.25f), new Color(0f, 1f, 0f, 1f));
        DrawFor("exit_1", new Color(0f, 1f, 0f, 0.25f), new Color(0f, 1f, 0f, 1f));
        DrawFor("exit_tob2", new Color(1f, 0.9f, 0f, 0.25f), new Color(1f, 0.9f, 0f, 1f));
        DrawFor("exit_b2", new Color(1f, 0.9f, 0f, 0.25f), new Color(1f, 0.9f, 0f, 1f));
    }

    void DrawFor(string startsWithLower, Color fill, Color line)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go || !go.scene.IsValid()) continue;
            var n = go.name.ToLower();
            if (!n.StartsWith(startsWithLower)) continue;

            var bc = go.GetComponent<BoxCollider>(); if (!bc) continue;
            var m = Matrix4x4.TRS(go.transform.position, go.transform.rotation, Vector3.one);
            Gizmos.matrix = m;
            Gizmos.color = fill; Gizmos.DrawCube(bc.center, bc.size);
            Gizmos.color = line; Gizmos.DrawWireCube(bc.center, bc.size);
#if UNITY_EDITOR
            Handles.Label(go.transform.position + Vector3.up * 1.6f, go.name);
#endif
        }
    }
#endif
}
