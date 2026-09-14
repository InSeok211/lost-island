using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void HarborExterior(Transform root){
            for(int step=0;step<3;step++){float height=.6f-step*.2f;Part(root,"Harbor approach step",new Vector3(5.35f+step*.7f,height*.5f,-10),new Vector3(.7f,height,4),Cream);}
            var boards=Mat("harbor salt timber",new Color(.39f,.35f,.28f));
            var canvas=Mat("harbor sailcloth",new Color(.66f,.62f,.48f));
            var iron=Mat("harbor iron",new Color(.25f,.29f,.28f));
            foreach(int side in new[]{-1,1}){
                for(int row=0;row<10;row++)Part(root,"Harbor weatherboard",new Vector3(side*9.08f,.7f+row*.7f,0),new Vector3(.12f,.62f,13.7f),boards,PrimitiveType.Cube,false);
                for(int z=-6;z<=6;z+=6)Part(root,"Harbor timber brace",new Vector3(side*9.2f,4,z),new Vector3(.3f,8,.3f),Wood);
                Part(root,"Roof edge trim",new Vector3(side*9.7f,8.4f,0),new Vector3(.25f,.5f,16),Wood,PrimitiveType.Cube,false);
            }
            // Sheltered cargo yard is beside the office, outside the pier entrance.
            var shelter=new GameObject("Covered cargo yard").transform;shelter.SetParent(root,false);shelter.localPosition=Ground(root,new Vector3(19,0,4));
            foreach(int x in new[]{-4,4})foreach(int z in new[]{-3,3})Part(shelter,"Shelter post",new Vector3(x,2.5f,z),new Vector3(.25f,5,.25f),Wood);
            var roof=Part(shelter,"Canvas cargo roof",new Vector3(0,5.2f,0),new Vector3(9,.18f,7.5f),canvas,PrimitiveType.Cube,false);roof.transform.localRotation=Quaternion.Euler(0,0,5);
            for(int i=0;i<6;i++){
                Vector3 p=new Vector3(i%3*2.3f-2.3f,.9f,i/3*2.5f-1.2f);
                Part(shelter,"Cargo box",p,new Vector3(1.8f,1.8f,1.8f),boards);
                foreach(int side in new[]{-1,1})Part(shelter,"Crate strap",p+new Vector3(side*.55f,0,-.92f),new Vector3(.12f,1.85f,.06f),iron,PrimitiveType.Cube,false);
            }
            Label(shelter,"하역 물자 · 보관 구역",new Vector3(0,5.8f,0));
            var rack=new GameObject("Oar rack").transform;rack.SetParent(root,false);rack.localPosition=Ground(root,new Vector3(-14,0,-4));
            Part(rack,"Rack crossbar",Vector3.up*1.5f,new Vector3(4,.18f,.25f),Wood);
            foreach(int side in new[]{-1,1})Part(rack,"Rack foot",new Vector3(side*1.7f,.8f,0),new Vector3(.2f,1.6f,.6f),Wood);
            for(int i=0;i<4;i++){float x=i-1.5f;Part(rack,"Oar shaft",new Vector3(x,1.9f,.25f),new Vector3(.1f,3.4f,.1f),boards,PrimitiveType.Cube,false);Part(rack,"Oar blade",new Vector3(x,.45f,.25f),new Vector3(.35f,.8f,.12f),boards,PrimitiveType.Cube,false);}
        }
    }
}
