using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorResidentIdle : MonoBehaviour {
        Transform[] parts;Vector3[] positions;Quaternion[] rotations;
        Quaternion restRotation;float phase;AnchorExplorer player;
        public string Hint {
            get {if(name.Contains("조사원")||name.Contains("연구원"))return "연구원 주변에서 F: 첫 조사 / G: 물자 의뢰";
                if(name.Contains("상인"))return "개척자 시장 · 거래 준비 중 / C: 직접 제작";
                if(name.Contains("항만")||name.Contains("하역"))return "침묵의 항구 · 부두 산책 가능 / 선박 운항 준비 중";
                return "앵커 광장 · M으로 연구원과 항구의 길 안내를 선택하세요.";}
        }
        void Start(){
            restRotation=transform.localRotation;phase=(AnchorSession.Stable(name)&1023)*.013f;
            parts=new Transform[transform.childCount];positions=new Vector3[parts.Length];rotations=new Quaternion[parts.Length];
            for(int i=0;i<parts.Length;i++){parts[i]=transform.GetChild(i);positions[i]=parts[i].localPosition;rotations[i]=parts[i].localRotation;}
            player=FindAnyObjectByType<AnchorExplorer>();
        }
        void Update(){
            if(!player)player=FindAnyObjectByType<AnchorExplorer>();
            if(!player||(player.transform.position-transform.position).sqrMagnitude>70*70)return;
            var desired=restRotation;
            var offset=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up);
            if(offset.sqrMagnitude<8*8&&offset.sqrMagnitude>.1f){
                Quaternion world=Quaternion.LookRotation(offset);desired=transform.parent?Quaternion.Inverse(transform.parent.rotation)*world:world;
            }
            transform.localRotation=Quaternion.Slerp(transform.localRotation,desired,1-Mathf.Exp(-2*Time.deltaTime));
            float breath=Mathf.Sin(Time.time*1.6f+phase)*.015f;
            for(int i=0;i<parts.Length;i++){
                if(!parts[i])continue;string part=parts[i].name;
                if(part=="Head"||part=="Coat")parts[i].localPosition=positions[i]+Vector3.up*breath;
                if(part=="Arm")parts[i].localRotation=rotations[i]*Quaternion.Euler(Mathf.Sin(Time.time*.8f+phase+i)*5,0,0);
            }
        }
    }
}
