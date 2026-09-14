using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorBuildingInterior : MonoBehaviour {
        public bool Inside=>room;
        public Vector3 ExteriorPosition {get;private set;}
        public string Title {get;private set;}
        AnchorExplorer player;GameObject room;Material wood,wall,cloth,stone;Camera lens;LostIsland.QuarterViewCamera follow;
        Vector3 offset;float minimum,maximum,size;GUIStyle label;AnchorBuildingEntrance nearby;
        readonly List<Material> materials=new List<Material>();
        void Start(){player=GetComponent<AnchorExplorer>();}
        Material Mat(Color color,string finish=""){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};m.SetFloat("_Smoothness",.12f);AnchorMaterialFinish.Apply(m,finish);materials.Add(m);return m;}
        Transform Part(string name,Vector3 p,Vector3 scale,Material m,bool collider=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(room.transform,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Destroy(g.GetComponent<Collider>());return g.transform;}
        void Table(Vector3 p){Part("Worktop",p+Vector3.up*1.3f,new Vector3(4,.2f,2),wood);foreach(int x in new[]{-1,1})Part("Table support",p+new Vector3(x*1.5f,.6f,0),new Vector3(.2f,1.2f,1.5f),wood);}
        public void Enter(AnchorBuildingEntrance entrance){
            if(Inside||!entrance)return;if(!player)player=GetComponent<AnchorExplorer>();
            ExteriorPosition=transform.position;Title=entrance.title;player.GetComponent<AnchorSettlementGuide>()?.Cancel();
            room=new GameObject("Interior - "+Title);room.transform.position=new Vector3(ExteriorPosition.x,400,ExteriorPosition.z);
            wood=Mat(new Color(.48f,.36f,.25f),"wood");wall=Mat(new Color(.72f,.68f,.55f),"plaster");cloth=Mat(new Color(.35f,.49f,.45f),"cloth");stone=Mat(new Color(.36f,.36f,.31f));
            Part("Backdrop",new Vector3(0,-.8f,0),new Vector3(200,.2f,200),stone);
            Part("Floor",new Vector3(0,-.2f,0),new Vector3(24,.4f,20),wood);
            for(int z=-9;z<=9;z++)Part("Floor board seam",new Vector3(0,.012f,z),new Vector3(24,.015f,.025f),stone,false);
            Part("Back wall",new Vector3(0,2.2f,10),new Vector3(24,4.4f,.3f),wall);Part("Right wall",new Vector3(12,2.2f,0),new Vector3(.3f,4.4f,20),wall);
            Part("Cutaway front wall",new Vector3(0,.35f,-10),new Vector3(24,.7f,.3f),wall);Part("Cutaway left wall",new Vector3(-12,.35f,0),new Vector3(.3f,.7f,20),wall);
            for(int x=-10;x<=10;x+=5){Part("Wall timber",new Vector3(x,2.2f,9.75f),new Vector3(.2f,4.4f,.2f),wood);Part("Window",new Vector3(x,2.6f,9.7f),new Vector3(2.6f,1.6f,.1f),cloth,false);}
            Part("Exit mat",new Vector3(0,.03f,-7),new Vector3(4,.04f,3),cloth,false);
            if(entrance.kind=="research"){
                Table(new Vector3(-5,0,3));Table(new Vector3(5,0,3));
                for(int i=0;i<5;i++)Part("Fossil specimen",new Vector3(-6+i*.7f,1.6f,3),new Vector3(.45f,.4f,.6f),stone,false);
                Part("Map tabletop",new Vector3(5,1.45f,3),new Vector3(3,.05f,1.4f),wall,false);
                Shelves(new Vector3(-9,0,7));Shelves(new Vector3(8,0,7));
            }else if(entrance.kind=="rest"){
                for(int i=0;i<3;i++){var p=new Vector3(-8+i*7,0,6);Part("Bed frame",p+Vector3.up*.45f,new Vector3(3,.9f,5),wood);Part("Blanket",p+Vector3.up*.95f,new Vector3(2.8f,.18f,4.6f),cloth);Part("Pillow",p+new Vector3(0,1.1f,1.7f),new Vector3(2,.25f,.8f),wall,false);}
                Table(new Vector3(5,0,-2));
            }else{
                Part("Service counter",new Vector3(0,1.1f,4),new Vector3(12,2.2f,2),wood);Shelves(new Vector3(-8,0,7));Shelves(new Vector3(7,0,7));
                for(int i=0;i<4;i++)Part("Supply crate",new Vector3(-9+i*2,1,-1),Vector3.one*1.6f,wall);
                if(entrance.kind=="harbor")Table(new Vector3(6,0,-2));
            }
            lens=Camera.main;follow=lens.GetComponent<LostIsland.QuarterViewCamera>();offset=follow.offset;minimum=follow.minZoom;maximum=follow.maxZoom;size=lens.orthographicSize;
            follow.offset=new Vector3(-16,23,-20);follow.minZoom=12;follow.maxZoom=18;follow.SetZoom(15);
            SetPosition(room.transform.position+new Vector3(0,1,-7));lens.transform.position=transform.position+follow.offset;lens.transform.rotation=Quaternion.LookRotation(-follow.offset);
            AnchorSession.Current.message=Title+" 입장 · J 나가기 · 실내에서도 C 제작 / I 가방 사용 가능";
        }
        void Shelves(Vector3 p){for(int level=0;level<3;level++){Part("Shelf",p+Vector3.up*(.5f+level*1.2f),new Vector3(5,.15f,1.5f),wood);for(int item=0;item<4;item++)Part("Storage",p+new Vector3(item-1.5f,.9f+level*1.2f,0),new Vector3(.7f,.65f,1),wall,false);}foreach(int side in new[]{-1,1})Part("Shelf post",p+new Vector3(side*2.3f,1.8f,0),new Vector3(.2f,3.6f,.2f),wood);}
        void SetPosition(Vector3 p){var c=GetComponent<CharacterController>();c.enabled=false;transform.position=p;c.enabled=true;}
        public void Exit(){if(!Inside)return;Destroy(room);room=null;foreach(var m in materials)if(m){if(m.mainTexture)Destroy(m.mainTexture);Destroy(m);}materials.Clear();follow.offset=offset;follow.minZoom=minimum;follow.maxZoom=maximum;follow.SetZoom(size);player.Teleport(ExteriorPosition.x,ExteriorPosition.z);lens.transform.position=transform.position+offset;lens.transform.rotation=Quaternion.LookRotation(-offset);AnchorSession.Current.message=Title+"에서 나왔습니다.";}
        void Update(){if(!player)return;var k=Keyboard.current;
            if(!Inside){nearby=null;float distance=6*6;foreach(var door in FindObjectsByType<AnchorBuildingEntrance>()){float d=(door.transform.position-transform.position).sqrMagnitude;if(d<distance){distance=d;nearby=door;}}}
            if(k!=null&&Application.isFocused&&!player.MenuOpen&&k.jKey.wasPressedThisFrame){if(Inside)Exit();else if(nearby)Enter(nearby);}
            if(Inside&&transform.position.y<398)SetPosition(room.transform.position+new Vector3(0,1,-7));
        }
        void OnGUI(){if(!player||player.MenuOpen)return;if(label==null)label=new GUIStyle(GUI.skin.label){font=player.font,fontSize=18,alignment=TextAnchor.MiddleCenter};var old=GUI.matrix;GUI.matrix=Matrix4x4.identity;if(Inside||nearby)GUI.Label(new Rect(Screen.width/2-280,Screen.height-115,560,35),Inside?Title+" · [J] 나가기":"[J] "+nearby.title+" 들어가기",label);GUI.matrix=old;}
        void OnDestroy(){if(room)Destroy(room);foreach(var m in materials)if(m){if(m.mainTexture)Destroy(m.mainTexture);Destroy(m);}}
    }
}
