using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland {
    public sealed class LostIslandQuarterCamera : MonoBehaviour {
        public Transform target;
        public Vector3 offset=new Vector3(-28,36,-28);
        public float smooth=8;
        Camera lens;
        void Awake() { lens=GetComponent<Camera>(); if(target) transform.position=target.position+offset; transform.rotation=Quaternion.LookRotation(-offset); }
        void LateUpdate() {
            if(!target) return;
            var wanted=target.position+offset;
            transform.position=Vector3.Distance(transform.position,wanted)>100?wanted:Vector3.Lerp(transform.position,wanted,1-Mathf.Exp(-smooth*Time.deltaTime));
            if(Mouse.current!=null && Application.isFocused) lens.orthographicSize=Mathf.Clamp(lens.orthographicSize-Mouse.current.scroll.ReadValue().y*.025f,10,150);
        }
    }
}
