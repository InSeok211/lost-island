using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void PlazaExterior(Transform root){
            var stone=Mat("plaza weathered stone",new Color(.46f,.45f,.38f));
            var canvas=Mat("plaza shelter linen",new Color(.68f,.61f,.43f));
            // Trim only the brightly coloured original anchor placeholder, not POI state/colliders.
            foreach(var renderer in root.parent.GetComponentsInChildren<Renderer>()){
                if(renderer.transform.IsChildOf(root)||!renderer.sharedMaterial)continue;
                var c=renderer.sharedMaterial.color;
                if(c.b>.7f&&c.g>.5f&&c.r<.3f){renderer.enabled=false;var collider=renderer.GetComponent<Collider>();if(collider)collider.enabled=false;}
            }
            for(int i=0;i<16;i++){
                float a=i*Mathf.PI*2/16;var block=Part(root,"Monument stone rim",new Vector3(Mathf.Sin(a)*4.6f,.28f,Mathf.Cos(a)*4.6f),new Vector3(1.55f,.35f,.75f),stone);
                block.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
            // Ring at the head of the existing stylised anchor.
            for(int i=0;i<12;i++){
                float a=i*Mathf.PI*2/12;var segment=Part(root,"Anchor ring segment",new Vector3(Mathf.Sin(a)*.85f,9+Mathf.Cos(a)*.85f,0),new Vector3(.48f,.24f,.28f),Teal,PrimitiveType.Cube,false);
                segment.transform.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);
            }
            foreach(int side in new[]{-1,1}){
                var shelter=new GameObject("Rest shelter").transform;shelter.SetParent(root,false);shelter.localPosition=Ground(root,new Vector3(side*23,0,25));
                foreach(int x in new[]{-3,3})foreach(int z in new[]{-2,2})Part(shelter,"Timber shelter post",new Vector3(x,2.3f,z),new Vector3(.2f,4.6f,.2f),Wood);
                foreach(int wing in new[]{-1,1}){var roof=Part(shelter,"Shelter cloth",new Vector3(wing*1.7f,4.8f,0),new Vector3(3.7f,.14f,5.5f),canvas,PrimitiveType.Cube,false);roof.transform.localRotation=Quaternion.Euler(0,0,-wing*12);}
                Bench(shelter,Vector3.zero);
                Part(shelter,"Travel supply crate",new Vector3(2,.65f,1),Vector3.one*1.3f,Wood);
                Label(shelter,"개척자 쉼터",new Vector3(0,4,-2.4f));
            }
        }
    }
}
