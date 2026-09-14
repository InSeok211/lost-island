using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static class AnchorSurface {
        public static AnchorIslandLayout Layout { get; private set; }
        static float[] poiHeights;
        static float Smooth(float start,float end,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,value));
        public static void Configure(AnchorIslandLayout value) {
            Layout=value; poiHeights=new float[value.pois.Length];
            for(int i=0;i<poiHeights.Length;i++) poiHeights[i]=Mathf.Max(value.seaLevelMeters+2,Base(value.pois[i].x,value.pois[i].z));
        }
        public static float Ellipse(float x,float z,AnchorZone zone) => Mathf.Sqrt(Mathf.Pow((x-zone.x)/zone.radiusX,2)+Mathf.Pow((z-zone.z)/zone.radiusZ,2));
        public static AnchorZone Zone(float x,float z) {
            AnchorZone best=null; float score=float.MaxValue;
            foreach(var zone in Layout.zones) {
                float d=Ellipse(x,z,zone);
                if(d>1)continue;
                if(zone.type=="settlement") return zone;
                if(zone.id=="restricted-north")return zone;
                if(d<score) {score=d;best=zone;}
            }
            return best;
        }
        public static bool InZone(float x,float z,string id) { var zone=Zone(x,z); return zone!=null && zone.id==id; }
        public static float Base(float x,float z) {
            var l=Layout;
            float radial=Mathf.Sqrt(Mathf.Pow(x/l.islandRadiusX,2)+Mathf.Pow(z/l.islandRadiusZ,2));
            float edge=radial+(Mathf.PerlinNoise((x+l.seed)*l.coastNoiseScale,(z-l.seed)*l.coastNoiseScale)-.5f)*l.coastNoiseStrength*2;
            float skirt=l.seaLevelMeters-12;
            float mask=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge));
            float h=l.seaLevelMeters+mask*(95+55*Mathf.SmoothStep(0,1,Mathf.InverseLerp(150,1200,z)));
            float west=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-400,-1200,x));
            h+=west*65*(1-Smooth(.80f,1,edge));
            h+=(Mathf.PerlinNoise((x+731)*.0021f,(z-271)*.0021f)-.5f)*24*mask;
            if(edge>1) h=Mathf.Lerp(l.seaLevelMeters,skirt,Mathf.SmoothStep(0,1,(edge-1)/.18f));
            float wet=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(Mathf.Pow((x-1030)/510,2)+Mathf.Pow((z-20)/690,2)));
            float channel=Mathf.Exp(-Mathf.Pow((x-1030-95*Mathf.Sin(z*.012f))/24,2));
            h=Mathf.Lerp(h,l.seaLevelMeters+1.3f-3.8f*channel,wet);
            // Navigable harbor inlet joins the specified inland harbor coordinate to the sea.
            if(z<-700 && z>-1320) {
                float inlet=1-Smooth(45,115,Mathf.Abs(x-220));
                float along=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-705,-820,z));
                h=Mathf.Lerp(h,l.seaLevelMeters-6,inlet*along);
            }
            foreach(var f in l.flatZones) {
                float d=Vector2.Distance(new Vector2(x,z),new Vector2(f.x,f.z));
                h=Mathf.Lerp(h,f.targetHeightMeters,1-Smooth(f.radius*.65f,f.radius,d));
            }
            return Mathf.Clamp(h,2,l.terrainHeightMeters-2);
        }
        public static float Height(float x,float z) {
            float h=Base(x,z);
            for(int i=0;i<Layout.pois.Length;i++) {
                var p=Layout.pois[i]; if(Mathf.Abs(x-p.x)>55||Mathf.Abs(z-p.z)>55)continue;
                float radius=p.type=="farm"?38:25;
                h=Mathf.Lerp(h,poiHeights[i],1-Smooth(radius,radius+25,Vector2.Distance(new Vector2(x,z),new Vector2(p.x,p.z))));
            }
            return h;
        }
        public static float RoadDistance(float x,float z,out float width) {
            float min=float.MaxValue; width=0;
            foreach(var r in Layout.roads) {
                var a=System.Array.Find(Layout.pois,p=>p.id==r.from); var b=System.Array.Find(Layout.pois,p=>p.id==r.to);
                Vector2 start=new Vector2(a.x,a.z), end=new Vector2(b.x,b.z), q=new Vector2(x,z);
                float t=Mathf.Clamp01(Vector2.Dot(q-start,end-start)/(end-start).sqrMagnitude);
                float d=Vector2.Distance(q,Vector2.Lerp(start,end,t)); if(d<min){min=d;width=r.width;}
            }
            return min;
        }
        public static bool Clear(float x,float z,float margin=8) {
            foreach(var p in Layout.pois) if(Vector2.Distance(new Vector2(x,z),new Vector2(p.x,p.z))<32+margin)return false;
            float d=RoadDistance(x,z,out float width); return d>width*.5f+margin;
        }
    }
}
