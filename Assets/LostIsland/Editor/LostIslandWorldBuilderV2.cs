
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostIsland.Editor
{
    public static class LostIslandWorldBuilderV2
    {
        const string ConfigPath = "Assets/LostIsland/Data/world-layout.json";
        const string BiomeRulesPath = "Assets/LostIsland/Data/biome-rules.json";
        const string BindingsPath = "Assets/LostIsland/Data/asset-bindings.json";
        public const string Root = "Assets/LostIsland/Generated/WorldV2";

        static WorldLayout layout;
        static BiomeRuleCollection biomeRules;
        static AssetBindings bindings;
        static TerrainLayer[] terrainLayers;
        static Dictionary<string, GameObject> placeholders = new Dictionary<string, GameObject>();

        [MenuItem("Lost Island/V2/Build Streaming World")]
        public static void BuildStreamingWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            try {
            LoadData();
            PrepareFolders();
            EditorSceneManager.SaveScene(scratch, Root + "/BuildWorkspace.unity");
            CreateMaterialsAndPlaceholderPrefabs();
            terrainLayers = CreateTerrainLayers();

            int tiles = Mathf.RoundToInt(layout.worldSizeMeters / layout.tileSizeMeters);
            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var existing in EditorBuildSettings.scenes)
                if (!existing.path.StartsWith(Root + "/")) buildScenes.Add(existing);

            for (int z = 0; z < tiles; z++)
                for (int x = 0; x < tiles; x++)
                    {
                        EditorUtility.DisplayProgressBar("Lost Island V2", $"Terrain {z * tiles + x + 1}/{tiles * tiles}", (z * tiles + x) / (float)(tiles * tiles));
                        buildScenes.Add(BuildTileScene(x, z, tiles));
                    }

            var bootstrap = BuildBootstrapScene(tiles);
            buildScenes.Insert(0, bootstrap);
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Lost Island V2] Built " + (tiles * tiles) + " terrain scenes and bootstrap.");
            } finally {
                EditorUtility.ClearProgressBar();
                if (scratch.IsValid()) EditorSceneManager.CloseScene(scratch, true);
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        static void LoadData()
        {
            layout = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(ConfigPath));
            biomeRules = JsonUtility.FromJson<BiomeRuleCollection>(File.ReadAllText(BiomeRulesPath));
            bindings = JsonUtility.FromJson<AssetBindings>(File.ReadAllText(BindingsPath));
            if (layout == null || layout.regions == null || layout.pois == null) throw new Exception("Invalid world layout");
            if (layout.tileSizeMeters <= 0 || layout.worldSizeMeters % layout.tileSizeMeters != 0 || layout.terrainHeightMeters <= layout.seaLevelMeters) throw new Exception("Invalid world dimensions");
            WorldSurface.Configure(layout);
        }

        static void PrepareFolders()
        {
            EnsureFolder(Root);
            foreach (var p in new[] { "TerrainData","Scenes","Prefabs","Materials","Textures","Layers" })
                EnsureFolder($"{Root}/{p}");
        }

        static EditorBuildSettingsScene BuildTileScene(int tx, int tz, int tiles)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string sceneName = $"LI_Tile_{tx}_{tz}";
            SceneManager.SetActiveScene(scene);

            float half = layout.worldSizeMeters * .5f;
            Vector3 tileOrigin = new Vector3(-half + tx * layout.tileSizeMeters, 0f,
                                             -half + tz * layout.tileSizeMeters);
            Vector3 tileCenter = tileOrigin + new Vector3(layout.tileSizeMeters * .5f, 0f, layout.tileSizeMeters * .5f);

            var root = new GameObject(sceneName);
            var terrain = BuildTerrain(tx, tz, tileOrigin, root.transform);
            BuildTileWater(tileCenter, root.transform);
            PopulateEnvironment(tx, tz, terrain, tileOrigin, root.transform);
            BuildPoisInTile(tx, tz, tileOrigin, terrain, root.transform);
            BuildSpawnZonesInTile(tx, tz, tileOrigin, terrain, root.transform);

            string path = $"{Root}/Scenes/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            return new EditorBuildSettingsScene(path, true);
        }

        static Terrain BuildTerrain(int tx, int tz, Vector3 tileOrigin, Transform parent)
        {
            string dataPath = $"{Root}/TerrainData/Terrain_{tx}_{tz}.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath))
                AssetDatabase.DeleteAsset(dataPath);

            var data = new TerrainData {
                heightmapResolution = layout.heightmapResolution,
                alphamapResolution = layout.alphamapResolution,
                baseMapResolution = 512,
                size = new Vector3(layout.tileSizeMeters, layout.terrainHeightMeters, layout.tileSizeMeters),
                terrainLayers = terrainLayers
            };

            int res = data.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++) {
                    float wx = tileOrigin.x + (x / (float)(res - 1)) * layout.tileSizeMeters;
                    float wz = tileOrigin.z + (y / (float)(res - 1)) * layout.tileSizeMeters;
                    heights[y, x] = EvaluateHeightMeters(wx, wz) / layout.terrainHeightMeters;
                }

            data.SetHeights(0, 0, heights);
            AssetDatabase.CreateAsset(data, dataPath);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = $"Terrain_{tx}_{tz}";
            go.transform.position = tileOrigin;
            go.transform.SetParent(parent);
            var terrain = go.GetComponent<Terrain>();
            terrain.allowAutoConnect = true;
            terrain.groupingID = 2;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8;
            terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/Terrain.mat");
            PaintBiome(data, tileOrigin);
            return terrain;
        }

        static float EvaluateHeightMeters(float wx, float wz) => WorldSurface.Height(wx, wz);

        static void PaintBiome(TerrainData data, Vector3 origin)
        {
            int w = data.alphamapWidth, h = data.alphamapHeight;
            float[,,] alpha = new float[h, w, terrainLayers.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    float wx = origin.x + x / (float)(w - 1) * data.size.x;
                    float wz = origin.z + y / (float)(h - 1) * data.size.z;
                    var r = FindNearestLandRegion(wx, wz);
                    float height = EvaluateHeightMeters(wx, wz);
                    int layer = height < layout.seaLevelMeters + 6 ? 5 : (r == null ? 4 : BiomeLayer(r.biome));
                    alpha[y, x, Mathf.Clamp(layer, 0, terrainLayers.Length - 1)] = 1f;
                }

            data.SetAlphamaps(0, 0, alpha);
        }

        static void PopulateEnvironment(int tx, int tz, Terrain terrain, Vector3 origin, Transform parent)
        {
            int seed = layout.seed ^ (tx * 73856093) ^ (tz * 19349663);
            var rnd = new System.Random(seed);
            var env = new GameObject("Environment");
            env.transform.SetParent(parent);
            // Select the biome at each sample, not at the tile center: mixed tiles retain both biomes.
            foreach (var category in new[] { "Tree", "Rock", "Bush", "Ruin" })
                for (int i=0; i<1200; i++)
                    TryPlace(category, null, terrain, origin, env.transform, rnd, 1, 1);
        }

        static void PlaceCategory(string category, int count, BiomeRule rule, RegionDef region, Terrain terrain,
                                  Vector3 origin, Transform parent, System.Random rnd)
        {
            for (int i = 0; i < count; i++)
                TryPlace(category, region, terrain, origin, parent, rnd, rule.minScale, rule.maxScale);
        }

        static void TryPlace(string category, RegionDef region, Terrain terrain, Vector3 origin, Transform parent,
                             System.Random rnd, float minScale, float maxScale)
        {
            float x = origin.x + (float)rnd.NextDouble() * layout.tileSizeMeters;
            float z = origin.z + (float)rnd.NextDouble() * layout.tileSizeMeters;
            region = FindNearestLandRegion(x,z);
            if (region == null) return;
            var rule = GetBiomeRule(region.biome);
            if (rule == null) return;
            int density = category == "Tree" ? rule.treesPerTile : category == "Rock" ? rule.rocksPerTile : category == "Bush" ? rule.bushesPerTile : rule.ruinsPerTile;
            if (rnd.NextDouble() > density * (category == "Ruin" ? 1f : 8f) / 1200f) return;
            minScale = rule.minScale; maxScale = rule.maxScale;
            float dx = (x - region.x) / Mathf.Max(1f, region.radiusX);
            float dz = (z - region.z) / Mathf.Max(1f, region.radiusZ);
            if (dx * dx + dz * dz > 1.05f) return;

            float y = EvaluateHeightMeters(x, z);
            if (y < layout.seaLevelMeters + 3f) return;

            region = FindNearestLandRegion(x, z);
            if (region == null) return;
            float slope = terrain.terrainData.GetSteepness((x-origin.x)/layout.tileSizeMeters,(z-origin.z)/layout.tileSizeMeters);
            if (slope > (category == "Rock" ? 48 : 28) || WorldSurface.NearPoi(x,z,55)) return;
            y = terrain.SampleHeight(new Vector3(x,0,z)) + terrain.transform.position.y;
            var prefab = GetBoundOrPlaceholder(category);
            if (!prefab) return;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            float s = Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble());
            go.transform.localScale *= s;
            if (category == "Tree" && (region.biome == "snow" || region.biome == "jungle"))
                go.transform.localScale = Vector3.Scale(go.transform.localScale,new Vector3(region.biome == "snow" ? .65f : 1.2f,region.biome == "snow" ? 1.5f : 1.8f,region.biome == "snow" ? .65f : 1.2f));
            var tag = go.GetComponent<LostIslandBiomeTag>() ?? go.AddComponent<LostIslandBiomeTag>();
            tag.biomeId = region.biome;
            tag.regionId = region.id;
        }

        static void BuildTileWater(Vector3 center, Transform parent)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water_Blockout";
            water.transform.SetParent(parent);
            water.transform.position = new Vector3(center.x, layout.seaLevelMeters, center.z);
            water.transform.localScale = Vector3.one * (layout.tileSizeMeters / 10f);
            var col = water.GetComponent<Collider>();
            if (col) UnityEngine.Object.DestroyImmediate(col);
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/Water.mat");
            if (mat) water.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void BuildPoisInTile(int tx, int tz, Vector3 origin, Terrain terrain, Transform parent)
        {
            var poiroot = new GameObject("POI");
            poiroot.transform.SetParent(parent);

            foreach (var p in layout.pois) {
                if (!PointInTile(p.x, p.z, origin)) continue;

                float y = terrain.SampleHeight(new Vector3(p.x,0,p.z)) + terrain.transform.position.y;
                var prefab = GetPoiPrefab(p.type);
                var go = prefab ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject();
                go.name = "POI_" + p.displayName;
                go.transform.SetParent(poiroot.transform);
                go.transform.position = new Vector3(p.x, y, p.z);

                var marker = go.GetComponent<LostIslandPoi>() ?? go.AddComponent<LostIslandPoi>();
                marker.id = p.id;
                marker.displayName = p.displayName;
                marker.poiType = p.type;
                marker.surface = WorldSurface.PoiMode(p);
                if (p.type == "settlement" && marker.surface == "land") {
                    var plaza=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plaza.name="Settlement courtyard"; plaza.transform.SetParent(go.transform);
                    plaza.transform.localPosition=new Vector3(0,.04f,-10);
                    plaza.transform.localScale=new Vector3(48,.08f,46);
                    plaza.GetComponent<Renderer>().sharedMaterial=Mat("Ruin");
                    foreach(float side in new[] { -1f, 1f }) {
                        var tree=(GameObject)PrefabUtility.InstantiatePrefab(GetBoundOrPlaceholder("Tree"));
                        tree.name="Courtyard tree"; tree.transform.SetParent(go.transform);
                        tree.transform.localPosition=new Vector3(side*19,0,-17);
                    }
                }
                if (marker.surface == "platform") {
                    go.transform.position = new Vector3(p.x, layout.seaLevelMeters + 3, p.z);
                    var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    deck.name = "Offshore foundation"; deck.transform.SetParent(go.transform);
                    deck.transform.localPosition = new Vector3(0,-1,0); deck.transform.localScale = new Vector3(35,2,35);
                    deck.GetComponent<Renderer>().sharedMaterial = Mat("Harbor");
                }
            }
        }

        static void BuildSpawnZonesInTile(int tx, int tz, Vector3 origin, Terrain terrain, Transform parent)
        {
            var root = new GameObject("SpawnZones");
            root.transform.SetParent(parent);

            foreach (var r in layout.regions) {
                if (r.creatures == null || r.creatures.Length == 0) continue;
                if (!PointInTile(r.x, r.z, origin)) continue;

                float y = r.kind == "land"
                    ? Mathf.Max(layout.seaLevelMeters + 3f, EvaluateHeightMeters(r.x, r.z))
                    : layout.seaLevelMeters - 5f;

                var go = new GameObject("SpawnZone_" + r.displayName);
                go.transform.SetParent(root.transform);
                go.transform.position = new Vector3(r.x, y, r.z);
                var zone = go.AddComponent<LostIslandSpawnZone>();
                zone.regionId = r.id;
                zone.creatureIds = r.creatures;
                zone.size = new Vector2(r.radiusX * 2f, r.radiusZ * 2f);
                zone.marine = r.kind != "land";
                zone.seaLevel = layout.seaLevelMeters;
                zone.seed = layout.seed + tx * 101 + tz * 977;
                zone.terrain = terrain;

                var preview = new GameObject("CreaturePreviewSpawner");
                preview.transform.SetParent(go.transform);
                var sp = preview.AddComponent<LostIslandCreaturePreviewSpawner>();
                sp.zone = zone;
                sp.previewCount = Mathf.Clamp(r.creatures.Length * 2, 4, 12);
            }
        }

        static EditorBuildSettingsScene BuildBootstrapScene(int tiles)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var systems = new GameObject("WORLD_SYSTEMS");
            var streamer = systems.AddComponent<LostIslandWorldStreamer>();
            streamer.worldSizeMeters = layout.worldSizeMeters;
            streamer.tileSizeMeters = layout.tileSizeMeters;
            streamer.tilesPerAxis = tiles;
            streamer.loadRadiusTiles = Mathf.Max(1, layout.loadRadiusTiles);

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "DemoPlayer";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, EvaluateHeightMeters(0,80) + 3f, 80f);
            player.GetComponent<Renderer>().sharedMaterial = Mat("Danger");
            var oldCol = player.GetComponent<CapsuleCollider>();
            if (oldCol) UnityEngine.Object.DestroyImmediate(oldCol);
            player.AddComponent<CharacterController>();
            player.AddComponent<LostIslandDemoMover>();
            streamer.target = player.transform;
            streamer.seaLevel = layout.seaLevelMeters;
            streamer.layoutAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigPath);
            player.GetComponent<LostIslandDemoMover>().streamer = streamer;

            var cameraGo = new GameObject("QuarterViewCamera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.nearClipPlane = .3f;
            cam.farClipPlane = 5000f;
            cam.orthographic = true; cam.orthographicSize = 25;
            cam.backgroundColor = new Color(.15f,.32f,.42f);
            cameraGo.AddComponent<AudioListener>();
            var follow = cameraGo.AddComponent<LostIslandQuarterCamera>();
            follow.target = player.transform;
            cameraGo.transform.position = player.transform.position + follow.offset;
            cameraGo.transform.LookAt(player.transform.position);
            player.GetComponent<LostIslandDemoMover>().view = cameraGo.transform;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f,.66f,.72f);
            light.shadows = LightShadows.Soft;

            string path = $"{Root}/Scenes/LostIsland_Bootstrap.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            return new EditorBuildSettingsScene(path, true);
        }

        static void CreateMaterialsAndPlaceholderPrefabs()
        {
            var mats = new Dictionary<string, Color> {
                ["Tree"] = new Color(.12f,.34f,.10f),
                ["Trunk"] = new Color(.30f,.18f,.08f),
                ["Rock"] = new Color(.35f,.36f,.34f),
                ["Bush"] = new Color(.18f,.45f,.14f),
                ["Ruin"] = new Color(.42f,.40f,.35f),
                ["Anchor"] = new Color(.10f,.65f,.85f),
                ["Settlement"] = new Color(.55f,.42f,.24f),
                ["Research"] = new Color(.72f,.74f,.70f),
                ["Harbor"] = new Color(.34f,.23f,.12f),
                ["Danger"] = new Color(.65f,.15f,.10f),
                ["Landmark"] = new Color(.55f,.50f,.42f),
                ["Water"] = new Color(.08f,.28f,.48f, .72f)
            };

            foreach (var kv in mats) CreateMaterial(kv.Key, kv.Value);
            var terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            DeleteIfExists($"{Root}/Materials/Terrain.mat");
            AssetDatabase.CreateAsset(terrainMaterial, $"{Root}/Materials/Terrain.mat");

            placeholders["Tree"] = MakeTreePrefab();
            placeholders["Rock"] = MakeRockPrefab();
            placeholders["Bush"] = MakeBushPrefab();
            placeholders["Ruin"] = MakeRuinPrefab();
            placeholders["Anchor"] = MakeBeaconPrefab("Anchor", 8f, 1.2f);
            placeholders["Harbor"] = MakeStructurePrefab("Harbor", 12f, 4f);
            placeholders["Research"] = MakeStructurePrefab("Research", 10f, 6f);
            placeholders["Settlement"] = MakeStructurePrefab("Settlement", 9f, 5f);
            placeholders["Landmark"] = MakeBeaconPrefab("Landmark", 12f, 1.8f);
            placeholders["Danger"] = MakeBeaconPrefab("Danger", 10f, 1.4f);
        }

        static TerrainLayer[] CreateTerrainLayers()
        {
            string[] names = { "Grass","Jungle","Swamp","Snow","Rock","Desert","Unknown" };
            Color[] colors = {
                new Color(.28f,.48f,.20f), new Color(.08f,.30f,.11f), new Color(.21f,.28f,.16f),
                new Color(.87f,.90f,.93f), new Color(.34f,.33f,.31f), new Color(.72f,.56f,.31f),
                new Color(.30f,.36f,.32f)
            };

            var result = new TerrainLayer[names.Length];
            for (int i = 0; i < names.Length; i++) {
                string texPath = $"{Root}/Textures/{names[i]}.asset";
                string layerPath = $"{Root}/Layers/{names[i]}.terrainlayer";
                DeleteIfExists(texPath); DeleteIfExists(layerPath);

                var tex = new Texture2D(2, 2);
                tex.name = names[i] + "_Color";
                tex.SetPixels(new[]{colors[i],colors[i],colors[i],colors[i]});
                tex.Apply();
                AssetDatabase.CreateAsset(tex, texPath);

                var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(28,28) };
                AssetDatabase.CreateAsset(layer, layerPath);
                result[i] = layer;
            }
            return result;
        }

        static Material CreateMaterial(string name, Color color)
        {
            string path = $"{Root}/Materials/{name}.mat";
            DeleteIfExists(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (name == "Water") {
                mat.color = color;
                mat.color = new Color(color.r, color.g, color.b, 1); // Opaque blockout sea; no incomplete transparent state.
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject MakeTreePrefab()
        {
            var root = new GameObject("Tree_Placeholder");
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0, 2.5f, 0);
            trunk.transform.localScale = new Vector3(.55f, 2.5f, .55f);
            trunk.GetComponent<Renderer>().sharedMaterial = Mat("Trunk");
            // Keep trunk collision for the movement blockout.

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(root.transform);
            crown.transform.localPosition = new Vector3(0, 6f, 0);
            crown.transform.localScale = new Vector3(4.5f, 4f, 4.5f);
            crown.GetComponent<Renderer>().sharedMaterial = Mat("Tree");
            RemoveCollider(crown);
            return SavePrefab(root, "Tree");
        }

        static GameObject MakeRockPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Rock_Placeholder";
            root.transform.localScale = new Vector3(3f, 1.6f, 2.2f);
            root.GetComponent<Renderer>().sharedMaterial = Mat("Rock");
            return SavePrefab(root, "Rock");
        }

        static GameObject MakeBushPrefab()
        {
            var root = new GameObject("Bush_Placeholder");
            for (int i = 0; i < 3; i++) {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(root.transform);
                s.transform.localPosition = new Vector3((i-1)*1.1f, .8f + (i%2)*.4f, 0);
                s.transform.localScale = Vector3.one * 1.7f;
                s.GetComponent<Renderer>().sharedMaterial = Mat("Bush");
                RemoveCollider(s);
            }
            return SavePrefab(root, "Bush");
        }

        static GameObject MakeRuinPrefab()
        {
            var root = new GameObject("Ruin_Placeholder");
            for (int i=0;i<4;i++) {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(root.transform);
                c.transform.localScale = new Vector3(1.2f, 4f + i, 1.2f);
                c.transform.localPosition = new Vector3((i%2)*5f, c.transform.localScale.y*.5f, (i/2)*5f);
                c.GetComponent<Renderer>().sharedMaterial = Mat("Ruin");
            }
            return SavePrefab(root, "Ruin");
        }

        static GameObject MakeBeaconPrefab(string type, float height, float width)
        {
            var root = new GameObject(type + "_Placeholder");
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(root.transform);
            pillar.transform.localPosition = new Vector3(0,height*.5f,0);
            pillar.transform.localScale = new Vector3(width,height*.5f,width);
            pillar.GetComponent<Renderer>().sharedMaterial = Mat(type);
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(root.transform);
            orb.transform.localPosition = new Vector3(0,height+1f,0);
            orb.transform.localScale = Vector3.one*2f;
            orb.GetComponent<Renderer>().sharedMaterial = Mat(type);
            return SavePrefab(root, type);
        }

        static GameObject MakeStructurePrefab(string type, float width, float height)
        {
            var root = new GameObject(type + "_Placeholder");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0,height*.5f,0);
            body.transform.localScale = new Vector3(width,height,width*.7f);
            body.GetComponent<Renderer>().sharedMaterial = Mat(type);
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.transform.SetParent(root.transform);
            tower.transform.localPosition = new Vector3(width*.3f,height+2f,0);
            tower.transform.localScale = new Vector3(width*.18f,4f,width*.18f);
            tower.GetComponent<Renderer>().sharedMaterial = Mat(type);
            return SavePrefab(root, type);
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            string path = $"{Root}/Prefabs/{name}.prefab";
            DeleteIfExists(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject GetBoundOrPlaceholder(string category)
        {
            string path = "";
            if (bindings != null) {
                if (category=="Tree") path = bindings.treePrefab;
                else if (category=="Rock") path = bindings.rockPrefab;
                else if (category=="Bush") path = bindings.bushPrefab;
                else if (category=="Ruin") path = bindings.ruinPrefab;
            }
            if (!string.IsNullOrEmpty(path)) {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p) return p;
            }
            return placeholders.TryGetValue(category, out var ph) ? ph : null;
        }

        static GameObject GetPoiPrefab(string type)
        {
            string key = type=="anchor" ? "Anchor" :
                         type=="harbor" ? "Harbor" :
                         type=="research" ? "Research" :
                         type=="settlement" ? "Settlement" :
                         type=="danger" ? "Danger" :
                         type=="landmark" ? "Landmark" :
                         type=="ruin" ? "Ruin" : "Landmark";
            string boundPath = key == "Anchor" ? bindings.anchorPrefab : key == "Harbor" ? bindings.harborPrefab : key == "Research" ? bindings.researchPrefab : key == "Settlement" ? bindings.settlementPrefab : key == "Danger" ? bindings.dangerPrefab : key == "Ruin" ? bindings.ruinPrefab : bindings.landmarkPrefab;
            if (!string.IsNullOrEmpty(boundPath)) { var bound = AssetDatabase.LoadAssetAtPath<GameObject>(boundPath); if (bound) return bound; }
            return placeholders.TryGetValue(key, out var p) ? p : null;
        }

        static RegionDef FindNearestLandRegion(float x, float z)
        {
            RegionDef best = null; float bestScore = float.MaxValue;
            foreach (var r in layout.regions) {
                if (r.kind!="land") continue;
                float dx=(x-r.x)/Mathf.Max(1,r.radiusX), dz=(z-r.z)/Mathf.Max(1,r.radiusZ);
                float d=dx*dx+dz*dz;
                if (d<bestScore) { bestScore=d; best=r; }
            }
            return bestScore <= 1.35f ? best : null;
        }

        static BiomeRule GetBiomeRule(string id)
        {
            if (biomeRules?.biomes == null) return null;
            foreach (var b in biomeRules.biomes) if (b.id == id) return b;
            return null;
        }

        static int BiomeLayer(string biome)
        {
            switch (biome) {
                case "grass": return 0;
                case "tropical":
                case "jungle": return 1;
                case "swamp": return 2;
                case "snow": return 3;
                case "desert": return 5;
                case "unknown": return 6;
                default: return 4;
            }
        }

        static bool PointInTile(float x, float z, Vector3 origin)
        {
            return x >= origin.x && x < origin.x + layout.tileSizeMeters &&
                   z >= origin.z && z < origin.z + layout.tileSizeMeters;
        }

        static Material Mat(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{n}.mat");
        static void RemoveCollider(GameObject g) { var c=g.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c); }
        static void DeleteIfExists(string p) { if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p)) AssetDatabase.DeleteAsset(p); }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\","/");
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
