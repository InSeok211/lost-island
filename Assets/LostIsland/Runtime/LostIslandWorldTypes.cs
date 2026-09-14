
using System;
using UnityEngine;

namespace LostIsland
{
    [Serializable] public class WorldLayout {
        public string worldName; public int seed; public float worldSizeMeters;
        public float tileSizeMeters; public float terrainHeightMeters; public float seaLevelMeters;
        public int heightmapResolution; public int alphamapResolution; public int loadRadiusTiles;
        public RegionDef[] regions; public PoiDef[] pois;
    }

    [Serializable] public class RegionDef {
        public string id; public string displayName; public string kind; public string biome;
        public float x; public float z; public float radiusX; public float radiusZ;
        public float height; public float relief; public string[] creatures;
    }

    [Serializable] public class PoiDef {
        public string id; public string displayName; public string type; public float x; public float z;
    }

    [Serializable] public class BiomeRuleCollection { public BiomeRule[] biomes; }

    [Serializable] public class BiomeRule {
        public string id; public int treesPerTile; public int rocksPerTile; public int bushesPerTile;
        public int ruinsPerTile; public float minScale = .8f; public float maxScale = 1.4f;
    }

    [Serializable] public class AssetBindings {
        public string note;
        public string treePrefab; public string rockPrefab; public string bushPrefab; public string ruinPrefab;
        public string anchorPrefab; public string harborPrefab; public string researchPrefab;
        public string settlementPrefab; public string landmarkPrefab; public string dangerPrefab;
    }

}

