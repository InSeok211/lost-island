using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorHypsilophodon : MonoBehaviour {
        AnchorCreatureAgent agent;Transform left,right,head,tail;float phase;Vector3 origin;
        static Material skin,belly;
        public string State {get;private set;}="대기";
        Transform Piece(string name,Vector3 p,Vector3 size,Material material){
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(transform,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go.transform;
        }
        void Start(){
            agent=GetComponent<AnchorCreatureAgent>();origin=agent.home;phase=(AnchorSession.Stable(name)&255)*.07f;
            foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;
            if(!skin){skin=new Material(Shader.Find("Universal Render Pipeline/Lit"));skin.color=new Color(.27f,.42f,.2f);belly=new Material(skin);belly.color=new Color(.68f,.65f,.4f);}
            transform.localScale=Vector3.one;
            Piece("Torso",new Vector3(0,.65f,0),new Vector3(.65f,.8f,1.25f),skin);
            Piece("Breast",new Vector3(0,.7f,.45f),new Vector3(.5f,.6f,.5f),belly);
            head=Piece("Head",new Vector3(0,1.25f,.7f),new Vector3(.4f,.42f,.65f),skin);
            tail=Piece("Long balancing tail",new Vector3(0,.65f,-1.15f),new Vector3(.28f,.3f,1.8f),skin);
            left=Piece("Left hind leg",new Vector3(-.3f,.05f,-.1f),new Vector3(.22f,1,.3f),skin);
            right=Piece("Right hind leg",new Vector3(.3f,.05f,-.1f),new Vector3(.22f,1,.3f),skin);
            for(int i=-1;i<=1;i+=2){Piece("Forelimb",new Vector3(i*.25f,.65f,.55f),new Vector3(.12f,.4f,.15f),belly);Piece("Foot",new Vector3(i*.3f,-.4f,.12f),new Vector3(.2f,.13f,.5f),belly);Piece("Eye",new Vector3(i*.18f,1.32f,.85f),Vector3.one*.07f,belly);}
        }
        void Update(){
            if(!agent||!agent.zone)return;
            var player=FindAnyObjectByType<AnchorExplorer>();float distance=player?Vector3.Distance(player.transform.position,transform.position):100;
            bool flee=distance<12;float cycle=(Time.time+phase)%18;bool walk=flee||cycle<10;
            State=flee?"경계 · 도주":walk?"무리 이동":cycle<15?"풀 뜯기":"대기";
            Vector3 target=origin+new Vector3(Mathf.Sin(Time.time*.08f),0,Mathf.Cos(Time.time*.08f))*8;
            if(flee)target=transform.position+Vector3.ProjectOnPlane(transform.position-player.transform.position,Vector3.up).normalized*8;
            var direction=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
            if(walk&&direction.sqrMagnitude>.01f){
                var next=transform.position+direction.normalized*(flee?5:1.1f)*Time.deltaTime;
                if(agent.zone.Accept(next.x,next.z)){next.y=agent.zone.SurfaceY(next.x,next.z);transform.position=next;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),Time.deltaTime*5);}
            }
            float swing=walk?Mathf.Sin(Time.time*(flee?16:7)+phase)*30:0;
            left.localRotation=Quaternion.Euler(swing,0,0);right.localRotation=Quaternion.Euler(-swing,0,0);
            head.localPosition=new Vector3(0,State=="풀 뜯기"?.75f:1.25f,.7f);tail.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*2+phase)*7,0);
        }
    }
}
