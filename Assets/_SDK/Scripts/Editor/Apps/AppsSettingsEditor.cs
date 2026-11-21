using UnityEditor;
using UnityEngine;

namespace apps.editor
{
    /// <summary>
    /// AppsSettings 的自定义 Inspector。
    /// 已移除第三方统计/归因 SDK 相关项，仅保留 ADS 与调试开关。
    /// </summary>
    [CustomEditor(typeof(AppsSettings))]
    public class AppsSettingsEditor : Editor
    {
        SerializedProperty autoInitialize;
        SerializedProperty integrateADS;
        SerializedProperty showADSType;
        SerializedProperty adsInfo;
        SerializedProperty debugMode;

        protected void OnEnable()
        {
            autoInitialize = serializedObject.FindProperty("autoInitialize");
            integrateADS = serializedObject.FindProperty("integrateADS");
            showADSType = serializedObject.FindProperty("showADSType");
            adsInfo = serializedObject.FindProperty("adsInfo");
            debugMode = serializedObject.FindProperty("debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AppsSettings appsSettings = (AppsSettings)target;

            GUIStyle header = new GUIStyle(GUI.skin.label);
            header.margin = new RectOffset(25, 20, 20, 5);
            header.fontStyle = FontStyle.Bold;
            GUILayout.Label("Auto Apps Initialize", header);
            EditorGUILayout.PropertyField(autoInitialize);

            GUILayout.Label("Ads Settings", header);
            EditorGUILayout.PropertyField(integrateADS);
            if (appsSettings.integrateADS)
            {
                EditorGUILayout.PropertyField(showADSType);
                EditorGUILayout.PropertyField(adsInfo);
            }

            GUILayout.Label("Other Settings", header);
            EditorGUILayout.PropertyField(debugMode);

            GUILayout.Label("Player Information", header);
            GUIStyle headInfo = new GUIStyle(GUI.skin.label);
            headInfo.margin = new RectOffset(40, 0, 0, 0);

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Package name:   " + PlayerSettings.applicationIdentifier, headInfo);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Default orientation:   " + PlayerSettings.defaultInterfaceOrientation, headInfo);
            GUILayout.EndHorizontal();

            GUILayout.Label("Android", headInfo);

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Min sdk version:   " + PlayerSettings.Android.minSdkVersion, headInfo);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Target sdk version:   " + PlayerSettings.Android.targetSdkVersion, headInfo);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Target architectures:   " + PlayerSettings.Android.targetArchitectures, headInfo);
            GUILayout.EndHorizontal();

            GUIStyle headButton = new GUIStyle(GUI.skin.button);
            headButton.margin = new RectOffset(0, 0, 20, 0);
            headButton.fixedHeight = 50;
            headButton.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Save And Refresh", headButton))
            {
                AppsUtility.SaveData(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static AppsSettings GetAppSettings()
        {
            AppsSettings[] appsSettings = ScriptableObjectUtility.GetScriptableObjects<AppsSettings>();
            if (appsSettings == null || appsSettings.Length <= 0)
            {
                appsSettings = new AppsSettings[]
                {
                    AppsUtility.CreateAsset<AppsSettings>(AppsSettings.globalDirectoryPath, "AppsSettings.asset")
                };

                Debug.Log("The appsSettings is created...");
            }

            if (1 < appsSettings.Length)
            {
                Debug.LogWarning("You have 2 or plus AppsSettings, please leave just one for be sure that we use the right one!...");
            }

            return appsSettings[0];
        }

        public static void PingAppsSettings(AppsSettings settings)
        {
            if (settings == null)
                throw new System.ArgumentNullException("The AppsSettings has null variable.");

            AppsUtility.SaveData(settings);
            AppsUtility.PingObject(settings);
        }
    }
}
