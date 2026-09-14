using UnityEngine;

namespace LostIsland
{
    // Every terrain tile samples the same continuous world-space function.
    public static class WorldSurface
    {
        static WorldLayout layout;
        static float[] poiHeights;
        public static void Configure(WorldLayout value)
        {
            layout = value;
            poiHeights = new float[layout.pois.Length];
            for (int i=0;i<poiHeights.Length;i++) {
                var p = layout.pois[i];
                poiHeights[i] = BaseHeight(p.x,p.z);
            }
        }

        public static string PoiMode(PoiDef p)
        {
            float height = BaseHeight(p.x,p.z);
            if (height >= layout.seaLevelMeters + 2) return "land";
            return p.type == "anchor" || p.type == "research" || p.type == "harbor" || p.type == "settlement" ? "platform" : "underwater";
        }

        public static float Height(float x, float z)
        {
            float h = BaseHeight(x,z);
            for (int i=0;i<layout.pois.Length;i++) {
                var p = layout.pois[i];
                if (Mathf.Abs(x-p.x)>120 || Mathf.Abs(z-p.z)>120 || poiHeights[i]<layout.seaLevelMeters+2) continue;
                float d = Vector2.Distance(new Vector2(x,z),new Vector2(p.x,p.z));
                h = Mathf.Lerp(h, poiHeights[i],1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,120,d)));
            }
            return Mathf.Clamp(h,0,layout.terrainHeightMeters);
        }

        static float BaseHeight(float x, float z)
        {
            float sea = layout.seaLevelMeters;
            float h = sea - 110 + 16 * Mathf.PerlinNoise((x+layout.seed)*.001f,(z+layout.seed)*.001f);
            foreach(var r in layout.regions) {
                float dx=(x-r.x)/r.radiusX, dz=(z-r.z)/r.radiusZ;
                if (Mathf.Abs(dx)>1.35f || Mathf.Abs(dz)>1.35f) continue;
                float d=Mathf.Sqrt(dx*dx+dz*dz);
                if(r.kind!="land") {
                    float weight=1-Mathf.SmoothStep(0,1,d);
                    if(r.biome=="coral") h=Mathf.Lerp(h,sea-18+7*Mathf.PerlinNoise(x*.006f,z*.006f),weight);
                    if(r.biome=="deep-ocean" || r.biome=="abyss") {
                        float trench=Mathf.Exp(-Mathf.Pow((x-r.x-250*Mathf.Sin(z*.002f))/250,2));
                        h=Mathf.Lerp(h,10+35*(1-trench),weight);
                    }
                    continue;
                }
                if(d>=1.3f) continue;
                float f=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(d));
                float n=Mathf.PerlinNoise((x+layout.seed)*.0007f,(z+layout.seed)*.0007f)-.5f;
                float land=sea+r.height*f*(1+n*r.relief);
                // Broad submerged skirt meets the floor continuously, without the V2 110 m ledge.
                if(d>1) land=Mathf.Lerp(sea,sea-110,Mathf.SmoothStep(0,1,(d-1)/.3f));
                if(r.biome=="swamp") {
                    float channel=Mathf.Exp(-Mathf.Pow((x-r.x-350*Mathf.Sin(z*.002f))/120,2));
                    land=Mathf.Lerp(land,sea+4-9*channel,f);
                }
                h=Mathf.Max(h,land);
            }
            return h;
        }

        public static bool NearPoi(float x,float z,float radius)
        {
            foreach(var p in layout.pois) if(Mathf.Abs(x-p.x)<radius && Mathf.Abs(z-p.z)<radius) return true;
            return false;
        }
    }

    public static class WorldCoordinates
    {
        // Image origin: top left. World north: +Z. V2 layout uses a square world extent.
        public static Vector3 MapToWorld(Vector2 pixel, Vector2 imageSize, float worldSize, float y=0) =>
            new Vector3((pixel.x/imageSize.x-.5f)*worldSize,y,(.5f-pixel.y/imageSize.y)*worldSize);
        public static Vector2 WorldToMap(Vector3 world, Vector2 imageSize, float worldSize) =>
            new Vector2((world.x/worldSize+.5f)*imageSize.x,(.5f-world.z/worldSize)*imageSize.y);
        public static Vector2Int Tile(Vector3 position,float worldSize,float tileSize) =>
            new Vector2Int(Mathf.FloorToInt((position.x+worldSize*.5f)/tileSize),Mathf.FloorToInt((position.z+worldSize*.5f)/tileSize));
    }
}
