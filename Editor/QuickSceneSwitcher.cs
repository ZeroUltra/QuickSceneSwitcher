using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
/// <summary>
/// description:      
/// </summary>
namespace GameFramework.Editor
{
    public class QuickSceneSwitcher : EditorWindow
    {
        public List<SceneAsset> sceneList;
        private SerializedObject so;
        private SerializedProperty sp_ListScene;
        private bool isEditMode = false;
        private string savePath;
        private GUIStyle buttonStyleBold, buttonPStyle;

        private Vector2 scrollPosEditMode, scrollPosEditModeExit;

        [MenuItem("Window/QuickSceneSwitcher")]
        public static void ShowWindow()
        {
            //InitWindow("QuickSceneSwitcher", size: new Vector2(600, 600), icon: "SceneAsset Icon", guiScript: false);
            //设置窗口在屏幕中间偏上
            //以左上角为起点  直接设置为中心的画有偏移
            string windowName = "QuickSceneSwitcher";
            //如果是单例 并且已经打开 则返回已经打开的窗口
            if (EditorWindow.HasOpenInstances<QuickSceneSwitcher>())
            {
                GetWindow<QuickSceneSwitcher>(windowName);
                return;
            }
            var resolution = Screen.currentResolution;
            Rect rect = new Rect();
            rect.size = new Vector2(600, 600);
            rect.center = new Vector2(resolution.width * 0.4f, resolution.height * 0.35f);
            var window = CreateWindow<QuickSceneSwitcher>(windowName);
            window.position = rect;
            Texture2D tex = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            window.titleContent = EditorGUIUtility.TrTextContentWithIcon(windowName, tex);
        }
        void OnEnable()
        {
            so = new SerializedObject(this);
            sp_ListScene = so.FindProperty("sceneList");
            savePath = Application.dataPath + "/../ProjectSettings/QuickSceneSwitcherPrefs.json";
            LoadPrefs();
        }

        void OnGUI()
        {
            // base.OnGUI();
            if (buttonStyleBold == null)
            {
                buttonStyleBold = new GUIStyle(GUI.skin.button);
                buttonStyleBold.fixedHeight = 22;
                buttonStyleBold.fontStyle = FontStyle.Bold;

                buttonPStyle = new GUIStyle(buttonStyleBold);
                buttonPStyle.fontStyle = FontStyle.BoldAndItalic;
                buttonPStyle.normal.textColor = Color.cyan;
            }

            if (isEditMode)
            {
                so.Update();
                if (sp_ListScene != null)
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosEditMode))
                    {
                        scrollPosEditMode = scroll.scrollPosition;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(sp_ListScene);
                        if (EditorGUI.EndChangeCheck())
                        {
                            so.ApplyModifiedProperties();
                            SavePrefs();
                        }
                    }
                }
            }
            else
            {
                if (sceneList != null)
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosEditModeExit))
                    {
                        scrollPosEditModeExit = scroll.scrollPosition;
                        foreach (var scene in sceneList)
                        {
                            if (scene == null)
                            {
                                Color cacheColor = GUI.backgroundColor;
                                GUI.backgroundColor = Color.red;
                                EditorGUI.BeginDisabledGroup(true);
                                GUILayout.Button("NULL", buttonStyleBold);
                                EditorGUI.EndDisabledGroup();
                                GUI.backgroundColor = cacheColor;
                                continue;
                            }
                            bool isCurrentScene = EditorSceneManager.GetActiveScene().path == AssetDatabase.GetAssetPath(scene);
                            GUILayout.BeginHorizontal();
                            GUI.backgroundColor = isCurrentScene ? Color.green : new Color(0.35f, 0.35f, 0.35f, 1);
                            if (GUILayout.Button($"{scene.name}", buttonStyleBold))
                            {
                                OpenScene(scene);
                            }
                            Color defaultColor = GUI.backgroundColor;
                            GUI.backgroundColor = Color.cyan;
                            if (GUILayout.Button("P", buttonPStyle, GUILayout.Width(25), GUILayout.Height(22)))
                            {
                                EditorGUIUtility.PingObject(scene);
                            }
                            GUI.backgroundColor = defaultColor;
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
        private void ShowButton(Rect rect)
        {
            //AddTitleButton(rect);
            var guiStyle = isEditMode ? GUI.skin.GetStyle("ArrowNavigationLeft") : GUI.skin.GetStyle("ArrowNavigationRight");
            isEditMode = GUI.Toggle(rect, this.isEditMode, GUIContent.none, guiStyle);
        }
        //protected override void AddTitleButton(Rect rect)
        //{
        //    base.AddTitleButton(rect);
        //    var guiStyle = isEditMode ? GUI.skin.GetStyle("ArrowNavigationLeft") : GUI.skin.GetStyle("ArrowNavigationRight");
        //    isEditMode = GUI.Toggle(rect, this.isEditMode, GUIContent.none, guiStyle);
        //}

        private void OpenScene(SceneAsset scene)
        {
            string scenePath = AssetDatabase.GetAssetPath(scene);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
        //private bool IsCurrentScene(SceneAsset scene)
        //{
        //    return EditorSceneManager.GetActiveScene().path == AssetDatabase.GetAssetPath(scene);
        //}

        private void SavePrefs()
        {
            var sceneJsonEntity = new SceneJsonEntity();
            sceneJsonEntity.sceneListGuid = new List<string>();
            foreach (SceneAsset item in sceneList)
            {
                string sceneGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                sceneJsonEntity.sceneListGuid.Add(sceneGUID);
            }
            string json = sceneJsonEntity.ToJson();
            System.IO.File.WriteAllText(savePath, json);
        }

        private void LoadPrefs()
        {
            // Scenes Count
            sceneList = new List<SceneAsset>();
            if (System.IO.File.Exists(savePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(savePath);
                    SceneJsonEntity sceneJsonEntity = SceneJsonEntity.FromJson(json);
                    foreach (string guid in sceneJsonEntity.sceneListGuid)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        sceneList.Add(sceneAsset);
                    }
                    //var ja = JSONNode.Parse(System.IO.File.ReadAllText(savePath)).AsArray;
                    //foreach (var item in ja.Children)
                    //{
                    //    string path = AssetDatabase.GUIDToAssetPath(item.Value);
                    //    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    //    sceneList.Add(sceneAsset);
                    //}
                }
                catch (System.Exception e)
                {
                    Debug.LogError("quick scene 配置加载错误: " + e.Message);
                }
            }
        }
        [System.Serializable]
        public class SceneJsonEntity
        {
            public List<string> sceneListGuid;

            public string ToJson()
            {
                return JsonUtility.ToJson(this, true);
            }

            public static SceneJsonEntity FromJson(string json)
            {
                return JsonUtility.FromJson<SceneJsonEntity>(json);
            }
        }
    }
}