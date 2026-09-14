using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorSaveGame : MonoBehaviour {
        [Serializable] public class Item { public string id; public int count; }
        [Serializable] public class Harvest { public string id; public double day; }
        [Serializable] public class Data {
            public int version=1;
            public float x,z;
            public double days;
            public string tool;
            public int quest,collected;
            public bool observed;
            public int supplyStage,supplyWood;
            public bool supplyCrafted;
            public List<Item> items=new List<Item>();
            public List<Harvest> nodes=new List<Harvest>();
            public List<string> research=new List<string>();
        }
        public static string SavePath=>Path.Combine(Application.persistentDataPath,"AnchorIsland","save-v1.json");
        AnchorSession session; AnchorExplorer player; Data latest; float timer; bool blocked;
        bool Testing {
            get {
                if(Application.isBatchMode)return true;
#if UNITY_EDITOR
                if(UnityEditor.SessionState.GetBool("AnchorIsland.Validation",false))return true;
#endif
                return false;
            }
        }
        void Start(){
            if(Testing){enabled=false;return;}
            session=GetComponent<AnchorSession>();player=FindAnyObjectByType<AnchorExplorer>();
            if(!player){enabled=false;return;}
            if(File.Exists(SavePath)){
                try{Restore(session,player,Read(SavePath));session.message="저장한 탐험을 이어갑니다. F5 수동 저장 / 30초마다 자동 저장";}
                catch(Exception e){
                    try{Restore(session,player,Read(SavePath+".bak"));session.message="저장 파일을 읽지 못해 이전 백업으로 복구했습니다.";}
                    catch{blocked=true;session.message="저장 파일을 읽지 못했습니다. 기존 파일 보호를 위해 자동 저장을 중지했습니다.";}
                    Debug.LogWarning("Anchor save load: "+e.Message);
                }
            }
            latest=Capture(session,player);
        }
        void LateUpdate(){
            if(!session||!player)return;
            latest=Capture(session,player);timer+=Time.unscaledDeltaTime;
            var k=Keyboard.current;
            if(timer>=30||(Application.isFocused&&k!=null&&k.f5Key.wasPressedThisFrame))SaveNow();
        }
        public void SaveNow(){
            if(Testing||blocked||latest==null)return;
            try{Write(SavePath,latest);timer=0;if(session)session.message="탐험 저장 완료 · F5 수동 저장";}
            catch(Exception e){timer=0;if(session)session.message="저장 실패: "+e.Message;Debug.LogWarning("Anchor save: "+e.Message);}
        }
        void OnApplicationPause(bool paused){if(paused)SaveNow();}
        void OnDisable(){SaveNow();}
        public static Data Capture(AnchorSession s,AnchorExplorer p){
            var q=s.GetComponent<AnchorFirstSurvey>();var d=new Data{x=p.transform.position.x,z=p.transform.position.z,days=s.Days,tool=s.tool,quest=(int)q.stage,collected=q.collected,observed=q.observed};
            foreach(var pair in s.inventory)d.items.Add(new Item{id=pair.Key,count=pair.Value});
            var supply=s.GetComponent<AnchorSupplyQuest>();d.supplyStage=supply.stage;d.supplyWood=supply.wood;d.supplyCrafted=supply.crafted;
            var interior=p.GetComponent<AnchorBuildingInterior>();if(interior&&interior.Inside){d.x=interior.ExteriorPosition.x;d.z=interior.ExteriorPosition.z;}
            foreach(var pair in s.harvested)d.nodes.Add(new Harvest{id=pair.Key,day=pair.Value});
            d.research.AddRange(s.research);return d;
        }
        public static Data Read(string path){var d=JsonUtility.FromJson<Data>(File.ReadAllText(path));Validate(d);return d;}
        static void Validate(Data d){
            if(d!=null&&(d.supplyStage<0||d.supplyStage>2||d.supplyWood<0||d.supplyWood>5))throw new InvalidDataException("Invalid supply quest");
            if(d==null||d.version!=1||float.IsNaN(d.x)||float.IsNaN(d.z)||Mathf.Abs(d.x)>1980||Mathf.Abs(d.z)>1980||double.IsNaN(d.days)||double.IsInfinity(d.days)||d.days<0||d.quest<0||d.quest>2||d.collected<0||d.collected>5||d.items==null||d.nodes==null||d.research==null)throw new InvalidDataException("Unsupported or damaged save");
            foreach(var i in d.items)if(i==null||string.IsNullOrEmpty(i.id)||i.count<0)throw new InvalidDataException("Invalid inventory");
            foreach(var n in d.nodes)if(n==null||string.IsNullOrEmpty(n.id)||double.IsNaN(n.day)||double.IsInfinity(n.day)||n.day<0)throw new InvalidDataException("Invalid resource state");
        }
        public static void Write(string path,Data d){
            Validate(d);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(d,true));
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
        }
        public static void Restore(AnchorSession s,AnchorExplorer p,Data d){
            Validate(d);s.inventory.Clear();s.harvested.Clear();s.research.Clear();
            foreach(var i in d.items)s.inventory[i.id]=i.count;
            foreach(var n in d.nodes)s.harvested[n.id]=n.day;
            foreach(var r in d.research)if(!string.IsNullOrEmpty(r))s.research.Add(r);
            s.SetDays(d.days);s.tool=string.IsNullOrEmpty(d.tool)?"hand":d.tool;
            var q=s.GetComponent<AnchorFirstSurvey>();q.stage=(AnchorFirstSurvey.Stage)d.quest;q.collected=d.collected;q.observed=d.observed;q.progress=0;
            p.Teleport(d.x,d.z);
            var supply=s.GetComponent<AnchorSupplyQuest>();supply.stage=d.supplyStage;supply.wood=d.supplyWood;supply.crafted=d.supplyCrafted;
        }
    }
}
