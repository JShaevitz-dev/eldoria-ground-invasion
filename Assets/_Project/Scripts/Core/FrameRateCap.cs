using UnityEngine;

namespace Eldoria.Core
{
    /// <summary>
    /// Caps the frame rate at about 60 fps so the GPU isn't driven flat out: vsync on, waiting for as many screen refreshes
    /// per frame as make about 60 fps (1 at 60 Hz, 2 at 120-144 Hz, 4 at 240 Hz), and a 60 fps target for when the driver
    /// ignores vsync. Without it the game renders as fast as it can (hundreds or
    /// thousands of frames a second on the menus), which shows up as large GPU spikes. Applied when the built game starts.
    /// </summary>
    public static class FrameRateCap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            // Not in the editor or in batch runs (the tests and the headless smoke test), which should run as fast as they can.
            if (Application.isEditor || Application.isBatchMode) return;
            // Vsync at about 60 fps: every frame on a 60 Hz screen, every 2nd on 120-144 Hz, every 4th on 240 Hz.
            double hz = Screen.currentResolution.refreshRateRatio.value;
            QualitySettings.vSyncCount = hz > 0 ? Mathf.Clamp(Mathf.RoundToInt((float)(hz / TargetFrameRate)), 1, 4) : 1;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
