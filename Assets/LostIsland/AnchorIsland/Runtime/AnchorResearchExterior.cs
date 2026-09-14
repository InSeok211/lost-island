using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void ResearchExterior(Transform root){
            var timber=Mat("research weathered timber",new Color(.38f,.32f,.23f));
            var canvas=Mat("research field canvas",new Color(.66f,.62f,.43f));
            var glass=Mat("research dusty glass",new Color(.3f,.43f,.4f));
            // Keep the original building collider and entrance position unchanged.
            foreach(int side in new[]{-1,1}){
                for(int row=0;row<10;row++){
                    Part(root,"Side weatherboards",new Vector3(side*9.05f,.7f+row*.7f,0),new Vector3(.12f,.62f,13.7f),timber,PrimitiveType.Cube,false);
                    Part(root,"Rear weatherboards",new Vector3(side*4.4f,.7f+row*.7f,7.08f),new Vector3(8.7f,.62f,.12f),timber,PrimitiveType.Cube,false);
                }
                foreach(int z in new[]{-6,0,6})Part(root,"Structural post",new Vector3(side*9.2f,4,z),new Vector3(.35f,8,.35f),Wood);
                for(int z=-3;z<=3;z+=6){
                    Part(root,"Side window frame",new Vector3(side*9.28f,4.5f,z),new Vector3(.2f,2.3f,3.2f),Cream,PrimitiveType.Cube,false);
                    Part(root,"Side window glass",new Vector3(side*9.4f,4.5f,z),new Vector3(.08f,1.9f,2.8f),glass,PrimitiveType.Cube,false);
                    Part(root,"Window mullion",new Vector3(side*9.46f,4.5f,z),new Vector3(.1f,2,.13f),Wood,PrimitiveType.Cube,false);
                }
                var awning=Part(root,"Canvas entrance wing",new Vector3(side*2.5f,4.7f,-10),new Vector3(5.2f,.16f,5.8f),canvas,PrimitiveType.Cube,false);awning.transform.localRotation=Quaternion.Euler(0,0,-side*10);
                Part(root,"Roof fascia",new Vector3(side*9.6f,8.4f,0),new Vector3(.3f,.7f,16),Wood,PrimitiveType.Cube,false);
            }
            Part(root,"Entrance ridge",new Vector3(0,5.15f,-10),new Vector3(.18f,.18f,6),Wood,PrimitiveType.Cube,false);
            // Exterior specimen racks stay beside, not in front of, the interaction point.
            var rack=new GameObject("Expedition specimen rack").transform;rack.SetParent(root,false);rack.localPosition=Ground(root,new Vector3(14,0,2));
            foreach(int x in new[]{-2,2})Part(rack,"Rack upright",new Vector3(x,1.8f,0),new Vector3(.2f,3.6f,1.4f),Wood);
            for(int level=0;level<3;level++){
                Part(rack,"Rack shelf",new Vector3(0,.4f+level*1.2f,0),new Vector3(4.5f,.18f,1.6f),timber);
                for(int item=0;item<3;item++)Part(rack,"Specimen box",new Vector3(-1.4f+item*1.4f,.8f+level*1.2f,0),new Vector3(1,.65f,1),canvas,PrimitiveType.Cube,false);
            }
            Label(rack,"야외 표본 보관",new Vector3(0,4,0));
            var barrelPoint=Ground(root,new Vector3(-13,0,5));Part(root,"Rainwater barrel",barrelPoint+Vector3.up*1.1f,new Vector3(1.5f,1.1f,1.5f),timber,PrimitiveType.Cylinder);
            Part(root,"Barrel lid",barrelPoint+Vector3.up*2.25f,new Vector3(1.65f,.08f,1.65f),Wood,PrimitiveType.Cylinder,false);
        }
    }
}
