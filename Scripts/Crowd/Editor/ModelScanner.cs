using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class ModelScanner
{
    const string ROOT = "Assets/Avatars/Models";

    [MenuItem("Tools/Crowd/0) Report RPM Models")]
    public static void Report()
    {
        AssetDatabase.Refresh();

        var guids = AssetDatabase.FindAssets("", new[] { ROOT });
        var glbs = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToList();

        Debug.Log($"[ModelScanner] {ROOT} 아래 .glb 개수: {glbs.Count}");
        foreach (var p in glbs.Take(50)) Debug.Log(" - " + p);
        if (glbs.Count == 0) Debug.LogWarning($"[ModelScanner] {ROOT}에서 .glb를 못 찾음. 경로/확장자 확인.");
    }
}
