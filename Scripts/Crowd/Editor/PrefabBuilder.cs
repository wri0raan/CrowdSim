using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class PrefabBuilder
{
    const string MODEL_ROOT = "Assets/Avatars/Models";
    const string PREFAB_ROOT = "Assets/Avatars/Prefabs";
    const string RES_ROOT = "Assets/Resources/Crowd/RPM";
    const string CTRL_PATH = "Assets/Animations/RPM/CrowdHumanoid.controller";

    static readonly Regex namePat = new(@"(Fe|Ma)\s*([1-4])\s*\((\d+)\)", RegexOptions.IgnoreCase);

    [MenuItem("Tools/Crowd/3) Build LOD Prefabs from GLB")]
    public static void Build()
    {
        Directory.CreateDirectory(PREFAB_ROOT);
        Directory.CreateDirectory(RES_ROOT);

        string rootAbs = Path.GetFullPath(MODEL_ROOT);
        var paths = Directory.EnumerateFiles(rootAbs, "*.glb", SearchOption.AllDirectories)
            .Select(p => "Assets" + p.Replace(Path.GetFullPath("Assets"), "").Replace("\\", "/"))
            .ToList();

        if (paths.Count == 0)
        {
            Debug.LogWarning($"[PrefabBuilder] '{MODEL_ROOT}' 아래에서 .glb를 찾지 못했습니다. 폴더/확장자 확인.");
            return;
        }

        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CTRL_PATH);
        if (!ctrl) Debug.LogWarning($"[PrefabBuilder] Animator Controller가 없습니다: {CTRL_PATH}");

        int made = 0;

        foreach (var path in paths)
        {

            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!src)
            {

                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                src = all.OfType<GameObject>().FirstOrDefault();
            }
            if (!src)
            {
                Debug.LogWarning($"[PrefabBuilder] GameObject를 로드하지 못함: {path}");
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(path);
            var m = namePat.Match(fileName);
            string gender = m.Success ? m.Groups[1].Value : "X";
            string type = m.Success ? m.Groups[2].Value : "0";
            string idx = m.Success ? m.Groups[3].Value : "0";

            var root = new GameObject($"{gender}{type}_{idx}_LOD");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(src);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            var anim = root.AddComponent<Animator>();
            var exist = model.GetComponent<Animator>();
            if (exist) Object.DestroyImmediate(exist);
            anim.applyRootMotion = false;
            if (ctrl) anim.runtimeAnimatorController = ctrl;

            var impostor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            impostor.name = "Impostor";
            impostor.transform.SetParent(root.transform, false);
            var col = impostor.GetComponent<Collider>(); if (col) col.enabled = false;
            impostor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var renderers = model.GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers)
            {
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;

            lodGroup.SetLODs(new[]
            {
                new LOD(0.01f, renderers),
                new LOD(0.00f, new[]{ impostor.GetComponent<Renderer>() }),
});

            lodGroup.RecalculateBounds();
            lodGroup.size = Mathf.Max(2f, lodGroup.size);

            root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            if (!root.GetComponent<CrowdAgentAnimator>()) root.AddComponent<CrowdAgentAnimator>();
            if (!root.GetComponent<AgentMotor>()) root.AddComponent<AgentMotor>();

            string pfFolder = Path.Combine(PREFAB_ROOT, $"{gender}{type}").Replace("\\", "/");
            string resFolder = Path.Combine(RES_ROOT, $"{gender}{type}").Replace("\\", "/");
            Directory.CreateDirectory(pfFolder);
            Directory.CreateDirectory(resFolder);

            string pfPath = $"{pfFolder}/{gender}{type}_{idx}.prefab";
            string resPath = $"{resFolder}/{gender}{type}_{idx}.prefab";

            PrefabUtility.SaveAsPrefabAsset(root, pfPath);
            PrefabUtility.SaveAsPrefabAsset(root, resPath);

            Object.DestroyImmediate(root);
            made++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PrefabBuilder] 완료: {made}개 프리팹 생성 (from {paths.Count} glb) → Avatars/Prefabs + Resources/Crowd/RPM");
    }
}
