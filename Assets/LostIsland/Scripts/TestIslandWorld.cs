using UnityEngine;

namespace LostIsland
{
    public sealed class TestIslandWorld : MonoBehaviour
    {
        public void Build()
        {
            var sand = Material("Warm sand", new Color(0.83f, 0.72f, 0.46f));
            var grass = Material("Meadow", new Color(0.32f, 0.55f, 0.28f));
            var sea = Material("Lagoon", new Color(0.06f, 0.43f, 0.53f));
            var rock = Material("Coastal stone", new Color(0.37f, 0.45f, 0.45f));
            var bark = Material("Wood", new Color(0.34f, 0.23f, 0.14f));
            var leaf = Material("Tree canopy", new Color(0.14f, 0.35f, 0.23f));
            var cloth = Material("Explorer coat", new Color(0.91f, 0.40f, 0.19f));
            var cream = Material("Explorer hat", new Color(0.95f, 0.87f, 0.65f));
            var seaObject = Shape("Sea", PrimitiveType.Cube, new Vector3(0,-0.85f,0), new Vector3(350,0.3f,350), sea);
            RemoveCollider(seaObject);
            Land(sand, grass);
            for (int i = 0; i < 9; i++)
            {
                float angle = (i * 31 + 15) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (7 + (i % 3) * 0.55f);
                Shape("Tree trunk " + i, PrimitiveType.Cylinder, p + Vector3.up * 1.25f, new Vector3(0.35f,0.9f,0.35f), bark);
                var crown = Shape("Tree canopy " + i, PrimitiveType.Sphere, p + Vector3.up * 2.9f, new Vector3(2.1f,2.7f,2.1f), leaf);
                RemoveCollider(crown);
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = (i * 53 + 10) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)) * 9.5f;
                var stone = Shape("Shore rock " + i, PrimitiveType.Sphere, p + Vector3.up * 0.3f, new Vector3(1.4f,1.1f,1.0f), rock);
                stone.transform.rotation = Quaternion.Euler(i*7, i*41, i*13);
            }
            Shape("Camp platform", PrimitiveType.Cube, new Vector3(-3,0.47f,-2), new Vector3(2.6f,0.2f,2), bark);
            Shape("Camp marker", PrimitiveType.Cylinder, new Vector3(-3,1.9f,-2), new Vector3(0.12f,1.4f,0.12f), bark);
            var flag = Shape("Orange camp flag", PrimitiveType.Cube, new Vector3(-2.55f,2.8f,-2), new Vector3(0.9f,0.5f,0.06f), cloth);
            RemoveCollider(flag);

            var player = new GameObject("Explorer");
            player.transform.SetParent(transform);
            player.transform.position = new Vector3(0,0.7f,0);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.32f; controller.center = Vector3.up * 0.9f;
            controller.stepOffset = 0.3f; controller.slopeLimit = 45;
            var body = Shape("Coat", PrimitiveType.Capsule, Vector3.zero, new Vector3(0.65f,0.65f,0.65f), cloth);
            body.transform.SetParent(player.transform); body.transform.localPosition = Vector3.up * 0.85f; RemoveCollider(body);
            var hat = Shape("Hat", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f,0.1f,0.9f), cream);
            hat.transform.SetParent(player.transform); hat.transform.localPosition = Vector3.up * 1.55f; RemoveCollider(hat);
            var nose = Shape("Facing indicator", PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f,0.15f,0.25f), cream);
            nose.transform.SetParent(player.transform); nose.transform.localPosition = new Vector3(0,1.25f,0.34f); RemoveCollider(nose);

            var cameraObject = new GameObject("Quarter View Camera", typeof(Camera), typeof(AudioListener), typeof(QuarterViewCamera));
            cameraObject.transform.SetParent(transform); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 12; camera.farClipPlane = 500;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f,0.43f,0.53f);
            var follow = cameraObject.GetComponent<QuarterViewCamera>(); follow.target = player.transform;
            cameraObject.transform.position = player.transform.position + follow.offset;
            cameraObject.transform.rotation = Quaternion.LookRotation(-follow.offset);
            player.AddComponent<IslandPlayer>().view = cameraObject.transform;
            var sunObject = new GameObject("Afternoon sun", typeof(Light)); sunObject.transform.SetParent(transform);
            var sun = sunObject.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 2;
            sun.color = new Color(1,0.94f,0.82f); sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48,-35,0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f,0.65f,0.72f);
            RenderSettings.sun = sun;
        }

        void Land(Material sand, Material grass)
        {
            const int count = 64;
            Vector3[] v = new Vector3[1 + count * 2]; v[0] = new Vector3(0,0.35f,0);
            var inner = new int[count*3]; var outer = new int[count*6];
            for (int i=0;i<count;i++)
            {
                float a = i * Mathf.PI * 2 / count;
                float r = 1 + 0.065f * Mathf.Sin(a*3) + 0.04f * Mathf.Cos(a*5);
                v[i+1] = new Vector3(Mathf.Cos(a)*9.8f*r,0.35f,Mathf.Sin(a)*9.8f*r);
                v[i+1+count] = new Vector3(Mathf.Cos(a)*12*r,-0.5f,Mathf.Sin(a)*12*r);
                int n = (i+1)%count;
                inner[i*3]=0; inner[i*3+1]=n+1; inner[i*3+2]=i+1;
                int t=i*6;
                outer[t]=i+1; outer[t+1]=n+1; outer[t+2]=i+1+count;
                outer[t+3]=n+1; outer[t+4]=n+1+count; outer[t+5]=i+1+count;
            }
            var mesh = new Mesh { name = "Island terrain", vertices = v, subMeshCount = 2 };
            mesh.SetTriangles(inner,0); mesh.SetTriangles(outer,1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var land = new GameObject("Island terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            land.transform.SetParent(transform); land.GetComponent<MeshFilter>().sharedMesh = mesh;
            land.GetComponent<MeshRenderer>().sharedMaterials = new[] { grass, sand };
            land.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        Material Material(string name, Color color)
        {
            var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            result.SetFloat("_Smoothness", 0.12f); return result;
        }
        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(type); result.name = name; result.transform.SetParent(transform);
            result.transform.position = position; result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material; return result;
        }
        static void RemoveCollider(GameObject obj) { Object.DestroyImmediate(obj.GetComponent<Collider>()); }
    }
}
