using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Zerah.CrowdSim
{

    public class CrowdSpawner : MonoBehaviour
    {
        [Header("Counts")]
        public int nearCount = 30;
        public int farCount = 70;

        [Header("Spawn Areas (실내 스폰 허용 구역)")]
        public List<BoxCollider> spawnAreas = new List<BoxCollider>();

        [Header("Legacy Fallback (안 쓰는 걸 권장)")]
        public Vector2 areaX = new(-25, 25);
        public Vector2 areaZ = new(-25, 25);

        [Header("POIs / Goals (기존 랜덤 목적지, 폴백용)")]
        public Transform poiRoot;
        public List<Transform> poiTargets = new List<Transform>();

        [Header("Exit Weighted Routing (B2 vs 1)")]
        [Tooltip("체크 시: 스폰된 에이전트를 B2/1 출구 그룹 중 하나로 가중치에 따라 보냄")]
        public bool routeWeightedToExits = true;

        [Range(0, 100)] public int weightB2 = 70;
        [Range(0, 100)] public int weight1 = 30;

        [Tooltip("B2 출구들의 부모 Transform (선택). 지정하면 자식들을 자동 수집.")]
        public Transform exitB2Root;
        [Tooltip("1번 출구들의 부모 Transform (선택). 지정하면 자식들을 자동 수집.")]
        public Transform exit1Root;

        [Tooltip("B2 출구 Transform들을 직접 여러 개 넣을 수도 있음.")]
        public List<Transform> exitB2List = new();
        [Tooltip("1번 출구 Transform들을 직접 여러 개 넣을 수도 있음.")]
        public List<Transform> exit1List = new();

        [Header("Camera")]
        public Camera mainCam;

        readonly List<GameObject> prefabs = new();
        Transform holderNear, holderFar;

        System.Random _rng = new System.Random();

        void Awake()
        {
            holderNear = new GameObject("CROWD_NEAR").transform;
            holderFar = new GameObject("CROWD_FAR").transform;

            foreach (var p in Resources.LoadAll<GameObject>("Crowd/RPM"))
                if (p) prefabs.Add(p);

            if (prefabs.Count == 0)
                Debug.LogError("Resources/Crowd/RPM 에 프리팹이 없습니다.");

            if (!mainCam) mainCam = Camera.main;

            if (poiRoot && poiTargets.Count == 0)
            {
                foreach (Transform t in poiRoot)
                    if (t && t.gameObject.activeInHierarchy) poiTargets.Add(t);
            }

            if (exitB2Root && exitB2List.Count == 0)
                foreach (Transform t in exitB2Root)
                    if (t && t.gameObject.activeInHierarchy) exitB2List.Add(t);

            if (exit1Root && exit1List.Count == 0)
                foreach (Transform t in exit1Root)
                    if (t && t.gameObject.activeInHierarchy) exit1List.Add(t);
        }

        void Start()
        {

            Spawn(nearCount, holderNear);
            Spawn(farCount, holderFar, forceFar: true);
        }

        void Spawn(int count, Transform parent, bool forceFar = false)
        {
            if (prefabs.Count == 0) return;

            for (int i = 0; i < count; i++)
            {

                if (!TryGetSpawnPoint(out var hit)) continue;

                var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
                var rotY = UnityEngine.Random.Range(0f, 360f);
                var go = Instantiate(prefab, hit.position, Quaternion.Euler(0, rotY, 0), parent);

                var agent = go.GetComponent<NavMeshAgent>();
                if (!agent) agent = go.AddComponent<NavMeshAgent>();
                agent.Warp(hit.position);

                var ca = go.GetComponent<CrowdAgent>();
                if (ca)
                {

                    if (routeWeightedToExits && (exitB2List.Count > 0 || exit1List.Count > 0))
                    {
                        var chosen = ChooseExitByWeight();
                        if (chosen)
                        {

                            ca.SetRoute(new Transform[] { chosen });
                        }
                        else
                        {

                            Transform[] pois = (poiTargets != null && poiTargets.Count > 0)
                                ? poiTargets.ToArray()
                                : null;
                            ca.Init(pois, _rng);
                        }
                    }
                    else
                    {

                        Transform[] pois = (poiTargets != null && poiTargets.Count > 0)
                            ? poiTargets.ToArray()
                            : null;
                        ca.Init(pois, _rng);
                    }
                }

                if (forceFar)
                {
                    var lod = go.GetComponent<LODGroup>();
                    if (lod)
                    {
                        go.transform.position += Vector3.back * 9999f;
                        var motor = go.GetComponent<AgentMotor>();
                        if (motor) motor.enabled = false;
                        StartCoroutine(ForceFar(go, hit.position));
                    }
                }
            }
        }

        Transform ChooseExitByWeight()
        {
            int wB2 = Mathf.Max(0, weightB2);
            int w1 = Mathf.Max(0, weight1);
            int total = wB2 + w1;

            bool hasB2 = exitB2List.Count > 0;
            bool has1 = exit1List.Count > 0;

            if (!hasB2 && !has1) return null;
            if (hasB2 && !has1) return exitB2List[UnityEngine.Random.Range(0, exitB2List.Count)];
            if (!hasB2 && has1) return exit1List[UnityEngine.Random.Range(0, exit1List.Count)];

            if (total <= 0) total = 1;
            float r = UnityEngine.Random.Range(0f, total);
            bool pickB2 = r < wB2;

            var list = pickB2 ? exitB2List : exit1List;
            if (list.Count == 0) list = pickB2 ? exit1List : exitB2List;
            if (list.Count == 0) return null;

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        System.Collections.IEnumerator ForceFar(GameObject go, Vector3 pos)
        {
            yield return null;
            if (go)
            {
                go.transform.position = pos;
                var motor = go.GetComponent<AgentMotor>();
                if (motor) motor.enabled = true;
            }
        }

        bool TryGetSpawnPoint(out NavMeshHit hit)
        {
            if (spawnAreas != null && spawnAreas.Count > 0)
            {
                for (int t = 0; t < 24; t++)
                {
                    var zone = spawnAreas[UnityEngine.Random.Range(0, spawnAreas.Count)];
                    if (!zone) continue;

                    var b = zone.bounds;
                    var candidate = new Vector3(
                        UnityEngine.Random.Range(b.min.x, b.max.x),
                        b.center.y,
                        UnityEngine.Random.Range(b.min.z, b.max.z)
                    );

                    if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
                        return true;
                }
            }

            for (int t = 0; t < 24; t++)
            {
                var candidate = new Vector3(
                    UnityEngine.Random.Range(areaX.x, areaX.y),
                    0f,
                    UnityEngine.Random.Range(areaZ.x, areaZ.y)
                );
                if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
                    return true;
            }

            hit = default;
            return false;
        }
    }
}
