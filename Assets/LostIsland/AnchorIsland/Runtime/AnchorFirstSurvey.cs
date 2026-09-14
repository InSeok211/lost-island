using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    // Quest state is persisted by AnchorSaveGame; transient action progress is not.
    public sealed class AnchorFirstSurvey : MonoBehaviour {
        public enum Stage { Available, Active, Completed }
        public Stage stage;
        public bool observed;
        public int collected;
        public float progress;
        void Awake(){if(!GetComponent<AnchorSupplyQuest>())gameObject.AddComponent<AnchorSupplyQuest>();}
        public string action;
        AnchorCreatureAgent watching;
        AnchorResourceNode gathering;
        bool latched;
        float harvestProgress;
        GUIStyle label;
        public Vector3 Research => new Vector3(-330,AnchorSurface.Height(-330,-180),-180);
        public bool Ready => stage==Stage.Active&&observed&&collected>=5&&Fiber>=5;
        int Fiber { get { AnchorSession.Current.inventory.TryGetValue("fern-fiber",out int n);return n; } }
        public bool Interact(Vector3 position) {
            var s=AnchorSession.Current;
            if(Vector3.Distance(position,Research)>32)return false;
            if(stage==Stage.Available){stage=Stage.Active;s.message="첫 생태 조사 수락: Hypsilophodon 관찰 + 고사리 섬유 5개 채집. 보상: 연구 토큰 10개";}
            else if(Ready){s.inventory["fern-fiber"]-=5;s.Add("research-token",10);stage=Stage.Completed;s.message="조사 완료! 고사리 섬유 5개 제출 / 연구 토큰 +10";}
            else s.message=stage==Stage.Completed?"첫 조사를 완료했습니다.":"관찰과 의뢰 수락 후 채집을 마치고 섬유 5개를 가져오세요.";
            return true;
        }
        public void RecordHarvest(string id,int amount){if(stage==Stage.Active&&id=="fern-fiber")collected=Mathf.Min(5,collected+amount);}
        public void RecordObservation(string id){if(stage==Stage.Active&&id=="hypsilophodon-foxii")observed=true;}
        public void TickObservation(AnchorCreatureAgent target,bool held,float dt,Vector3 position){
            if(!held||!target||Vector3.Distance(position,target.transform.position)>30){watching=null;progress=0;latched=false;return;}
            if(watching!=target){watching=target;progress=0;latched=false;}
            action="관찰 중 · Q 유지";
            if(latched)return;
            progress=Mathf.Min(3,progress+dt);
            if(progress>=3){target.Observe();RecordObservation(target.definition.data.id);latched=true;}
        }
        void Update(){
            var p=FindAnyObjectByType<AnchorExplorer>();var k=Keyboard.current;
            if(!p||k==null)return;
            bool enabledInput=Application.isFocused&&!p.MenuOpen&&p.streamer.Ready(p.transform.position);
            if(enabledInput&&k.fKey.wasPressedThisFrame)Interact(p.transform.position);
            bool q=enabledInput&&k.qKey.isPressed;
            TickObservation(p.NearestCreature(),q,Time.deltaTime,p.transform.position);
            if(q){gathering=null;harvestProgress=0;return;}
            var node=p.NearestResource();
            if(!enabledInput||!k.eKey.isPressed||!node){gathering=null;harvestProgress=0;progress=0;return;}
            if(gathering!=node){gathering=node;harvestProgress=0;}
            action="채집 중 · E 유지";harvestProgress+=Time.deltaTime*2;progress=harvestProgress;
            if(harvestProgress>=3){node.Harvest(AnchorSession.Current.tool,out _);gathering=null;harvestProgress=0;progress=0;}
        }
        Vector3 Target(out string name){
            name="연구원 · 가까이 가서 F";
            if(stage!=Stage.Active||Ready)return Research;
            var p=FindAnyObjectByType<AnchorExplorer>();Vector3 origin=p.transform.position;float best=float.MaxValue;Vector3 result=Vector3.zero;
            if(!observed){name="Hypsilophodon · 30m 안에서 Q 3초";foreach(var a in FindObjectsByType<AnchorCreatureAgent>())if(a.definition.data.id=="hypsilophodon-foxii"){float d=(a.transform.position-origin).sqrMagnitude;if(d<best){best=d;result=a.transform.position;}}}
            else {name="고사리 섬유 · 1번 맨손 / E 1.5초";foreach(var n in FindObjectsByType<AnchorResourceNode>())if(n.Available&&n.definition.data.id=="fern-fiber"){float d=(n.transform.position-origin).sqrMagnitude;if(d<best){best=d;result=n.transform.position;}}}
            if(best<float.MaxValue)return result;
            name=observed?"북부 숲 · 고사리 섬유 탐색":"북동 초지 · 낮에 생물 탐색";
            return observed?new Vector3(-100,80,630):new Vector3(620,69,430);
        }
        void OnGUI(){
            var p=FindAnyObjectByType<AnchorExplorer>();if(!p)return;
            if(p.GetComponent<AnchorBuildingInterior>()?.Inside==true)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=p.font,fontSize=15,wordWrap=true};
            var previous=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.6f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;
            if(!p.MenuOpen){
                GUI.Box(new Rect(20,190,440,140),GUIContent.none);
                string status=stage==Stage.Available?"연구원에게 첫 조사 의뢰 받기 [F]":stage==Stage.Completed?"완료 · 연구 토큰 10개 지급":$"Hypsilophodon 관찰: {(observed?"완료":"미완료")}\n고사리 섬유 채집: {collected}/5 · 보유 {Fiber}/5\n{(Ready?"연구원에게 돌아가 F로 제출":"보상: 연구 토큰 10개")}";
                GUI.Label(new Rect(35,200,410,125),"첫 생태 조사\n"+status,label);
                if(stage!=Stage.Completed){var target=Target(out string name);var camera=Camera.main;if(camera){var screen=camera.WorldToScreenPoint(target+Vector3.up*3);float x=screen.z>0?Mathf.Clamp(screen.x/scale,120,width-260):width/2;float y=screen.z>0?Mathf.Clamp(height-screen.y/scale,345,height-220):height-220;GUI.Box(new Rect(x,y,255,65),GUIContent.none);GUI.Label(new Rect(x+8,y+5,240,60),$"◆ {name}\n{Vector3.Distance(p.transform.position,target):0} m",label);}}
                if(Vector3.Distance(p.transform.position,Research)<=32)GUI.Label(new Rect(width/2-180,height-200,360,35),"[F] 연구원 — 의뢰 수락 / 제출",label);
                if(progress>0){GUI.Box(new Rect(width/2-150,height-260,300,50),GUIContent.none);GUI.Label(new Rect(width/2-140,height-255,280,40),$"{action}  {Mathf.Clamp01(progress/3)*100:0}%",label);}
            }
            GUI.matrix=previous;
        }
    }
}
