using System;
using System.Collections.Generic;

namespace Zerah.CrowdSim
{
    [Serializable]
    public class EventItem
    {
        public float t;
        public string type;
        public string poi;
        public float radius = 6f;
        public float fraction = .5f;
        public float mult = 2f;
        public float duration = 3f;
    }
    [Serializable] public class EventPlan { public List<EventItem> events = new(); }
}
