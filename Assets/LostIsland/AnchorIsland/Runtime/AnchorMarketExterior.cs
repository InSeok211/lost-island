using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void MarketExterior(Transform root){
            var canvas=Mat("market flax canvas",new Color(.66f,.59f,.39f));
            var cloth=Mat("market faded teal",new Color(.29f,.4f,.36f));
            var red=Mat("market ochre",new Color(.53f,.33f,.22f));
            var metal=Mat("market tool iron",new Color(.36f,.39f,.37f));
            for(int i=0;i<6;i++){
                float x=(i%3-1)*7,z=(i/3*2-1)*6;
                Material fabric=i%3==0?canvas:i%3==1?cloth:red;
                foreach(int side in new[]{-1,1}){
                    var wing=Part(root,"Sloped canvas stall roof",new Vector3(x+side*1.55f,4.5f,z),new Vector3(3.35f,.13f,4.5f),fabric,PrimitiveType.Cube,false);
                    wing.transform.localRotation=Quaternion.Euler(0,0,-side*12);
                    Part(root,"Awning fringe",new Vector3(x+side*3,4.05f,z),new Vector3(.1f,.4f,4.5f),fabric,PrimitiveType.Cube,false);
                    Part(root,"Rear frame upright",new Vector3(x+side*2.6f,2.1f,z+(z>0?1.5f:-1.5f)),new Vector3(.16f,4.2f,.16f),Wood);
                }
                Part(root,"Canvas ridge pole",new Vector3(x,4.9f,z),new Vector3(.15f,.15f,4.8f),Wood,PrimitiveType.Cube,false);
                for(int plank=0;plank<8;plank++)Part(root,"Counter front boards",new Vector3(x-2.2f+plank*.63f,1,z+(z>0?-1.55f:1.55f)),new Vector3(.57f,1.8f,.13f),Wood,PrimitiveType.Cube,false);
                if(i%3==1){
                    for(int tool=0;tool<3;tool++){
                        Part(root,"Display tool handle",new Vector3(x-1+tool,2.3f,z),new Vector3(.09f,.1f,1.1f),Wood,PrimitiveType.Cube,false);
                        Part(root,"Display axe head",new Vector3(x-1+tool,2.4f,z+.4f),new Vector3(.4f,.18f,.25f),metal,PrimitiveType.Cube,false);
                    }
                }else if(i%3==2){
                    for(int roll=0;roll<3;roll++){var bundle=Part(root,"Rolled cloth",new Vector3(x-1+roll,2.4f,z),new Vector3(.55f,.6f,.55f),canvas,PrimitiveType.Cylinder,false);bundle.transform.localRotation=Quaternion.Euler(90,0,0);}
                }
            }
            // Life props stay outside the six-metre central aisle.
            var cart=new GameObject("Handcart").transform;cart.SetParent(root,false);cart.localPosition=Ground(root,new Vector3(-16,0,9));
            Part(cart,"Cart bed",Vector3.up*.9f,new Vector3(2.2f,.25f,3.2f),Wood);
            foreach(int side in new[]{-1,1}){
                Part(cart,"Cart side",new Vector3(side*1,1.5f,0),new Vector3(.15f,1,3.2f),Wood);
                var wheel=Part(cart,"Wheel",new Vector3(side*1.3f,.65f,.4f),new Vector3(1.2f,.12f,1.2f),Wood,PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                Part(cart,"Cart handle",new Vector3(side*.6f,.9f,-2.3f),new Vector3(.12f,.12f,2),Wood);
            }
            Part(cart,"Supply sack",new Vector3(0,1.5f,0),new Vector3(1.4f,1,1.5f),canvas,PrimitiveType.Sphere,false);
            var table=Ground(root,new Vector3(16,0,-7));
            Part(root,"Public meal table",table+Vector3.up*1.2f,new Vector3(3,.2f,2),Wood);
            foreach(int side in new[]{-1,1})Part(root,"Table leg",table+new Vector3(side*1,.6f,0),new Vector3(.2f,1.2f,1.5f),Wood);
            Bench(root,table+new Vector3(0,0,2));
        }
    }
}
