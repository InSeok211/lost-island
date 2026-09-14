using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    // A walking guide: no teleporting, no quest-state changes.
    public sealed class AnchorSettlementGuide : MonoBehaviour {
        public static readonly string[] Names={"앵커 광장","개척자 시장","세계생태연구원","침묵의 항구"};
        public static readonly Vector3[] Stops={new Vector3(0,0,-18),new Vector3(-130,0,-80),new Vector3(-348,0,-183),new Vector3(220,0,-730)};
        public bool Open {get;private set;}
        public int Destination {get;private set;}=-1;
        public readonly List<Vector3> Route=new List<Vector3>();
        AnchorExplorer player;int waypoint;float refresh;GUIStyle label,button;GameObject markerRoot;Material material;
        readonly List<Transform> markers=new List<Transform>();
        AnchorResidentIdle nearby;float residentCheck;
        public static Vector3 OnGround(Vector3 point){point.y=Mathf.Max(18,AnchorSurface.Height(point.x,point.z))+.3f;return point;}
        void Start(){player=GetComponent<AnchorExplorer>();material=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.8f,.68f,.32f)};markerRoot=new GameObject("Walking guide markers");for(int i=0;i<12;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Trail marker";g.transform.SetParent(markerRoot.transform);g.transform.localScale=new Vector3(.7f,.12f,.7f);g.GetComponent<Renderer>().sharedMaterial=material;Destroy(g.GetComponent<Collider>());g.SetActive(false);markers.Add(g.transform);}}
        public void Select(int destination){
            if(destination<0||destination>=Stops.Length)return;
            if(!player)player=GetComponent<AnchorExplorer>();Destination=destination;waypoint=0;Route.Clear();Open=false;
            // Join the closest settlement hub, then follow the existing spoke roads.
            int nearest=0;float best=float.MaxValue;for(int i=0;i<Stops.Length;i++){float d=Vector3.ProjectOnPlane(player.transform.position-Stops[i],Vector3.up).sqrMagnitude;if(d<best){best=d;nearest=i;}}
            if(nearest!=destination){
                Route.Add(OnGround(Stops[nearest]));
                if(nearest==1){Route.Add(OnGround(new Vector3(-119.6f,0,-86)));Route.Add(OnGround(new Vector3(-127.6f,0,-99.9f)));Route.Add(OnGround(new Vector3(-130,0,-116)));Route.Add(OnGround(new Vector3(-85,0,-115)));}
                if(nearest==2){Route.Add(OnGround(new Vector3(-352,0,-210)));Route.Add(OnGround(new Vector3(-305,0,-210)));Route.Add(OnGround(new Vector3(-195,0,-140)));Route.Add(OnGround(new Vector3(-145,0,-140)));Route.Add(OnGround(new Vector3(-85,0,-115)));}
                if(nearest==3){Route.Add(OnGround(new Vector3(244,0,-730)));Route.Add(OnGround(new Vector3(244,0,-690)));}
                if(nearest!=0&&destination!=0)Route.Add(OnGround(Stops[0]));
            }
            if(destination==1){Route.Add(OnGround(new Vector3(-85,0,-115)));Route.Add(OnGround(new Vector3(-130,0,-116)));Route.Add(OnGround(new Vector3(-127.6f,0,-99.9f)));Route.Add(OnGround(new Vector3(-119.6f,0,-86)));}
            if(destination==2){Route.Add(OnGround(new Vector3(-85,0,-115)));Route.Add(OnGround(new Vector3(-145,0,-140)));Route.Add(OnGround(new Vector3(-195,0,-140)));Route.Add(OnGround(new Vector3(-305,0,-210)));Route.Add(OnGround(new Vector3(-352,0,-210)));}
            if(destination==3){Route.Add(OnGround(new Vector3(244,0,-690)));Route.Add(OnGround(new Vector3(244,0,-730)));}
            Route.Add(OnGround(Stops[destination]));refresh=0;
            AnchorSession.Current.message=Names[destination]+" 길 안내 시작 · 금색 표식 / M 안내창";
        }
        public void Cancel(){Destination=-1;Route.Clear();foreach(var marker in markers)if(marker)marker.gameObject.SetActive(false);}
        void Update(){
            if(!player)return;var k=Keyboard.current;
            if(player.GetComponent<AnchorBuildingInterior>()&&player.GetComponent<AnchorBuildingInterior>().Inside){Open=false;Cancel();return;}
            residentCheck-=Time.deltaTime;if(residentCheck<=0){residentCheck=.3f;nearby=null;float best=7*7;foreach(var resident in FindObjectsByType<AnchorResidentIdle>()){float residentDistance=(resident.transform.position-player.transform.position).sqrMagnitude;if(residentDistance<best){best=residentDistance;nearby=resident;}}}
            if(k!=null&&Application.isFocused){if(k.mKey.wasPressedThisFrame)Open=!Open;if(k.escapeKey.wasPressedThisFrame||k.iKey.wasPressedThisFrame||k.cKey.wasPressedThisFrame||k.f1Key.wasPressedThisFrame)Open=false;}
            if(Destination<0)return;
            while(waypoint<Route.Count&&Vector3.ProjectOnPlane(Route[waypoint]-player.transform.position,Vector3.up).sqrMagnitude<(waypoint==Route.Count-1?6*6:3*3))waypoint++;
            if(waypoint>=Route.Count){AnchorSession.Current.message=Names[Destination]+" 도착";Cancel();return;}
            refresh-=Time.deltaTime;if(refresh>0)return;refresh=.2f;
            Vector3 target=Route[waypoint],direction=Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up);float distance=direction.magnitude;
            for(int i=0;i<markers.Count;i++){float step=(i+1)*4;bool visible=step<distance&&!player.MenuOpen;var p=OnGround(player.transform.position+direction.normalized*step);visible&=player.streamer.Ready(p);markers[i].gameObject.SetActive(visible);if(visible)markers[i].position=p;}
        }
        void OnGUI(){
            if(player&&player.GetComponent<AnchorBuildingInterior>()?.Inside==true)return;
            if(!player)return;if(label==null){label=new GUIStyle(GUI.skin.label){font=player.font,fontSize=16,wordWrap=true};button=new GUIStyle(GUI.skin.button){font=player.font,fontSize=16};}
            var old=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.6f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;
            if(Open){float x=width/2-230;GUI.Box(new Rect(x,190,460,350),GUIContent.none);GUI.Label(new Rect(x+20,205,420,50),"정착지 길 안내 · M / Esc 닫기\n목적지를 선택하면 걸어서 안내합니다.",label);for(int i=0;i<Names.Length;i++)if(GUI.Button(new Rect(x+20,270+i*45,420,36),Names[i],button))Select(i);if(GUI.Button(new Rect(x+20,460,420,35),"길 안내 취소",button)){Cancel();Open=false;}}
            else if(!player.MenuOpen){GUI.Label(new Rect(width-270,height-95,250,70),Destination<0?"M 정착지 길 안내":$"{Names[Destination]} 안내 중\n다음 지점 {Vector3.ProjectOnPlane(Route[waypoint]-player.transform.position,Vector3.up).magnitude:0}m · M 취소",label);}
            if(nearby&&!player.MenuOpen){GUI.Box(new Rect(width-320,280,300,115),GUIContent.none);GUI.Label(new Rect(width-308,288,276,100),nearby.name.Replace("Resident - ","")+"\n"+nearby.Hint,label);}
            GUI.matrix=old;
        }
        void OnDestroy(){if(markerRoot)Destroy(markerRoot);if(material)Destroy(material);}
    }
}
