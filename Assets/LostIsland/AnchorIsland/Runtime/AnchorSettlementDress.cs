using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    // Runtime dressing keeps generated terrain scenes and existing saves intact.
    public static partial class AnchorSettlementDress {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Material Mat(string key,Color color){
            if(materials.TryGetValue(key,out var existing)&&existing)return existing;
            var shader=Shader.Find("Universal Render Pipeline/Lit");var m=new Material(shader?shader:Shader.Find("Standard")){name="Settlement "+key};m.color=color;m.SetFloat("_Smoothness",.15f);AnchorMaterialFinish.Apply(m,key);materials[key]=m;return m;
        }
        static Material Wood=>Mat("wood",new Color(.32f,.2f,.12f));
        static Material Cream=>Mat("cream",new Color(.85f,.79f,.6f));
        static Material Teal=>Mat("teal",new Color(.1f,.43f,.45f));
        static GameObject Part(Transform root,string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube,bool solid=true){
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)Object.Destroy(go.GetComponent<Collider>());return go;
        }
        static void Label(Transform root,string words,Vector3 point){
            var go=new GameObject("Sign: "+words);go.transform.SetParent(root,false);go.transform.localPosition=point;
            var mesh=go.AddComponent<TextMesh>();mesh.text=words;mesh.characterSize=.14f;mesh.fontSize=48;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=Color.white;
            var explorer=Object.FindAnyObjectByType<AnchorExplorer>();if(explorer&&explorer.font){mesh.font=explorer.font;mesh.GetComponent<MeshRenderer>().sharedMaterial=explorer.font.material;}
            go.AddComponent<AnchorSignFacing>();
        }
        static void Bench(Transform root,Vector3 p){
            Part(root,"Bench seat",p+Vector3.up*.9f,new Vector3(4,.25f,1.3f),Wood);
            Part(root,"Bench back",p+new Vector3(0,1.55f,.55f),new Vector3(4,1,.2f),Wood);
            foreach(int side in new[]{-1,1})Part(root,"Bench foot",p+new Vector3(side*1.4f,.4f,0),new Vector3(.3f,.8f,1),Teal);
        }
        static void Lamp(Transform root,Vector3 p){
            Part(root,"Lamp post",p+Vector3.up*2.5f,new Vector3(.25f,5,.25f),Teal);
            var lamp=Part(root,"Lantern",p+Vector3.up*5,new Vector3(.65f,.9f,.65f),Cream,PrimitiveType.Cube,false);
            // Emissive marker instead of dozens of real-time lights.
            var m=Mat("lantern",new Color(1,.78f,.36f));m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.6f,.15f));lamp.GetComponent<Renderer>().sharedMaterial=m;
        }
        public static void Build(AnchorIslandPoiMarker poi){
            if(poi.transform.Find("Settlement dressing"))return;
            if(poi.poiType!="anchor"&&poi.poiType!="market"&&poi.poiType!="harbor"&&poi.id!="bio-research")return;
            var root=new GameObject("Settlement dressing").transform;root.SetParent(poi.transform,false);
            Village(root,poi);
            if(poi.poiType!="anchor")Label(root,poi.displayName,new Vector3(0,7,-12));
            Lamp(root,new Vector3(-12,0,-12));Lamp(root,new Vector3(12,0,-12));
            if(poi.poiType=="anchor"){
                foreach(int side in new[]{-1,1}){Bench(root,new Vector3(side*14,0,12));Bench(root,new Vector3(side*14,0,-12));}
                Part(root,"Anchor stem",new Vector3(0,5,0),new Vector3(.6f,7,.6f),Teal);
                Part(root,"Anchor crossbar",new Vector3(0,6,0),new Vector3(5,.5f,.5f),Cream);
                foreach(int side in new[]{-1,1}){var arm=Part(root,"Anchor fluke",new Vector3(side*1.7f,2.7f,0),new Vector3(3,.6f,.6f),Teal);arm.transform.localRotation=Quaternion.Euler(0,0,side*35);}
                Part(root,"Wayfinding post",new Vector3(9,2,-17),new Vector3(.3f,4,.3f),Wood);
                Label(root,"연구원 ◆  /  시장  /  항구\n목표 표식을 따라 이동",new Vector3(9,4.5f,-17));
            }else if(poi.id=="bio-research"){
                Resident(root,new Vector3(3,0,-12),"생태 조사원 · F 조사 / G 물자",Cream);
                Part(root,"Specimen table",new Vector3(-6,1.1f,-12),new Vector3(4,2.2f,1.6f),Wood);
                for(int i=0;i<3;i++)Part(root,"Fossil specimen",new Vector3(-7+i,2.5f,-12),new Vector3(.6f,.5f,.8f),Cream,PrimitiveType.Sphere,false);
            }else if(poi.poiType=="market"){
                for(int i=0;i<6;i++){
                    float x=(i%3-1)*7,z=(i/3*2-1)*6;
                    for(int j=0;j<4;j++)Part(root,"Produce",new Vector3(x-1.5f+j,2.4f,z),Vector3.one*.6f,Mat("produce",new Color(.6f,.48f,.17f)),PrimitiveType.Sphere,false);
                    Part(root,"Supply crate",new Vector3(x, .7f,z+(z>0?3:-3)),Vector3.one*1.4f,Wood);
                }
                Label(root,"개척 물자 · 식량 · 채집 도구\n상점 기능 준비 중",new Vector3(0,5,0));
            }else {
                for(int i=0;i<5;i++){Part(root,"Dock cargo",new Vector3(-15+i*3,1,10),Vector3.one*2,Wood);Part(root,"Mooring bollard",new Vector3((i%2==0?-5:5),1,-16-i*8),new Vector3(.6f,2,.6f),Teal,PrimitiveType.Cylinder);}
                Label(root,"침묵의 항구\n부두 진입",new Vector3(0,5,-15));
                var berth=poi.transform.position+new Vector3(15,0,-420);berth.y=19;
                var boat=new GameObject("Moored boat - decorative placeholder").transform;boat.SetParent(root,false);boat.position=berth;boat.rotation=Quaternion.identity;
                Part(boat,"Hull",Vector3.zero,new Vector3(5,2,13),Wood,PrimitiveType.Sphere);
                Part(boat,"Deck",Vector3.up,new Vector3(4,.3f,10),Cream);
                Part(boat,"Mast",new Vector3(0,5,0),new Vector3(.25f,9,.25f),Wood);
                Part(boat,"Furled sail",new Vector3(0,7,0),new Vector3(4,.5f,.7f),Cream,PrimitiveType.Cube,false);
            }
        }
    }
    public sealed class AnchorSignFacing : MonoBehaviour {
        AnchorExplorer player;Renderer display;Camera lens;float check;
        void Start(){player=Object.FindAnyObjectByType<AnchorExplorer>();display=GetComponent<Renderer>();lens=Camera.main;}
        void LateUpdate(){if(!lens)lens=Camera.main;if(lens)transform.rotation=lens.transform.rotation;check-=Time.deltaTime;if(check>0)return;check=.25f;if(!player)player=Object.FindAnyObjectByType<AnchorExplorer>();if(display&&player)display.enabled=!player.MenuOpen&&(player.transform.position-transform.position).sqrMagnitude<24*24;}
    }
}
