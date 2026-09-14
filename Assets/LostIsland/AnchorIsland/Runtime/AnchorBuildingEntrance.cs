using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorBuildingEntrance : MonoBehaviour {
        public string title,kind;
        public static void Add(Transform parent,string title,string kind,Vector3 local){var g=new GameObject("Interior entrance - "+title);g.transform.SetParent(parent,false);g.transform.localPosition=local;var entrance=g.AddComponent<AnchorBuildingEntrance>();entrance.title=title;entrance.kind=kind;}
    }
}
