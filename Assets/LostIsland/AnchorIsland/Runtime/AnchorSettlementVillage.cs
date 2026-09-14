using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static Material Plaster=>Mat("plaster",new Color(.77f,.72f,.57f));
        static Material Tile=>Mat("roof tile",new Color(.34f,.23f,.19f));
        static Vector3 Ground(Transform root,Vector3 local){var w=root.TransformPoint(local);w.y=AnchorSurface.Height(w.x,w.z);return root.InverseTransformPoint(w);}
        static void Planter(Transform root,Vector3 p){
            p=Ground(root,p);Part(root,"Stone planter",p+Vector3.up*.45f,new Vector3(2.5f,.9f,2.5f),Cream);
            Part(root,"Fern bed",p+Vector3.up*1.15f,new Vector3(2,1.1f,2),Mat("foliage",new Color(.25f,.38f,.19f)),PrimitiveType.Sphere,false);
        }
        static void Resident(Transform root,Vector3 p,string role,Material coat){
            var body=new GameObject("Resident - "+role).transform;body.SetParent(root,false);body.localPosition=Ground(root,p);
            Part(body,"Coat",Vector3.up*1.1f,new Vector3(.75f,.8f,.55f),coat,PrimitiveType.Capsule);
            Part(body,"Head",Vector3.up*2.05f,Vector3.one*.48f,Mat("skin",new Color(.7f,.48f,.3f)),PrimitiveType.Sphere,false);
            for(int side=-1;side<=1;side+=2){Part(body,"Boot",new Vector3(side*.19f,.3f,0),new Vector3(.24f,.6f,.35f),Wood);Part(body,"Arm",new Vector3(side*.48f,1.25f,0),new Vector3(.2f,.7f,.2f),coat,PrimitiveType.Capsule,false);}
            Label(body,role,Vector3.up*2.9f);
            body.gameObject.AddComponent<AnchorResidentIdle>();
        }
        static void House(Transform parent,Vector3 position,float yaw,string name){
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=Ground(parent,position);root.localRotation=Quaternion.Euler(0,yaw,0);
            Part(root,"Stone footing",Vector3.up*.2f,new Vector3(11,.4f,9),Cream);
            Part(root,"Plaster walls",Vector3.up*2.8f,new Vector3(10,5.2f,8),Plaster);
            ArchitectureFinish(root);
            Part(root,"Door",new Vector3(0,1.7f,-4.1f),new Vector3(1.5f,2.8f,.15f),Wood);
            foreach(int side in new[]{-1,1}){Part(root,"Window frame",new Vector3(side*3,2.8f,-4.12f),new Vector3(2,1.9f,.2f),Wood);Part(root,"Window glass",new Vector3(side*3,2.8f,-4.24f),new Vector3(1.6f,1.5f,.06f),Teal,PrimitiveType.Cube,false);}
            Part(root,"Chimney",new Vector3(3,6.5f,2),new Vector3(1.1f,3,1.1f),Cream);
            Label(root,name,new Vector3(0,4,-4.5f));
            if(name=="휴게소"||name=="물자 조합"||name=="항만 사무소")AnchorBuildingEntrance.Add(root,name,name=="휴게소"?"rest":name=="항만 사무소"?"harbor":"market",new Vector3(0,0,-6));
        }
        static void Facade(Transform root){
            foreach(int x in new[]{-7,-4,4,7}){Part(root,"Timber frame",new Vector3(x,4,-7.15f),new Vector3(.18f,7,.18f),Wood);Part(root,"Window surround",new Vector3(x,4,-7.25f),new Vector3(2.2f,2.5f,.18f),Cream);Part(root,"Teal shutters",new Vector3(x,4,-7.4f),new Vector3(1.7f,2,.12f),Teal);}
            Part(root,"Porch canopy",new Vector3(0,4,-10),new Vector3(10,.35f,5),Teal);
            foreach(int side in new[]{-1,1})Part(root,"Porch support",new Vector3(side*4.4f,2,-12),new Vector3(.25f,4,.25f),Wood);
        }
        static void Village(Transform root,AnchorIslandPoiMarker poi){
            if(poi.poiType=="anchor"){
                PlazaExterior(root);
                NatureCorridor(root);
                House(root,new Vector3(-35,0,30),10,"개척자 숙소");House(root,new Vector3(35,0,30),-10,"휴게소");
                for(int side=-1;side<=1;side+=2){Planter(root,new Vector3(side*21,0,18));Planter(root,new Vector3(side*21,0,-18));Resident(root,new Vector3(side*11,0,16),side<0?"개척 주민":"순찰대원",side<0?Plaster:Teal);}
                var board=Part(root,"Notice board",new Vector3(-10,2,-17),new Vector3(4,2.5f,.25f),Wood);
                Label(root,"닻섬 생활 안내\n연구원: 조사 의뢰\n시장: 개척 물자",board.transform.localPosition+new Vector3(0,0,-.2f));
                // Ground-following paving and lamps follow the existing road centerlines.
                foreach(var dest in new[]{new Vector3(-130,0,-80),new Vector3(-330,0,-180),new Vector3(220,0,-720)}){
                    int steps=Mathf.CeilToInt(dest.magnitude/22);for(int i=2;i<steps;i++){
                        Vector3 p=dest*(i/(float)steps),side=Vector3.Cross(Vector3.up,dest.normalized);
                        if(i%3==0){var at=Ground(root,p+side*6);Lamp(root,at);}
                        var tile=Part(root,"Waystone",Ground(root,p+side*5)+Vector3.up*.18f,new Vector3(.65f,.35f,1.2f),Cream);tile.transform.localRotation=Quaternion.LookRotation(dest);
                    }
                }
            }else if(poi.id=="bio-research"){
                AnchorBuildingEntrance.Add(root,"세계생태연구원","research",new Vector3(0,0,-14));
                ResearchExterior(root);
                Facade(root);House(root,new Vector3(-25,0,4),90,"표본 보관실");
                Planter(root,new Vector3(-12,0,-16));Planter(root,new Vector3(12,0,-16));
                Resident(root,new Vector3(9,0,-17),"현장 연구원",Cream);Bench(root,Ground(root,new Vector3(-10,0,-20)));
                Label(root,"세계생태연구원\nF 첫 조사 · G 물자 의뢰",new Vector3(0,5,-12.5f));
            }else if(poi.poiType=="market"){
                MarketExterior(root);
                House(root,new Vector3(-24,0,5),90,"물자 조합");House(root,new Vector3(24,0,5),-90,"개척 식당");
                for(int i=0;i<3;i++){Resident(root,new Vector3((i-1)*7,0,9),new[]{"식량 상인","도구 상인","섬유 상인"}[i],i==1?Teal:Plaster);}
                Planter(root,new Vector3(-19,0,-16));Planter(root,new Vector3(19,0,-16));
                for(int i=0;i<5;i++){Part(root,"Barrel",Ground(root,new Vector3(16,0,i*2-3))+Vector3.up*.7f,new Vector3(1, .7f,1),Wood,PrimitiveType.Cylinder);}
            }else if(poi.poiType=="harbor"){
                HarborExterior(root);
                Facade(root);House(root,new Vector3(-27,0,6),90,"항만 사무소");
                Resident(root,new Vector3(-10,0,-13),"항만 관리인",Teal);Resident(root,new Vector3(15,0,9),"하역 인부",Plaster);
                for(int i=0;i<40;i++){
                    float centerZ=poi.transform.position.z-14-i*10;
                    float top=Mathf.Max(21,AnchorSurface.Height(poi.transform.position.x,centerZ)+.5f);
                    float nextTop=Mathf.Max(21,AnchorSurface.Height(poi.transform.position.x,centerZ-10)+.5f);
                    Vector3 a=new Vector3(poi.transform.position.x,top,centerZ),b=new Vector3(poi.transform.position.x,nextTop,centerZ-10);
                    var deck=Part(root,"Raised harbor boardwalk",root.InverseTransformPoint((a+b)*.5f),new Vector3(10,.3f,Vector3.Distance(a,b)+.1f),Wood);
                    deck.transform.rotation=Quaternion.LookRotation(b-a);
                    foreach(int side in new[]{-1,1}){Vector3 world=new Vector3(poi.transform.position.x+side*5.5f,top+1,poi.transform.position.z-14-i*10);Part(root,"Pier railing post",root.InverseTransformPoint(world),new Vector3(.2f,2,.2f),Wood);Part(root,"Pier safety rail",root.InverseTransformPoint(world+new Vector3(0,.6f,-5)),new Vector3(.15f,.15f,10),Wood);}
                }
            }
        }
    }
}
