using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace LostIsland.AnchorIsland.Editor {
    public static partial class AnchorIslandBuilder {
        static List<AnchorCreatureDefinition> creatureDefs=new List<AnchorCreatureDefinition>();
        static List<AnchorResourceDefinition> resourceDefs=new List<AnchorResourceDefinition>();
        static TerrainLayer[] layers;static Material terrainMat;
        [MenuItem("Lost Island/Anchor Island/Open Playable Island")]
        public static void OpenIsland(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Root+"/Scenes/AnchorIsland_Bootstrap.unity");}
        static void CreateDefinitionsAndLayers(){
            creatureDefs.Clear();resourceDefs.Clear();
            foreach(var c in C.creatures){var d=ScriptableObject.CreateInstance<AnchorCreatureDefinition>();d.data=c;d.prefab=CreaturePrefab(c);Save(d,Root+"/Definitions/Creature_"+c.id+".asset");creatureDefs.Add(d);}
            foreach(var r in R.resources){var d=ScriptableObject.CreateInstance<AnchorResourceDefinition>();d.data=r;d.prefab=placeholders[ResourceKind(r.category)];Save(d,Root+"/Definitions/Resource_"+r.id+".asset");resourceDefs.Add(d);}
            var colors=new[]{new Color(.32f,.45f,.21f),new Color(.16f,.29f,.14f),new Color(.75f,.65f,.44f),new Color(.34f,.37f,.38f),new Color(.28f,.34f,.20f),new Color(.46f,.40f,.29f)};
            layers=new TerrainLayer[colors.Length];
            for(int i=0;i<colors.Length;i++){
                var texture=new Texture2D(32,32);var pixels=new Color[1024];for(int p=0;p<pixels.Length;p++)pixels[p]=colors[i]*Mathf.Lerp(.91f,1.08f,Mathf.PerlinNoise((p%32)*.31f,(p/32)*.31f));texture.SetPixels(pixels);texture.Apply();
                Save(texture,Root+"/Layers/Texture_"+i+".asset");var layer=new TerrainLayer{diffuseTexture=texture,tileSize=new Vector2(10,10),smoothness=0};Save(layer,Root+"/Layers/Layer_"+i+".terrainlayer");layers[i]=layer;
            }
            terrainMat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));Save(terrainMat,Root+"/Materials/Terrain.mat");
        }
        static void Save(UnityEngine.Object obj,string path){Delete(path);AssetDatabase.CreateAsset(obj,path);}
        static void PaintTerrain(TerrainData data,Vector3 origin){
            data.terrainLayers=layers;int n=data.alphamapResolution;float[,,] alpha=new float[n,n,layers.Length];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float wx=origin.x+x/(float)(n-1)*1000,wz=origin.z+z/(float)(n-1)*1000;
                var zone=AnchorSurface.Zone(wx,wz);float h=HeightMeters(wx,wz);int index=h<21?2:zone?.biome=="forest"?1:zone?.biome=="cliff"?3:zone?.biome=="wetland"?4:zone?.type=="settlement"?5:0;
                if(AnchorSurface.RoadDistance(wx,wz,out float roadWidth)<roadWidth*.65f)index=2;
                alpha[z,x,index]=1;
            }data.SetAlphamaps(0,0,alpha);
        }
        static GameObject CreaturePrefab(CreatureDef c){
            var root=new GameObject(c.displayName+"_Placeholder");
            bool marine=c.role.Contains("marine")||c.role=="ammonite"||c.role=="belemnite";
            bool bird=c.role.Contains("pterosaur")||c.role.Contains("aerial")||c.role.Contains("arboreal");
            Color color=c.danger>=4?new Color(.63f,.17f,.13f):marine?new Color(.18f,.52f,.63f):bird?new Color(.79f,.64f,.32f):new Color(.48f,.59f,.27f);
            var mat=Mat("Species_"+c.id,color);float size=c.role=="large-herbivore"?2:c.role.Contains("small")?.8f:1.2f;
            AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,.8f,0),new Vector3(size, size*.8f,size*1.8f),mat);
            AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,1,1.1f*size),Vector3.one*size*.65f,mat);
            if(bird){AddPrimitive(root,PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(size*4,.12f,size*.6f),mat);}
            else if(c.role=="ammonite"){AddPrimitive(root,PrimitiveType.Cylinder,Vector3.up*.8f,new Vector3(1.3f,.2f,1.3f),Mat("Shell",new Color(.84f,.75f,.54f)),Quaternion.Euler(90,0,0));}
            else {AddPrimitive(root,PrimitiveType.Sphere,new Vector3(0,.7f,-size),new Vector3(size*.3f,size*.25f,size*1.7f),mat);}
            if(c.id.StartsWith("plesiosaurus")||c.id.StartsWith("leptocleidus"))AddPrimitive(root,PrimitiveType.Capsule,new Vector3(0,1,size*1.9f),new Vector3(.3f,1.2f,.3f),mat,Quaternion.Euler(65,0,0));
            foreach(var col in root.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
            var path=Root+"/Prefabs/Creature_"+c.id+".prefab";Delete(path);var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static void Piece(GameObject root,string name,Vector3 p,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube,Quaternion? rotation=null){AddPrimitive(root,type,p,size,mat,rotation);root.transform.GetChild(root.transform.childCount-1).name=name;}
        static void DressPoi(GameObject root,AnchorPoi poi){
            // Replace the generic starter body with recognizable, explicitly placeholder structures.
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            while(root.transform.childCount>0)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            var wood=Mat("Timber",new Color(.35f,.23f,.13f));var stone=Mat("Masonry",new Color(.53f,.55f,.49f));var roof=Mat("Roof",new Color(.22f,.36f,.38f));var canvas=Mat("Canvas",new Color(.85f,.67f,.34f));
            Piece(root,"Foundation",new Vector3(0,.15f,0),new Vector3(23,.3f,21),stone);
            if(poi.type=="anchor"){
                Piece(root,"Plaza",new Vector3(0,.06f,0),new Vector3(48,.12f,48),stone);
                Piece(root,"Anchor pedestal",Vector3.up, new Vector3(6,2,6),wood);
                Piece(root,"Anchor beacon",Vector3.up*7,new Vector3(2,5,2),Mat("AnchorCrystal",new Color(.1f,.82f,.94f)),PrimitiveType.Cylinder);
                Piece(root,"Anchor crown",Vector3.up*13,Vector3.one*4,Mat("AnchorCrystal",Color.cyan),PrimitiveType.Sphere);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Piece(root,"Plaza post",new Vector3(Mathf.Cos(a)*20,1.4f,Mathf.Sin(a)*20),new Vector3(.7f,2.8f,.7f),wood);}
            }else if(poi.type=="market"){
                for(int i=0;i<6;i++){float x=(i%3-1)*7,z=(i/3*2-1)*6;Piece(root,"Market stall",new Vector3(x,1,z),new Vector3(5,2,3),wood);Piece(root,"Awning",new Vector3(x,4,z),new Vector3(6,.35f,4),canvas);for(int k=-1;k<=1;k+=2)Piece(root,"Awning pole",new Vector3(x+k*2.5f,2,z),new Vector3(.2f,4,.2f),wood);}
            }else if(poi.type=="farm"){
                Piece(root,"Cultivated field",new Vector3(0,.2f,0),new Vector3(65,.4f,45),Mat("Soil",new Color(.24f,.17f,.10f)));
                for(int i=0;i<10;i++)Piece(root,"Crop row",new Vector3(-28+i*6,.9f,0),new Vector3(2,1.4f,40),Mat("Crop",new Color(.61f,.61f,.26f)));
                Building(root,new Vector3(-20,0,30),new Vector3(14,8,12),wood,roof);
            }else if(poi.type=="checkpoint"){
                foreach(int side in new[]{-1,1}){Building(root,new Vector3(side*12,0,0),new Vector3(7,11,7),stone,roof);Piece(root,"Restricted fence",new Vector3(side*33,2,0),new Vector3(32,4,.5f),wood);}
                Piece(root,"Gate overhead",new Vector3(0,8,0),new Vector3(20,2,2),Mat("Warning",new Color(.67f,.25f,.14f)));
            }else if(poi.type=="wreck"){
                Piece(root,"Broken hull",new Vector3(0,2,0),new Vector3(9,3,23),wood,PrimitiveType.Cube,Quaternion.Euler(8,0,16));
                for(int i=0;i<6;i++)Piece(root,"Hull rib",new Vector3(0,4,-10+i*4),new Vector3(10,.3f,.4f),wood);
            }else if(poi.type=="landmark"){
                Piece(root,"Lookout tower",Vector3.up*9,new Vector3(5,18,5),stone,PrimitiveType.Cylinder);Piece(root,"Lantern",Vector3.up*20,new Vector3(7,4,7),canvas);
            }else{
                Building(root,Vector3.zero,new Vector3(poi.type=="warehouse"?24:18,poi.type=="administration"?12:8,14),poi.type=="research"?stone:wood,roof);
                Piece(root,"Entrance porch",new Vector3(0,.4f,-10),new Vector3(10,.8f,6),stone);
                if(poi.type=="research")Piece(root,"Observation dome",Vector3.up*11,Vector3.one*7,Mat("Glass",new Color(.33f,.65f,.68f)),PrimitiveType.Sphere);
                if(poi.type=="harbor")BuildPier(root,poi,wood);
                if(poi.type=="warehouse")for(int i=0;i<7;i++)Piece(root,"Cargo crate",new Vector3(-15+(i%3)*3,1,12+(i/3)*3),Vector3.one*2,canvas);
            }
        }
        static void Building(GameObject root,Vector3 p,Vector3 size,Material walls,Material roof){Piece(root,"Building walls",p+Vector3.up*size.y*.5f,size,walls);Piece(root,"Roof",p+Vector3.up*(size.y+.7f),new Vector3(size.x+2,1.4f,size.z+2),roof);Piece(root,"Door",p+new Vector3(0,1.6f,-size.z*.5f-.03f),new Vector3(2.5f,3.2f,.12f),Mat("Door",new Color(.13f,.17f,.17f)));}
        static void BuildPier(GameObject root,AnchorPoi poi,Material wood){
            // Ramp from the plateau into the inlet; shared height profile stays below walkable slope limits.
            for(int i=0;i<44;i++){
                float z=poi.z-14-i*10;float top=Mathf.Lerp(root.transform.position.y,21,Mathf.Clamp01(i/20f));
                Piece(root,"Pier deck",new Vector3(0,top-root.transform.position.y-.25f,z-poi.z),new Vector3(12,.5f,10.05f),wood);
                foreach(int side in new[]{-1,1})Piece(root,"Pier piling",new Vector3(side*5,(top+5)/2-root.transform.position.y,z-poi.z),new Vector3(.7f,top-5,.7f),wood);
            }
        }
        static void DecorateResource(GameObject go,ResourceDef data){
            var color=data.rarity=="rare"?new Color(.28f,.75f,.8f):data.rarity=="artifact"?new Color(.86f,.55f,.21f):new Color(.86f,.79f,.43f);
            AddPrimitive(go,PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(3.4f,.07f,3.4f),Mat("Node_"+data.rarity,color));
            var c=go.transform.GetChild(go.transform.childCount-1).GetComponent<Collider>();UnityEngine.Object.DestroyImmediate(c);
        }
        static void BuildRoads(Vector3 origin,Transform parent){
            var root=new GameObject("Roads");root.transform.SetParent(parent);
            foreach(var road in L.roads){
                var a=Array.Find(L.pois,p=>p.id==road.from);var b=Array.Find(L.pois,p=>p.id==road.to);
                var start=new Vector3(a.x,0,a.z);var end=new Vector3(b.x,0,b.z);float distance=Vector3.Distance(start,end);int steps=Mathf.CeilToInt(distance);
                var vertices=new List<Vector3>();var indices=new List<int>();var side=Vector3.Cross((end-start).normalized,Vector3.up)*road.width*.5f;
                for(int i=0;i<steps;i++){
                    var p=Vector3.Lerp(start,end,i/(float)steps);var q=Vector3.Lerp(start,end,(i+1)/(float)steps);var mid=(p+q)*.5f;if(!PointInTile(mid.x,mid.z,origin))continue;
                    var pts=new[]{p-side,p+side,q-side,q+side};int offset=vertices.Count;
                    foreach(var v in pts)vertices.Add(new Vector3(v.x,HeightMeters(v.x,v.z)+.4f,v.z));
                    indices.AddRange(new[]{offset,offset+1,offset+2,offset+1,offset+3,offset+2});
                }
                if(vertices.Count==0)continue;
                var mesh=new Mesh{name=road.id};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();
                Save(mesh,$"{Root}/Roads/{road.id}_{origin.x}_{origin.z}.asset");
                var go=new GameObject(road.id,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Mat("Road",new Color(.56f,.47f,.32f));
            }
        }
    }
}
