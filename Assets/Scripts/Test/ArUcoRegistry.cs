using System.Collections.Generic;
using UnityEngine;

public static class ArUcoRegistry
{
    static readonly Dictionary<int, ArUcoTarget> map = new();

    public static void Register(ArUcoTarget t)
    {
        if (!t) return;
        map[t.markerId] = t;
        Debug.Log($"Registered marker {t.markerId} on {t.name}");
    }

    public static void Unregister(ArUcoTarget t)
    {
        if (!t) return;
        if (map.TryGetValue(t.markerId, out var cur) && cur == t) map.Remove(t.markerId);
    }

    public static bool TryGet(int id, out ArUcoTarget t) => map.TryGetValue(id, out t);
}