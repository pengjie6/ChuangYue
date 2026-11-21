#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using main.level;

namespace BalloonManEditor
{
    /// <summary>
    /// 关卡引用修复工具。
    /// 场景：Level 1~10.prefab 是 Level.prefab 的实例，Level 组件随实例继承，
    /// 但 LevelContainer._levels 里保存的 fileID 属于旧版 Level.prefab，已解析不到对象，
    /// 导致 LevelsManager.currentLevel == null -> Instantiate(null) -> 卡在加载界面。
    /// 这里用 Unity 官方 API 让编辑器自己写回正确的 fileID（含必要的 stripped 引用）。
    /// </summary>
    public static class LevelsReferenceFixer
    {
        const string ContainerPath = "Assets/ScriptableObjects/Levels/_Container.asset";
        const string PrefabPattern = "Assets/Prefabs/Levels/Level {0}.prefab";
        const int LevelCount = 10;
        const string SessionKey = "BalloonMan.LevelsReferenceFixer.Ran.v1";

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += AutoCheck;
        }

        static void AutoCheck()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            try
            {
                if (!HasBrokenReference()) return;
                Debug.Log("[LevelsFixer] 检测到 _Container._levels 存在空引用，开始自动修复…");
                Fix();
            }
            catch (Exception e)
            {
                Debug.LogError("[LevelsFixer] 自动修复失败：" + e);
            }
        }

        [MenuItem("Tools/星石/修复关卡引用 (Fix Level References)", false, 1)]
        public static void FixMenu()
        {
            Fix();
        }

        [MenuItem("Tools/星石/检查关卡引用 (Validate)", false, 2)]
        public static void ValidateMenu()
        {
            Debug.Log(HasBrokenReference()
                ? "[LevelsFixer] 检查结果：_Container._levels 存在空引用，请执行修复。"
                : "[LevelsFixer] 检查结果：_Container._levels 10 项全部有效。");
        }

        static bool HasBrokenReference()
        {
            var container = AssetDatabase.LoadAssetAtPath<LevelContainer>(ContainerPath);
            if (container == null) return false;

            var so = new SerializedObject(container);
            var arr = so.FindProperty("_levels");
            if (arr == null) return false;
            if (arr.arraySize != LevelCount) return true;

            for (int i = 0; i < arr.arraySize; i++)
            {
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == null) return true;
            }
            return false;
        }

        static void Fix()
        {
            var container = AssetDatabase.LoadAssetAtPath<LevelContainer>(ContainerPath);
            if (container == null)
            {
                Debug.LogError("[LevelsFixer] 找不到 " + ContainerPath);
                return;
            }

            var so = new SerializedObject(container);
            var arr = so.FindProperty("_levels");
            if (arr == null)
            {
                Debug.LogError("[LevelsFixer] _Container 上没有 _levels 字段");
                return;
            }

            arr.arraySize = LevelCount;
            int bound = 0;

            for (int i = 0; i < LevelCount; i++)
            {
                string path = string.Format(PrefabPattern, i + 1);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var level = go != null ? go.GetComponent<Level>() : null;

                if (level == null)
                    Debug.LogError("[LevelsFixer] " + path + " 上找不到 Level 组件（应继承自 Level.prefab）");
                else
                    bound++;

                arr.GetArrayElementAtIndex(i).objectReferenceValue = level;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelsFixer] 完成，成功绑定 " + bound + "/" + LevelCount + " 个关卡。");
        }
    }
}
#endif
