using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    // Lightweight original explorer silhouette. Movement collision remains on the controller.
    public sealed class AnchorExplorerAppearance:MonoBehaviour {
        readonly List<Material> materials=new List<Material>();Transform leftLeg,rightLeg,leftArm,rightArm,body;Vector3 previous;float phase;
        Material Make(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};m.SetFloat("_Smoothness",.1f);materials.Add(m);return m;}
        Transform Piece(Transform root,string name,Vector3 p,Vector3 size,Material m,PrimitiveType shape=PrimitiveType.Cube){var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;Destroy(g.GetComponent<Collider>());return g.transform;}
        Transform Limb(string name,Vector3 pivot,Material m){var t=new GameObject(name).transform;t.SetParent(body,false);t.localPosition=pivot;Piece(t,"Clothing",new Vector3(0,-.24f,0),new Vector3(.21f,.5f,.24f),m,PrimitiveType.Capsule);return t;}
        void Start(){
            var renderer=GetComponent<MeshRenderer>();if(renderer)renderer.enabled=false;
            body=new GameObject("Explorer outfit").transform;body.SetParent(transform,false);
            var cloth=Make(new Color(.27f,.39f,.34f));var leather=Make(new Color(.24f,.17f,.11f));var skin=Make(new Color(.68f,.46f,.3f));var linen=Make(new Color(.74f,.68f,.49f));
            Piece(body,"Field jacket",new Vector3(0,.2f,0),new Vector3(.64f,.72f,.4f),cloth,PrimitiveType.Capsule);
            Piece(body,"Belt",new Vector3(0,-.08f,0),new Vector3(.62f,.1f,.43f),leather);
            Piece(body,"Head",new Vector3(0,.72f,0),new Vector3(.4f,.43f,.38f),skin,PrimitiveType.Sphere);
            Piece(body,"Hat brim",new Vector3(0,.91f,0),new Vector3(.67f,.035f,.62f),linen,PrimitiveType.Cylinder);
            Piece(body,"Hat crown",new Vector3(0,1.01f,0),new Vector3(.43f,.1f,.4f),linen,PrimitiveType.Cylinder);
            Piece(body,"Travel backpack",new Vector3(0,.25f,-.29f),new Vector3(.48f,.57f,.23f),leather);
            Piece(body,"Bedroll",new Vector3(0,.58f,-.3f),new Vector3(.65f,.2f,.24f),linen,PrimitiveType.Capsule).localRotation=Quaternion.Euler(0,0,90);
            leftLeg=Limb("Left leg",new Vector3(-.17f,-.15f,0),leather);rightLeg=Limb("Right leg",new Vector3(.17f,-.15f,0),leather);
            leftArm=Limb("Left arm",new Vector3(-.4f,.4f,0),cloth);rightArm=Limb("Right arm",new Vector3(.4f,.4f,0),cloth);
            foreach(var leg in new[]{leftLeg,rightLeg})Piece(leg,"Boot",new Vector3(0,-.64f,.06f),new Vector3(.24f,.28f,.38f),leather);
            foreach(var arm in new[]{leftArm,rightArm})Piece(arm,"Hand",new Vector3(0,-.52f,0),Vector3.one*.19f,skin,PrimitiveType.Sphere);
            previous=transform.position;
        }
        void LateUpdate(){if(!body)return;float distance=Vector3.ProjectOnPlane(transform.position-previous,Vector3.up).magnitude;previous=transform.position;float speed=distance/Mathf.Max(Time.deltaTime,.001f);if(distance>2)speed=0;phase+=Mathf.Min(speed,12)*Time.deltaTime*1.4f;float swing=Mathf.Sin(phase)*Mathf.Min(speed*5,32);leftLeg.localRotation=Quaternion.Euler(swing,0,0);rightLeg.localRotation=Quaternion.Euler(-swing,0,0);leftArm.localRotation=Quaternion.Euler(-swing*.65f,0,8);rightArm.localRotation=Quaternion.Euler(swing*.65f,0,-8);}
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);}
    }
}
