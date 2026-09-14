using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostIsland.Editor
{
    [InitializeOnLoad]
    public static class TestIslandSetup
    {
        const string ScenePath = "Assets/LostIsland/Scenes/TestIsland.unity";
        static TestIslandSetup() { EditorApplication.delayCall += FirstSetup; }

        static void FirstSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || File.Exists(ScenePath)) return;
            Create();
        }

        [MenuItem("Lost Island/Open Test Island")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Create();
            EditorSceneManager.OpenScene(ScenePath);
        }

        static void Create()
        {
            Directory.CreateDirectory("Assets/LostIsland/Scenes");
            Directory.CreateDirectory("Assets/LostIsland/Generated");
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            bool openAutomatically = SceneManager.sceneCount == 1 && !previous.isDirty;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Test Island");
            root.AddComponent<TestIslandWorld>().Build();
            var materials = new HashSet<Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials) materials.Add(material);
            foreach (var material in materials)
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/LostIsland/Generated/" + material.name + ".mat"));
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                    AssetDatabase.CreateAsset(filter.sharedMesh, AssetDatabase.GenerateUniqueAssetPath("Assets/LostIsland/Generated/IslandTerrain.asset"));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (openAutomatically) EditorSceneManager.CloseScene(previous, true);
            else { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            Debug.Log("[Lost Island] TestIsland ready. Open Lost Island > Open Test Island, then press Play.");
        }
    }
}
