using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static class AnchorHud {
        public static string Name(AnchorSession s,string id){
            if(id=="fiber-rope")return "섬유 밧줄";if(id=="stone-axe")return "돌도끼";
            switch(id){case "hand":return "맨손";case "axe":return "도끼";case "knife":return "칼";case "shovel":return "삽";case "pickaxe":return "곡괭이";case "salvage_tool":return "회수 도구";case "research-token":return "연구 토큰";}
            foreach(var r in s.resources)if(r&&r.data.id==id)return r.data.displayName;
            if(id.StartsWith("sample:")){string species=id.Substring(7);foreach(var c in s.creatures)if(c&&c.data.id==species)return c.data.displayName+" 관찰 샘플";}
            return id;
        }
        public static Vector2 MapPoint(Vector3 position,Rect map)=>new Vector2(map.x+(position.x+2000)/4000*map.width,map.y+(2000-position.z)/4000*map.height);
        public static void MiniMap(AnchorExplorer player,AnchorSession s,float width,GUIStyle label){
            Rect box=new Rect(width-240,20,220,245),map=new Rect(width-230,55,200,180);
            GUI.Box(box,GUIContent.none);GUI.Label(new Rect(box.x+10,25,200,25),"닻섬 위치도 · 위쪽 북쪽",label);
            var previous=GUI.color;GUI.color=new Color(.1f,.25f,.3f);GUI.DrawTexture(map,Texture2D.whiteTexture);GUI.color=previous;
            foreach(var zone in AnchorSurface.Layout.zones){var point=MapPoint(new Vector3(zone.x,0,zone.z),map);GUI.Label(new Rect(point.x-30,point.y-8,80,25),zone.displayName,label);}
            var research=MapPoint(new Vector3(-330,0,-180),map);GUI.color=Color.yellow;GUI.DrawTexture(new Rect(research.x-3,research.y-3,6,6),Texture2D.whiteTexture);
            var p=MapPoint(player.transform.position,map);GUI.color=Color.cyan;GUI.DrawTexture(new Rect(p.x-4,p.y-4,8,8),Texture2D.whiteTexture);GUI.color=previous;
            GUI.Label(new Rect(box.x+10,237,205,25),"청록: 나 · 노랑: 연구원",label);
        }
    }
}
