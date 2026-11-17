using System.Collections.Generic;
using Eldoria.CameraControl;
using Eldoria.Gameplay;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using L = Eldoria.Terrain.DesertLayout;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Builds the Desert Surprise map (Scenes/Desert): 140 × 140 m of open salt flat with 180° symmetry (everything at (x, z)
    /// is also at (−x, −z), turned round), for armor and long-range fire. Long sightlines across a nearly bare flat, a few
    /// low dunes for hull-down positions, sparse cover (rock outcrops and wrecks: Full-cover; scrub and cairns:
    /// Half-cover), and the surprise: a dry wadi on each side, a sunken channel from near each deployment strip to the
    /// centre, out of sight of the flat. The shape comes from <see cref="DesertLayout"/>. Menu: Ground Invasion > Build
    /// Desert Surprise.
    /// </summary>
    public static class DesertMapBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Desert.unity";
        private const string MeshDir = Root + "/Meshes";
        private const float Resolution = 0.5f;

        private static Material sand, rock, wreck, rust, scrub, cairn;

        [MenuItem("Ground Invasion/Build Desert Surprise")]
        public static void Build()
        {
            UrbanMapBuilder.PrepareShared();
            sand = UrbanMapBuilder.Mat("Salt_Flat", new Color(0.86f, 0.78f, 0.62f),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Materials/FloorGrid.png"));
            rock = UrbanMapBuilder.Mat("Desert_Rock", new Color(0.58f, 0.46f, 0.34f));   // shared with Ft Solstice's rocks: the same colour
            wreck = UrbanMapBuilder.Mat("Wreck_Hull", new Color(0.42f, 0.44f, 0.46f));
            rust = UrbanMapBuilder.Mat("Wreck_Rust", new Color(0.45f, 0.25f, 0.14f));
            scrub = UrbanMapBuilder.Mat("Desert_Scrub", new Color(0.46f, 0.47f, 0.28f));
            cairn = UrbanMapBuilder.Mat("Cairn_Stone", new Color(0.7f, 0.64f, 0.55f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("DesertMap");
            BuildGround(map.transform);

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(map.transform, false);
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
            Debug.Log($"[Eldoria] Desert Surprise map built at {ScenePath}");
        }

        // ------------------------------------------------------------------ Ground

        /// <summary>The salt flat as a height grid (structure terrain: dune crests and wadi banks block line of sight).</summary>
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

            string path = $"{MeshDir}/Desert_Ground.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "Desert_Ground";
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
            go.AddComponent<MeshRenderer>().sharedMaterial = sand;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.layer = TerrainLayers.Structure;
            go.AddComponent<StructureTerrain>().Configure(StructureKind.Ground);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ Cover

        /// <summary>The props, each at (x, z) and, turned round, at (−x, −z). Sparse: this is a map for long sightlines.</summary>
        internal static readonly (string kind, Vector2 at, float yaw)[] Props =
        {
            // Rock outcrops (Full-cover): a few places to hide a tank.
            ("Rock Outcrop", new Vector2(34f, -6f), 20f),
            ("Rock Outcrop", new Vector2(-46f, -12f), -15f),
            ("Rock Outcrop", new Vector2(4f, -50f), 50f),
            // The sand-blasted wreck of a freighter near the centre, and burned-out tanks (Full-cover).
            ("Freighter Wreck", new Vector2(9f, 9f), 30f),
            ("Tank Wreck", new Vector2(28f, -48f), 40f),
            ("Tank Wreck", new Vector2(-24f, -48f), -25f),
            // Scrub and stone cairns (Half-cover) scattered over the flat.
            ("Scrub", new Vector2(12f, -20f), 0f),
            ("Scrub", new Vector2(-34f, -50f), 0f),
            ("Scrub", new Vector2(52f, -40f), 0f),
            ("Scrub", new Vector2(-4f, -12f), 0f),
            ("Cairn", new Vector2(40f, -30f), 10f),
            ("Cairn", new Vector2(-14f, -38f), 60f),
            ("Cairn", new Vector2(40f, 8f), -20f),
        };

        private static void BuildProps(Transform parent)
        {
            var props = new GameObject("Desert Props").transform;
            props.SetParent(parent, false);
            foreach (var (kind, at, yaw) in Props)
                foreach (var (p, y) in new[] { (at, yaw), (L.Mirror(at), yaw + 180f) })
                    Make(props, kind, p, y);
        }

        internal static Vector3 SizeOf(string kind) => kind switch
        {
            "Rock Outcrop" => new Vector3(4.5f, 2.8f, 3.2f),
            "Freighter Wreck" => new Vector3(4f, 3.6f, 12f),
            "Tank Wreck" => new Vector3(3.2f, 1.9f, 5.5f),
            "Scrub" => new Vector3(1.8f, 0.9f, 1.8f),
            _ => new Vector3(1.6f, 1.1f, 1.6f),   // Cairn
        };

        private static void Make(Transform parent, string kind, Vector2 p, float yaw)
        {
            var size = SizeOf(kind);
            switch (kind)
            {
                case "Rock Outcrop":
                    PropAt(parent, kind, PrimitiveType.Cube, rock, p, size, yaw, false, false, "Full-cover");
                    break;
                case "Freighter Wreck":
                {
                    var hull = PropAt(parent, kind, PrimitiveType.Cube, wreck, p, size, yaw, false, false, "Full-cover");
                    // Rusted ribs along the top and a broken fin, for looks only.
                    foreach (float z in new[] { -0.3f, 0f, 0.3f })
                        UrbanMapBuilder.Decor(hull.transform, "Rib", new Vector3(0f, 0.52f, z), new Vector2(1.05f, 0.04f), rust);
                    break;
                }
                case "Tank Wreck":
                {
                    var hull = PropAt(parent, kind, PrimitiveType.Cube, rust, p, size, yaw, false, false, "Full-cover");
                    UrbanMapBuilder.Decor(hull.transform, "Scorch", new Vector3(0f, 0.51f, 0f), new Vector2(0.7f, 0.5f), wreck);
                    break;
                }
                case "Scrub":
                    PropAt(parent, kind, PrimitiveType.Cylinder, scrub, p, new Vector3(size.x, size.y / 2f, size.z), yaw, true, false, "Half-cover");
                    break;
                default:
                    PropAt(parent, kind, PrimitiveType.Cube, cairn, p, size, yaw, true, true, "Half-cover");
                    break;
            }
        }

        /// <summary>Points across a prop's footprint (its turned rectangle): where the ground under it is sampled.</summary>
        internal static IEnumerable<Vector2> Footprint(Vector2 p, Vector3 scale, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            for (int i = -4; i <= 4; i++)
            for (int j = -4; j <= 4; j++)
            {
                var w = rot * new Vector3(scale.x / 2f * i / 4f, 0f, scale.z / 2f * j / 4f);
                yield return new Vector2(p.x + w.x, p.y + w.z);
            }
        }

        private static GameObject PropAt(Transform parent, string name, PrimitiveType shape, Material mat, Vector2 p, Vector3 scale, float yaw,
            bool mantle, bool climb, string keyword)
        {
            float halfHeight = shape == PrimitiveType.Cube ? scale.y / 2f : scale.y;   // cylinders are 2 units tall
            // Set into the sand: as deep as the ground falls away under the prop's footprint, so no side floats.
            float centre = L.GroundHeight(p.x, p.y), lowest = centre;
            foreach (var q in Footprint(p, scale, yaw)) lowest = Mathf.Min(lowest, L.GroundHeight(q.x, q.y));
            float sink = 0.05f + (centre - lowest);
            return UrbanMapBuilder.Prop(parent, name, shape, mat, new Vector3(p.x, centre + halfHeight - sink, p.y), scale, yaw,
                cover: true, mantle: mantle, climb: climb, keyword);
        }

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
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.95f, 0.85f);   // a high, hot desert sun
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(58f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.8f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.62f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.38f, 0.3f);

            // Camera starts over the player's side, looking north across the map.
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, 0f, -45f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.78f, 0.66f);   // a dusty sky
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 700f;
            camGo.AddComponent<AudioListener>();
            var controller = rig.AddComponent<TacticalCameraController>();
            var so = new SerializedObject(controller);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.FindProperty("boundsMin").vector2Value = new Vector2(-L.Half, -L.Half);
            so.FindProperty("boundsMax").vector2Value = new Vector2(L.Half, L.Half);
            so.FindProperty("maxDistance").floatValue = 150f;
            so.ApplyModifiedPropertiesWithoutUndo();
            camGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            camGo.transform.localPosition = camGo.transform.localRotation * new Vector3(0f, 0f, -55f);
            return cam;
        }
    }
}
