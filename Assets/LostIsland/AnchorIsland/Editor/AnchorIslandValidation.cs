using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LostIsland.AnchorIsland.Editor {
    [InitializeOnLoad] public static class AnchorIslandValidation {
        const string Root=AnchorIslandBuilder.Root, ReportPath="Logs/AnchorIsland-validation.json";
        const string Key="AnchorIsland.Validation";
        [Serializable] public class Report {public int tiles,pois,nodes,spawnZones,roads,creatureDefinitions,resourceDefinitions;public float seamError;public bool assetsPassed,playPassed;public string status;public List<string> checks=new List<string>();}
        static Report report;static double deadline,moveStart;static int phase,settled;static bool teleported;static Vector3 initial;
        static readonly Vector2[] stops={new Vector2(0,-18),new Vector2(620,430),new Vector2(-1040,170),new Vector2(1030,20),new Vector2(-50,1070),new Vector2(0,-1500),new Vector2(-100,630)};
        static AnchorIslandValidation(){EditorApplication.update+=Tick;}
        public static void BuildBatch(){
            try{EditorSceneManager.OpenScene("Assets/LostIsland/Scenes/TestIsland.unity");AnchorIslandBuilder.Build();StartValidation();}
            catch(Exception e){Fail(e);}
        }
        [MenuItem("Lost Island/Anchor Island/Validate Playable Island")]
        public static void StartValidation(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try{report=Assets();Write();if(!report.assetsPassed)throw new Exception("Asset validation failed");EditorSceneManager.OpenScene(Root+"/Scenes/AnchorIsland_Bootstrap.unity");SessionState.SetBool(Key,true);report=null;EditorApplication.isPlaying=true;}
            catch(Exception e){Fail(e);}
        }
        static void Fail(Exception e){report=report??new Report();report.status=e.ToString();report.playPassed=false;Write();SessionState.SetBool(Key,false);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        static void Write(){Directory.CreateDirectory("Logs");File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));}
        static Report Assets(){
            AnchorSurface.Configure(JsonUtility.FromJson<AnchorIslandLayout>(File.ReadAllText("Assets/LostIsland/AnchorIsland/Data/anchor-island-layout.json")));
            var r=new Report();var resources=new HashSet<string>();var species=new HashSet<string>();var roads=new HashSet<string>();
            if(Mathf.Abs(AnchorSurface.Height(0,0)-38)>.1f)throw new Exception("Central plaza flat height must be 38m");
            for(int z=0;z<4;z++)for(int x=0;x<4;x++){
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>($"{Root}/TerrainData/Terrain_{x}_{z}.asset");if(!data)throw new Exception("Terrain missing");r.tiles++;
                int n=data.heightmapResolution;
                for(int axis=0;axis<2;axis++)if((axis==0?x:z)<3){var b=AssetDatabase.LoadAssetAtPath<TerrainData>($"{Root}/TerrainData/Terrain_{x+(axis==0?1:0)}_{z+(axis==1?1:0)}.asset");var aa=axis==0?data.GetHeights(n-1,0,1,n):data.GetHeights(0,n-1,n,1);var bb=axis==0?b.GetHeights(0,0,1,n):b.GetHeights(0,0,n,1);for(int i=0;i<n;i++)r.seamError=Mathf.Max(r.seamError,Mathf.Abs(axis==0?aa[i,0]-bb[i,0]:aa[0,i]-bb[0,i])*260);}
                var scene=EditorSceneManager.OpenScene($"{Root}/Scenes/AnchorIsland_Tile_{x}_{z}.unity",OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects()){
                    foreach(var t in root.GetComponentsInChildren<Transform>()){if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script on "+t.name);if(t.name.StartsWith("road-"))roads.Add(t.name);}
                    foreach(var p in root.GetComponentsInChildren<AnchorIslandPoiMarker>()){r.pois++;if(Mathf.Abs(p.transform.position.y-AnchorSurface.Height(p.transform.position.x,p.transform.position.z))>1)throw new Exception("Floating POI "+p.id);}
                    foreach(var node in root.GetComponentsInChildren<AnchorResourceNode>()){r.nodes++;resources.Add(node.definition.data.id);if(!AnchorSurface.InZone(node.transform.position.x,node.transform.position.z,node.zoneId))throw new Exception("Resource zone mismatch "+node.nodeId);}
                    foreach(var zone in root.GetComponentsInChildren<AnchorCreatureSpawnZone>()){r.spawnZones++;species.Add(zone.definition.data.id);if(zone.definition.data.id.StartsWith("deinonychus")&&zone.zoneId!="restricted-north")throw new Exception("Deinonychus outside restricted area");}
                }
                EditorSceneManager.CloseScene(scene,true);
            }
            r.creatureDefinitions=AssetDatabase.FindAssets("t:AnchorCreatureDefinition",new[]{Root}).Length;r.resourceDefinitions=AssetDatabase.FindAssets("t:AnchorResourceDefinition",new[]{Root}).Length;r.roads=roads.Count;
            r.checks.Add($"Resource types placed: {resources.Count}/19; spawn species: {species.Count}/22");
            if(resources.Count!=19)r.checks.Add("Placed IDs: "+string.Join(",",resources));
            r.assetsPassed=r.tiles==16&&r.pois==16&&r.seamError<.02f&&r.creatureDefinitions==22&&r.resourceDefinitions==19&&resources.Count==19&&species.Count==22&&r.roads==7;
            r.status="Asset checks complete";return r;
        }
        static void Tick(){
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try{
                if(report==null){report=JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));deadline=EditorApplication.timeSinceStartup+150;phase=0;settled=0;teleported=false;moveStart=0;Application.runInBackground=true;}
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Play validation timed out");
                var player=UnityEngine.Object.FindAnyObjectByType<AnchorExplorer>();var s=AnchorSession.Current;if(!player||!s)return;
                if(!teleported){player.Teleport(stops[phase].x,stops[phase].y);teleported=true;settled=0;}
                if(player.streamer.busy||!player.streamer.Ready(player.transform.position)){settled=0;return;}if(++settled<45)return;
                if(phase==0){
                    if(moveStart==0){initial=player.transform.position;moveStart=EditorApplication.timeSinceStartup;player.testInput=Vector2.right;return;}
                    if(EditorApplication.timeSinceStartup-moveStart<1.2)return;player.testInput=Vector2.zero;
                    float d=Vector3.ProjectOnPlane(player.transform.position-initial,Vector3.up).magnitude;if(d<2)throw new Exception("Controller did not move");report.checks.Add($"Movement: {d:F2}m");
                    TestHarvest(s);TestTownWalk(player);TestInteriors(player);Capture("settlement");CaptureVillage();
                    var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
                    camera.transform.position=new Vector3(0,750,-550);camera.transform.LookAt(new Vector3(0,38,-120));camera.orthographicSize=410;Capture("overview");
                    camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;
                }
                int individuals=0;foreach(var zone in UnityEngine.Object.FindObjectsByType<AnchorCreatureSpawnZone>(FindObjectsSortMode.None)){
                    if(phase==4&&zone.zoneId=="restricted-north")zone.Spawn(true);
                    if(zone.Live.Count>0&&(zone.Live.Count<zone.definition.data.groupMin||zone.Live.Count>zone.definition.data.groupMax))throw new Exception("Invalid herd size");
                    foreach(var agent in zone.Live){if(!zone.Accept(agent.transform.position.x,agent.transform.position.z))throw new Exception("Creature outside habitat");individuals++;}
                }
                foreach(var c in s.creatures)if(s.groups.TryGetValue(c.data.id,out int count)&&count>c.data.maxGroups)throw new Exception("Exceeded max groups");
                if(phase==1||phase==6){int natural=0;string habitat=phase==1?"northeast-grassland":"north-forest";foreach(var z in UnityEngine.Object.FindObjectsByType<AnchorCreatureSpawnZone>(FindObjectsSortMode.None))if(z.zoneId==habitat)natural+=z.Live.Count;if(natural==0)throw new Exception("No natural creatures in "+habitat);report.checks.Add("Natural spawn (no debug force): "+habitat+" = "+natural);}
                if(phase==1){TestSurvey(s);var a=UnityEngine.Object.FindAnyObjectByType<AnchorCreatureAgent>();if(!a)throw new Exception("No creature instantiated");a.Observe();if(s.research.Count==0)throw new Exception("Observation did not record research");Capture("grassland");}
                if(phase==4){bool seen=false;foreach(var a in UnityEngine.Object.FindObjectsByType<AnchorCreatureAgent>(FindObjectsSortMode.None))if(a.definition.data.id.StartsWith("deinonychus"))seen=true;if(!seen)throw new Exception("Restricted rare creature cannot spawn even in verification mode");}
                if(phase==5)Capture("nearshore");if(phase==6)Capture("forest");
                report.checks.Add($"Stop {phase}: {player.streamer.Loaded} tiles loaded, {individuals} creatures, position {player.transform.position}");
                if(player.transform.position.y<AnchorSurface.Height(player.transform.position.x,player.transform.position.z)-1)throw new Exception("Player below terrain");
                phase++;teleported=false;deadline=EditorApplication.timeSinceStartup+150;
                if(phase==stops.Length){report.playPassed=true;report.status="PASS";Write();SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;Debug.Log("[AnchorIsland QA] PASS");if(Application.isBatchMode)EditorApplication.Exit(0);}
            }catch(Exception e){Fail(e);}
        }
        static void TestInteriors(AnchorExplorer player){
            var interior=player.GetComponent<AnchorBuildingInterior>();Vector3 saved=player.transform.position;int rooms=0;
            foreach(var door in UnityEngine.Object.FindObjectsByType<AnchorBuildingEntrance>()){
                player.Teleport(door.transform.position.x,door.transform.position.z);Vector3 outside=player.transform.position;interior.Enter(door);Physics.SyncTransforms();
                var controller=player.GetComponent<CharacterController>();for(int i=0;i<30;i++)controller.Move(Vector3.down*.2f);
                Vector3 from=player.transform.position;for(int i=0;i<20;i++)controller.Move(Vector3.forward*.2f+Vector3.down*.05f);
                if(!interior.Inside||player.transform.position.y<399||Vector3.Distance(from,player.transform.position)<3)throw new Exception("Interior walking failed "+door.title);
                var data=AnchorSaveGame.Capture(AnchorSession.Current,player);if(Mathf.Abs(data.x-outside.x)>.01f||Mathf.Abs(data.z-outside.z)>.01f)throw new Exception("Interior save position unsafe");
                Capture("interior-"+door.kind);interior.Exit();if(interior.Inside||Vector3.ProjectOnPlane(player.transform.position-outside,Vector3.up).magnitude>.01f)throw new Exception("Interior exit failed");rooms++;
            }
            if(rooms!=4)throw new Exception("Expected four building interiors, got "+rooms);player.Teleport(saved.x,saved.z);report.checks.Add("Interiors: four entrances, floor collision/walking, exterior save location and exit PASS");
        }
        static void TestTownWalk(AnchorExplorer player){
            var guide=player.GetComponent<AnchorSettlementGuide>();if(!guide)throw new Exception("Settlement guide missing");
            Vector3 saved=player.transform.position;var controller=player.GetComponent<CharacterController>();Physics.SyncTransforms();
            int residents=UnityEngine.Object.FindObjectsByType<AnchorResidentIdle>().Length;if(residents<8)throw new Exception("Residents missing");
            player.Teleport(0,-18);for(int settle=0;settle<30;settle++)controller.Move(Vector3.down*.2f);
            foreach(int destination in new[]{1,2,3,0}){
                guide.Select(destination);var route=new List<Vector3>(guide.Route);float walked=0;
                foreach(var point in route){
                    int stationary=0;
                    for(int step=0;step<5000;step++){
                        Vector3 delta=Vector3.ProjectOnPlane(point-player.transform.position,Vector3.up);if(delta.magnitude<2)break;
                        Vector3 before=player.transform.position;controller.Move(delta.normalized*.4f+Vector3.down*.16f);float advance=Vector3.ProjectOnPlane(player.transform.position-before,Vector3.up).magnitude;walked+=advance;stationary=advance<.01f?stationary+1:0;
                        if(stationary>15||step==4999)throw new Exception("Town walking route blocked: "+AnchorSettlementGuide.Names[destination]+" at "+player.transform.position);
                    }
                }
                report.checks.Add("CharacterController town walk: "+AnchorSettlementGuide.Names[destination]+" / "+walked.ToString("F1")+"m");
            }
            guide.Cancel();player.Teleport(saved.x,saved.z);report.checks.Add("Settlement residents "+residents+"; route navigation PASS");
        }
        static void TestSurvey(AnchorSession s){
            var quest=s.GetComponent<AnchorFirstSurvey>();if(!quest)throw new Exception("Quest missing");
            if(quest.Interact(Vector3.zero))throw new Exception("Remote quest interaction allowed");
            quest.RecordHarvest("fern-fiber",5);if(quest.collected!=0)throw new Exception("Pre-quest harvest counted");
            quest.Interact(quest.Research);quest.Interact(quest.Research);
            if(quest.stage!=AnchorFirstSurvey.Stage.Active)throw new Exception("Premature quest completion");
            AnchorCreatureAgent target=null;foreach(var a in UnityEngine.Object.FindObjectsByType<AnchorCreatureAgent>())if(a.definition.data.id=="hypsilophodon-foxii"){target=a;break;}
            if(!target)throw new Exception("Natural survey species missing");
            quest.TickObservation(target,true,2,target.transform.position);if(quest.observed)throw new Exception("Observation too short accepted");
            quest.TickObservation(target,false,0,target.transform.position);if(quest.progress!=0)throw new Exception("Observation not reset");
            quest.TickObservation(target,true,3,target.transform.position);if(!quest.observed)throw new Exception("Observation not recorded");
            foreach(var node in UnityEngine.Object.FindObjectsByType<AnchorResourceNode>())if(node.definition.data.id=="fern-fiber"&&node.Available){node.Harvest("hand",out _);if(quest.Ready)break;}
            if(!quest.Ready)throw new Exception("Survey collection failed");
            s.inventory.TryGetValue("fern-fiber",out int before);quest.Interact(quest.Research);quest.Interact(quest.Research);
            if(quest.stage!=AnchorFirstSurvey.Stage.Completed||s.inventory["fern-fiber"]!=before-5||s.inventory["research-token"]!=10)throw new Exception("Submission/reward not exactly once");
            report.checks.Add("Survey: accept, interrupted/3s observation, real fiber harvest, consume 5, reward once PASS");
            var player=UnityEngine.Object.FindAnyObjectByType<AnchorExplorer>();
            var snapshot=AnchorSaveGame.Capture(s,player);string saveTest="Logs/Anchor-save-test-"+Guid.NewGuid().ToString("N")+".json";
            AnchorSaveGame.Write(saveTest,snapshot);AnchorSaveGame.Write(saveTest,snapshot);
            s.inventory.Clear();s.harvested.Clear();s.research.Clear();s.SetDays(0);quest.stage=AnchorFirstSurvey.Stage.Available;quest.observed=false;quest.collected=0;player.Teleport(0,0);
            AnchorSaveGame.Restore(s,player,AnchorSaveGame.Read(saveTest));
            var restored=AnchorSaveGame.Capture(s,player);
            if(JsonUtility.ToJson(snapshot)!=JsonUtility.ToJson(restored))throw new Exception("Save roundtrip state mismatch");
            if(!File.Exists(saveTest+".bak"))throw new Exception("Save backup missing");
            quest.Interact(quest.Research);if(s.inventory["research-token"]!=10)throw new Exception("Reload duplicated reward");
            File.WriteAllText(saveTest,"invalid-json");bool rejected=false;try{AnchorSaveGame.Read(saveTest);}catch{rejected=true;}
            if(!rejected)throw new Exception("Corrupt save accepted");
            AnchorSaveGame.Restore(s,player,AnchorSaveGame.Read(saveTest+".bak"));
            report.checks.Add("Save: disk roundtrip all state, atomic replacement/backup, corrupt rejection, backup restore, reward once PASS");
            var beforeCraft=AnchorSaveGame.Capture(s,player);s.inventory.Clear();
            if(AnchorCrafting.Craft(s,"stone-axe")||s.inventory.Count!=0)throw new Exception("Crafting accepted missing ingredients");
            s.Add("fern-fiber",3);s.Add("driftwood",2);s.Add("flint",2);
            if(!AnchorCrafting.Craft(s,"fiber-rope")||!AnchorCrafting.Craft(s,"stone-axe")||s.inventory["stone-axe"]!=1||s.inventory["fiber-rope"]!=0||s.tool!="axe"||AnchorCrafting.Craft(s,"stone-axe"))throw new Exception("Crafting consumption/equip failed");
            AnchorSaveGame.Restore(s,player,beforeCraft);
            if(LostIsland.QuarterViewCamera.ZoomSize(24,10000,12,120)!=12||LostIsland.QuarterViewCamera.ZoomSize(24,-10000,12,120)!=120||LostIsland.QuarterViewCamera.ZoomSize(12,-1,12,120)<=12)throw new Exception("Zoom limits or reverse input failed");
            report.checks.Add("Crafting materials/consumption/equip and zoom hard limits/reverse input PASS");
            var supply=s.GetComponent<AnchorSupplyQuest>();var supplyBefore=AnchorSaveGame.Capture(s,player);
            supply.Interact(quest.Research);supply.Harvested("conifer-timber",5);if(supply.wood!=0)throw new Exception("Supply accepted harvest before crafting");
            s.Add("driftwood",2);s.Add("flint",2);s.Add("fiber-rope",1);AnchorCrafting.Craft(s,"stone-axe");
            s.Add("conifer-timber",5);supply.Harvested("conifer-timber",5);
            int tokens=AnchorCrafting.Count(s,"research-token"),timber=AnchorCrafting.Count(s,"conifer-timber");
            supply.Interact(quest.Research);supply.Interact(quest.Research);
            if(supply.stage!=2||AnchorCrafting.Count(s,"research-token")!=tokens+15||AnchorCrafting.Count(s,"conifer-timber")!=timber-5)throw new Exception("Supply submission/reward failed");
            var completedSupply=AnchorSaveGame.Capture(s,player);supply.stage=0;AnchorSaveGame.Restore(s,player,completedSupply);if(supply.stage!=2)throw new Exception("Supply save failed");
            AnchorSaveGame.Restore(s,player,supplyBefore);report.checks.Add("Supply quest crafting gate, timber submission, reward once and restore PASS");
        }
        static void TestHarvest(AnchorSession s){
            var nodes=UnityEngine.Object.FindObjectsByType<AnchorResourceNode>(FindObjectsSortMode.None);if(nodes.Length==0)throw new Exception("No resource nodes loaded");var node=nodes[0];
            if(node.Harvest("invalid-tool",out _))throw new Exception("Tool requirement ignored");
            if(!node.Harvest(node.definition.data.tool,out int amount)||amount<node.definition.data.yieldMin||amount>node.definition.data.yieldMax)throw new Exception("Harvest failed");
            if(node.Harvest(node.definition.data.tool,out _))throw new Exception("Depleted resource harvested twice");
            var day=s.Days;if(node.definition.data.respawnDays>0){s.AdvanceDays(node.definition.data.respawnDays+.01f);if(!node.Available)throw new Exception("Resource did not respawn");}
            s.AdvanceDays((float)(day-s.Days));report.checks.Add("Harvest: tool gate, yield, depletion and respawn passed.");
        }
        static void CaptureVillage(){
            var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;float size=cam.orthographicSize;
            foreach(var poi in UnityEngine.Object.FindObjectsByType<AnchorIslandPoiMarker>()){
                if(poi.id!="anchor-plaza"&&poi.id!="bio-research"&&poi.id!="frontier-market"&&poi.id!="silent-harbor")continue;
                if(!poi.transform.Find("Settlement dressing"))throw new Exception("Village dressing missing "+poi.id);
                cam.transform.position=poi.transform.position+new Vector3(-45,65,-55);cam.transform.LookAt(poi.transform.position);cam.orthographicSize=45;Capture("village-"+poi.id);
            }
            foreach(var door in UnityEngine.Object.FindObjectsByType<AnchorBuildingEntrance>())if(door.kind=="rest"){var target=door.transform.parent.position+Vector3.up*3;cam.transform.position=target+new Vector3(-18,16,-22);cam.transform.LookAt(target);cam.orthographicSize=11;Capture("architecture-detail");break;}
            cam.transform.SetPositionAndRotation(pos,rot);cam.orthographicSize=size;
        }
        static void Capture(string label){
            var cam=Camera.main;if(!cam)return;var previous=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1440,900,24);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var texture=new Texture2D(1440,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply();File.WriteAllBytes("Logs/AnchorIsland-"+label+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);}
            finally{cam.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
