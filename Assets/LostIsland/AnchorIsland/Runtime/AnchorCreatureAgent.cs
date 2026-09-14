using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorCreatureAgent : MonoBehaviour {
        public AnchorCreatureDefinition definition;
        public AnchorCreatureSpawnZone zone;
        public Vector3 home;
        float phase;
        void Start(){phase=(AnchorSession.Stable(name)&65535)*.01f;if(definition.data.id=="hypsilophodon-foxii")gameObject.AddComponent<AnchorHypsilophodon>();}
        void Update(){
            if(!zone||!AnchorSession.Current)return;
            if(definition.data.id=="hypsilophodon-foxii")return;
            Vector3 next=home+new Vector3(Mathf.Sin(Time.time*.12f+phase),0,Mathf.Cos(Time.time*.12f+phase))*4;
            if(zone.Accept(next.x,next.z)){
                next.y=zone.SurfaceY(next.x,next.z);var delta=next-transform.position;
                if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up)),Time.deltaTime*2);
                transform.position=Vector3.MoveTowards(transform.position,next,Time.deltaTime*1.2f);
            }
        }
        public void Observe(){var s=AnchorSession.Current;if(!s)return;s.Add("sample:"+definition.data.id,1);s.research.Add(definition.data.sampleUnlock);s.message=definition.data.displayName+" 관찰 / 연구: "+definition.data.sampleUnlock;}
    }
}
