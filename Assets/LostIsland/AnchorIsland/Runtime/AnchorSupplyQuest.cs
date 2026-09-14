using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorSupplyQuest : MonoBehaviour {
        public int stage,wood;
        public bool crafted;
        GUIStyle label;
        public bool Unlocked=>GetComponent<AnchorFirstSurvey>().stage==AnchorFirstSurvey.Stage.Completed;
        public void Crafted(){if(stage==1)crafted=true;}
        public void Harvested(string id,int count){if(stage==1&&crafted&&id=="conifer-timber"&&AnchorSession.Current.tool=="axe")wood=Mathf.Min(5,wood+count);}
        public bool Interact(Vector3 position){
            if(!Unlocked||Vector3.Distance(position,GetComponent<AnchorFirstSurvey>().Research)>32)return false;
            var s=AnchorSession.Current;
            if(stage==0){stage=1;s.message="정착지 물자 의뢰 수락 · C로 새 돌도끼 제작 후 목재 5개 채집";}
            else if(stage==1&&crafted&&wood>=5&&AnchorCrafting.Count(s,"conifer-timber")>=5){s.inventory["conifer-timber"]-=5;s.Add("research-token",15);stage=2;s.message="물자 의뢰 완료 · 목재 5개 제출 / 연구 토큰 +15";}
            else s.message=stage==2?"물자 의뢰를 완료했습니다.":"수락 후 돌도끼를 제작하고 목재 5개를 채집해 가져오세요.";
            return true;
        }
        void Update(){var k=Keyboard.current;var p=FindAnyObjectByType<AnchorExplorer>();if(p&&k!=null&&Application.isFocused&&!p.MenuOpen&&k.gKey.wasPressedThisFrame)Interact(p.transform.position);}
        void OnGUI(){
            var p=FindAnyObjectByType<AnchorExplorer>();if(!p||p.MenuOpen||!Unlocked)return;
            if(p.GetComponent<AnchorBuildingInterior>()?.Inside==true)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=p.font,fontSize=15,wordWrap=true};
            var old=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.6f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            GUI.Box(new Rect(20,345,440,135),GUIContent.none);
            string details=stage==0?"연구원에서 G로 의뢰 수락":stage==2?"완료 · 연구 토큰 15개 지급":$"새 돌도끼 제작: {(crafted?"완료":"C 제작창")}\n도끼로 목재 채집: {wood}/5 · 보유 {AnchorCrafting.Count(AnchorSession.Current,"conifer-timber")}/5\n완료 후 연구원에서 G로 제출";
            GUI.Label(new Rect(35,355,410,115),"정착지 물자 의뢰\n"+details,label);GUI.matrix=old;
        }
    }
}
