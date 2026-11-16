using System.Collections.Generic;
using System.Linq;
using Eldoria.CameraControl;
using Eldoria.Gameplay;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using L = Eldoria.Terrain.PolarLayout;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Builds the Polar Nonsense map (Scenes/Polar): 100 × 100 m of snow, laid out with 180° symmetry (everything at (x, z)
    /// is also at (−x, −z), turned round). Two knolls, two ridges and two low rises whose crests block line of sight; a
    /// small polar station: the Relay Hut in the centre, and a Weather Station and a Supply Shed on each side (roofless,
    /// with doors and windows); and cover: snowbanks, fuel drums and supply crates (Half-cover), ice boulders and fuel
    /// tanks (Full-cover). The shape comes from <see cref="PolarLayout"/>. Menu: Ground Invasion > Build Polar Nonsense.
    /// </summary>
    public static class PolarMapBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Polar.unity";
        private const string MeshDir = Root + "/Meshes";
        private const float Resolution = 0.5f;

        private static Material snow, ice, panel, snowbank, drum, tank, crate;

        [MenuItem("Ground Invasion/Build Polar Nonsense")]
        public static void Build()
        {
            UrbanMapBuilder.PrepareShared();
            snow = UrbanMapBuilder.Mat("Snow", new Color(0.88f, 0.91f, 0.95f),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Materials/FloorGrid.png"));
            ice = UrbanMapBuilder.Mat("Glacial_Ice", new Color(0.62f, 0.74f, 0.85f));
            panel = UrbanMapBuilder.Mat("Station_Panel", new Color(0.84f, 0.36f, 0.16f));
            snowbank = UrbanMapBuilder.Mat("Snowbank", new Color(0.95f, 0.96f, 0.98f));
            drum = UrbanMapBuilder.Mat("Fuel_Drum", new Color(0.62f, 0.16f, 0.12f));
            tank = UrbanMapBuilder.Mat("Fuel_Tank", new Color(0.78f, 0.78f, 0.74f));
            crate = UrbanMapBuilder.Mat("Supply_Crate", new Color(0.36f, 0.42f, 0.3f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("PolarMap");
            BuildGround(map.transform);

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(map.transform, false);
            BuildStation(terrain.transform);
            BuildProps(terrain.transform);

            map.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            map.AddComponent<BattlefieldNavigation>().SetBoardSize(new Vector2(L.Size, L.Size));

            BuildDeploymentZones(map.transform);
            var cam = BuildLightingAndCamera();
            Physics.SyncTransforms();
            new GameObject("Armies");   // armies are deployed when a battle starts (ArmyDeployment)

            var gameController = new GameObject("GameController");
            var mover = gameController.AddComponent<MovementController>();
            var mso = new SerializedObject(mover);
            mso.FindProperty("cam").objectReferenceValue = cam;
            mso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectScenes.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Eldoria] Polar Nonsense map built at {ScenePath}");
        }

        // ------------------------------------------------------------------ Ground

        /// <summary>The snowy ground as a height grid (structure terrain: its crests block line of sight).</summary>
        private static void BuildGround(Transform parent)
        {
            int n = Mathf.RoundToInt(L.Size / Resolution) + 1;
            var verts = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                float x = -L.Half + ix * Resolution, z = -L.Half + iz * Resolution;
                verts[iz * n + ix] = new Vector3(x, L.GroundHeight(x, z), z);
                uvs[iz * n + ix] = new Vector2(x / 5f, z / 5f);   // 5 m grid tiles
            }
            var tris = new List<int>();
            for (int iz = 0; iz < n - 1; iz++)
            for (int ix = 0; ix < n - 1; ix++)
            {
                int i0 = iz * n + ix, i1 = i0 + 1, i2 = i0 + n, i3 = i2 + 1;
                tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
            }

            string path = $"{MeshDir}/Polar_Ground.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "Polar_Ground";
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);

            var go = new GameObject("Ground");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = snow;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.layer = TerrainLayers.Structure;
            go.AddComponent<StructureTerrain>().Configure(StructureKind.Ground);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        private static Vector3 OnGround(float x, float z, float lift = 0f) => new Vector3(x, L.GroundHeight(x, z) + lift, z);

        // ------------------------------------------------------------------ The station

        private static void BuildStation(Transform parent)
        {
            var D = (System.Func<float, float, UrbanMapBuilder.Opening>)((o, w) => UrbanMapBuilder.Opening.Door(o, w));
            var W = (System.Func<float, UrbanMapBuilder.Opening>)(o => UrbanMapBuilder.Opening.Win(o));

            // The Relay Hut, in the centre: a door north and south, a window east and west (symmetrical itself).
            UrbanMapBuilder.Building(parent, "Relay Hut", OnGround(0f, 0f), 0f, 6f, 6f, 2.6f, panel,
                north: new[] { D(0f, 1.6f) }, east: new[] { W(0f) }, south: new[] { D(0f, 1.6f) }, west: new[] { W(0f) });

            // A Weather Station and a Supply Shed on each side, the second of each turned round.
            foreach (float s in new[] { 1f, -1f })
            {
                float yaw = s > 0f ? 0f : 180f;
                var ws = L.Pads.First(p => p.Name == "Weather Station" && Mathf.Sign(p.Centre.y) == -s).Centre;
                UrbanMapBuilder.Building(parent, "Weather Station", OnGround(ws.x, ws.y), yaw, 7f, 5f, 2.6f, panel,
                    north: new[] { D(-1.5f, 1.6f), W(1.8f) }, east: new[] { W(0f) }, south: new[] { W(-1.5f), W(1.8f) }, west: null);
                var shed = L.Pads.First(p => p.Name == "Supply Shed" && Mathf.Sign(p.Centre.x) == -s).Centre;
                UrbanMapBuilder.Building(parent, "Supply Shed", OnGround(shed.x, shed.y), yaw, 5f, 4f, 2.3f, panel,
                    north: null, east: new[] { D(0f, 1.6f) }, south: new[] { W(0f) }, west: null);
            }
        }

        // ------------------------------------------------------------------ Cover

        /// <summary>Every prop is placed twice: at (x, z) and, turned round, at (−x, −z).</summary>
        private static void BuildProps(Transform parent)
        {
            var props = new GameObject("Polar Props").transform;
            props.SetParent(parent, false);

            // Snowbanks across the open ground.
            Snowbank(props, -10f, -10f, 30f, 6f);
            Snowbank(props, 8f, -34f, 0f, 7f);
            Snowbank(props, -22f, -33f, -20f, 6f);
            Snowbank(props, 42f, -18f, 90f, 6f);
            Snowbank(props, 22f, -6f, 60f, 5f);
            Snowbank(props, -40f, -32f, 15f, 5f);
            // Fuel drums by the Weather Station, and supply crates by the Supply Shed.
            Drums(props, new Vector2(19.5f, -18.5f), new Vector2(20.4f, -18.9f), new Vector2(19.9f, -17.6f));
            Drums(props, new Vector2(6f, -26f));
            Crate(props, -34.5f, -2.5f, 15f);
            Crate(props, -35.5f, -10f, 40f);
            Crate(props, -33f, -11f, 0f);
            // Ice boulders and the station's fuel tanks (Full-cover).
            IceBoulder(props, -4f, -17f, 20f);
            IceBoulder(props, -42f, -24f, -10f);
            IceBoulder(props, 34f, -34f, 35f);
            FuelTank(props, 7f, 4f);
            // The radio mast at the Relay Hut (a thin pole: no cover).
            Pair(p => UrbanMapBuilder.Prop(props, "Radio Mast", PrimitiveType.Cylinder, tank, OnGround(p.x, p.y, 4f), new Vector3(0.25f, 4f, 0.25f), 0f,
                cover: false, mantle: false, climb: false), new Vector2(4f, -4f), 0f);
        }

        /// <summary>Calls <paramref name="make"/> at the point and at its mirror (turned round).</summary>
        private static void Pair(System.Action<Vector2, float> make, Vector2 p, float yaw)
        {
            make(p, yaw);
            make(L.Mirror(p), yaw + 180f);
        }

        private static void Pair(System.Func<Vector2, GameObject> make, Vector2 p, float yaw) => Pair((q, _) => make(q), p, yaw);

        private static GameObject PropAt(Transform parent, string name, PrimitiveType shape, Material mat, Vector2 p, Vector3 scale, float yaw,
            bool mantle, bool climb, string keyword)
        {
            float halfHeight = shape == PrimitiveType.Cube ? scale.y / 2f : scale.y;   // cylinders are 2 units tall
            // Set into the snow: as deep as the ground falls away under the prop's footprint, so no side floats on a slope.
            float r = Mathf.Max(scale.x, scale.z) / 2f, centre = L.GroundHeight(p.x, p.y), lowest = centre;
            foreach (var d in new[] { new Vector2(r, 0f), new Vector2(-r, 0f), new Vector2(0f, r), new Vector2(0f, -r) })
                lowest = Mathf.Min(lowest, L.GroundHeight(p.x + d.x, p.y + d.y));
            float sink = 0.05f + (centre - lowest);
            return UrbanMapBuilder.Prop(parent, name, shape, mat, OnGround(p.x, p.y, halfHeight - sink), scale, yaw,
                cover: true, mantle: mantle, climb: climb, keyword);
        }

        /// <summary>A snowbank (1 m high, <paramref name="length"/> long): soldiers step over it. Half-cover.</summary>
        private static void Snowbank(Transform parent, float x, float z, float yaw, float length) =>
            Pair((p, y) => PropAt(parent, "Snowbank", PrimitiveType.Cube, snowbank, p, new Vector3(1.4f, 1f, length), y, true, false, "Half-cover"),
                new Vector2(x, z), yaw);

        /// <summary>Fuel drums (1.1 m): step over them or climb on top. Half-cover.</summary>
        private static void Drums(Transform parent, params Vector2[] spots)
        {
            foreach (var spot in spots)
                Pair((p, y) => PropAt(parent, "Fuel Drum", PrimitiveType.Cylinder, drum, p, new Vector3(0.7f, 0.55f, 0.7f), y, true, true, "Half-cover"),
                    spot, 0f);
        }

        /// <summary>A supply crate (1.2 m): climb on it or over it. Half-cover.</summary>
        private static void Crate(Transform parent, float x, float z, float yaw) =>
            Pair((p, y) => PropAt(parent, "Supply Crate", PrimitiveType.Cube, crate, p, Vector3.one * 1.2f, y, true, true, "Half-cover"),
                new Vector2(x, z), yaw);

        /// <summary>A block of glacial ice (2.6 m): blocks movement; Full-cover.</summary>
        private static void IceBoulder(Transform parent, float x, float z, float yaw) =>
            Pair((p, y) => PropAt(parent, "Ice Boulder", PrimitiveType.Cube, ice, p, new Vector3(3f, 2.6f, 2.4f), y, false, false, "Full-cover"),
                new Vector2(x, z), yaw);

        /// <summary>A fuel tank (2.5 m tall, 3 m across): blocks movement; Full-cover.</summary>
        private static void FuelTank(Transform parent, float x, float z) =>
            Pair((p, y) => PropAt(parent, "Fuel Tank", PrimitiveType.Cylinder, tank, p, new Vector3(3f, 1.25f, 3f), y, false, false, "Full-cover"),
                new Vector2(x, z), 0f);

        // ------------------------------------------------------------------ Deployment, camera

        private static void BuildDeploymentZones(Transform parent)
        {
            float z = L.Half - L.DeployDepth / 2f;
            Zone(parent, "Deployment (Player)", Side.Player, new Vector3(0f, 0f, -z), UrbanMapBuilder.Mat("Deploy_Player", new Color(0.33f, 0.38f, 0.47f)));
            Zone(parent, "Deployment (AI)", Side.AI, new Vector3(0f, 0f, z), UrbanMapBuilder.Mat("Deploy_AI", new Color(0.47f, 0.35f, 0.34f)));
        }

        private static void Zone(Transform parent, string name, Side side, Vector3 centre, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.AddComponent<DeploymentZone>().Configure(side, new Vector2(L.Size, L.DeployDepth));
            UrbanMapBuilder.Decor(go.transform, "Marking", new Vector3(0f, 0.01f, 0f), new Vector2(L.Size, L.DeployDepth), mat);
        }

        private static Camera BuildLightingAndCamera()
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(0.94f, 0.96f, 1f);   // a low, cold polar sun
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(24f, -55f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.76f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.6f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.38f, 0.42f);

            // Camera starts over the player's side, looking north across the map.
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, 0f, -30f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.84f, 0.9f);   // a pale polar sky
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            var controller = rig.AddComponent<TacticalCameraController>();
            var so = new SerializedObject(controller);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.FindProperty("boundsMin").vector2Value = new Vector2(-L.Half, -L.Half);
            so.FindProperty("boundsMax").vector2Value = new Vector2(L.Half, L.Half);
            so.FindProperty("maxDistance").floatValue = 110f;
            so.ApplyModifiedPropertiesWithoutUndo();
            camGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            camGo.transform.localPosition = camGo.transform.localRotation * new Vector3(0f, 0f, -45f);
            return cam;
        }
    }
}
