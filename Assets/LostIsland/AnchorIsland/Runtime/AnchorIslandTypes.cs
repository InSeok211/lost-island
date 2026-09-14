using System;
using UnityEngine;

namespace LostIsland.AnchorIsland
{
    [Serializable] public class AnchorIslandLayout {
        public string islandId, displayName;
        public int seed;
        public float worldSizeMeters, tileSizeMeters, terrainHeightMeters, seaLevelMeters;
        public int heightmapResolution, alphamapResolution;
        public float islandCenterX, islandCenterZ, islandRadiusX, islandRadiusZ;
        public float coastNoiseScale, coastNoiseStrength;
        public AnchorZone[] zones;
        public FlatZone[] flatZones;
        public AnchorPoi[] pois;
        public RoadDef[] roads;
    }

    [Serializable] public class AnchorZone {
        public string id, displayName, type, biome;
        public float x, z, radiusX, radiusZ;
        public int danger;
    }

    [Serializable] public class FlatZone {
        public string id;
        public float x, z, radius, targetHeightMeters;
    }

    [Serializable] public class AnchorPoi {
        public string id, displayName, type;
        public float x, z, yaw;
        public int importance;
    }

    [Serializable] public class RoadDef {
        public string id, from, to;
        public float width;
    }

    [Serializable] public class CreatureCollection { public string note; public CreatureDef[] creatures; }

    [Serializable] public class CreatureDef {
        public string id, displayName, role, activity, aggression, sampleUnlock;
        public string[] zones, drops;
        public int groupMin, groupMax, maxGroups, danger;
        public float spawnWeight;
    }

    [Serializable] public class ResourceCollection { public ResourceDef[] resources; }

    [Serializable] public class ResourceDef {
        public string id, displayName, category, tool, rarity;
        public string[] zones, craftingTags;
        public float spawnPerKm2, respawnDays;
        public int yieldMin, yieldMax;
    }

}

