using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorCrafting : MonoBehaviour {
        public bool Open {get;private set;}
        GUIStyle label,button;
        public static int Count(AnchorSession s,string id){s.inventory.TryGetValue(id,out int n);return n;}
        public static bool CanCraft(AnchorSession s,string id)=>id=="fiber-rope"?Count(s,"fern-fiber")>=3:id=="stone-axe"&&Count(s,"driftwood")>=2&&Count(s,"flint")>=2&&Count(s,"fiber-rope")>=1;
        public static bool Craft(AnchorSession s,string product){
            string[] ids;int[] amounts;
            if(product=="fiber-rope"){ids=new[]{"fern-fiber"};amounts=new[]{3};}
            else if(product=="stone-axe"){ids=new[]{"driftwood","flint","fiber-rope"};amounts=new[]{2,2,1};}
            else return false;
            for(int i=0;i<ids.Length;i++){s.inventory.TryGetValue(ids[i],out int n);if(n<amounts[i]){s.message="제작 재료가 부족합니다.";return false;}}
            for(int i=0;i<ids.Length;i++)s.inventory[ids[i]]-=amounts[i];
            s.Add(product,1);if(product=="stone-axe")s.tool="axe";
            if(product=="stone-axe")s.GetComponent<AnchorSupplyQuest>()?.Crafted();
            s.message=product=="stone-axe"?"돌도끼 제작 완료 · 도끼 자동 장착":"섬유 밧줄 +1";return true;
        }
        void Update(){var k=Keyboard.current;if(k==null||!Application.isFocused)return;if(k.cKey.wasPressedThisFrame)Open=!Open;if(k.escapeKey.wasPressedThisFrame||k.iKey.wasPressedThisFrame||k.f1Key.wasPressedThisFrame||k.mKey.wasPressedThisFrame)Open=false;}
        void OnGUI(){
            if(!Open)return;var s=AnchorSession.Current;var player=FindAnyObjectByType<AnchorExplorer>();if(!s||!player)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){font=player.font,fontSize=17,wordWrap=true};button=new GUIStyle(GUI.skin.button){font=player.font,fontSize=16};}
            var old=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.6f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float x=Screen.width/scale/2-250;
            GUI.Box(new Rect(x,200,500,330),GUIContent.none);GUI.Label(new Rect(x+20,215,460,40),"간이 제작 · C / Esc 닫기",label);
            bool wasEnabled=GUI.enabled;
            GUI.Label(new Rect(x+20,265,460,50),$"섬유 밧줄 — 보유 / 필요\n고사리 섬유 {Count(s,"fern-fiber")}/3",label);
            GUI.enabled=wasEnabled&&CanCraft(s,"fiber-rope");
            if(GUI.Button(new Rect(x+20,310,460,35),"밧줄 만들기",button))Craft(s,"fiber-rope");
            GUI.enabled=wasEnabled;
            GUI.Label(new Rect(x+20,365,460,50),$"돌도끼 — 보유 / 필요\n표류목 {Count(s,"driftwood")}/2 · 부싯돌 {Count(s,"flint")}/2 · 밧줄 {Count(s,"fiber-rope")}/1",label);
            GUI.enabled=wasEnabled&&CanCraft(s,"stone-axe");
            if(GUI.Button(new Rect(x+20,415,460,35),"돌도끼 만들고 장착",button))Craft(s,"stone-axe");
            GUI.enabled=wasEnabled;
            GUI.Label(new Rect(x+20,470,460,50),"재료 부족 시 제작 불가 · 제작물은 자동 저장\n기존 검수용 도구도 계속 사용할 수 있습니다.",label);GUI.matrix=old;
        }
    }
}
