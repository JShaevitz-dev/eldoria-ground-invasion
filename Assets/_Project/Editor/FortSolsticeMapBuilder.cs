using System.Collections.Generic;
using Eldoria.CameraControl;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Builds the map "Siege of Ft Solstice" (Scenes/FortSolstice): 100 m wide × 130 m deep of flat desert, asymmetrical.
    /// A massive fortress fills the north; the defenders deploy inside it, the attackers on the open sand outside it to
    /// the south, with 42 m of open desert between them and the outer wall.
    ///
    ///   South (z -65..-57): attacker deployment zone (Side.Player, south)
    ///   The approach (z -57..-15): open desert with sandstone rocks and boulders for cover
    ///   Outer wall (z -15): three 6 m gates (west, centre, east), one per lane
    ///   The three lanes (z -15..9): split by two walls running north; sandbag barricades and crates in each
    ///   Middle wall (x ±30, z 9..55): three gates in its south face, one at the end of each lane
    ///   The keep (x ±12, z 21..45): a gate in each face; the objective marker in its centre (0, 33)
    ///   North courtyard (z 46..54, inside the middle wall): the defenders' default slots (Side.AI, north); they may
    ///   deploy anywhere in the fortress (inside the outer wall, and inside the middle wall)
    ///   Wall walks: platforms behind the outer and middle walls (not the north face, or the lane walls), 0.8 m below
    ///   the wall top, with battlements (Half-cover) along the top and ramps up from the ground
    ///
    /// Win condition: an attacker's unit leader ending a move on the objective marker (see <see cref="ObjectiveMarker"/>),
    /// besides wiping out the enemy. Walls are structure terrain (they block movement and sight); gates are wide enough
    /// for a tank. Menu: Eldoria > Build Ft Solstice, or -executeMethod Eldoria.EditorTools.FortSolsticeMapBuilder.Build
    /// </summary>
    public static class FortSolsticeMapBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/FortSolstice.unity";
        private const string MeshDir = Root + "/Meshes";

        /// <summary>The board: 100 m east-west, 130 m north-south (30 m longer than it is wide, for the attackers' approach).</summary>
        public const float Width = 100f, Depth = 130f, HalfWidth = 50f, HalfDepth = 65f;
        public const float GateWidth = 6f;

        // The fortress, in metres (x across, z north). It sits 10 m from the north edge.
        public const float OuterSouthZ = -15f, OuterHalfWidth = 42f;
        public const float LaneDividerX = 14f;
        public const float MiddleSouthZ = 9f, MiddleNorthZ = 55f, MiddleHalfWidth = 30f;
        public const float KeepSouthZ = 21f, KeepNorthZ = 45f, KeepHalfWidth = 12f;
        public static readonly Vector3 ObjectiveCentre = new Vector3(0f, 0f, 33f);
        public const float ObjectiveRadius = 2.5f;

        /// <summary>The three lanes' gates in the outer wall and the middle wall (x of each gate's centre).</summary>
        public static readonly float[] OuterGateX = { -28f, 0f, 28f };
        public static readonly float[] MiddleGateX = { -22f, 0f, 22f };

        private const float OuterHeight = 5f, OuterThickness = 1.2f;
        private const float MiddleHeight = 4f, MiddleThickness = 1f;
        private const float KeepHeight = 3.5f, KeepThickness = 1f;

        /// <summary>Wall walks: platform depth behind the wall, how far below the wall top they are, the battlements on top.</summary>
        public const float WalkDepth = 2.5f, WalkBelowTop = 0.8f, BattlementHeight = 0.5f;
        public const float OuterWalkHeight = OuterHeight - WalkBelowTop, MiddleWalkHeight = MiddleHeight - WalkBelowTop;
        private const float RampAngle = 30f, RampWidth = 2.4f;

        private static Material sand, wallStone, keepStone, rock, sandbagMat, crateMat, marker;

        [MenuItem("Eldoria/Build Ft Solstice")]
        public static void Build()
        {
            UrbanMapBuilder.PrepareShared();
            sand = UrbanMapBuilder.Mat("Desert_Sand", new Color(0.82f, 0.71f, 0.5f),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Materials/FloorGrid.png"));
            wallStone = UrbanMapBuilder.Mat("Fort_Wall", new Color(0.72f, 0.58f, 0.4f));
            keepStone = UrbanMapBuilder.Mat("Fort_Keep", new Color(0.62f, 0.5f, 0.36f));
            rock = UrbanMapBuilder.Mat("Desert_Rock", new Color(0.58f, 0.46f, 0.34f));
            sandbagMat = UrbanMapBuilder.Mat("Sandbag", new Color(0.62f, 0.56f, 0.4f));
            crateMat = UrbanMapBuilder.Mat("Wood", new Color(0.55f, 0.4f, 0.22f));
            marker = UrbanMapBuilder.Mat("Objective_Marker", new Color(0.95f, 0.78f, 0.2f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("FortSolsticeMap");
            BuildGround(map.transform);

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(map.transform, false);
            BuildFortress(terrain.transform);
            BuildCover(terrain.transform);
            BuildObjective(map.transform);

            map.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            map.AddComponent<BattlefieldNavigation>().SetBoardSize(new Vector2(Width, Depth));

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
            Debug.Log($"[Eldoria] Ft Solstice map built at {ScenePath}");
        }

        // ------------------------------------------------------------------ Ground

        private static void BuildGround(Transform parent)
        {
            const float step = 5f;
            int nx = Mathf.RoundToInt(Width / step) + 1, nz = Mathf.RoundToInt(Depth / step) + 1;
            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                float x = -HalfWidth + ix * step, z = -HalfDepth + iz * step;
                verts[iz * nx + ix] = new Vector3(x, 0f, z);
                uvs[iz * nx + ix] = new Vector2(x / 5f, z / 5f);
            }
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int iz = 0; iz < nz - 1; iz++)
            for (int ix = 0; ix < nx - 1; ix++)
            {
                int i0 = iz * nx + ix, i1 = i0 + 1, i2 = i0 + nx, i3 = i2 + 1;
                tris[k++] = i0; tris[k++] = i2; tris[k++] = i1;
                tris[k++] = i1; tris[k++] = i2; tris[k++] = i3;
            }
            string path = $"{MeshDir}/FortSolsticeGround.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "FortSolsticeGround";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
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
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        // ------------------------------------------------------------------ The fortress

        private static void BuildFortress(Transform parent)
        {
            var fort = new GameObject("Ft Solstice");
            fort.transform.SetParent(parent, false);
            var t = fort.transform;

            // Outer wall: the south face with the three lane gates, the sides of the lanes, and the short stretches that
            // close the lanes off against the middle wall.
            var outer = Group(t, "Outer Wall");
            WallX(outer, "South Face", OuterSouthZ, -OuterHalfWidth, OuterHalfWidth, OuterGateX, OuterHeight, OuterThickness, wallStone);
            WallZ(outer, "West Face", -OuterHalfWidth, OuterSouthZ, MiddleSouthZ, null, OuterHeight, OuterThickness, wallStone);
            WallZ(outer, "East Face", OuterHalfWidth, OuterSouthZ, MiddleSouthZ, null, OuterHeight, OuterThickness, wallStone);
            WallX(outer, "North-West Face", MiddleSouthZ, -OuterHalfWidth, -MiddleHalfWidth, null, OuterHeight, OuterThickness, wallStone);
            WallX(outer, "North-East Face", MiddleSouthZ, MiddleHalfWidth, OuterHalfWidth, null, OuterHeight, OuterThickness, wallStone);
            foreach (float x in new[] { -OuterHalfWidth, OuterHalfWidth })
                Tower(outer, new Vector3(x, 0f, OuterSouthZ), 3.2f, OuterHeight + 1.5f);
            foreach (float gx in OuterGateX)
            foreach (float side in new[] { -1f, 1f })
                Tower(outer, new Vector3(gx + side * (GateWidth / 2f + 1.3f), 0f, OuterSouthZ), 2.6f, OuterHeight + 1f);

            // The lanes: two walls running north from the outer wall to the middle wall.
            var lanes = Group(t, "Lane Walls");
            foreach (float x in new[] { -LaneDividerX, LaneDividerX })
                WallZ(lanes, x < 0f ? "West Lane Wall" : "East Lane Wall", x, OuterSouthZ, MiddleSouthZ, null, OuterHeight, MiddleThickness, wallStone);

            // Middle wall: a gate at the end of each lane; the other faces solid.
            var middle = Group(t, "Middle Wall");
            WallX(middle, "South Face", MiddleSouthZ, -MiddleHalfWidth, MiddleHalfWidth, MiddleGateX, MiddleHeight, MiddleThickness, wallStone);
            WallZ(middle, "West Face", -MiddleHalfWidth, MiddleSouthZ, MiddleNorthZ, null, MiddleHeight, MiddleThickness, wallStone);
            WallZ(middle, "East Face", MiddleHalfWidth, MiddleSouthZ, MiddleNorthZ, null, MiddleHeight, MiddleThickness, wallStone);
            WallX(middle, "North Face", MiddleNorthZ, -MiddleHalfWidth, MiddleHalfWidth, null, MiddleHeight, MiddleThickness, wallStone);
            foreach (float x in new[] { -MiddleHalfWidth, MiddleHalfWidth })
            foreach (float z in new[] { MiddleSouthZ, MiddleNorthZ })
                Tower(middle, new Vector3(x, 0f, z), 2.8f, MiddleHeight + 1.5f);

            // The keep: a gate in each face, the objective in its courtyard.
            var keep = Group(t, "Keep");
            WallX(keep, "South Face", KeepSouthZ, -KeepHalfWidth, KeepHalfWidth, new[] { 0f }, KeepHeight, KeepThickness, keepStone);
            WallX(keep, "North Face", KeepNorthZ, -KeepHalfWidth, KeepHalfWidth, new[] { 0f }, KeepHeight, KeepThickness, keepStone);
            WallZ(keep, "West Face", -KeepHalfWidth, KeepSouthZ, KeepNorthZ, new[] { ObjectiveCentre.z }, KeepHeight, KeepThickness, keepStone);
            WallZ(keep, "East Face", KeepHalfWidth, KeepSouthZ, KeepNorthZ, new[] { ObjectiveCentre.z }, KeepHeight, KeepThickness, keepStone);
            foreach (float x in new[] { -KeepHalfWidth, KeepHalfWidth })
            foreach (float z in new[] { KeepSouthZ, KeepNorthZ })
                Tower(keep, new Vector3(x, 0f, z), 2.4f, KeepHeight + 2f);

            BuildWallWalks(t);
        }

        // ------------------------------------------------------------------ Manning the walls

        /// <summary>
        /// Platforms behind the outer and middle walls, so soldiers can man the walls: 2.5 m deep, 0.8 m below the wall
        /// top (so a soldier's eyes are above it), with battlements along the wall top (Half-cover for the soldiers behind
        /// them) and ramps up from the ground. Each stretch of wall between gates has its own walk; the walks meet at the
        /// corners. The north face (the board edge behind it) and the lane walls have none.
        /// </summary>
        private static void BuildWallWalks(Transform fort)
        {
            var walks = Group(fort, "Wall Walks");
            // Outer wall: the south face, the west and east faces, and the short north stretches. The lane walls are taller
            // than the walk and split it, so each piece of the south walk has its own ramp (two in each lane).
            WalkX(walks, "Outer South Walk", OuterSouthZ, -OuterHalfWidth, OuterHalfWidth, OuterGateX, OuterHeight, OuterThickness, +1f,
                new[] { -36.5f, -18f, -10f, 10f, 18f, 36.5f });
            WalkZ(walks, "Outer West Walk", -OuterHalfWidth, OuterSouthZ, MiddleSouthZ, OuterHeight, OuterThickness, +1f, new[] { -1f });
            WalkZ(walks, "Outer East Walk", OuterHalfWidth, OuterSouthZ, MiddleSouthZ, OuterHeight, OuterThickness, -1f, new[] { -1f });
            WalkX(walks, "Outer North-West Walk", MiddleSouthZ, -OuterHalfWidth, -MiddleHalfWidth, null, OuterHeight, OuterThickness, -1f, null);
            WalkX(walks, "Outer North-East Walk", MiddleSouthZ, MiddleHalfWidth, OuterHalfWidth, null, OuterHeight, OuterThickness, -1f, null);
            // Middle wall: the south face (over the lanes' gates) and the west and east faces.
            WalkX(walks, "Middle South Walk", MiddleSouthZ, -MiddleHalfWidth, MiddleHalfWidth, MiddleGateX, MiddleHeight, MiddleThickness, +1f,
                new[] { -11f, 11f });
            WalkZ(walks, "Middle West Walk", -MiddleHalfWidth, MiddleSouthZ, MiddleNorthZ, MiddleHeight, MiddleThickness, +1f, new[] { 32f });
            WalkZ(walks, "Middle East Walk", MiddleHalfWidth, MiddleSouthZ, MiddleNorthZ, MiddleHeight, MiddleThickness, -1f, new[] { 32f });
        }

        /// <summary>The stretches of a wall from a to b between its gates.</summary>
        private static IEnumerable<(float from, float to)> Stretches(float a, float b, float[] gates)
        {
            float cursor = a;
            foreach (float g in gates ?? new float[0])
            {
                yield return (cursor, g - GateWidth / 2f);
                cursor = g + GateWidth / 2f;
            }
            yield return (cursor, b);
        }

        /// <summary>A walk behind an east-west wall at <paramref name="z"/>, on its <paramref name="inward"/> side (+1 north, -1 south).</summary>
        private static void WalkX(Transform parent, string name, float z, float x0, float x1, float[] gates, float wallHeight, float thickness,
            float inward, float[] rampsAtX)
        {
            float h = wallHeight - WalkBelowTop;
            float zc = z + inward * (thickness / 2f + WalkDepth / 2f);
            foreach (var (a, b) in Stretches(x0, x1, gates))
            {
                Block(parent, name, new Vector3((a + b) / 2f, h / 2f, zc), new Vector3(b - a, h, WalkDepth), wallStone);
                Battlements(parent, new Vector3((a + b) / 2f, wallHeight + BattlementHeight / 2f, z), new Vector3(b - a, BattlementHeight, thickness));
            }
            float edge = z + inward * (thickness / 2f + WalkDepth);
            foreach (float x in rampsAtX ?? new float[0]) Ramp(parent, new Vector3(x, h, edge), new Vector3(0f, 0f, inward));
        }

        /// <summary>A walk behind a north-south wall at <paramref name="x"/>, on its <paramref name="inward"/> side (+1 east, -1 west).</summary>
        private static void WalkZ(Transform parent, string name, float x, float z0, float z1, float wallHeight, float thickness,
            float inward, float[] rampsAtZ)
        {
            float h = wallHeight - WalkBelowTop;
            float xc = x + inward * (thickness / 2f + WalkDepth / 2f);
            Block(parent, name, new Vector3(xc, h / 2f, (z0 + z1) / 2f), new Vector3(WalkDepth, h, z1 - z0), wallStone);
            Battlements(parent, new Vector3(x, wallHeight + BattlementHeight / 2f, (z0 + z1) / 2f), new Vector3(thickness, BattlementHeight, z1 - z0));
            float edge = x + inward * (thickness / 2f + WalkDepth);
            foreach (float z in rampsAtZ ?? new float[0]) Ramp(parent, new Vector3(edge, h, z), new Vector3(inward, 0f, 0f));
        }

        /// <summary>Battlements along a wall top: an object (it doesn't block sight) giving Half-cover to the soldiers behind it.</summary>
        private static void Battlements(Transform parent, Vector3 centre, Vector3 size) =>
            UrbanMapBuilder.Prop(parent, "Battlements", PrimitiveType.Cube, wallStone, centre, size, 0f,
                cover: true, mantle: false, climb: false, "Half-cover");

        /// <summary>A ramp (structure slope) from a walk's inner edge at <paramref name="top"/> down to the ground, heading <paramref name="dir"/>.</summary>
        private static void Ramp(Transform parent, Vector3 top, Vector3 dir)
        {
            float run = top.y / Mathf.Tan(RampAngle * Mathf.Deg2Rad);
            Vector3 bottom = new Vector3(top.x, 0f, top.z) + dir * run;
            Vector3 along = top - bottom;
            const float thickness = 0.3f;
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.SetParent(parent, false);
            var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
            ramp.transform.localRotation = rot;
            ramp.transform.localPosition = (top + bottom) / 2f - rot * Vector3.up * (thickness / 2f);
            ramp.transform.localScale = new Vector3(RampWidth, thickness, along.magnitude + 0.1f);
            ramp.GetComponent<Renderer>().sharedMaterial = wallStone;
            ramp.layer = TerrainLayers.Structure;
            ramp.AddComponent<StructureTerrain>().Configure(StructureKind.Slope);
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>A wall running east-west at <paramref name="z"/> from x0 to x1, with gates centred on <paramref name="gates"/>.</summary>
        private static void WallX(Transform parent, string name, float z, float x0, float x1, float[] gates, float height, float thickness, Material mat)
        {
            float cursor = x0;
            foreach (float g in gates ?? new float[0])
            {
                Block(parent, name, new Vector3((cursor + g - GateWidth / 2f) / 2f, height / 2f, z), new Vector3(g - GateWidth / 2f - cursor, height, thickness), mat);
                cursor = g + GateWidth / 2f;
            }
            Block(parent, name, new Vector3((cursor + x1) / 2f, height / 2f, z), new Vector3(x1 - cursor, height, thickness), mat);
        }

        /// <summary>A wall running north-south at <paramref name="x"/> from z0 to z1, with gates centred on <paramref name="gates"/>.</summary>
        private static void WallZ(Transform parent, string name, float x, float z0, float z1, float[] gates, float height, float thickness, Material mat)
        {
            float cursor = z0;
            foreach (float g in gates ?? new float[0])
            {
                Block(parent, name, new Vector3(x, height / 2f, (cursor + g - GateWidth / 2f) / 2f), new Vector3(thickness, height, g - GateWidth / 2f - cursor), mat);
                cursor = g + GateWidth / 2f;
            }
            Block(parent, name, new Vector3(x, height / 2f, (cursor + z1) / 2f), new Vector3(thickness, height, z1 - cursor), mat);
        }

        /// <summary>A square tower (structure) centred on <paramref name="at"/>.</summary>
        private static void Tower(Transform parent, Vector3 at, float width, float height) =>
            Block(parent, "Tower", new Vector3(at.x, height / 2f, at.z), new Vector3(width, height, width), wallStone);

        private static void Block(Transform parent, string name, Vector3 centre, Vector3 size, Material mat)
        {
            if (size.x < 0.05f || size.z < 0.05f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.layer = TerrainLayers.Structure;
            go.AddComponent<StructureTerrain>().Configure(StructureKind.Building);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ Cover

        private static void BuildCover(Transform parent)
        {
            var cover = Group(parent, "Cover");

            // The approach: sandstone rocks and a few boulders on the open sand, in two bands (near the walls and halfway).
            var rocks = Group(cover, "Desert Rocks");
            foreach (var p in new[] { new Vector2(-36f, -23f), new Vector2(-20f, -20f), new Vector2(-8f, -24f), new Vector2(8f, -21f),
                                      new Vector2(19f, -24f), new Vector2(34f, -20f), new Vector2(-44f, -19f), new Vector2(44f, -23f),
                                      new Vector2(-40f, -44f), new Vector2(-26f, -40f), new Vector2(-12f, -46f), new Vector2(3f, -41f),
                                      new Vector2(15f, -45f), new Vector2(30f, -42f), new Vector2(45f, -47f) })
                UrbanMapBuilder.Prop(rocks, "Sandstone Rock", PrimitiveType.Cube, rock, new Vector3(p.x, 0.5f, p.y), new Vector3(2.2f, 1f, 1.4f),
                    p.x * 7f % 40f, cover: true, mantle: true, climb: true, "Half-cover");
            foreach (var p in new[] { new Vector2(-28f, -25f), new Vector2(0f, -21f), new Vector2(28f, -25f),
                                      new Vector2(-18f, -50f), new Vector2(21f, -51f) })
                UrbanMapBuilder.Prop(rocks, "Boulder", PrimitiveType.Cube, rock, new Vector3(p.x, 1.25f, p.y), new Vector3(3f, 2.5f, 2.4f),
                    15f, cover: true, mantle: false, climb: false, "Full-cover");

            // The lanes: a sandbag barricade across part of each lane, and crates.
            var lanes = Group(cover, "Lane Defences");
            foreach (float x in OuterGateX)
            {
                UrbanMapBuilder.Prop(lanes, "Sandbags", PrimitiveType.Cube, sandbagMat, new Vector3(x - 4f, 0.5f, -5f), new Vector3(6f, 1f, 1f),
                    0f, cover: true, mantle: true, climb: false, "Half-cover");
                UrbanMapBuilder.Prop(lanes, "Sandbags", PrimitiveType.Cube, sandbagMat, new Vector3(x + 5f, 0.5f, 2f), new Vector3(5f, 1f, 1f),
                    0f, cover: true, mantle: true, climb: false, "Half-cover");
                UrbanMapBuilder.Prop(lanes, "Crate", PrimitiveType.Cube, crateMat, new Vector3(x + 6f, 0.6f, -9f), Vector3.one * 1.2f,
                    20f, cover: true, mantle: true, climb: true, "Half-cover");
            }

            // Between the middle wall and the keep: sandbag posts and crates covering the keep's gates.
            var inner = Group(cover, "Inner Defences");
            foreach (var p in new[] { new Vector3(-6f, 0.5f, 17f), new Vector3(6f, 0.5f, 17f), new Vector3(-18f, 0.5f, 27f), new Vector3(18f, 0.5f, 39f),
                                      new Vector3(-18f, 0.5f, 39f), new Vector3(18f, 0.5f, 27f) })
                UrbanMapBuilder.Prop(inner, "Sandbags", PrimitiveType.Cube, sandbagMat, p, new Vector3(4f, 1f, 1f),
                    Mathf.Abs(p.x) > 10f ? 90f : 0f, cover: true, mantle: true, climb: false, "Half-cover");
            // (Nothing in the north courtyard: that's where the defenders deploy.)
            foreach (var p in new[] { new Vector3(-24f, 0.6f, 19f), new Vector3(24f, 0.6f, 19f) })
                UrbanMapBuilder.Prop(inner, "Crate", PrimitiveType.Cube, crateMat, p, Vector3.one * 1.2f, 30f,
                    cover: true, mantle: true, climb: true, "Half-cover");

            // The keep: low walls around the objective.
            var keep = Group(cover, "Keep Defences");
            foreach (var p in new[] { new Vector3(-5f, 0.5f, 28f), new Vector3(5f, 0.5f, 38f) })
                UrbanMapBuilder.Prop(keep, "Low Wall", PrimitiveType.Cube, keepStone, p, new Vector3(5f, 1f, 0.6f),
                    0f, cover: true, mantle: true, climb: false, "Half-cover");
        }

        // ------------------------------------------------------------------ The objective

        private static void BuildObjective(Transform parent)
        {
            var go = new GameObject("Objective: Heart of Ft Solstice");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ObjectiveCentre;
            go.AddComponent<ObjectiveMarker>().Configure("the heart of Ft Solstice", Side.Player, ObjectiveRadius);

            // Visual only: a gold disc on the ground and a banner pole (no colliders).
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Marker";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(go.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            disc.transform.localScale = new Vector3(ObjectiveRadius * 2f, 0.01f, ObjectiveRadius * 2f);
            var r = disc.GetComponent<Renderer>();
            r.sharedMaterial = marker;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Banner Pole";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0f, 2f, 0f);
            pole.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
            pole.GetComponent<Renderer>().sharedMaterial = marker;
        }

        // ------------------------------------------------------------------ Deployment, camera

        private static void BuildDeploymentZones(Transform parent)
        {
            // Attackers (south): the open sand along the south edge.
            Zone(parent, "Deployment (Player, attackers)", Side.Player, new Vector3(0f, 0f, -HalfDepth + 4f), new Vector2(90f, 8f),
                UrbanMapBuilder.Mat("Deploy_Player", new Color(0.33f, 0.38f, 0.47f)));
            // Defenders (north): their army's default slots are in the courtyard between the keep and the middle wall's north
            // face, but they may deploy anywhere in the fortress: inside the outer wall (the lanes) and inside the middle wall.
            float z0 = KeepNorthZ + KeepThickness / 2f + 0.5f, z1 = MiddleNorthZ - MiddleThickness / 2f - 0.5f;
            var defenders = Zone(parent, "Deployment (AI, defenders)", Side.AI, new Vector3(0f, 0f, (z0 + z1) / 2f),
                new Vector2((MiddleHalfWidth - MiddleThickness) * 2f - 2f, z1 - z0), null);
            defenders.SetKeepArmyInside(true);   // a big army's back rows would be beyond the north wall
            var mat = UrbanMapBuilder.Mat("Deploy_Fort", new Color(0.76f, 0.6f, 0.46f));   // a light tint: it covers the whole fortress
            foreach (var (centre, size) in DefenderAreas)
            {
                defenders.AddArea(centre, size);
                UrbanMapBuilder.Decor(defenders.transform, "Marking", centre - defenders.transform.position + new Vector3(0f, 0.01f, 0f), size, mat);
            }
        }

        /// <summary>The fortress, where the defenders may deploy: inside the outer wall, and inside the middle wall.</summary>
        public static readonly (Vector3 centre, Vector2 size)[] DefenderAreas =
        {
            (new Vector3(0f, 0f, (OuterSouthZ + MiddleSouthZ) / 2f), new Vector2(OuterHalfWidth * 2f, MiddleSouthZ - OuterSouthZ)),
            (new Vector3(0f, 0f, (MiddleSouthZ + MiddleNorthZ) / 2f), new Vector2(MiddleHalfWidth * 2f, MiddleNorthZ - MiddleSouthZ)),
        };

        private static DeploymentZone Zone(Transform parent, string name, Side side, Vector3 centre, Vector2 size, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            var zone = go.AddComponent<DeploymentZone>();
            zone.Configure(side, size);
            if (mat != null) UrbanMapBuilder.Decor(go.transform, "Marking", new Vector3(0f, 0.01f, 0f), size, mat);
            return zone;
        }

        private static Camera BuildLightingAndCamera()
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.94f, 0.82f);   // hard desert sun
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(58f, -25f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.7f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.48f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.18f);

            // Camera starts over the attackers, looking north at the fortress.
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, 0f, -45f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.7f, 0.55f);   // a hazy desert sky
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            var controller = rig.AddComponent<TacticalCameraController>();
            var so = new SerializedObject(controller);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.FindProperty("boundsMin").vector2Value = new Vector2(-HalfWidth, -HalfDepth);
            so.FindProperty("boundsMax").vector2Value = new Vector2(HalfWidth, HalfDepth);
            so.FindProperty("maxDistance").floatValue = 130f;
            so.ApplyModifiedPropertiesWithoutUndo();
            camGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            camGo.transform.localPosition = camGo.transform.localRotation * new Vector3(0f, 0f, -45f);
            return cam;
        }
    }
}
