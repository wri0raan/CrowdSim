using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Zerah.CrowdSim
{
    public class EventScheduler : MonoBehaviour
    {
        [Header("Inputs")]
        public TextAsset planJson;
        public Transform poiRoot;
        public float timeScale = 1f;

        [Header("State")]
        public bool armed;
        public float startTime;
        public int seed = 12345;

        EventPlan plan;

#if UNITY_2023_1_OR_NEWER
        static T[] FindAll<T>() where T : Object
            => Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        static T[] FindAll<T>() where T : Object
            => Object.FindObjectsOfType<T>();
#endif

        public void ArmAndStart(int seedFromUI = 12345)
        {
            seed = seedFromUI;
            LoadPlan();
            startTime = Time.time;
            armed = true;
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        void LoadPlan()
        {

            if (!planJson || string.IsNullOrWhiteSpace(planJson.text))
            {
                plan = new EventPlan
                {
                    events = new List<EventItem>
                    {
                        new EventItem { t = 0.5f, type = "rush_start",    poi = "GateA",      radius = 6f,  fraction = 0.6f, mult = 2.0f, duration = 6f },
                        new EventItem { t = 3.0f, type = "stumble",       poi = "HallCenter", radius = 3.5f, fraction = 0.15f,               duration = 2f },
                        new EventItem { t = 8.0f, type = "staff_gesture", poi = "Stairs1",    radius = 10f, fraction = 0.4f }
                    }
                };
                return;
            }

            string txt = planJson.text.Trim();
            if (txt.StartsWith("[")) txt = "{\"events\":" + txt + "}";
            plan = JsonUtility.FromJson<EventPlan>(txt) ?? new EventPlan();
            plan.events = plan.events.OrderBy(e => e.t).ToList();
        }

        IEnumerator Run()
        {
            if (plan == null || plan.events == null) yield break;
            foreach (var e in plan.events)
            {
                float when = e.t / Mathf.Max(0.001f, timeScale);
                float wait = startTime + when - Time.time;
                if (wait > 0) yield return new WaitForSeconds(wait);
                TryApply(e);
            }
        }

        void TryApply(EventItem e)
        {
            var p = FindPOI(e.poi);
            if (p == null) return;

            var agents = FindAll<NavMeshAgent>();
            var nearby = new List<NavMeshAgent>();
            float r2 = e.radius * e.radius;

            for (int i = 0; i < agents.Length; i++)
            {
                var a = agents[i];
                if (!a) continue;
                if ((a.transform.position - p.position).sqrMagnitude <= r2)
                    nearby.Add(a);
            }
            if (nearby.Count == 0) return;

            int take = Mathf.Clamp(Mathf.RoundToInt(nearby.Count * Mathf.Clamp01(e.fraction)), 1, nearby.Count);
            var rng = MakeRng(seed, e);
            Shuffle(nearby, rng);

            for (int i = 0; i < take; i++)
            {
                var a = nearby[i];
                var mod = a.GetComponent<AgentModifiers>() ?? a.gameObject.AddComponent<AgentModifiers>();
                switch (e.type)
                {
                    case "rush_start":
                        mod.Rush(e.mult, e.duration);
                        mod.Retarget(p.position);
                        break;
                    case "stumble":
                        mod.Stumble(e.duration);
                        break;
                    case "staff_gesture":
                        mod.Retarget(p.position);
                        break;
                }
            }

            Debug.Log($"[Event] {e.type} @ {e.poi} ({take}/{nearby.Count})");
        }

        Transform FindPOI(string name)
        {
            if (!poiRoot) return null;
            foreach (Transform t in poiRoot)
                if (t && t.name == name) return t;
            return null;
        }

        System.Random MakeRng(int baseSeed, EventItem e)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in (e.type + "|" + e.poi)) h = h * 31 + c;
                int s = baseSeed ^ h ^ Mathf.RoundToInt(e.t * 1000f);
                return new System.Random(s);
            }
        }

        void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
