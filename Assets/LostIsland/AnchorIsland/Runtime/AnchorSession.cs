using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorSession : MonoBehaviour {
        public static AnchorSession Current {get;private set;}
        public TextAsset layoutJson;
        public AnchorCreatureDefinition[] creatures;
        public AnchorResourceDefinition[] resources;
        public float hour=10, daySeconds=600;
        public double Days {get;private set;}
        public string tool="hand",message="앵커 광장에서 시작합니다. F1: 탐험 지도 / E: 채집 / Q: 관찰";
        public readonly Dictionary<string,int> inventory=new Dictionary<string,int>();
        public readonly Dictionary<string,double> harvested=new Dictionary<string,double>();
        public readonly HashSet<string> research=new HashSet<string>();
        public readonly Dictionary<string,int> groups=new Dictionary<string,int>();
        public readonly Dictionary<string,int> spawnAttempts=new Dictionary<string,int>();
        readonly Dictionary<string,float> nextSpawnAttempt=new Dictionary<string,float>();
        void Awake(){Current=this; AnchorSurface.Configure(JsonUtility.FromJson<AnchorIslandLayout>(layoutJson.text));if(!GetComponent<AnchorFirstSurvey>())gameObject.AddComponent<AnchorFirstSurvey>();if(!GetComponent<AnchorSaveGame>())gameObject.AddComponent<AnchorSaveGame>();}
        void OnDestroy(){if(Current==this)Current=null;}
        void Start(){if(!GetComponent<AnchorCrafting>())gameObject.AddComponent<AnchorCrafting>();if(!GetComponent<AnchorArtLighting>())gameObject.AddComponent<AnchorArtLighting>();}
        void Update(){ Days+=Time.deltaTime/daySeconds; hour=(10+(float)Days*24)%24; }
        public void AdvanceDays(float days){Days+=days;hour=(10+(float)Days*24)%24;}
        public void SetDays(double days){Days=days;hour=(10+(float)Days*24)%24;}
        public void Add(string id,int n){inventory.TryGetValue(id,out int old);inventory[id]=old+n;}
        public bool Active(CreatureDef d) => d.activity=="all" || (d.activity=="night" ? hour<6||hour>=19 : d.activity=="dawn_dusk" ? hour>=5&&hour<8||hour>=17&&hour<20 : hour>=6&&hour<19);
        public bool AllowGroup(CreatureDef d,string key,bool force=false) {
            groups.TryGetValue(d.id,out int count); if(count>=d.maxGroups)return false;
            if(force)return true;
            // maxGroups limits living herds, not lifetime attempts. Failed rolls can retry.
            if(nextSpawnAttempt.TryGetValue(key,out float next)&&Time.time<next)return false;
            spawnAttempts.TryGetValue(key,out int attempts);
            spawnAttempts[key]=attempts+1;
            var rng=new System.Random(Stable(key+":"+attempts));
            bool allowed=rng.NextDouble()<Mathf.Clamp01(d.spawnWeight);
            if(!allowed)nextSpawnAttempt[key]=Time.time+60f;
            else nextSpawnAttempt.Remove(key);
            return allowed;
        }
        public static int Stable(string text){unchecked{int hash=17;foreach(char c in text)hash=hash*31+c;return hash;}}
    }
}
