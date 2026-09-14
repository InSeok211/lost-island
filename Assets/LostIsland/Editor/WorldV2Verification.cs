using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostIsland.Editor
{
    // One-time integration run for this imported pack. Subsequent builds are explicit menu actions.
    [InitializeOnLoad]
    public static class WorldV2Verification
    {
        const string ReportPath = "Logs/LostIslandV2Validation.json";
        const string Bootstrap = LostIslandWorldBuilderV2.Root + "/Scenes/LostIsland_Bootstrap.unity";
        const string StateKey = "LostIslandV2.VerificationState";
        static double deadline;
        static int settled;
        static int phase;
        static bool moved;
        static double movementStarted;
        static Vector3 movementOrigin;
        static readonly Vector3[] stops = { new Vector3(0,0,80), new Vector3(2200,0,2500), new Vector3(-4400,0,1200),new Vector3(7900,0,7900) };
        [Serializable] public class Report {
            public int terrainTiles, tileScenes, pois, spawnZones;
            public float maxSeamErrorMeters;
            public bool missingScripts, terrainChecksPassed, streamingChecksPassed;
            public string status;
            public List<string> checks = new List<string>();
        }
        static Report report;
        static WorldV2Verification() { EditorApplication.update += Tick; }

        [MenuItem("Lost Island/V2/Validate World and Streaming")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try { report=ValidateAssets(); Write(); BeginPlay(); }
            catch(Exception e) { Debug.LogException(e); }
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            int state = SessionState.GetInt(StateKey,0);
            if(state==0 && !Application.isBatchMode && File.Exists("Assets/LostIsland/Data/v2-auto-build.request") && !File.Exists(Bootstrap) && !File.Exists(ReportPath)) {
                if(EditorApplication.isPlayingOrWillChangePlaymode) return;
                for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) return;
                SessionState.SetInt(StateKey,1);
                try {
                    if(!File.Exists(Bootstrap)) LostIslandWorldBuilderV2.BuildStreamingWorld();
                    report=ValidateAssets(); Write(); BeginPlay();
                } catch(Exception e) {
                    report = report ?? new Report(); report.status="FAILED: "+e.Message; Write();
                    SessionState.SetInt(StateKey,9); Debug.LogException(e);
                }
                return;
            }
            if(state!=2 || !EditorApplication.isPlaying) return;
            if(report==null) {
                report=JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
                deadline=EditorApplication.timeSinceStartup+180; phase=0; settled=0; moved=false;
                movementStarted=0;
                Application.runInBackground=true;
            }
            if(EditorApplication.timeSinceStartup>deadline) { Finish(false,"Streaming timed out"); return; }
            var streamer=UnityEngine.Object.FindAnyObjectByType<LostIslandWorldStreamer>();
            var mover=UnityEngine.Object.FindAnyObjectByType<LostIslandDemoMover>();
            if(!streamer || !mover) return;
            if(!moved) {
                var p=stops[phase]; p.y=WorldSurface.Height(p.x,p.z)+4;
                mover.Teleport(p); moved=true; settled=0;
            }
            if(streamer.Busy || !streamer.ReadyAt(mover.transform.position)) { settled=0; return; }
            if(++settled<30) return;
            if(phase==0) {
                if(movementStarted==0) {
                    movementOrigin=mover.transform.position; movementStarted=EditorApplication.timeSinceStartup;
                    mover.verificationInput=Vector2.right; return;
                }
                if(EditorApplication.timeSinceStartup-movementStarted<1.2) return;
                mover.verificationInput=Vector2.zero;
                float distance=Vector3.ProjectOnPlane(mover.transform.position-movementOrigin,Vector3.up).magnitude;
                if(distance<1) { Finish(false,"Movement input did not move the controller"); return; }
                report.checks.Add($"Movement input: moved {distance:F2} m on terrain.");
            }
            var c=streamer.CurrentTile;
            int expected=0;
            for(int z=c.y-1;z<=c.y+1;z++) for(int x=c.x-1;x<=c.x+1;x++) {
                if(x<0||z<0||x>=8||z>=8) continue;
                expected++;
                if(!SceneManager.GetSceneByName($"LI_Tile_{x}_{z}").isLoaded) { Finish(false,"Missing neighbor "+x+","+z); return; }
            }
            if(streamer.LoadedCount!=expected) { Finish(false,"Unexpected loaded tile count"); return; }
            float ground=WorldSurface.Height(mover.transform.position.x,mover.transform.position.z);
            if(mover.transform.position.y<ground-.5f) { Finish(false,"Player below terrain"); return; }
            report.checks.Add($"Stop {phase}: tile {c}, loaded {expected}, player above terrain. Position {mover.transform.position}");
            if(phase==0) CapturePreview();
            phase++; moved=false; deadline=EditorApplication.timeSinceStartup+180;
            if(phase==stops.Length) { mover.Teleport(new Vector3(0,WorldSurface.Height(0,0)+4,0)); Finish(true,"Asset and streaming checks passed"); }
        }

        static void BeginPlay()
        {
            if(!report.terrainChecksPassed) throw new Exception("Asset validation failed; inspect "+ReportPath);
            EditorSceneManager.OpenScene(Bootstrap);
            report.status="Terrain checks passed; testing streaming"; Write();
            SessionState.SetInt(StateKey,2); report=null;
            EditorApplication.isPlaying=true;
        }

        static void Finish(bool pass,string message)
        {
            report.streamingChecksPassed=pass; report.status=message; Write();
            SessionState.SetInt(StateKey,9); EditorApplication.isPlaying=false;
            Debug.Log("[Lost Island V2 QA] "+message+". Report: "+ReportPath);
            if(Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
        }
        static void Write() { Directory.CreateDirectory("Logs"); File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true)); }

        public static Report ValidateAssets()
        {
            WorldSurface.Configure(JsonUtility.FromJson<WorldLayout>(File.ReadAllText("Assets/LostIsland/Data/world-layout.json")));
            var result=new Report();
            for(int z=0;z<8;z++) for(int x=0;x<8;x++) {
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>($"{LostIslandWorldBuilderV2.Root}/TerrainData/Terrain_{x}_{z}.asset");
                if(!data) throw new Exception($"Missing terrain {x},{z}");
                result.terrainTiles++;
                int n=data.heightmapResolution;
                if(x<7) CompareEdge(data,AssetDatabase.LoadAssetAtPath<TerrainData>($"{LostIslandWorldBuilderV2.Root}/TerrainData/Terrain_{x+1}_{z}.asset"),true,result,n);
                if(z<7) CompareEdge(data,AssetDatabase.LoadAssetAtPath<TerrainData>($"{LostIslandWorldBuilderV2.Root}/TerrainData/Terrain_{x}_{z+1}.asset"),false,result,n);
                string path=$"{LostIslandWorldBuilderV2.Root}/Scenes/LI_Tile_{x}_{z}.unity";
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                result.tileScenes++;
                foreach(var root in scene.GetRootGameObjects()) {
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) result.missingScripts=true;
                    foreach(var poi in root.GetComponentsInChildren<LostIslandPoi>()) {
                        result.pois++;
                        if(poi.surface=="land" && Mathf.Abs(poi.transform.position.y-WorldSurface.Height(poi.transform.position.x,poi.transform.position.z))>1)
                            throw new Exception("POI not grounded: "+poi.id);
                    }
                    result.spawnZones+=root.GetComponentsInChildren<LostIslandSpawnZone>().Length;
                }
                EditorSceneManager.CloseScene(scene,true);
            }
            result.terrainChecksPassed=result.terrainTiles==64 && result.tileScenes==64 && result.pois==30 && !result.missingScripts && result.maxSeamErrorMeters<.02f;
            result.status="Terrain validation complete";
            return result;
        }

        static void CompareEdge(TerrainData a,TerrainData b,bool east,Report result,int n)
        {
            if(!b) throw new Exception("Missing terrain neighbor");
            var aa=east?a.GetHeights(n-1,0,1,n):a.GetHeights(0,n-1,n,1);
            var bb=east?b.GetHeights(0,0,1,n):b.GetHeights(0,0,n,1);
            for(int i=0;i<n;i++) result.maxSeamErrorMeters=Mathf.Max(result.maxSeamErrorMeters,Mathf.Abs(east?aa[i,0]-bb[i,0]:aa[0,i]-bb[0,i])*a.size.y);
        }

        static void CapturePreview()
        {
            if(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera=Camera.main; if(!camera) return;
            var prior=camera.targetTexture;
            var rt=new RenderTexture(1280,720,24);
            var active=RenderTexture.active;
            try {
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                var tex=new Texture2D(1280,720,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply();
                File.WriteAllBytes("Logs/LostIslandV2-preview.png",tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            } finally { camera.targetTexture=prior; RenderTexture.active=active; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        }

        public static void BuildAndValidateBatch()
        {
            SessionState.SetInt(StateKey,1);
            try {
                EditorSceneManager.OpenScene("Assets/LostIsland/Scenes/TestIsland.unity");
                LostIslandWorldBuilderV2.BuildStreamingWorld();
                report=ValidateAssets(); Write(); BeginPlay();
            } catch(Exception e) {
                report=report??new Report(); report.status="FAILED: "+e; Write();
                Debug.LogException(e); EditorApplication.Exit(1);
            }
        }
    }
}
