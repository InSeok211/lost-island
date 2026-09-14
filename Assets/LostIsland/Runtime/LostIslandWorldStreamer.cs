using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LostIsland {
    public sealed class LostIslandWorldStreamer : MonoBehaviour {
        public Transform target;
        public TextAsset layoutAsset;
        public float worldSizeMeters=16000, tileSizeMeters=2000, seaLevel=150, refreshSeconds=.15f;
        public int tilesPerAxis=8, loadRadiusTiles=1;
        public string scenePrefix="LI_Tile_";
        public bool Busy { get; private set; }
        public WorldLayout Layout { get; private set; }
        public int LoadedCount { get { int count=0; for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).name.StartsWith(scenePrefix)) count++; return count; } }
        public Vector2Int CurrentTile => WorldCoordinates.Tile(target.position,worldSizeMeters,tileSizeMeters);
        public bool ReadyAt(Vector3 p) {
            var tile=WorldCoordinates.Tile(p,worldSizeMeters,tileSizeMeters);
            var scene=SceneManager.GetSceneByName(Name(tile.x,tile.y));
            return scene.IsValid() && scene.isLoaded;
        }
        string Name(int x,int z)=>$"{scenePrefix}{x}_{z}";
        IEnumerator Start() {
            if(layoutAsset) { Layout=JsonUtility.FromJson<WorldLayout>(layoutAsset.text); WorldSurface.Configure(Layout); }
            if(!target) yield break;
            while(true) {
                var c=CurrentTile;
                var desired=new HashSet<string>(); var order=new List<Vector2Int>();
                for(int z=c.y-loadRadiusTiles;z<=c.y+loadRadiusTiles;z++) for(int x=c.x-loadRadiusTiles;x<=c.x+loadRadiusTiles;x++)
                    if(x>=0 && z>=0 && x<tilesPerAxis && z<tilesPerAxis) { desired.Add(Name(x,z)); order.Add(new Vector2Int(x,z)); }
                order.Sort((a,b)=>(a-c).sqrMagnitude.CompareTo((b-c).sqrMagnitude));
                Busy=true;
                var remove=new List<string>();
                for(int i=0;i<SceneManager.sceneCount;i++) { var s=SceneManager.GetSceneAt(i); if(s.name.StartsWith(scenePrefix) && !desired.Contains(s.name)) remove.Add(s.name); }
                foreach(var s in remove) { var op=SceneManager.UnloadSceneAsync(s); if(op!=null) yield return op; }
                foreach(var tile in order) {
                    if(CurrentTile!=c) break; // Reconcile the next location after a teleport, never track a pending load as loaded.
                    var name=Name(tile.x,tile.y); var s=SceneManager.GetSceneByName(name);
                    if(s.IsValid() && s.isLoaded) continue;
                    if(!Application.CanStreamedLevelBeLoaded(name)) { Debug.LogError("Missing tile in Build Settings: "+name); enabled=false; yield break; }
                    var op=SceneManager.LoadSceneAsync(name,LoadSceneMode.Additive); if(op!=null) yield return op;
                }
                ConnectTerrains(); Busy=false;
                yield return new WaitForSecondsRealtime(refreshSeconds);
            }
        }
        void ConnectTerrains() {
            var lookup=new Dictionary<Vector2Int,Terrain>();
            foreach(var t in Terrain.activeTerrains) lookup[WorldCoordinates.Tile(t.transform.position+Vector3.one,worldSizeMeters,tileSizeMeters)]=t;
            foreach(var entry in lookup) { var c=entry.Key; lookup.TryGetValue(c+Vector2Int.left,out var left); lookup.TryGetValue(c+Vector2Int.up,out var top); lookup.TryGetValue(c+Vector2Int.right,out var right); lookup.TryGetValue(c+Vector2Int.down,out var bottom); entry.Value.SetNeighbors(left,top,right,bottom); }
        }
    }
}
