using System.Collections.Generic;
using UnityEngine;
namespace LostIsland {
    public sealed class LostIslandCreaturePreviewSpawner : MonoBehaviour, ILostIslandCreatureSpawner {
        public LostIslandSpawnZone zone;
        public int previewCount=6;
        public bool spawnOnStart=true;
        readonly List<GameObject> previews=new List<GameObject>();
        void Start() { if(spawnOnStart) Spawn(); }
        public void Spawn() {
            if(!zone || !zone.terrain || zone.creatureIds==null || zone.creatureIds.Length==0) return;
            Despawn(); var random=new System.Random(zone.seed);
            for(int i=0,tries=0;i<previewCount && tries<previewCount*20;tries++) {
                var p=zone.transform.position+new Vector3(((float)random.NextDouble()-.5f)*zone.size.x,0,((float)random.NextDouble()-.5f)*zone.size.y);
                var t=zone.terrain; var local=p-t.transform.position;
                if(local.x<0 || local.z<0 || local.x>=t.terrainData.size.x || local.z>=t.terrainData.size.z) continue;
                float ground=t.SampleHeight(p)+t.transform.position.y;
                if(zone.marine ? ground>zone.seaLevel-5 : ground<zone.seaLevel+3) continue;
                p.y=zone.marine?Mathf.Lerp(ground,zone.seaLevel,.7f):ground+1.5f;
                var obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.name="CreaturePreview_"+zone.creatureIds[i%zone.creatureIds.Length];
                obj.transform.SetParent(transform); obj.transform.position=p;
                obj.transform.localScale=new Vector3(1.2f,1.5f,1.2f); Destroy(obj.GetComponent<Collider>());
                previews.Add(obj); i++;
            }
        }
        public void Despawn() { foreach(var obj in previews) if(obj) Destroy(obj); previews.Clear(); }
    }
}
