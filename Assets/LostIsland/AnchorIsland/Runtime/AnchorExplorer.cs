using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    [RequireComponent(typeof(CharacterController))]
    public sealed class AnchorExplorer : MonoBehaviour {
        public AnchorTileStreamer streamer;public Transform view; public Font font;
        public float speed=8,sprint=2.5f;public Vector2 testInput;
        CharacterController controller;float velocity;bool map,inventory;Vector2 scroll;GUIStyle text,title;
        readonly string[] tools={"hand","axe","knife","shovel","pickaxe","salvage_tool"};
        void Awake(){controller=GetComponent<CharacterController>();if(!GetComponent<AnchorExplorerAppearance>())gameObject.AddComponent<AnchorExplorerAppearance>();if(!GetComponent<AnchorToolFeedback>())gameObject.AddComponent<AnchorToolFeedback>();if(!GetComponent<AnchorSettlementGuide>())gameObject.AddComponent<AnchorSettlementGuide>();if(!GetComponent<AnchorBuildingInterior>())gameObject.AddComponent<AnchorBuildingInterior>();}
        public bool MenuOpen=>map||inventory||(GetComponent<AnchorSettlementGuide>()&&GetComponent<AnchorSettlementGuide>().Open)||(AnchorSession.Current&&AnchorSession.Current.GetComponent<AnchorCrafting>()&&AnchorSession.Current.GetComponent<AnchorCrafting>().Open);
        public void Teleport(float x,float z){var interior=GetComponent<AnchorBuildingInterior>();if(interior&&interior.Inside)interior.Exit();controller.enabled=false;transform.position=new Vector3(x,Mathf.Max(18,AnchorSurface.Height(x,z))+2,z);controller.enabled=true;velocity=0;}
        void Update(){
            var s=AnchorSession.Current;if(!s)return;var k=Keyboard.current;Vector2 input=testInput;
            if(k!=null&&Application.isFocused){
                if(k.f1Key.wasPressedThisFrame){map=!map;inventory=false;}if(k.iKey.wasPressedThisFrame){inventory=!inventory;map=false;}
                if(k.escapeKey.wasPressedThisFrame){map=false;inventory=false;}
                if(k.cKey.wasPressedThisFrame){map=false;inventory=false;}
                if(k.mKey.wasPressedThisFrame){map=false;inventory=false;}
                if(k.rKey.wasPressedThisFrame)Teleport(0,-18);
                for(int i=0;i<6;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)s.tool=tools[i];
                if(!MenuOpen)input+=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
            }
            if(!streamer.Ready(transform.position))return;
            input=Vector2.ClampMagnitude(input,1);var f=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;var r=Vector3.ProjectOnPlane(view.right,Vector3.up).normalized;
            var dir=f*input.y+r*input.x;var delta=dir*speed*(k!=null&&k.leftShiftKey.isPressed?sprint:1)*Time.deltaTime;
            var next=transform.position+delta;if(Mathf.Abs(next.x)>1980||Mathf.Abs(next.z)>1980||!streamer.Ready(next))delta=Vector3.zero;
            if(controller.isGrounded&&velocity<0)velocity=-2;velocity-=25*Time.deltaTime;
            controller.Move(delta+Vector3.up*velocity*Time.deltaTime);
            float ground=AnchorSurface.Height(transform.position.x,transform.position.z);
            if(transform.position.y<ground-3)Teleport(transform.position.x,transform.position.z);
            if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),1-Mathf.Exp(-12*Time.deltaTime));
            if(transform.position.y<19 && AnchorSurface.Height(transform.position.x,transform.position.z)<18){controller.enabled=false;transform.position=new Vector3(transform.position.x,19,transform.position.z);controller.enabled=true;velocity=0;}
        }
        public AnchorResourceNode NearestResource(){AnchorResourceNode best=null;float d=5*5;foreach(var n in FindObjectsByType<AnchorResourceNode>(FindObjectsSortMode.None)){float q=(n.transform.position-transform.position).sqrMagnitude;if(n.Available&&q<d){best=n;d=q;}}return best;}
        public AnchorCreatureAgent NearestCreature(){AnchorCreatureAgent best=null;float d=30*30;foreach(var a in FindObjectsByType<AnchorCreatureAgent>(FindObjectsSortMode.None)){float q=(a.transform.position-transform.position).sqrMagnitude;if(q<d){best=a;d=q;}}return best;}
        void OnGUI(){
            var s=AnchorSession.Current;if(!s)return;
            if(text==null){text=new GUIStyle(GUI.skin.label){font=font,fontSize=15,wordWrap=true};text.normal.textColor=new Color(.88f,.92f,.84f);title=new GUIStyle(text){fontSize=21,fontStyle=FontStyle.Bold};}
            var oldMatrix=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.6f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float sw=Screen.width/scale,sh=Screen.height/scale;
            var interior=GetComponent<AnchorBuildingInterior>();
            if(interior&&interior.Inside){GUI.Box(new Rect(20,20,510,105),GUIContent.none);GUI.Label(new Rect(38,30,470,30),interior.Title,title);GUI.Label(new Rect(38,66,475,50),"WASD 이동 · 휠 줌 · J 나가기\nI 가방 · C 제작 · F5 저장 (재접속 시 건물 입구)",text);DrawInventory(s,sw);GUI.matrix=oldMatrix;return;}
            GUI.Box(new Rect(20,20,510,155),GUIContent.none);GUI.Label(new Rect(38,30,470,30),"닻섬  /  ANCHOR ISLAND",title);
            var zone=AnchorSurface.Zone(transform.position.x,transform.position.z);
            GUI.Label(new Rect(38,66,475,28),$"{zone?.displayName??"외곽 해안"}   위험 {zone?.danger??1}   {s.hour:00}:00   타일 {streamer.Loaded} {(streamer.busy?"로딩 중":"")}",text);
            GUI.Label(new Rect(38,96,475,70),"WASD 이동 · Shift 달리기 · 휠 줌 · I 가방\nE 1.5초 채집 · Q 3초 관찰 · F 연구원\nF1 검수 · F5 저장 · R 귀환 · 도구: "+AnchorHud.Name(s,s.tool)+" (1–6)",text);
            GUI.Box(new Rect(20,sh-100,Mathf.Min(790,sw-40),80),GUIContent.none);GUI.Label(new Rect(38,sh-91,Mathf.Min(750,sw-76),65),s.message,text);
            var resource=NearestResource();if(resource){var d=resource.definition.data;GUI.Label(new Rect(sw/2-220,sh-160,440,60),$"[E] {d.displayName} · {d.rarity}\n{AnchorHud.Name(s,d.tool)} / 수확 {d.yieldMin}–{d.yieldMax}",title);}
            var animal=NearestCreature();if(animal)GUI.Label(new Rect(sw/2-240,sh-350,480,70),$"[Q] {animal.definition.data.displayName}\n위험 {animal.definition.data.danger} · {animal.definition.data.aggression} · {animal.definition.data.activity}",text);
            foreach(var poi in FindObjectsByType<AnchorIslandPoiMarker>(FindObjectsSortMode.None))if(Vector3.Distance(poi.transform.position,transform.position)<95){var p=Camera.main.WorldToScreenPoint(poi.transform.position+Vector3.up*12);if(p.z>0)GUI.Label(new Rect(p.x/scale-100,sh-p.y/scale,250,35),poi.displayName,title);}
            if(map){
                GUI.Box(new Rect(20,188,510,480),GUIContent.none);GUI.Label(new Rect(38,198,470,30),"탐험 위치 선택 — 테스트용 이동",title);
                int row=0;foreach(var z in AnchorSurface.Layout.zones){if(GUI.Button(new Rect(38,240+row*35,465,29),z.displayName+" / 위험 "+z.danger)){Teleport(z.x,z.z);map=false;}row++;}
                if(GUI.Button(new Rect(38,535,225,30),"시간 +6시간"))s.AdvanceDays(.25f);
                if(GUI.Button(new Rect(275,535,228,30),"현재 구역 스폰 검수")){int count=0;foreach(var z in FindObjectsByType<AnchorCreatureSpawnZone>(FindObjectsSortMode.None))if(z.zoneId==zone?.id&&z.Spawn(true))count++;s.message="검수용 강제 스폰: "+count+" 무리. 자연 출현 확률과 별도.";}
                if(GUI.Button(new Rect(38,576,465,30),"현재 구역 가장 가까운 채집물로 이동")){AnchorResourceNode closest=null;float distance=float.MaxValue;foreach(var n in FindObjectsByType<AnchorResourceNode>(FindObjectsSortMode.None)){if(n.zoneId!=zone?.id)continue;float d=(n.transform.position-transform.position).sqrMagnitude;if(d<distance){distance=d;closest=n;}}if(closest){Teleport(closest.transform.position.x+3,closest.transform.position.z);s.tool=closest.definition.data.tool;map=false;}}
                GUI.Label(new Rect(38,617,465,40),"근해는 검수용 수면 이동을 지원합니다.",text);
            }
            if(!MenuOpen)AnchorHud.MiniMap(this,s,sw,text);
            DrawInventory(s,sw);GUI.matrix=oldMatrix;
        }
        void DrawInventory(AnchorSession s,float sw){if(inventory){GUI.Box(new Rect(sw-350,20,330,500),GUIContent.none);GUI.Label(new Rect(sw-330,32,290,30),"채집 가방 / 연구 "+s.research.Count,title);scroll=GUI.BeginScrollView(new Rect(sw-330,75,290,420),scroll,new Rect(0,0,270,s.inventory.Count*55+40));int i=0;foreach(var item in s.inventory)GUI.Label(new Rect(0,i++*55,270,55),AnchorHud.Name(s,item.Key)+" × "+item.Value,text);GUI.EndScrollView();}}
    }
}
