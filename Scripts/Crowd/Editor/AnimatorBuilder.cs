using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorBuilder
{
    const string ANIM_DIR = "Assets/Animations/RPM/";
    const string CTRL_PATH = "Assets/Animations/RPM/CrowdHumanoid.controller";

    [MenuItem("Tools/Crowd/2) Create CrowdHumanoid Animator")]
    public static void CreateController()
    {
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CTRL_PATH);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var sm = ctrl.layers[0].stateMachine;

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, ctrl);

        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(ANIM_DIR + "Idle.fbx");
        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(ANIM_DIR + "Walk.fbx");
        var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(ANIM_DIR + "Run.fbx");
        if (!idle || !walk || !run) { Debug.LogWarning("Idle/Walk/Run.fbx를 Animations/RPM에 넣어주세요."); return; }

        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 0.5f);
        tree.AddChild(run, 1f);

        var st = sm.AddState("Locomotion");
        st.motion = tree;

        AssetDatabase.SaveAssets();
        Debug.Log("[AnimatorBuilder] CrowdHumanoid.controller 생성 완료");
    }
}
