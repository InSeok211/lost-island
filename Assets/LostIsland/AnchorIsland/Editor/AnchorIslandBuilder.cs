#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostIsland.AnchorIsland.Editor
{
    public static partial class AnchorIslandBuilder
    {
        public const string Root = "Assets/LostIsland/AnchorIsland/Generated";
        const string LayoutPath = "Assets/LostIsland/AnchorIsland/Data/anchor-island-layout.json";
        const string CreaturePath = "Assets/LostIsland/AnchorIsland/Data/anchor-island-creatures.json";
        const string ResourcePath = "Assets/LostIsland/AnchorIsland/Data/anchor-island-resources.json";

        static AnchorIslandLayout L;
        static CreatureCollection C;
        static ResourceCollection R;
        static Dictionary<string, GameObject> placeholders = new Dictionary<string, GameObject>();
        static System.Random rng;

        [MenuItem("Lost Island/Anchor Island/Build Complete Blockout")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            try {
            Load();
            rng = new System.Random(L.seed);
            PrepareFolders();
            EditorSceneManager.SaveScene(scratch,Root+"/BuildWorkspace.unity");
            CreatePlaceholders();
            CreateDefinitionsAndLayers();

            int tiles = Mathf.RoundToInt(L.worldSizeMeters / L.tileSizeMeters);
            var buildScenes = new List<EditorBuildSettingsScene>();

            for(int z=0; z<tiles; z++)
                for(int x=0; x<tiles; x++)
                    { EditorUtility.DisplayProgressBar("닻섬 생성",$"타일 {z*tiles+x+1}/16",(z*tiles+x)/16f); buildScenes.Add(BuildTile(x,z)); }

            buildScenes.Insert(0, BuildBootstrap());
            foreach(var prior in EditorBuildSettings.scenes) if(!prior.path.StartsWith(Root+"/"))buildScenes.Add(prior);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnchorIsland] 16 tiles built.");
            } finally {EditorUtility.ClearProgressBar();if(scratch.IsValid())EditorSceneManager.CloseScene(scratch,true);EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }

        static void Load()
        {
            L = JsonUtility.FromJson<AnchorIslandLayout>(File.ReadAllText(LayoutPath));
            C = JsonUtility.FromJson<CreatureCollection>(File.ReadAllText(CreaturePath));
            R = JsonUtility.FromJson<ResourceCollection>(File.ReadAllText(ResourcePath));
            if(L==null || C==null || R==null) throw new Exception("닻섬 JSON 로드 실패");
            AnchorSurface.Configure(L);
        }

        static EditorBuildSettingsScene BuildTile(int tx,int tz)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            string name=$"AnchorIsland_Tile_{tx}_{tz}";
            SceneManager.SetActiveScene(scene);

            float half=L.worldSizeMeters*.5f;
            Vector3 origin=new Vector3(-half+tx*L.tileSizeMeters,0,-half+tz*L.tileSizeMeters);
            var root=new GameObject(name);
            Terrain terrain=CreateTerrain(tx,tz,origin,root.transform);
            CreateWater(origin,root.transform);
            PopulateEnvironment(origin,terrain,root.transform);
            PlacePois(origin,terrain,root.transform);
            PlaceCreatureZones(origin,terrain,root.transform);
            PlaceResources(origin,terrain,root.transform);
            BuildRoads(origin,root.transform);

            string path=$"{Root}/Scenes/{name}.unity";
            EditorSceneManager.SaveScene(scene,path);
            EditorSceneManager.CloseScene(scene,true);
            return new EditorBuildSettingsScene(path,true);
        }

        static Terrain CreateTerrain(int tx,int tz,Vector3 origin,Transform parent)
        {
            string path=$"{Root}/TerrainData/Terrain_{tx}_{tz}.asset";
            Delete(path);
            var data=new TerrainData{
                heightmapResolution=L.heightmapResolution,
                alphamapResolution=L.alphamapResolution,
                baseMapResolution=512,
                size=new Vector3(L.tileSizeMeters,L.terrainHeightMeters,L.tileSizeMeters)
            };

            int res=data.heightmapResolution;
            var h=new float[res,res];
            for(int y=0;y<res;y++) for(int x=0;x<res;x++){
                float wx=origin.x+x/(float)(res-1)*L.tileSizeMeters;
                float wz=origin.z+y/(float)(res-1)*L.tileSizeMeters;
                h[y,x]=HeightMeters(wx,wz)/L.terrainHeightMeters;
            }
            data.SetHeights(0,0,h);
            AssetDatabase.CreateAsset(data,path);
            var go=Terrain.CreateTerrainGameObject(data);
            go.name=$"Terrain_{tx}_{tz}";
            go.transform.position=origin; go.transform.SetParent(parent);
            var terrain=go.GetComponent<Terrain>();terrain.materialTemplate=terrainMat;terrain.allowAutoConnect=true;terrain.groupingID=3;terrain.drawInstanced=true;terrain.heightmapPixelError=4;
            PaintTerrain(data,origin);
            return terrain;
        }

        public static float HeightMeters(float x,float z)=>AnchorSurface.Height(x,z);

        static float EllipseMask(float x,float z,float cx,float cz,float rx,float rz)
        {
            float dx=(x-cx)/rx, dz=(z-cz)/rz;
            return 1f-Mathf.Clamp01(Mathf.Sqrt(dx*dx+dz*dz));
        }

        static void PopulateEnvironment(Vector3 origin,Terrain terrain,Transform parent)
        {
            var root=new GameObject("Environment"); root.transform.SetParent(parent);
            foreach(var zone in L.zones){
                if(zone.type=="marine" || zone.type=="settlement") continue;
                rng=new System.Random(L.seed+AnchorSession.Stable(zone.id));
                int density = zone.biome=="forest"?11000 : zone.biome=="grassland"?4500 : zone.biome=="cliff"?1500 : zone.biome=="wetland"?4500 : zone.biome=="beach"?600 : 400;
                for(int i=0;i<density;i++){
                    Vector2 p=RandomInEllipse(zone);
                    if(!PointInTile(p.x,p.y,origin)) continue;
                    float y=HeightMeters(p.x,p.y);
                    if(y<L.seaLevelMeters+.5f || !AnchorSurface.InZone(p.x,p.y,zone.id) || !AnchorSurface.Clear(p.x,p.y,8)) continue;
                    float slope=terrain.terrainData.GetSteepness((p.x-origin.x)/1000,(p.y-origin.z)/1000);if(slope>38)continue;
                    string kind = zone.biome=="cliff" ? (Rnd()<.72f?"Rock":"Bush") :
                                  zone.biome=="beach" ? (Rnd()<.55f?"Driftwood":"Rock") :
                                  zone.biome=="wetland" ? (Rnd()<.55f?"Reed":"Fern") :
                                  zone.biome=="grassland" ? (Rnd()<.72f?"Grass":"Bush") :
                                  (Rnd()<.56f?"Tree":Rnd()<.72f?"Fern":"Rock");
                    Spawn(kind,new Vector3(p.x,terrain.SampleHeight(new Vector3(p.x,0,p.y)),p.y),root.transform,.8f,2f);
                }
            }
        }

        static void PlacePois(Vector3 origin,Terrain terrain,Transform parent)
        {
            var r=new GameObject("POI"); r.transform.SetParent(parent);
            foreach(var p in L.pois){
                if(!PointInTile(p.x,p.z,origin)) continue;
                float y=terrain.SampleHeight(new Vector3(p.x,0,p.z));
                string kind = p.type=="anchor"?"Anchor" :
                              p.type=="harbor"?"Harbor" :
                              p.type=="research"?"Research" :
                              p.type=="market"?"Market" :
                              p.type=="farm"?"Farm" :
                              p.type=="administration"?"Building" :
                              p.type=="warehouse"?"Warehouse" :
                              p.type=="outpost"||p.type=="checkpoint"?"Outpost" :
                              p.type=="wreck"?"Wreck" :
                              p.type=="camp"?"Camp" :
                              "Landmark";
                var go=Spawn(kind,new Vector3(p.x,y,p.z),r.transform,1f,1f);
                go.name="POI_"+p.displayName;
                go.transform.rotation=Quaternion.Euler(0,p.yaw,0);
                var m=go.AddComponent<AnchorIslandPoiMarker>();
                m.id=p.id; m.displayName=p.displayName; m.poiType=p.type; m.importance=p.importance;
                DressPoi(go,p);
            }
        }

        static void PlaceCreatureZones(Vector3 origin,Terrain terrain,Transform parent) {
            var root=new GameObject("CreatureSpawnZones");root.transform.SetParent(parent);
            foreach(var d in creatureDefs)foreach(var zid in d.data.zones) {
                var z=FindZone(zid);
                float minX=Mathf.Max(origin.x,z.x-z.radiusX),maxX=Mathf.Min(origin.x+1000,z.x+z.radiusX);
                float minZ=Mathf.Max(origin.z,z.z-z.radiusZ),maxZ=Mathf.Min(origin.z+1000,z.z+z.radiusZ);
                if(minX>=maxX||minZ>=maxZ)continue;
                var go=new GameObject("Spawn_"+d.data.displayName+"_"+zid);go.transform.SetParent(root.transform);
                go.transform.position=new Vector3((minX+maxX)/2,HeightMeters((minX+maxX)/2,(minZ+maxZ)/2),(minZ+maxZ)/2);
                var zone=go.AddComponent<AnchorCreatureSpawnZone>();zone.definition=d;zone.zoneId=zid;zone.terrain=terrain;
                zone.min=new Vector2(minX,minZ);zone.max=new Vector2(maxX,maxZ);zone.spawnId=d.data.id+":"+zid+":"+origin.x+":"+origin.z;
            }
        }

        static void PlaceResources(Vector3 origin,Terrain terrain,Transform parent)
        {
            var rr=new GameObject("ResourceNodes"); rr.transform.SetParent(parent);
            foreach(var res in R.resources){
                foreach(var zid in res.zones){
                    var z=FindZone(zid); if(z==null)continue;
                    rng=new System.Random(L.seed+AnchorSession.Stable(res.id+zid));
                    float areaKm2=Mathf.PI*z.radiusX*z.radiusZ/1_000_000f;
                    int count=Mathf.Clamp(Mathf.RoundToInt(res.spawnPerKm2*areaKm2),2,300);
                    for(int i=0;i<count;i++){
                        Vector2 p=Vector2.zero;bool valid=false;
                        for(int attempt=0;attempt<300;attempt++){p=RandomInEllipse(z);float ground=HeightMeters(p.x,p.y);if(AnchorSurface.InZone(p.x,p.y,zid)&&AnchorSurface.Clear(p.x,p.y,5)&&(z.type=="marine"?ground<L.seaLevelMeters-.5f:ground>L.seaLevelMeters+.5f)){valid=true;break;}}
                        if(!valid||!PointInTile(p.x,p.y,origin))continue;
                        float y=terrain.SampleHeight(new Vector3(p.x,0,p.y));
                        if(z.type!="marine" && y<L.seaLevelMeters+.5f)continue;
                        var go=Spawn(ResourceKind(res.category),new Vector3(p.x,y,p.y),rr.transform,.55f,1.25f);
                        go.name="Resource_"+res.displayName;
                        var n=go.AddComponent<AnchorResourceNode>();
                        n.definition=resourceDefs.Find(d=>d.data.id==res.id); n.zoneId=zid; n.nodeId=res.id+":"+zid+":"+i;
                        DecorateResource(go,res);

                    }
                }
            }
        }

        static EditorBuildSettingsScene BuildBootstrap()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var player=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name="DemoPlayer"; player.tag="Player";
            player.transform.position=new Vector3(0,HeightMeters(0,-18)+2,-18);
            player.GetComponent<Renderer>().sharedMaterial=Mat("Explorer",new Color(.93f,.38f,.15f));
            var cap=player.GetComponent<CapsuleCollider>(); if(cap) UnityEngine.Object.DestroyImmediate(cap);
            player.AddComponent<CharacterController>();
            var mover=player.AddComponent<AnchorExplorer>();
            mover.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/LostIsland/AnchorIsland/Fonts/malgun.ttf");

            var camgo=new GameObject("QuarterCamera"); camgo.tag="MainCamera";
            var cam=camgo.AddComponent<Camera>(); cam.farClipPlane=5000;cam.orthographic=true;cam.orthographicSize=24;camgo.AddComponent<AudioListener>();
            cam.backgroundColor=new Color(.12f,.35f,.43f);
            var follow=camgo.AddComponent<LostIsland.QuarterViewCamera>(); follow.target=player.transform;follow.minZoom=18;follow.maxZoom=50;follow.offset=new Vector3(-42,54,-42);
            camgo.transform.position=player.transform.position+follow.offset;camgo.transform.rotation=Quaternion.LookRotation(-follow.offset);mover.view=camgo.transform;

            var lightGo=new GameObject("Directional Light");
            var light=lightGo.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f;
            lightGo.transform.rotation=Quaternion.Euler(50,-35,0);

            light.shadows=LightShadows.Soft;light.intensity=1.8f;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.65f,.72f);
            var session=new GameObject("Anchor Session").AddComponent<AnchorSession>();
            session.layoutJson=AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);session.creatures=creatureDefs.ToArray();session.resources=resourceDefs.ToArray();
            var loader=new GameObject("Tile Streamer").AddComponent<AnchorTileStreamer>();loader.target=player.transform;mover.streamer=loader;

            string path=$"{Root}/Scenes/AnchorIsland_Bootstrap.unity";
            EditorSceneManager.SaveScene(scene,path);
            EditorSceneManager.CloseScene(scene,true);
            return new EditorBuildSettingsScene(path,true);
        }

        static void CreateWater(Vector3 origin,Transform parent)
        {
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name="Water"; water.transform.SetParent(parent);
            water.transform.position=new Vector3(origin.x+L.tileSizeMeters*.5f,L.seaLevelMeters,origin.z+L.tileSizeMeters*.5f);
            water.transform.localScale=Vector3.one*(L.tileSizeMeters/10f);
            var col=water.GetComponent<Collider>(); if(col) UnityEngine.Object.DestroyImmediate(col);
            var mat=Mat("Water",new Color(.06f,.32f,.52f,.48f));
            mat.SetFloat("_Surface",1);mat.SetFloat("_ZWrite",0);
            mat.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;
            water.GetComponent<Renderer>().sharedMaterial=mat;
            water.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void CreatePlaceholders()
        {
            string[] kinds={"Tree","Fern","Grass","Reed","Bush","Rock","Driftwood","Creature","Anchor","Harbor","Research","Market","Farm","Building","Warehouse","Outpost","Wreck","Camp","Landmark","Resource"};
            foreach(var k in kinds) placeholders[k]=CreatePrefab(k);
        }

        static GameObject CreatePrefab(string kind)
        {
            string path=$"{Root}/Prefabs/{kind}.prefab"; Delete(path);
            GameObject root=new GameObject(kind+"_Placeholder");

            if(kind=="Tree"){
                AddPrimitive(root,PrimitiveType.Cylinder,new Vector3(0,3,0),new Vector3(.55f,3,.55f),Mat("Trunk",new Color(.28f,.16f,.07f)));
                AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,7,0),new Vector3(4,5,4),Mat("Green",new Color(.10f,.34f,.12f)));
            } else if(kind=="Fern"||kind=="Grass"||kind=="Reed"||kind=="Bush"){
                AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,.8f,0),kind=="Reed"?new Vector3(.5f,2.8f,.5f):new Vector3(2,1.2f,2),Mat("Plant",new Color(.18f,.46f,.15f)));
            } else if(kind=="Rock"||kind=="Resource"){
                AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,.7f,0),new Vector3(2.6f,1.4f,2.2f),Mat("Rock",new Color(.36f,.36f,.34f)));
            } else if(kind=="Driftwood"){
                AddPrimitive(root,PrimitiveType.Cylinder,new Vector3(0,.35f,0),new Vector3(.35f,3.5f,.35f),Mat("Trunk",new Color(.28f,.16f,.07f)),Quaternion.Euler(0,0,90));
            } else if(kind=="Creature"){
                AddPrimitive(root,PrimitiveType.Capsule,new Vector3(0,1,0),new Vector3(1.2f,1.2f,1.2f),Mat("Creature",new Color(.55f,.32f,.18f)));
            } else {
                Color c = kind=="Anchor"?new Color(.1f,.65f,.9f):kind=="Harbor"?new Color(.35f,.22f,.1f):new Color(.55f,.50f,.38f);
                AddPrimitive(root,PrimitiveType.Cube,new Vector3(0,3,0),new Vector3(8,6,7),Mat(kind,c));
                if(kind=="Anchor") AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,9,0),Vector3.one*2.2f,Mat(kind,c));
            }

            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static void AddPrimitive(GameObject root,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Quaternion? rot=null)
        {
            var g=GameObject.CreatePrimitive(type); g.transform.SetParent(root.transform);
            g.transform.localPosition=pos; g.transform.localScale=scale;
            if(rot.HasValue)g.transform.localRotation=rot.Value;
            g.GetComponent<Renderer>().sharedMaterial=mat;
            if(root.name.Contains("Fern")||root.name.Contains("Grass")||root.name.Contains("Reed")||root.name.Contains("Bush")||root.name.Contains("Creature"))UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static Material Mat(string name,Color color)
        {
            string path=$"{Root}/Materials/{name}.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m){m.color=color;m.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(m);return m;}
            var sh=Shader.Find("Universal Render Pipeline/Lit"); if(!sh)sh=Shader.Find("Standard");
            m=new Material(sh){color=color};m.SetFloat("_Smoothness",.12f); AssetDatabase.CreateAsset(m,path); return m;
        }

        static GameObject Spawn(string kind,Vector3 p,Transform parent,float minS,float maxS)
        {
            if(!placeholders.TryGetValue(kind,out var prefab)) prefab=placeholders["Resource"];
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent); go.transform.position=p;
            var localRandom=new System.Random(AnchorSession.Stable(kind+":"+p.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+p.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            float s=Mathf.Lerp(minS,maxS,(float)localRandom.NextDouble()); go.transform.localScale*=s;
            go.transform.rotation=Quaternion.Euler(0,(float)localRandom.NextDouble()*360,0);
            return go;
        }

        static string ResourceKind(string cat)
        {
            if(cat=="tree")return "Tree";
            if(cat=="plant")return "Fern";
            if(cat=="shore")return "Driftwood";
            return "Resource";
        }

        static AnchorZone FindZone(string id){ foreach(var z in L.zones)if(z.id==id)return z; return null; }

        static Vector2 RandomInEllipse(AnchorZone z)
        {
            double a=rng.NextDouble()*Math.PI*2, r=Math.Sqrt(rng.NextDouble());
            return new Vector2(z.x+(float)(Math.Cos(a)*r*z.radiusX),z.z+(float)(Math.Sin(a)*r*z.radiusZ));
        }

        static bool PointInTile(float x,float z,Vector3 o)=>x>=o.x&&x<o.x+L.tileSizeMeters&&z>=o.z&&z<o.z+L.tileSizeMeters;
        static float Rnd()=>(float)rng.NextDouble();

        static void PrepareFolders(){
            Ensure(Root);
            foreach(var n in new[]{"Scenes","TerrainData","Prefabs","Materials","Definitions","Layers","Roads"})Ensure($"{Root}/{n}");
        }
        static void Ensure(string p){
            if(AssetDatabase.IsValidFolder(p))return;
            string par=Path.GetDirectoryName(p)?.Replace("\\","/"), name=Path.GetFileName(p);
            if(!string.IsNullOrEmpty(par)&&!AssetDatabase.IsValidFolder(par))Ensure(par);
            AssetDatabase.CreateFolder(par,name);
        }
        static void Delete(string p){ if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p))AssetDatabase.DeleteAsset(p); }
    }
}
#endif
