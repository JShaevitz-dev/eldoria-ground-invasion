using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>In battle: using a Medkit on a soldier at full health plays the error sound and doesn't spend the action.</summary>
    public class MedkitPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            TestArmies.LoadScene("CubeRoom");
            yield return null;
            yield return null;
        }

        [Test]
        public void AMedkitOnAFullHealthSoldierPlaysAnErrorAndKeepsTheAction()
        {
#if UNITY_EDITOR
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/Medkit.asset");
#else
            Equipment item = null;
#endif
            Assert.That(item, Is.Not.Null);
            var medkit = item.abilities.Single();
            var controller = Object.FindFirstObjectByType<MovementController>();
            var squad = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).First(u => u.Side == Side.Player && u.DisplayName == "Infantry Squad");
            var medic = squad.Leader;
            var loadout = new SoldierLoadout();
            loadout.Equipment.Add(item);
            medic.ApplyLoadout(loadout);
            var patient = squad.LivingSoldiers.Where(s => s != medic).OrderBy(s => Vector3.Distance(s.transform.position, medic.transform.position)).First();
            Assert.That(Vector3.Distance(patient.transform.position, medic.transform.position), Is.LessThan(5f), "a squadmate within reach");

            Assert.That(controller.SelectUnit(squad), Is.True);
            controller.BeginAbility(medic, medkit);   // the HUD button
            int errorsBefore = CombatAudio.ErrorSoundsPlayed;
            var refused = controller.UseAbility(medkit, medic, patient);
            Assert.That(refused.Used, Is.False);
            Assert.That(refused.Reason, Does.Contain("full health"));
            Assert.That(CombatAudio.ErrorSoundsPlayed, Is.EqualTo(errorsBefore + 1), "error sound");
            Assert.That(controller.HasTakenAction, Is.False, "the action isn't wasted");
            Assert.That(controller.TargetingAbility, Is.EqualTo(medkit), "still choosing a target");

            patient.TakeDamage(3);
            Assert.That(controller.UseAbility(medkit, medic, patient).Used, Is.True);
            Assert.That(patient.Health, Is.EqualTo(patient.MaxHealth), "3 lost, up to 5 restored");
            Assert.That(controller.HasTakenAction, Is.True);
            Assert.That(CombatAudio.ErrorSoundsPlayed, Is.EqualTo(errorsBefore + 1), "no error for a wounded soldier");
        }
    }
}
