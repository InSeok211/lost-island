using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorResourceNode : MonoBehaviour {
        public AnchorResourceDefinition definition;
        public string nodeId,zoneId;
        Renderer[] visuals; Collider[] colliders;
        public bool Available {
            get { var s=AnchorSession.Current;if(!s)return false;if(!s.harvested.TryGetValue(nodeId,out double day))return true;return definition.data.respawnDays>0 && s.Days-day>=definition.data.respawnDays; }
        }
        void Awake(){visuals=GetComponentsInChildren<Renderer>();colliders=GetComponentsInChildren<Collider>();}
        void Update(){bool visible=Available;foreach(var r in visuals)r.enabled=visible;foreach(var c in colliders)c.enabled=visible;}
        public bool Harvest(string tool,out int amount){
            amount=0;var s=AnchorSession.Current;var d=definition.data;
            if(!Available)return false;
            if(tool!=d.tool){s.message=d.displayName+" — 필요한 도구: "+d.tool;return false;}
            amount=Random.Range(d.yieldMin,d.yieldMax+1);s.Add(d.id,amount);s.harvested[nodeId]=s.Days;
            s.GetComponent<AnchorFirstSurvey>()?.RecordHarvest(d.id,amount);
            s.GetComponent<AnchorSupplyQuest>()?.Harvested(d.id,amount);
            FindAnyObjectByType<AnchorToolFeedback>()?.Harvested(transform.position,d.displayName,amount);
            s.message=d.displayName+" +"+amount+" | 제작: "+string.Join(", ",d.craftingTags);return true;
        }
    }
}
