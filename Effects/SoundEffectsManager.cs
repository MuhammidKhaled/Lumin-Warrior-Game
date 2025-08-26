using System.Windows.Media;

namespace LuminWarrior.Effects
{
    public static class SoundEffectsManager
    {
        private static readonly List<MediaPlayer> activePlayers = new List<MediaPlayer>();
        private static readonly int maxConcurrentPlayers = 3;
        private static readonly object mediaPlayerLock = new object();
        private static bool isSilentMode = false;

        private static MediaPlayer? UIMusicPlayer;
        private static System.Windows.Threading.DispatcherTimer? UIMusicDelayTimer;
        
        private static void CleanupCompletedPlayers()
        {
            for (int i = activePlayers.Count - 1; i >= 0; i--)
            {
                if (activePlayers[i].Position >= activePlayers[i].NaturalDuration.TimeSpan)
                {
                    activePlayers[i].Close();
                    activePlayers.RemoveAt(i);
                }
            }

            if (activePlayers.Count >= maxConcurrentPlayers)
            {
                activePlayers[0].Close();
                activePlayers.RemoveAt(0);
            }
        }

        private static void PlaySound(string fileName, double volume, bool checkSilentMode = false)
        {
            if (checkSilentMode && isSilentMode) return;

            try
            {
                lock (mediaPlayerLock)
                {
                    CleanupCompletedPlayers();
                }

                MediaPlayer player = new MediaPlayer();
                player.Volume = volume;
                player.Balance = 0;

                string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"assets/sounds/{fileName}");
                Uri uri = new Uri(soundPath, UriKind.Absolute);
                player.Open(uri);

                player.MediaEnded += (s, e) =>
                {
                    if (s is MediaPlayer mp) 
                    {
                        mp.Close();
                        lock (mediaPlayerLock)
                        {
                            activePlayers.Remove(mp);
                        }
                    }
                };

                lock (mediaPlayerLock)
                {
                    activePlayers.Add(player);
                }

                player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoundManager] Error playing sound {fileName}: {ex.Message}");
            }
        }

        public static void PlayFiringSound() => PlaySound("lasershoot.wav", 0.1);
        public static void PlayDestructionSound() => PlaySound("explosionsound.wav", 0.5, true);
        public static void PlayPowerUpSound() => PlaySound("powerupsound.wav", 0.6);
        public static void PlayRockHitSound() => PlaySound("hitrocks.wav", 0.3);
        public static void PlayStationShakingSound() => PlaySound("shakingsound.wav", 0.2);
        public static void PlayVictorySound() => PlaySound("victorysound.wav", 0.7);
        public static void PlayButtonClickSound() => PlaySound("clicksound.wav", 0.4);

        public static void PlayUIMusic()
        {
            try
            {
                if (UIMusicPlayer != null)
                {
                    UIMusicPlayer.Stop();
                    UIMusicPlayer.Close();
                }

                if (UIMusicDelayTimer != null)
                {
                    UIMusicDelayTimer.Stop();
                    UIMusicDelayTimer = null;
                }

                UIMusicDelayTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.5)
                };

                UIMusicDelayTimer.Tick += (s, e) =>
                {
                    UIMusicDelayTimer.Stop();
                    UIMusicDelayTimer = null;

                    UIMusicPlayer = new MediaPlayer
                    {
                        Volume = 0.3,
                        Balance = 0
                    };

                    string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets/sounds/uisound.wav");
                    UIMusicPlayer.Open(new Uri(soundPath, UriKind.Absolute));

                    UIMusicPlayer.MediaEnded += (sender, args) =>
                    {
                        if (UIMusicPlayer != null)
                        {
                            UIMusicPlayer.Position = TimeSpan.Zero;
                            UIMusicPlayer.Play();
                        }
                    };

                    UIMusicPlayer.Play();
                };

                UIMusicDelayTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoundManager] Error playing UI music: {ex.Message}");
            }
        }

        public static void StopUIMusic()
        {
            try
            {
                if (UIMusicDelayTimer != null)
                {
                    UIMusicDelayTimer.Stop();
                    UIMusicDelayTimer = null;
                }

                if (UIMusicPlayer != null)
                {
                    UIMusicPlayer.Stop();
                    UIMusicPlayer.Close();
                    UIMusicPlayer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoundManager] Error stopping UI music: {ex.Message}");
            }
        }

        public static void StopAllSounds()
        {
            try
            {
                lock (mediaPlayerLock)
                {
                    foreach (var player in activePlayers)
                    {
                        player.Stop();
                        player.Close();
                    }
                    activePlayers.Clear();
                }

                StopUIMusic();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoundManager] Error stopping all sounds: {ex.Message}");
            }
        }

        public static void SetSilentMode(bool silent)
        {
            isSilentMode = silent;
        }
    }
}