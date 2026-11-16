using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Zerah.CrowdSim
{
    public class SpawnManager : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject agentPrefab;
        public Transform poiRoot;

        [Header("UI")]
        public TMP_InputField populationInput;
        public TMP_InputField seedInput;
        public Button startButton;
        public Button resetButton;

        [Header("Spawn Settings")]
        public Transform fallbackCenter;
        public float spawnRadius = 8f;

        [Header("Role Distribution (0~1)")]
        public float pctOutbound = 0.5f;
        public float pctInbound = 0.4f;
        public float pctStaff = 0.1f;

        [Header("Event Scheduler (optional)")]
        public EventScheduler scheduler;

        [Header("Safety")]
        public bool killSceneAgentsOnStart = true;

        [Header("Random Prefab Source")]
        public bool useResources = true;
        public string resourcesPath = "Crowd/RPM";
        readonly List<GameObject> _prefabs = new();

        readonly List<GameObject> _spawned = new();
        readonly List<Material> _spawnedMats = new();

        Transform[] _pois;
        Transform _gate, _hall, _stairs;
        Material _baseMat;

        void Awake()
        {

            if (poiRoot != null)
            {
                var list = new List<Transform>();
                foreach (Transform t in poiRoot) list.Add(t);
                _pois = list.ToArray();

                _gate = FindPOI("GateA");
                _hall = FindPOI("HallCenter");
                _stairs = FindPOI("Stairs1");
            }

            if (startButton) startButton.onClick.AddListener(UI_Start);
            if (resetButton) resetButton.onClick.AddListener(UI_Reset);

            if (useResources)
            {
                _prefabs.Clear();
                _prefabs.AddRange(Resources.LoadAll<GameObject>(resourcesPath));
                if (_prefabs.Count == 0)
                    Debug.LogError($"[SpawnManager] Resources/{resourcesPath} 에 프리팹이 없습니다. (Tools> Crowd > 3) 실행했는지 확인)");
            }
            else
            {
                if (agentPrefab) _prefabs.Add(agentPrefab);
                else Debug.LogError("[SpawnManager] Agent Prefab 비어있음");
            }

        }

        void Start()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            _baseMat = new Material(shader);
        }

        public void UI_Start() => OnStartClicked();
        public void UI_Reset() => ResetSpawned();

        void OnStartClicked()
        {
            int count = 100;
            if (populationInput && !string.IsNullOrWhiteSpace(populationInput.text))
                int.TryParse(populationInput.text, out count);

            int seed = 12345;
            if (seedInput && !string.IsNullOrWhiteSpace(seedInput.text))
                int.TryParse(seedInput.text, out seed);

            if (_prefabs.Count == 0) { Debug.LogError("[SpawnManager] 스폰할 프리팹이 없습니다."); return; }
            if (_pois == null || _pois.Length == 0) { Debug.LogError("[SpawnManager] POIs 비어있음"); return; }

            if (killSceneAgentsOnStart)
            {
#if UNITY_2023_1_OR_NEWER
                var existing = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
#else
                var existing = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
#endif
                foreach (var a in existing)
                    if (a && a.gameObject.scene.IsValid())
                        Destroy(a.gameObject);
            }

            ResetSpawned();

            var rng = new System.Random(seed);

            Vector3 center = poiRoot ? poiRoot.position : Vector3.zero;
            if (fallbackCenter) center = fallbackCenter.position;
            else if (_hall) center = _hall.position;

            float sum = Mathf.Max(0.0001f, pctOutbound + pctInbound + pctStaff);
            float p0 = pctOutbound / sum, p1 = pctInbound / sum;
            float c0 = p0;
            float c1 = p0 + p1;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = RandomPointOnNavMesh(center, spawnRadius, rng);

                var prefab = _prefabs[rng.Next(0, _prefabs.Count)];
                var go = Instantiate(prefab, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360.0), 0));

                _spawned.Add(go);

                var nav = go.GetComponent<NavMeshAgent>();
                if (nav)
                {
                    nav.avoidancePriority = 10 + rng.Next(0, 80);
                    nav.speed = Mathf.Clamp((float)(1.3 + NextGaussian(rng, 0, 0.15)), 0.9f, 2.2f);
                }

                var ca = go.GetComponent<CrowdAgent>();
                if (ca != null) ca.Init(_pois, rng);

                double u = rng.NextDouble();
                AgentRole role = (u < c0) ? AgentRole.Outbound : (u < c1) ? AgentRole.Inbound : AgentRole.Staff;
                ApplyRole(go, role, rng);

                var rend = go.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    var m = new Material(_baseMat);
                    m.color = RoleColor(role);
                    _spawnedMats.Add(m);
                    rend.material = m;
                }
            }

            Debug.Log($"[SpawnManager] Spawned {count} (Seed={seed})");

            if (scheduler) scheduler.ArmAndStart(seed);
        }

        public void ResetSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
                if (_spawned[i]) Destroy(_spawned[i]);
            _spawned.Clear();

            for (int i = 0; i < _spawnedMats.Count; i++)
                if (_spawnedMats[i]) Destroy(_spawnedMats[i]);
            _spawnedMats.Clear();

            Debug.Log("[SpawnManager] Reset done.");
        }

        void ApplyRole(GameObject go, AgentRole role, System.Random rng)
        {
            var ca = go.GetComponent<CrowdAgent>();
            var nav = go.GetComponent<NavMeshAgent>();
            if (ca) ca.role = role;

            Transform[] route = role switch
            {
                AgentRole.Outbound => new Transform[] { _gate ?? _hall, _hall ?? _gate, _stairs ?? _hall },
                AgentRole.Inbound => new Transform[] { _stairs ?? _hall, _hall ?? _gate, _gate ?? _hall },
                _ => new Transform[] { _hall ?? _gate, _stairs ?? _gate },
            };
            if (ca) ca.SetRoute(route);

            if (nav)
            {
                double dv = NextGaussian(rng, 0, 0.12);
                float baseV = role switch
                {
                    AgentRole.Outbound => 1.5f,
                    AgentRole.Inbound => 1.3f,
                    _ => 1.2f
                };
                nav.speed = Mathf.Clamp(baseV + (float)dv, 0.9f, 2.6f);
            }
        }

        Color RoleColor(AgentRole r) => r switch
        {
            AgentRole.Outbound => new Color(0.95f, 0.35f, 0.35f),
            AgentRole.Inbound => new Color(0.35f, 0.7f, 0.95f),
            _ => new Color(0.5f, 0.95f, 0.5f)
        };

        Transform FindPOI(string name)
        {
            if (!poiRoot) return null;
            foreach (Transform t in poiRoot)
                if (t && t.name == name) return t;
            return null;
        }

        Vector3 RandomPointOnNavMesh(Vector3 center, float radius, System.Random rng)
        {
            double th = 2 * Math.PI * rng.NextDouble();
            double r = radius * Math.Sqrt(rng.NextDouble());
            Vector3 raw = new Vector3(center.x + (float)(r * Math.Cos(th)), center.y,
                                      center.z + (float)(r * Math.Sin(th)));
            return NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : center;
        }

        double NextGaussian(System.Random rng, double mean = 0, double stdDev = 1.0)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * z0;
        }
    }
}
