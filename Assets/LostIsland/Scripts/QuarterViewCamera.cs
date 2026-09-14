using UnityEngine;
using UnityEngine.InputSystem;

namespace LostIsland
{
    [RequireComponent(typeof(Camera))]
    public sealed class QuarterViewCamera : MonoBehaviour
    {
        public Transform target;
        public float followSharpness = 7;
        public float minZoom = 6;
        public float maxZoom = 18;
        public Vector3 offset = new Vector3(-14, 18, -14);
        Camera lens;
        float desiredZoom;
        public void SetZoom(float value){desiredZoom=Mathf.Clamp(value,minZoom,maxZoom);if(lens)lens.orthographicSize=desiredZoom;}

        void Awake()
        {
            lens = GetComponent<Camera>();
            lens.orthographic = true;
            desiredZoom=Mathf.Clamp(lens.orthographicSize,minZoom,maxZoom);
            if (target) transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset);
        }

        void LateUpdate()
        {
            if (!target) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset,
                1 - Mathf.Exp(-followSharpness * Time.deltaTime));
            if (Mouse.current != null && Application.isFocused)
                desiredZoom = ZoomSize(desiredZoom, Mouse.current.scroll.ReadValue().y, minZoom, maxZoom);
            lens.orthographicSize=Mathf.Lerp(lens.orthographicSize,desiredZoom,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            lens.orthographicSize = Mathf.Clamp(lens.orthographicSize, Mathf.Max(.1f,minZoom), Mathf.Max(minZoom,maxZoom));
        }
        public static float ZoomSize(float size,float scroll,float minimum,float maximum){
            minimum=Mathf.Max(.1f,minimum);maximum=Mathf.Max(minimum,maximum);
            return Mathf.Clamp(size-scroll*5f,minimum,maximum);
        }
    }
}
