using System.Collections.Generic;
using Eldoria.CameraControl;
using Eldoria.Gameplay;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using L = Eldoria.Terrain.JungleLayout;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Builds the Untamed map (Scenes/Jungle): 100 × 100 m of dense alien jungle with 180° symmetry (everything at (x, z)
    /// is also at (−x, −z), turned round). Thickets of undergrowth (structure terrain, like walls: they block sight and
    /// movement) fill everything but the trails (7 m wide: three from each deployment strip to the central clearing, and
    /// cross trails through two glades on each side), so the fighting is close. Cover on the open ground: an ancient
    /// shrine and boulders (Full-cover), fallen trees and ferns (Half-cover). The shape comes from <see cref="JungleLayout"/>.
    /// Menu: Ground Invasion > Build Untamed.
    /// </summary>
    public static class JungleMapBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Jungle.unity";
        private const string MeshDir = Root + "/Meshes";
        private const float Resolution = 0.5f;
        /// <summary>How tall a thicket's mass of undergrowth is: over the head of anything that fights on foot (a Dragon's eyes are 3.2 m up).</summary>
        public const float ThicketHeight = 3.6f;

        private static Material loam, thicket, canopy, stone, bark, fern, moss;

        [MenuItem("Ground Invasion/Build Untamed")]
        public static void Build()
        {
            UrbanMapBuilder.PrepareShared();
            loam = UrbanMapBuilder.Mat("Jungle_Floor", new Color(0.36f, 0.33f, 0.22f),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Materials/FloorGrid.png"));
            thicket = UrbanMapBuilder.Mat("Jungle_Thicket", new Color(0.12f, 0.3f, 0.14f));
            canopy = UrbanMapBuilder.Mat("Jungle_Canopy", new Color(0.2f, 0.42f, 0.18f));
            stone = UrbanMapBuilder.Mat("Shrine_Stone", new Color(0.5f, 0.52f, 0.46f));
            bark = UrbanMapBuilder.Mat("Fallen_Bark", new Color(0.32f, 0.22f, 0.14f));
            fern = UrbanMapBuilder.Mat("Jungle_Fern", new Color(0.3f, 0.55f, 0.2f));
            moss = UrbanMapBuilder.Mat("Boulder_Moss", new Color(0.38f, 0.42f, 0.32f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("JungleMap");
            BuildGround(map.transform);

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(map.transform, false);
            BuildThickets(terrain.transform);
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
            Debug.Log($"[Eldoria] Untamed map built at {ScenePath}");
        }

        // ------------------------------------------------------------------ Ground

        /// <summary>The jungle floor as a height grid (structure terrain).</summary>
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

            string path = $"{MeshDir}/Jungle_Ground.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "Jungle_Ground";
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
            go.AddComponent<MeshRenderer>().sharedMaterial = loam;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.layer = TerrainLayers.Structure;
            go.AddComponent<StructureTerrain>().Configure(StructureKind.Ground);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ Thickets

        /// <summary>
        /// Each thicket: a mass of undergrowth (a cylinder on the Structure layer, so it blocks sight and movement) with a
        /// rounded canopy on top (looks only, no collider).
        /// </summary>
        private static void BuildThickets(Transform parent)
        {
            var group = new GameObject("Thickets").transform;
            group.SetParent(parent, false);
            foreach (var t in L.Thickets)
            {
                float ground = L.GroundHeight(t.Centre.x, t.Centre.y);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Thicket";
                go.transform.SetParent(group, false);
                // Sunk 0.6 m so no edge floats on the rolling floor.
                go.transform.localPosition = new Vector3(t.Centre.x, ground - 0.6f + (ThicketHeight + 0.6f) / 2f, t.Centre.y);
                go.transform.localScale = new Vector3(t.Radius * 2f, (ThicketHeight + 0.6f) / 2f, t.Radius * 2f);
                go.GetComponent<Renderer>().sharedMaterial = thicket;
                go.layer = TerrainLayers.Structure;
                go.AddComponent<StructureTerrain>().Configure(StructureKind.Thicket);
                // Its top isn't ground to stand on: not walkable (it still blocks the ground around it).
                var modifier = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = 1;   // Not Walkable
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);

                var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                top.name = "Canopy";
                Object.DestroyImmediate(top.GetComponent<Collider>());
                top.transform.SetParent(group, false);
                top.transform.localPosition = new Vector3(t.Centre.x, ground + ThicketHeight, t.Centre.y);
                top.transform.localScale = new Vector3(t.Radius * 2.15f, 1.3f, t.Radius * 2.15f);
                top.GetComponent<Renderer>().sharedMaterial = canopy;
                top.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;   // looks only: not walked on, not in the way
                GameObjectUtility.SetStaticEditorFlags(top, StaticEditorFlags.BatchingStatic);
            }
        }

        // ------------------------------------------------------------------ Cover

        /// <summary>The props on the open ground, each at (x, z) and, turned round, at (−x, −z).</summary>
        internal static readonly (string kind, Vector2 at, float yaw)[] Props =
        {
            ("Ancient Shrine", new Vector2(4f, 3.5f), 35f),       // in the central clearing
            ("Fallen Tree", new Vector2(-17f, -25f), 70f),        // in the glades
            ("Fallen Tree", new Vector2(23f, -28f), -20f),
            ("Boulder", new Vector2(-11.8f, -19.2f), 15f),        // at the trail edges, leaving room for a tank
            ("Boulder", new Vector2(19f, -12.5f), 40f),
            ("Boulder", new Vector2(-30.5f, -21.3f), -10f),
            ("Ferns", new Vector2(-22f, -11f), 0f),
            ("Ferns", new Vector2(4.5f, -22f), 0f),
            ("Ferns", new Vector2(32f, -18f), 0f),
            ("Ferns", new Vector2(-5f, -5f), 0f),
            ("Ferns", new Vector2(8.1f, -35.2f), 0f),
        };

        internal static Vector3 SizeOf(string kind) => kind switch
        {
            "Ancient Shrine" => new Vector3(3.5f, 2.4f, 1.4f),
            "Fallen Tree" => new Vector3(0.9f, 0.9f, 6f),
            "Boulder" => new Vector3(2.4f, 2.0f, 2.2f),
            _ => new Vector3(1.6f, 1.0f, 1.6f),   // Ferns
        };

        private static void BuildProps(Transform parent)
        {
            var props = new GameObject("Jungle Props").transform;
            props.SetParent(parent, false);
            foreach (var (kind, at, yaw) in Props)
                foreach (var (p, y) in new[] { (at, yaw), (L.Mirror(at), yaw + 180f) })
                {
                    var size = SizeOf(kind);
                    switch (kind)
                    {
                        case "Ancient Shrine":
                        {
                            var shrine = PropAt(props, kind, PrimitiveType.Cube, stone, p, size, y, false, false, "Full-cover");
                            UrbanMapBuilder.Decor(shrine.transform, "Moss", new Vector3(0f, 0.51f, 0f), new Vector2(0.8f, 0.7f), moss);
                            break;
                        }
                        case "Fallen Tree":
                            PropAt(props, kind, PrimitiveType.Cube, bark, p, size, y, true, false, "Half-cover");
                            break;
                        case "Boulder":
                            PropAt(props, kind, PrimitiveType.Cube, moss, p, size, y, false, false, "Full-cover");
                            break;
                        default:
                            PropAt(props, kind, PrimitiveType.Cylinder, fern, p, new Vector3(size.x, size.y / 2f, size.z), y, true, false, "Half-cover");
                            break;
                    }
                }
        }

        private static IEnumerable<Vector2> Footprint(Vector2 p, Vector3 scale, float yaw)
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
            light.intensity = 1.0f;
            light.color = new Color(0.92f, 1f, 0.86f);   // light filtered through the canopy
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(62f, -20f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.68f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.26f, 0.18f);

            // Camera starts over the player's side, looking north across the map.
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, 0f, -32f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.52f, 0.42f);   // a humid, hazy sky
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
            camGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            camGo.transform.localPosition = camGo.transform.localRotation * new Vector3(0f, 0f, -45f);
            return cam;
        }
    }
}
