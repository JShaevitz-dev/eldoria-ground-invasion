using System.Collections.Generic;
using Eldoria.CameraControl;
using Eldoria.Gameplay;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using L = Eldoria.Terrain.ShipLayout;
using O = Eldoria.EditorTools.UrbanMapBuilder.Opening;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Builds the Leisure Cruise map (Scenes/Ship): the cluttered inside of a ship, 50 m wide and 150 m long, laid out with
    /// 180° symmetry. A hangar bay at each end (the deployment zones); a 6 m spine corridor down the middle and a 4 m
    /// corridor along each side of the hull; sixteen compartments between them (armory, med bay, crew quarters, galley...),
    /// with doors and viewing windows, crossed by two cross corridors; and a cargo hold in the middle. Bulkheads are
    /// structure (they block line of sight); clutter is strewn everywhere: crates, coolant drums, debris, consoles, bunks
    /// (Half-cover), cargo containers, lockers and reactor housings (Full-cover). The shape comes from
    /// <see cref="ShipLayout"/>. Menu: Ground Invasion > Build Leisure Cruise.
    /// </summary>
    public static class ShipMapBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Ship.unity";
        private const string MeshDir = Root + "/Meshes";

        private static Material deck, bulkhead, hull, hazard, roomFloor, holdFloor, hangarFloor;
        private static Material crate, drum, debris, container, locker, console, bunk, reactor;

        [MenuItem("Ground Invasion/Build Leisure Cruise")]
        public static void Build()
        {
            UrbanMapBuilder.PrepareShared();
            deck = UrbanMapBuilder.Mat("Ship_Deck", new Color(0.42f, 0.45f, 0.48f),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Materials/FloorGrid.png"));
            bulkhead = UrbanMapBuilder.Mat("Ship_Bulkhead", new Color(0.6f, 0.63f, 0.66f));
            hull = UrbanMapBuilder.Mat("Ship_Hull", new Color(0.3f, 0.32f, 0.36f));
            hazard = UrbanMapBuilder.Mat("Hazard_Stripe", new Color(0.86f, 0.7f, 0.16f));
            roomFloor = UrbanMapBuilder.Mat("Ship_Room_Floor", new Color(0.34f, 0.37f, 0.4f));
            holdFloor = UrbanMapBuilder.Mat("Ship_Hold_Floor", new Color(0.36f, 0.33f, 0.3f));
            hangarFloor = UrbanMapBuilder.Mat("Ship_Hangar_Floor", new Color(0.3f, 0.33f, 0.36f));
            crate = UrbanMapBuilder.Mat("Supply_Crate", new Color(0.36f, 0.42f, 0.3f));
            drum = UrbanMapBuilder.Mat("Coolant_Drum", new Color(0.2f, 0.42f, 0.62f));
            debris = UrbanMapBuilder.Mat("Ship_Debris", new Color(0.4f, 0.36f, 0.33f));
            container = UrbanMapBuilder.Mat("Cargo_Container", new Color(0.66f, 0.36f, 0.16f));
            locker = UrbanMapBuilder.Mat("Ship_Locker", new Color(0.46f, 0.5f, 0.42f));
            console = UrbanMapBuilder.Mat("Ship_Console", new Color(0.18f, 0.2f, 0.24f));
            bunk = UrbanMapBuilder.Mat("Ship_Bunk", new Color(0.52f, 0.5f, 0.46f));
            reactor = UrbanMapBuilder.Mat("Reactor_Housing", new Color(0.7f, 0.72f, 0.74f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("ShipMap");
            BuildDeck(map.transform);

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(map.transform, false);
            BuildHull(terrain.transform);
            BuildBulkheads(terrain.transform);
            BuildClutter(terrain.transform);
            BuildDecor(map.transform);

            map.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            map.AddComponent<BattlefieldNavigation>().SetBoardSize(new Vector2(L.Width, L.Length));

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
            Debug.Log($"[Eldoria] Leisure Cruise map built at {ScenePath}");
        }

        // ------------------------------------------------------------------ Deck and hull

        /// <summary>The flat deck (structure ground), with world-space UVs so the measuring grid lines up.</summary>
        private static void BuildDeck(Transform parent)
        {
            const float step = 5f;
            int nx = Mathf.RoundToInt(L.Width / step) + 1, nz = Mathf.RoundToInt(L.Length / step) + 1;
            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                float x = -L.HalfWidth + ix * step, z = -L.HalfLength + iz * step;
                verts[iz * nx + ix] = new Vector3(x, 0f, z);
                uvs[iz * nx + ix] = new Vector2(x / 5f, z / 5f);
            }
            var tris = new List<int>();
            for (int iz = 0; iz < nz - 1; iz++)
            for (int ix = 0; ix < nx - 1; ix++)
            {
                int i0 = iz * nx + ix, i1 = i0 + 1, i2 = i0 + nx, i3 = i2 + 1;
                tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
            }

            string path = $"{MeshDir}/Ship_Deck.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "Ship_Deck";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);

            var go = new GameObject("Deck");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = deck;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.layer = TerrainLayers.Structure;
            go.AddComponent<StructureTerrain>().Configure(StructureKind.Ground);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>The hull round the edge of the map (4 m high, 0.5 m thick).</summary>
        private static void BuildHull(Transform parent)
        {
            var edge = new GameObject("Hull").transform;
            edge.SetParent(parent, false);
            const float t = 0.5f, h = L.HullHeight;
            HullWall(edge, "Hull", new Vector3(0f, h / 2f, L.HalfLength + t / 2f), new Vector3(L.Width + t * 2f, h, t));
            HullWall(edge, "Hull", new Vector3(0f, h / 2f, -L.HalfLength - t / 2f), new Vector3(L.Width + t * 2f, h, t));
            HullWall(edge, "Hull", new Vector3(L.HalfWidth + t / 2f, h / 2f, 0f), new Vector3(t, h, L.Length));
            HullWall(edge, "Hull", new Vector3(-L.HalfWidth - t / 2f, h / 2f, 0f), new Vector3(t, h, L.Length));
        }

        private static void HullWall(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = hull;
            wall.layer = TerrainLayers.Structure;
            wall.AddComponent<StructureTerrain>().Configure(StructureKind.Building);
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ Bulkheads

        private static O D(float offset, float width = 2f) => O.Door(offset, width);
        private static O W(float offset) => O.Win(offset);

        /// <summary>
        /// The interior walls of the northern half, each built again turned round in the southern half. Openings are
        /// offsets from the middle of the wall, along it from its first point to its second.
        /// </summary>
        private static void BuildBulkheads(Transform parent)
        {
            var walls = new GameObject("Bulkheads").transform;
            walls.SetParent(parent, false);
            float side = L.SideWallX, spine = L.SpineHalfWidth, hold = L.HoldHalfLength, end = L.HangarEdge;
            float gap = L.CrossCorridorWidth;

            // Along the ship: the side corridors' inner walls (beside the hold, then the compartments)...
            Wall(walls, new Vector2(side, 0f), new Vector2(side, end), D(-26f), D(5f, gap), D(-15f), D(-3f), D(13f), D(25f));
            Wall(walls, new Vector2(-side, 0f), new Vector2(-side, end), D(-25f), D(5f, gap), D(-17f), D(-1f), D(11f), D(27f));
            // ...and the spine corridor's walls (from the hold to the hangar), with viewing windows into two compartments.
            Wall(walls, new Vector2(spine, hold), new Vector2(spine, end), D(0f, gap), D(-20f), D(-8f), D(8f), D(20f), W(-16f));
            Wall(walls, new Vector2(-spine, hold), new Vector2(-spine, end), D(0f, gap), D(-22f), D(-6f), D(6f), D(22f), W(-10f));

            // Across the ship: the compartments' ends (the hold, the dividers, the cross corridor, the hangar bulkhead).
            float c0 = L.CrossCorridorZ - gap / 2f, c1 = L.CrossCorridorZ + gap / 2f;
            Across(walls, spine, side, hold, D(3f), W(-5f));
            Across(walls, spine, side, 22f, D(0f));
            Across(walls, spine, side, c0, D(-4f));
            Across(walls, spine, side, c1, D(4f));
            Across(walls, spine, side, 50f, D(0f));
            Across(walls, spine, side, end, D(0f));
            Across(walls, -side, -spine, hold, D(-3f), W(5f));
            Across(walls, -side, -spine, 22f, D(2f));
            Across(walls, -side, -spine, c0, D(3f));
            Across(walls, -side, -spine, c1, D(-3f));
            Across(walls, -side, -spine, 50f, D(-2f));
            Across(walls, -side, -spine, end, D(0f));
        }

        private static void Across(Transform parent, float x0, float x1, float z, params O[] openings) =>
            Wall(parent, new Vector2(x0, z), new Vector2(x1, z), openings);

        /// <summary>A bulkhead from a to b, and its twin from −a to −b (the same openings land at the mirrored points).</summary>
        private static void Wall(Transform parent, Vector2 a, Vector2 b, params O[] openings)
        {
            foreach (float s in new[] { 1f, -1f })
                UrbanMapBuilder.WallLine(parent, new Vector3(a.x * s, 0f, a.y * s), new Vector3(b.x * s, 0f, b.y * s), L.WallHeight, bulkhead,
                    openings, extend: true);
        }

        // ------------------------------------------------------------------ Clutter

        private static Transform clutter;

        /// <summary>Every piece is placed twice: at (x, z) and, turned round, at (−x, −z). Positions are listed for the north half.</summary>
        private static void BuildClutter(Transform parent)
        {
            clutter = new GameObject("Clutter").transform;
            clutter.SetParent(parent, false);

            // The compartments (positions from each room's centre).
            Room("Armory", r =>
            {
                r(Locker, 5.5f, 5.3f, 0f); r(Locker, -5.5f, 5.3f, 0f);
                r(Crate, 6.5f, -3.5f, 0f); r(Crate, 7.7f, -2.2f, 10f); r(Crate, -6.5f, -4f, 20f);
            });
            Room("Med Bay", r =>
            {
                r(MedBed, -6f, 3.2f, 90f); r(MedBed, -6f, -3.2f, 90f);
                r(Console, 6f, -4.5f, 0f); r(Console, 6f, 4.8f, 0f);
            });
            Room("Crew Quarters", r =>
            {
                r(Bunk, -5.5f, 4.2f, 0f); r(Bunk, 5.5f, 4.2f, 0f); r(Bunk, -5.5f, -4.2f, 0f);
                r(Locker, 8.3f, -3.5f, 90f);
            }, east: true);
            Room("Machine Shop", r =>
            {
                r(Workbench, -5f, 3.5f, 0f);
                r(Drum, 6f, -3f, 0f); r(Drum, 6.8f, -3.6f, 0f); r(Drum, 6.4f, -2.4f, 0f);
                r(Crate, 5.5f, 3.8f, 25f);
            });
            Room("Galley", r =>
            {
                r(Table, -4.5f, 1.5f, 0f); r(Table, 4.5f, 1.5f, 0f);
                r(Counter, -5.5f, 5.2f, 0f);
            });
            Room("Comms Room", r =>
            {
                r(Console, -6f, 4.5f, 0f); r(Console, 6f, 4.5f, 0f); r(Console, -6f, -4.5f, 0f);
            });
            Room("Crew Quarters", r =>
            {
                r(Bunk, 5.5f, 4.2f, 0f); r(Bunk, 5.5f, -4.2f, 0f); r(Bunk, -6f, 4.2f, 0f);
            }, east: false);
            Room("Reactor Control", r =>
            {
                r(Reactor, 5f, -2.5f, 0f);
                r(Console, -6f, 4.5f, 0f); r(Console, -6.5f, -4.5f, 0f);
            });

            // The side corridors: clutter pushed against the hull.
            Crate(24.2f, 20f, 0f); Crate(24.3f, 21.4f, 15f); Drum(24.4f, 31f); Drum(24.4f, 31.8f);
            Debris(22.6f, 48f, 0f); Crate(24.2f, 52f, 0f);
            Drum(-24.4f, 18f); Drum(-24.4f, 18.8f); Crate(-24.2f, 25f, 10f); Crate(-24.2f, 40f, 0f);
            Debris(-22.6f, 51f, 20f);
            Crate(24.2f, 3f, 20f); Crate(24.2f, 58f, 0f); Debris(-23.5f, 3f, 80f); Drum(-24.4f, 33f);

            // The spine corridor: against the walls, leaving at least 4 m for vehicles.
            Crate(2.2f, 24f, 0f); Drum(-2.3f, 22f); Drum(-2.3f, 22.8f);
            Debris(-2.1f, 48f, 10f); Crate(2.2f, 54f, 30f);
            Debris(2f, 32f, 0f); Drum(2.4f, 40f); Drum(2.4f, 40.8f); Crate(-2.2f, 61f, 15f);

            // The cross corridors.
            Crate(10f, 37.2f, 0f); Drum(15f, 34.7f); Drum(15.8f, 34.7f);
            Crate(-12f, 34.8f, 20f); Debris(-7f, 36.5f, 70f);

            // The cargo hold: containers (one in the very middle, blocking the spine's line of sight end to end) and loose cargo.
            UrbanMapBuilder.Prop(clutter, "Cargo Container", PrimitiveType.Cube, container, new Vector3(0f, 1.3f, 0f), new Vector3(6f, 2.6f, 2.4f), 0f,
                cover: true, mantle: false, climb: false, "Full-cover");
            Container(-12f, 5f, 0f); Container(14f, 5f, 90f);
            Crate(5.5f, 6.5f, 15f); Crate(-17f, 1.5f, 0f); Crate(18f, 8.5f, 30f);
            Drum(-6f, 7.5f); Drum(-6.8f, 7.9f);
            Debris(8f, 1.5f, 40f);

            // The hangar bays, just inside the bulkhead (clear of the deployment zones).
            Crate(8f, 63.5f, 0f); Crate(9.3f, 63.7f, 10f); Crate(-17f, 63.8f, 0f); Drum(18.5f, 63.5f);
        }

        private delegate void Place(System.Action<float, float, float> make, float x, float z, float yaw);

        /// <summary>Clutter inside a compartment, placed relative to its centre (the room is found by name, and by side for the two Crew Quarters).</summary>
        private static void Room(string name, System.Action<Place> fill, bool? east = null)
        {
            foreach (var room in L.Rooms)
            {
                if (room.Name != name || (east.HasValue && (room.Centre.x > 0f) != east.Value)) continue;
                var c = room.Centre;
                fill((make, x, z, yaw) => make(c.x + x, c.y + z, yaw));
            }
        }

        // Adapters so every kind of clutter fits Room's (x, z, yaw) shape.
        private static void Locker(float x, float z, float yaw) => Pair("Locker", PrimitiveType.Cube, locker, x, z, new Vector3(2f, 2.2f, 0.6f), yaw, false, false, "Full-cover");
        private static void Bunk(float x, float z, float yaw) => Pair("Bunk", PrimitiveType.Cube, bunk, x, z, new Vector3(2.1f, 0.9f, 1f), yaw, true, true, "Half-cover");
        private static void MedBed(float x, float z, float yaw) => Pair("Med Bed", PrimitiveType.Cube, bunk, x, z, new Vector3(2.1f, 0.9f, 1f), yaw, true, true, "Half-cover");
        private static void Console(float x, float z, float yaw) => Pair("Console", PrimitiveType.Cube, console, x, z, new Vector3(1.6f, 1.1f, 0.6f), yaw, true, false, "Half-cover");
        private static void Table(float x, float z, float yaw) => Pair("Table", PrimitiveType.Cube, bunk, x, z, new Vector3(2.4f, 0.8f, 1f), yaw, true, true, "Half-cover");
        private static void Counter(float x, float z, float yaw) => Pair("Counter", PrimitiveType.Cube, locker, x, z, new Vector3(3f, 1f, 0.8f), yaw, true, true, "Half-cover");
        private static void Workbench(float x, float z, float yaw) => Pair("Workbench", PrimitiveType.Cube, locker, x, z, new Vector3(2.4f, 1f, 1f), yaw, true, true, "Half-cover");
        private static void Reactor(float x, float z, float yaw) => Pair("Reactor Housing", PrimitiveType.Cylinder, reactor, x, z, new Vector3(3f, 1.3f, 3f), yaw, false, false, "Full-cover");
        private static void Crate(float x, float z, float yaw) => Pair("Supply Crate", PrimitiveType.Cube, crate, x, z, Vector3.one * 1.2f, yaw, true, true, "Half-cover");
        private static void Debris(float x, float z, float yaw) => Pair("Debris", PrimitiveType.Cube, debris, x, z, new Vector3(1.2f, 0.8f, 2.4f), yaw, true, false, "Half-cover");
        private static void Container(float x, float z, float yaw) => Pair("Cargo Container", PrimitiveType.Cube, container, x, z, new Vector3(6f, 2.6f, 2.4f), yaw, false, false, "Full-cover");
        private static void Drum(float x, float z) => Drum(x, z, 0f);
        private static void Drum(float x, float z, float yaw) => Pair("Coolant Drum", PrimitiveType.Cylinder, drum, x, z, new Vector3(0.7f, 0.55f, 0.7f), yaw, true, true, "Half-cover");

        /// <summary>A piece of clutter standing on the deck at (x, z), and its twin turned round at (−x, −z).</summary>
        private static void Pair(string name, PrimitiveType shape, Material mat, float x, float z, Vector3 scale, float yaw, bool mantle, bool climb, string keyword)
        {
            float halfHeight = shape == PrimitiveType.Cube ? scale.y / 2f : scale.y;   // cylinders are 2 units tall
            foreach (float s in new[] { 1f, -1f })
                UrbanMapBuilder.Prop(clutter, name, shape, mat, new Vector3(x * s, halfHeight, z * s), scale, yaw + (s > 0f ? 0f : 180f),
                    cover: true, mantle: mantle, climb: climb, keyword);
        }

        // ------------------------------------------------------------------ Decor, deployment, camera

        /// <summary>Floor colours and hazard stripes: visual only.</summary>
        private static void BuildDecor(Transform parent)
        {
            var decor = new GameObject("Decor").transform;
            decor.SetParent(parent, false);
            UrbanMapBuilder.Decor(decor, "Hold Deck", new Vector3(0f, 0.008f, 0f), new Vector2(L.SideWallX * 2f, L.HoldHalfLength * 2f), holdFloor);
            foreach (float s in new[] { 1f, -1f })
            {
                float hangarMid = (L.HangarEdge + L.HalfLength) / 2f;
                UrbanMapBuilder.Decor(decor, "Hangar Deck", new Vector3(0f, 0.006f, hangarMid * s), new Vector2(L.Width, L.HalfLength - L.HangarEdge), hangarFloor);
                UrbanMapBuilder.Decor(decor, "Hazard Stripe", new Vector3(0f, 0.011f, (L.HangarEdge + 0.4f) * s), new Vector2(L.SpineHalfWidth * 2f, 0.3f), hazard);
                UrbanMapBuilder.Decor(decor, "Spine Stripe", new Vector3(0f, 0.011f, (L.HoldHalfLength + L.HangarEdge) / 2f * s),
                    new Vector2(0.2f, L.HangarEdge - L.HoldHalfLength), hazard);
                foreach (var room in L.Rooms)
                    UrbanMapBuilder.Decor(decor, room.Name + " Floor", new Vector3(room.Centre.x * s, 0.008f, room.Centre.y * s), new Vector2(17.7f, 11.7f), roomFloor);
            }
        }

        private static void BuildDeploymentZones(Transform parent)
        {
            float z = L.HalfLength - L.DeployDepth / 2f;
            Zone(parent, "Deployment (Player)", Side.Player, new Vector3(0f, 0f, -z), UrbanMapBuilder.Mat("Deploy_Player", new Color(0.33f, 0.38f, 0.47f)));
            Zone(parent, "Deployment (AI)", Side.AI, new Vector3(0f, 0f, z), UrbanMapBuilder.Mat("Deploy_AI", new Color(0.47f, 0.35f, 0.34f)));
        }

        private static void Zone(Transform parent, string name, Side side, Vector3 centre, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.AddComponent<DeploymentZone>().Configure(side, new Vector2(L.Width, L.DeployDepth));
            UrbanMapBuilder.Decor(go.transform, "Marking", new Vector3(0f, 0.012f, 0f), new Vector2(L.Width, L.DeployDepth), mat);
        }

        private static Camera BuildLightingAndCamera()
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.95f;
            light.color = new Color(0.9f, 0.94f, 1f);   // cold ship lighting from overhead
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(70f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.56f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.43f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.25f, 0.28f);

            // Camera starts over the player's hangar, looking up the ship.
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, 0f, -55f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);   // the dark of space outside the hull
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            var controller = rig.AddComponent<TacticalCameraController>();
            var so = new SerializedObject(controller);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.FindProperty("boundsMin").vector2Value = new Vector2(-L.HalfWidth, -L.HalfLength);
            so.FindProperty("boundsMax").vector2Value = new Vector2(L.HalfWidth, L.HalfLength);
            so.FindProperty("maxDistance").floatValue = 110f;
            so.ApplyModifiedPropertiesWithoutUndo();
            camGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            camGo.transform.localPosition = camGo.transform.localRotation * new Vector3(0f, 0f, -40f);
            return cam;
        }
    }
}
