using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorIslandPoiMarker : MonoBehaviour {
        public string id,displayName,poiType; public int importance;
        void Start(){AnchorSettlementDress.Build(this);}
    }
}
