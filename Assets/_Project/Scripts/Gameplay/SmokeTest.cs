using System.Collections;
using System.Linq;
using Eldoria.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eldoria.Gameplay
{
    /// <summary>
    /// A quick end-to-end check of a built game, off unless the program is started with <c>-smokeTest</c>:
    /// from the main menu it chooses the Open Season, builds an ISF army and starts a game, selects a unit and previews a
    /// move, lets the AI play three turns, checks the runtime shaders and sounds, then quits with exit code 0 (passed) or 1
    /// (an error was logged or a check failed).
    /// Usage: GroundInvasion.exe -batchmode -nographics -smokeTest -logFile smoke.log
    /// With a window (no -batchmode) and <c>-smokeScreenshots folder</c>, it also saves screenshots of the main menu and
    /// the game (HUD and labels included) into that folder.
    /// </summary>
    public class SmokeTest : MonoBehaviour
    {
        private int problems;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("-smokeTest")) return;
            var go = new GameObject("SmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
        }

        /// <summary>Saves a screenshot (with the UI) if -smokeScreenshots was given and there is a window to capture.</summary>
        private IEnumerator Screenshot(string file)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-smokeScreenshots");
            if (i < 0 || i + 1 >= args.Length || Application.isBatchMode) yield break;
            for (int f = 0; f < 5; f++) yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(args[i + 1], file), tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log($"[Eldoria] Screenshot saved: {file}");
        }

        private void Check(bool ok, string what)
        {
            if (ok) Debug.Log($"[Eldoria] Smoke test ok: {what}");
            else
            {
                problems++;
                Debug.LogWarning($"[Eldoria] Smoke test FAILED: {what}");
            }
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += (condition, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) problems++;
            };

            yield return null;
            Check(SceneManager.GetActiveScene().name == MainMenu.MenuScene, "the game opens on the main menu");
            yield return Screenshot("menu.png");
            Check(FindFirstObjectByType<MainMenu>() != null, "the main menu is there");

            var menu = FindFirstObjectByType<MainMenu>();
            menu.OpenMapChoice();
            yield return Screenshot("maps.png");
            Check(Battlefields.All.Count == 5, "the map choice offers Boxed in, Open Season, the Siege of Ft Solstice, Polar Nonsense and Leisure Cruise");
            menu.ChooseMap(Battlefields.EnglishCountryside);
            yield return Screenshot("factions.png");
            // The three factions are there; the ISF has a Commander, so it can field an army.
            Check(menu.Builder != null, "the army builder opens");
            Check(menu.Builder != null && menu.Builder.Factions.Count == 3, $"the army builder has the three factions ({menu.Builder?.Factions.Count})");
            Check(menu.Builder != null && menu.Builder.CanFieldArmies, "a faction can field an army");
            var isf = menu.Builder?.Factions.FirstOrDefault();
            menu.Builder?.ChooseFaction(isf);
            yield return Screenshot("army.png");
            Eldoria.Units.UnitTemplate Unit(string name) => menu.Builder?.Available.FirstOrDefault(u => u.displayName == name);
            Check(menu.Builder != null && menu.Builder.Available.Count == 11, $"the ISF's units are listed ({menu.Builder?.Available.Count})");
            menu.Builder?.Clear();
            foreach (var name in new[] { "Field Commander", "ISF Rifle Squad", "Olympian Mech", "Mortar Team", "Sagittarius MBT", "ISF Ranger" })
                Check(menu.Builder?.Add(Unit(name)) == null, $"{name} is added");
            Check(menu.Builder != null && menu.Builder.Confirm(), "the army is valid");
            var army = menu.Builder != null ? menu.Builder.Army.ToList() : new System.Collections.Generic.List<Eldoria.Units.UnitTemplate>();
            Check(MainMenu.StartBattle(isf, army).Count == 0, "the battle starts");
            for (int i = 0; i < 10 && SceneManager.GetActiveScene().name != Battlefields.EnglishCountryside.Scene; i++) yield return null;
            yield return null;
            Check(SceneManager.GetActiveScene().name == Battlefields.EnglishCountryside.Scene, "Play starts a game on the chosen map");

            var controller = FindFirstObjectByType<MovementController>();
            Check(controller != null && controller.PlayerUnits.Count == army.Count, "the player's army is deployed");
            var catalog = Eldoria.Units.UnitCatalog.Load();
            Check(ArmySelection.HasArmies && ArmyBuilderScreen.FactionCanFieldArmy(catalog, ArmySelection.AIFaction),
                $"the AI fields a faction that can ({ArmySelection.AIFaction?.displayName})");
            Check(FindObjectsByType<Eldoria.Units.Unit>(FindObjectsSortMode.None).Count(u => u.Side == Eldoria.Units.Side.AI) == ArmySelection.AIArmy.Count
                  && ArmySelection.AIArmy.Count > 0, $"the AI's army is deployed ({ArmySelection.AIArmy?.Count} units)");
            if (controller != null)
            {
                Check(controller.IsDeploying, "the battle starts with deployment");
                Check(FindObjectsByType<Eldoria.Units.Unit>(FindObjectsSortMode.None).Where(u => u.Side == Eldoria.Units.Side.AI).All(u => u.Hidden),
                    "the enemy is hidden during deployment");
                var first = controller.PlayerUnits[0];
                var zone = controller.DeploymentArea;
                string why = "no zone";
                Check(zone != null && controller.DeployUnitAt(first, new Vector3(first.Leader.Position.x, 0f, zone.transform.position.z - 2.5f), out why),
                    $"a unit can be placed in the deployment zone ({why})");
                controller.ReadyDeployment();
                Check(!controller.IsDeploying && Turns.Current == 1, "Ready starts the first turn");
                Check(FindObjectsByType<Eldoria.Units.Unit>(FindObjectsSortMode.None).All(u => !u.Hidden), "the enemy is revealed");
                var unit = controller.PlayerUnits[0];
                Check(controller.SelectUnit(unit), "a unit can be selected");
                controller.PreviewAt(unit.Leader.Position + Vector3.forward * 8f);
                yield return Screenshot("game.png");
                for (int turn = 0; turn < 3 && !Victory.IsOver; turn++) controller.EndTurn(instant: true);
                Check(Turns.Current >= 2, $"the AI played its turns (now turn {Turns.Current})");
            }

            KeywordSparkles.RefreshNow();
            CloseRangeMarkers.RefreshNow();
            foreach (var shader in new[] { "Sprites/Default", "Universal Render Pipeline/Unlit" })
                Check(Shader.Find(shader) != null, $"shader '{shader}' is in the build");
            Check(Resources.Load<AudioClip>(CombatAudio.VolleySoundPath) != null, "the gunshot sound is in the build");

            yield return null;
            Debug.Log(problems == 0 ? "[Eldoria] Smoke test PASSED" : $"[Eldoria] Smoke test found {problems} problem(s)");
            Application.Quit(problems == 0 ? 0 : 1);
        }
    }
}
