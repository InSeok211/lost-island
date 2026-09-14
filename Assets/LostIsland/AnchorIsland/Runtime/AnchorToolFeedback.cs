using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorToolFeedback : MonoBehaviour {
        Transform grip,head;AnchorExplorer explorer;Material wood,metal;string selected;
        float popupTime;string popup;GUIStyle style;
        void Start(){
            explorer=GetComponent<AnchorExplorer>();
            wood=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.35f,.2f,.1f)};
            metal=new Material(wood){color=new Color(.55f,.6f,.63f)};
            grip=new GameObject("Equipped tool").transform;grip.SetParent(transform,false);grip.localPosition=new Vector3(.65f,.3f,.35f);
            Part("Handle",Vector3.zero,new Vector3(.12f,1.2f,.12f),wood);
            head=Part("Tool head",new Vector3(0,.5f,0),Vector3.one*.4f,metal);
        }
        Transform Part(string name,Vector3 position,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(grip,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;Destroy(g.GetComponent<Collider>());return g.transform;}
        void LateUpdate(){
            var s=AnchorSession.Current;if(!s||!grip)return;
            if(selected!=s.tool){selected=s.tool;grip.gameObject.SetActive(selected!="hand");head.localScale=selected=="pickaxe"?new Vector3(.95f,.15f,.2f):selected=="knife"?new Vector3(.15f,.55f,.25f):selected=="shovel"?new Vector3(.45f,.6f,.1f):new Vector3(.55f,.4f,.15f);}
            var q=s.GetComponent<AnchorFirstSurvey>();bool gathering=q&&q.progress>0&&q.action=="채집 중 · E 유지"&&!explorer.MenuOpen;
            grip.localRotation=Quaternion.Euler(gathering?Mathf.Sin(Time.time*14)*65:10,0,-15);
        }
        public void Harvested(Vector3 position,string name,int amount){
            popup=name+" +"+amount;popupTime=Time.time+2;
            // Short-lived debris has no physics or collision and cannot affect harvesting.
            for(int i=0;i<8;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Harvest debris";g.transform.position=position+Vector3.up+Random.insideUnitSphere*.8f;g.transform.localScale=Vector3.one*.16f;g.GetComponent<Renderer>().sharedMaterial=wood?wood:new Material(Shader.Find("Universal Render Pipeline/Lit"));Destroy(g.GetComponent<Collider>());Destroy(g,.65f);}
        }
        void OnGUI(){if(Time.time>popupTime||string.IsNullOrEmpty(popup)||!explorer)return;if(style==null)style=new GUIStyle(GUI.skin.label){font=explorer.font,fontSize=23,alignment=TextAnchor.MiddleCenter};var old=GUI.matrix;GUI.matrix=Matrix4x4.identity;GUI.Label(new Rect(Screen.width/2-240,Screen.height*.65f,480,40),popup,style);GUI.matrix=old;}
        void OnDestroy(){if(wood)Destroy(wood);if(metal)Destroy(metal);}
    }
}
