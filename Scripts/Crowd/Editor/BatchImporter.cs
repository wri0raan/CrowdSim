using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

public static class BatchImporter
{
    static readonly Regex rpmName = new(@"(Fe|Ma)[1-4]\s*\(\d+\)", RegexOptions.IgnoreCase);

    [MenuItem("Tools/Crowd/1) Configure RPM GLB as Humanoid")]
    public static void ConfigureHumanoid()
    {
        var guids = AssetDatabase.FindAssets("t:Model");
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                         .Where(p => rpmName.IsMatch(System.IO.Path.GetFileNameWithoutExtension(p)))
                         .ToList();

        int ok = 0, fail = 0;
        foreach (var path in paths)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { fail++; continue; }

            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.optimizeGameObjects = true;

            try { mi.SaveAndReimport(); ok++; } catch { fail++; }
        }
        UnityEngine.Debug.Log($"[BatchImporter] Humanoid 설정 완료: {ok}, 실패: {fail} (검색 수:{paths.Count})");
    }
}
