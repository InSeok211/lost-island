using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LostIsland.Editor
{
    public sealed class WorldV2AssetsWindow : EditorWindow
    {
        const string Path = "Assets/LostIsland/Data/asset-bindings.json";
        AssetBindings bindings;
        Vector2 scroll;
        [MenuItem("Lost Island/V2/Prefab Bindings")]
        static void Open() { GetWindow<WorldV2AssetsWindow>("World V2 Prefabs"); }
        void OnEnable() { if(File.Exists(Path)) bindings=JsonUtility.FromJson<AssetBindings>(File.ReadAllText(Path)); }
        void OnGUI()
        {
            if(bindings==null) { EditorGUILayout.HelpBox("Asset bindings file is missing.",MessageType.Error); return; }
            EditorGUILayout.HelpBox("Assign prefab assets, save, then rebuild the world. Empty slots use generated blockout prefabs. Existing generated V2 tiles are replaced when rebuilt.",MessageType.Info);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var field in typeof(AssetBindings).GetFields(BindingFlags.Public|BindingFlags.Instance)) {
                if(!field.Name.EndsWith("Prefab")) continue;
                string path=(string)field.GetValue(bindings);
                var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var value=(GameObject)EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name),old,typeof(GameObject),false);
                if(value!=old) {
                    if(value && !PrefabUtility.IsPartOfPrefabAsset(value)) EditorUtility.DisplayDialog("Prefab required","Choose a prefab asset.","OK");
                    else field.SetValue(bindings,value?AssetDatabase.GetAssetPath(value):"");
                }
                if(!string.IsNullOrEmpty(path) && !old) EditorGUILayout.HelpBox("Missing prefab: "+path,MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
            if(GUILayout.Button("Save bindings")) { File.WriteAllText(Path,JsonUtility.ToJson(bindings,true)); AssetDatabase.ImportAsset(Path); }
            if(GUILayout.Button("Select world layout")) Selection.activeObject=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/LostIsland/Data/world-layout.json");
        }
    }
}
