using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// Silences the game while the EditMode tests run: the master volume is 0 for the whole run and restored afterwards.
    /// Sounds are still "played" (the CombatAudio counters still count them), just not heard.
    /// </summary>
    [SetUpFixture]
    public class MuteAudioDuringEditModeTests
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

    public class MutedEditModeTests
    {
        [Test]
        public void TheTestsAreSilent() => Assert.That(AudioListener.volume, Is.EqualTo(0f));
    }
}
