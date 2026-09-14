using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static class AnchorMaterialFinish {
        // Original procedural surface treatment; no third-party textures or licences.
        public static void Apply(Material material,string key){
            key=key.ToLowerInvariant();bool grain=key.Contains("wood")||key.Contains("timber")||key.Contains("bark");bool weave=key.Contains("canvas")||key.Contains("cloth")||key.Contains("linen");bool plaster=key.Contains("plaster");
            bool stone=key.Contains("stone");if(!grain&&!weave&&!plaster&&!stone)return;
            const int n=128;var texture=new Texture2D(n,n,TextureFormat.RGB24,true){name="Procedural "+key,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float noise=Mathf.PerlinNoise(x*.09f,y*.09f);float value=.91f+noise*.09f;
                if(grain){float line=Mathf.Sin(y*.55f+Mathf.PerlinNoise(x*.018f,y*.03f)*5);value*=.94f+.06f*line;if(y%32<1)value*=.88f;}
                if(stone)value*=.82f+Mathf.PerlinNoise(x*.31f,y*.31f)*.18f;
                if(weave)value*=((x%4==0||y%4==0)?.86f:1);
                pixels[y*n+x]=new Color(value,value,value);
            }
            texture.SetPixels(pixels);texture.Apply();material.mainTexture=texture;material.SetFloat("_Smoothness",.08f);
        }
    }
}
