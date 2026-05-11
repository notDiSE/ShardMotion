using System.Collections.Generic;

namespace ShardMotion
{
    /// <summary>
    /// Global register of all active ArUcoTargets in scene 
    /// </summary>
    public static class ArUcoRegistry
    {
        static readonly Dictionary<int, ArUcoTarget> map = new(); // map used to lookup target based on ID
        public static IEnumerable<ArUcoTarget> All => map.Values; // pulls all the values from map

        /// <summary>
        /// Register new active Target
        /// </summary>
        /// <param name="t"> reference to target</param>
        public static void Register(ArUcoTarget t)
        {
            if (!t) return; // null reference check
            map[t.markerId] = t; // save under the id key
        }

        /// <summary>
        /// unregister Target as active 
        /// </summary>
        /// <param name="t">reference to target</param>
        public static void Unregister(ArUcoTarget t)
        {
            if (!t) return; // null reference check
            if (map.TryGetValue(t.markerId, out var cur) && cur == t) // if the id under which the target is registered and the reference match
            {
                map.Remove(t.markerId); // remove from map
            }
        }

        public static bool TryGet(int id, out ArUcoTarget t) => map.TryGetValue(id, out t); // tries to get the reference to target from map
    }
}