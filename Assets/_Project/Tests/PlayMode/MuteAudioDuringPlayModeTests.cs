using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>
    /// Silences the game while the PlayMode tests run: the master volume is 0 for the whole run and restored afterwards.
    /// Sounds are still "played" (the CombatAudio counters still count them), just not heard.
    /// </summary>
    [SetUpFixture]
    public class MuteAudioDuringPlayModeTests
    {
        private float volume;

        [OneTimeSetUp]
        public void Mute()
        {
            volume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [OneTimeTearDown]
        public void Restore() => AudioListener.volume = volume;
    }

    public class MutedPlayModeTests
    {
        [UnityTest]
        public IEnumerator TheTestsStaySilentAcrossSceneLoads()
        {
            Assert.That(AudioListener.volume, Is.EqualTo(0f));
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;
            Assert.That(AudioListener.volume, Is.EqualTo(0f), "loading a scene doesn't bring the sound back");
        }
    }
}
