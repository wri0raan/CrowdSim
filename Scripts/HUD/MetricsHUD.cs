using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Zerah.CrowdSim
{
    public class MetricsHUD : MonoBehaviour
    {
        public TMP_Text label;
        float fpsSmooth;

#if UNITY_2023_1_OR_NEWER
        static T[] FindAll<T>() where T : Object
            => Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        static T[] FindAll<T>() where T : Object
            => Object.FindObjectsOfType<T>();
#endif

        void Update()
        {

            float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            fpsSmooth = Mathf.Lerp(fpsSmooth, fps, 0.1f);

            var agents = FindAll<NavMeshAgent>();
            float avgSpeed = 0f;
            for (int i = 0; i < agents.Length; i++)
                if (agents[i]) avgSpeed += agents[i].velocity.magnitude;
            int n = Mathf.Max(1, agents.Length);
            avgSpeed /= n;

            if (label)
                label.text = $"Agents: {agents.Length}  |  FPS: {fpsSmooth:0}  |  AvgSpeed: {avgSpeed:0.00} m/s";
        }
    }
}
