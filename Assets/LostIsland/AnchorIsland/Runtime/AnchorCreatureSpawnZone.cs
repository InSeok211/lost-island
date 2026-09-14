using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorCreatureSpawnZone : MonoBehaviour {
        public AnchorCreatureDefinition definition;
        public string zoneId,spawnId;
        public Vector2 min,max;
        public Terrain terrain;
        public List<AnchorCreatureAgent> Live=new List<AnchorCreatureAgent>();
        bool counted,debugSpawn; float timer;
        public bool Aerial=>definition.data.role.Contains("pterosaur")||definition.data.role.Contains("aerial")||definition.data.role.Contains("arboreal");
        public bool Marine=>zoneId=="nearshore"&&!Aerial;
        public bool Accept(float x,float z){
            if(x<min.x||x>=max.x||z<min.y||z>=max.y||!AnchorSurface.InZone(x,z,zoneId))return false;
            float y=AnchorSurface.Height(x,z),sea=AnchorSurface.Layout.seaLevelMeters;
            return Marine ? y<sea-.6f : (Aerial?true:y>sea+.5f) && AnchorSurface.Clear(x,z,3);
        }
        public float SurfaceY(float x,float z){float h=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;return Marine?Mathf.Lerp(h,AnchorSurface.Layout.seaLevelMeters,.7f):h+(Aerial?7:.6f);}
        void Update(){timer-=Time.deltaTime;if(timer>0)return;timer=2;var s=AnchorSession.Current;if(!s)return;if(!s.Active(definition.data)){if(ShouldRetire())Despawn();return;}if(Live.Count==0)Spawn(false);}
        public bool ShouldRetire(){
            // Debug inspection herds stay until their tile unloads. Natural herds
            // leave after their active hours only when the player is far away.
            if(debugSpawn)return false;
            var player=UnityEngine.Object.FindAnyObjectByType<AnchorExplorer>();
            if(player)foreach(var agent in Live)if(agent&&(agent.transform.position-player.transform.position).sqrMagnitude<150*150)return false;
            return true;
        }
        public bool Spawn(bool force){
            var s=AnchorSession.Current;if(!s||!terrain||Live.Count>0||(!force&&!s.Active(definition.data))||!s.AllowGroup(definition.data,spawnId,force))return false;
            var r=new System.Random(AnchorSession.Stable(spawnId)); Vector3 center=Vector3.zero;bool found=false;
            for(int n=0;n<300;n++){float x=Mathf.Lerp(min.x,max.x,(float)r.NextDouble()),z=Mathf.Lerp(min.y,max.y,(float)r.NextDouble());if(Accept(x,z)){center=new Vector3(x,0,z);found=true;break;}}
            if(!found)return false;
            if(force){var player=UnityEngine.Object.FindAnyObjectByType<AnchorExplorer>();if(player){for(int n=0;n<24;n++){float angle=n*Mathf.PI/12;var q=player.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*18;if(Accept(q.x,q.z)){center=q;break;}}}}
            int count=r.Next(definition.data.groupMin,definition.data.groupMax+1);
            for(int i=0;i<count;i++){
                Vector3 p=center;for(int n=0;n<30;n++){var q=center+new Vector3((float)r.NextDouble()-.5f,0,(float)r.NextDouble()-.5f)*18;if(Accept(q.x,q.z)){p=q;break;}}
                p.y=SurfaceY(p.x,p.z);var g=Instantiate(definition.prefab,p,Quaternion.identity,transform);g.name=definition.data.displayName+" [Placeholder] "+i;
                var agent=g.AddComponent<AnchorCreatureAgent>();agent.definition=definition;agent.zone=this;agent.home=p;Live.Add(agent);
            }
            s.groups.TryGetValue(definition.data.id,out int old);s.groups[definition.data.id]=old+1;counted=true;debugSpawn=force;return true;
        }
        public void Despawn(){foreach(var a in Live)if(a)Destroy(a.gameObject);Live.Clear();debugSpawn=false;if(counted&&AnchorSession.Current){AnchorSession.Current.groups[definition.data.id]--;counted=false;}}
        void OnDisable(){Despawn();}
        void OnDrawGizmosSelected(){Gizmos.color=definition&&definition.data.danger>=4?Color.red:Color.cyan;Gizmos.DrawWireCube(new Vector3((min.x+max.x)*.5f,transform.position.y,(min.y+max.y)*.5f),new Vector3(max.x-min.x,15,max.y-min.y));}
    }
}
