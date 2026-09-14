using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorArtLighting : MonoBehaviour {
        Color oldAmbient;float oldIntensity;Light sun;Color oldSun;float oldSunIntensity;Quaternion oldRotation;
        void Start(){
            oldAmbient=RenderSettings.ambientLight;oldIntensity=RenderSettings.ambientIntensity;
            RenderSettings.ambientLight=new Color(.57f,.65f,.69f);RenderSettings.ambientIntensity=.8f;
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){sun=light;break;}
            if(sun){oldSun=sun.color;oldSunIntensity=sun.intensity;oldRotation=sun.transform.rotation;sun.color=new Color(1,.91f,.76f);sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(48,-35,0);}
        }
        void OnDestroy(){RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientIntensity=oldIntensity;if(sun){sun.color=oldSun;sun.intensity=oldSunIntensity;sun.transform.rotation=oldRotation;}}
    }
}
