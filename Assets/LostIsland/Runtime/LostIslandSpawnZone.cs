using UnityEngine;
namespace LostIsland {
    public sealed class LostIslandSpawnZone : MonoBehaviour {
        public string regionId;
        public string[] creatureIds;
        public Vector2 size = new Vector2(1000,1000);
        public bool marine;
        public float seaLevel = 150;
        public int seed;
        public Terrain terrain;
        void OnDrawGizmosSelected() { Gizmos.color=marine?Color.cyan:Color.green; Gizmos.DrawWireCube(transform.position,new Vector3(size.x,20,size.y)); }
    }
    public interface ILostIslandCreatureSpawner { void Spawn(); void Despawn(); }
}
