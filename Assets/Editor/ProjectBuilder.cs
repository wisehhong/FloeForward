using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FloeForward.Editor
{
    public static class ProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/Journey.unity";
        private const string PipelineAssetPath = "Assets/Settings/URP.asset";
        private const string RendererAssetPath = "Assets/Settings/URP_Renderer.asset";
        private const string LitShader = "Universal Render Pipeline/Lit";

        [MenuItem("Floe Forward/Build Windows")]
        public static void BuildWindows()
        {
            PrepareProject();
            Directory.CreateDirectory("Build/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = "Build/Windows/FloeForward.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("Floe Forward Windows build succeeded.");
        }

        public static void PrepareProject()
        {
            Directory.CreateDirectory("Assets/Scenes");
            EnsureUniversalRenderPipeline();
            EnsureShaderIncluded(LitShader);
            PlayerSettings.companyName = "Floe Forward";
            PlayerSettings.productName = "Floe Forward";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 5.7f, -17f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = false;
                camera.fieldOfView = 28f;
                camera.transform.LookAt(new Vector3(0, 0, 21f));
                camera.clearFlags = CameraClearFlags.SolidColor;
                var gameObject = new GameObject("Game");
                gameObject.AddComponent<GameUI>();
                gameObject.AddComponent<GameController>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void EnsureUniversalRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                Directory.CreateDirectory("Assets/Settings");
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int currentLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(currentLevel, false);
            RemoveIncludedShader("Standard");
            AssetDatabase.SaveAssets();
        }

        private static void RemoveIncludedShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) return;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            for (int i = shaders.arraySize - 1; i >= 0; i--)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    shaders.DeleteArrayElementAtIndex(i);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureShaderIncluded(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception("Required shader not found: " + shaderName);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void RunTests()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
