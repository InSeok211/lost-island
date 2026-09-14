using UnityEngine;
using UnityEngine.InputSystem;
namespace LostIsland {
    [RequireComponent(typeof(CharacterController))]
    public sealed class LostIslandDemoMover : MonoBehaviour {
        public float speed=12, sprint=3, gravity=-25, turnSpeed=12;
        public Transform view;
        public LostIslandWorldStreamer streamer;
        CharacterController cc; float yVel; Vector3 spawn;
        bool showRegions;
#if UNITY_EDITOR
        [System.NonSerialized] public Vector2 verificationInput;
#endif
        void Awake() { cc=GetComponent<CharacterController>(); spawn=transform.position; }
        public void Teleport(Vector3 p) { cc.enabled=false; transform.position=p; yVel=0; cc.enabled=true; }
        void Update() {
            if(!streamer || !streamer.ReadyAt(transform.position)) return;
            var k=Keyboard.current; Vector2 input=Vector2.zero;
            if(k!=null && Application.isFocused) {
                if(k.f1Key.wasPressedThisFrame) showRegions=!showRegions;
                if(k.rKey.wasPressedThisFrame) { Teleport(spawn); return; }
                input=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
            }
#if UNITY_EDITOR
            input+=verificationInput;
#endif
            input=Vector2.ClampMagnitude(input,1);
            var forward=view?Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized:Vector3.forward;
            var right=view?Vector3.ProjectOnPlane(view.right,Vector3.up).normalized:Vector3.right;
            var direction=forward*input.y+right*input.x;
            var delta=direction*speed*(k!=null&&k.leftShiftKey.isPressed?sprint:1)*Time.deltaTime;
            var next=transform.position+delta;
            float edge=streamer.worldSizeMeters*.5f-2;
            if(Mathf.Abs(next.x)>edge || Mathf.Abs(next.z)>edge || !streamer.ReadyAt(next)) delta=Vector3.zero;
            if(cc.isGrounded && yVel<0) yVel=-2;
            yVel+=gravity*Time.deltaTime;
            cc.Move(delta+Vector3.up*yVel*Time.deltaTime);
            if(direction.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-turnSpeed*Time.deltaTime));
            // Surface traversal for the blockout: sea routes remain testable without a boat system.
            if(transform.position.y<streamer.seaLevel+1 && WorldSurface.Height(transform.position.x,transform.position.z)<streamer.seaLevel)
                Teleport(new Vector3(transform.position.x,streamer.seaLevel+1,transform.position.z));
            if(transform.position.y < -10) Teleport(spawn);
        }
        void OnGUI() {
            GUI.Box(new Rect(16,16,420,100),GUIContent.none);
            GUI.Label(new Rect(30,26,400,25),"LOST ISLAND V2 / STREAMING BLOCKOUT");
            GUI.Label(new Rect(30,52,400,25),"WASD / Arrows: Move | Shift: Sprint | Wheel: Zoom | R: Home");
            if(streamer) GUI.Label(new Rect(30,78,400,25),$"Tile {streamer.CurrentTile} | Loaded {streamer.LoadedCount} | {(streamer.Busy?"Loading...":"Ready")} | Sea: surface travel");
            if(GUI.Button(new Rect(16,122,200,28),"F1 / Region travel (debug)")) showRegions=!showRegions;
            if(showRegions && streamer && streamer.Layout!=null) {
                var regions=streamer.Layout.regions;
                for(int i=0;i<regions.Length;i++) if(GUI.Button(new Rect(16+(i/7)*212,156+(i%7)*32,206,28),regions[i].id)) {
                    var r=regions[i]; Teleport(new Vector3(r.x,Mathf.Max(streamer.seaLevel,WorldSurface.Height(r.x,r.z))+4,r.z)); showRegions=false;
                }
            }
        }
    }
}
