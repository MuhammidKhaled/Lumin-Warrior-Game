using LuminWarrior.Effects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LuminWarrior
{
    public class Levels
    {
        private Canvas gameCanvas;
        private Random random;
        private GameManager gameManager;
        private BackgroundManager backgroundManager;
        private List<Enemy> enemies;
        private Obstacles obstacles;
        private DispatcherTimer messageTimer;
        private DispatcherTimer levelTransitionTimer;
        private TextBlock messageText;
        private HitEffects? hitEffects;
        private Player player;
        private PowerUps powerUps;
        private Health healthSystem;
        private UIManager uiManager;
        private bool gameStarted = false;

        public int CurrentLevel { get; private set; } = 1;
        public bool IsInLevelBreak => isInLevelBreak;

        private readonly int[] levelScoreThresholds = { 0, 100, 200, 300, 400, 500 };
        public int ScoreThreshold => levelScoreThresholds[Math.Min(CurrentLevel, levelScoreThresholds.Length - 1)];

        private bool levelCompleteMessageShown = false;
        private bool isInLevelBreak = false;

        private int enemySpawnLimit = 30;
        private int baseEnemySpeed = 2;
        private double rocketSpeed = 8;
        private bool enableObstacles = false;
        private bool enableSpinnerEnemies = false;

        public event Action<int>? OnLevelChanged;
        public event Action? OnVictory;

        public Levels(Canvas canvas, GameManager manager, Random rand, BackgroundManager bgManager,
        List<Enemy> enemyList, Obstacles obstaclesManager, Player player, PowerUps powerUps, Health health, UIManager ui, HitEffects? effects = null)
        {
            gameCanvas = canvas;
            gameManager = manager;
            random = rand;
            backgroundManager = bgManager;
            enemies = enemyList;
            obstacles = obstaclesManager;
            hitEffects = effects;
            this.player = player;
            this.powerUps = powerUps;
            this.healthSystem = health;
            this.uiManager = ui;

            messageText = new TextBlock
            {
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 5,
                    Opacity = 0.8,
                    BlurRadius = 10
                },
                Visibility = Visibility.Collapsed
            };

            Canvas.SetZIndex(messageText, 100);
            gameCanvas.Children.Add(messageText);

            messageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            messageTimer.Tick += (s, e) =>
            {
                messageText.Visibility = Visibility.Collapsed;
                messageTimer.Stop();
            };

            levelTransitionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            levelTransitionTimer.Tick += (s, e) =>
            {
                levelTransitionTimer.Stop();
                TransitionToNextLevel();
            };
        }

        public void Update(int currentScore)
        {
            if (isInLevelBreak)
            {
                obstacles?.SetEnabled(false);
                return;
            }

            obstacles?.SetEnabled(enableObstacles);

            if (currentScore >= ScoreThreshold && !levelCompleteMessageShown && CurrentLevel != 5 && CurrentLevel != 6)
            {
                levelCompleteMessageShown = true;
                StartLevelBreak();
            }
        }

        public void StartGame()
        {
            if (!gameStarted)
            {
                gameStarted = true;
                uiManager?.HideMenu();
                uiManager?.ShowOriginalUI(); //The panels of Score & levels stars at the top left

                EnablePlayer();
                ResetPlayerPosition();

                InitializeLevel(1);
            }
        }

        public void HandleMiniBossDefeated()
        {
            // This method is called from GameManager when miniBossDefeated becomes true
            if (CurrentLevel == 5)
            {
                StartLevelBreak();
            }
        }

        private void InitializeLevel(int level)
        {
            CurrentLevel = level;
            levelCompleteMessageShown = false;

            if (level > 1)
            {
                ResetPlayerHealth();
            }

            //Levels 
            switch (level)
            {
                case 1: // Tutorial
                    enemySpawnLimit = 30;
                    baseEnemySpeed = 2;
                    rocketSpeed = 8;
                    enableObstacles = false;
                    enableSpinnerEnemies = false;
                    break;

                case 2: // First Challenge
                    enemySpawnLimit = 25;
                    baseEnemySpeed = 3;
                    rocketSpeed = 9;
                    enableObstacles = false;
                    enableSpinnerEnemies = false;
                    break;

                case 3: // Dynamic Gameplay
                    enemySpawnLimit = 20;
                    baseEnemySpeed = 4;
                    rocketSpeed = 10;
                    enableObstacles = true;
                    enableSpinnerEnemies = false;
                    break;

                case 4: // Midpoint
                    enemySpawnLimit = 15;
                    baseEnemySpeed = 4;
                    rocketSpeed = 12;
                    enableObstacles = true;
                    enableSpinnerEnemies = true;
                    break;

                case 5: // Intensify
                    enemySpawnLimit = 10;
                    baseEnemySpeed = 5;
                    rocketSpeed = 14;
                    enableObstacles = true;
                    enableSpinnerEnemies = true;
                    break;

                case 6: // Final Battle
                    enemySpawnLimit = 5;
                    baseEnemySpeed = 5;
                    rocketSpeed = 14;
                    enableObstacles = true;
                    enableSpinnerEnemies = true;
                    break;
            }

            ApplyLevelSettings();

            ShowLevelTitle(level);

            if (level > 1)
            {
                EnablePlayer();
            }

            OnLevelChanged?.Invoke(CurrentLevel);
        }

        private void ApplyLevelSettings()
        {
            // Updating the gameManager with level-specific settings
            if (gameManager != null)
            {
                gameManager.RocketSpeed = rocketSpeed;
                gameManager.UpdateEnemySpawnLimit(enemySpawnLimit);
                gameManager.UpdateBaseEnemySpeed(baseEnemySpeed);
            }

            if (!isInLevelBreak)
            {
                obstacles?.SetEnabled(enableObstacles);
            }
        }

        private void ShowLevelTitle(int level)
        {
            messageText.Text = $"Level {level}";
            messageText.Width = gameCanvas.ActualWidth * 0.8;

            double leftPosition = (gameCanvas.ActualWidth - messageText.Width) / 2;
            double topPosition = (gameCanvas.ActualHeight - 50) / 2;

            Canvas.SetLeft(messageText, leftPosition);
            Canvas.SetTop(messageText, topPosition);

            Canvas.SetZIndex(messageText, 100);
            messageText.Visibility = Visibility.Visible;

            messageTimer.Stop();
            messageTimer.Interval = TimeSpan.FromSeconds(3);
            messageTimer.Start();
        }

        private void ShowLevelCompleteMessage()
        {
            ShowMessage("Mission Completed", 3);
        }

        public void ShowPlayerDestroyedMessage(int lives)
        {
            if (lives > 0)
            {
                ShowMessage("Try Again!", 3);
            }
            else
            {
                ShowMessage("Game Over!", 3);
            }
        }

        private void ShowMessage(string message, double seconds)
        {
            messageTimer.Stop();

            messageText.Text = message;
            messageText.Width = gameCanvas.ActualWidth * 0.8;

            double leftPosition = (gameCanvas.ActualWidth - messageText.Width) / 2;
            double topPosition = (gameCanvas.ActualHeight - 50) / 2;

            Canvas.SetLeft(messageText, leftPosition);
            Canvas.SetTop(messageText, topPosition);

            Canvas.SetZIndex(messageText, 100);
            messageText.Visibility = Visibility.Visible;

            messageTimer.Interval = TimeSpan.FromSeconds(seconds);
            messageTimer.Start();
        }

        private void StartLevelBreak()
        {
            isInLevelBreak = true;

            DisablePlayer();

            ClearEnemies();
            ClearRocks();
            ClearEnemyRockets();
            ClearPlayerRockets();
            ClearPowerUps();

            ShowLevelCompleteMessage();

            levelTransitionTimer.Interval = TimeSpan.FromSeconds(6);
            levelTransitionTimer.Start();
        }

        private void TransitionToNextLevel()
        {
            int nextLevel = CurrentLevel + 1;
            if (nextLevel <= 6)
            {
                ResetPlayerHealth();
                ResetPlayerPosition();
                EnablePlayer();

                isInLevelBreak = false;

                InitializeLevel(nextLevel);
            }
            else
            {
                isInLevelBreak = false;
            }
        }

        private void DisablePlayer()
        {
            if (gameManager != null)
            {
                Player player = gameManager.GetPlayer();
                player?.Disable();
            }
        }

        private void EnablePlayer()
        {
            if (gameManager != null)
            {
                Player player = gameManager.GetPlayer();
                player?.Enable();
            }
        }

        private void ResetPlayerPosition()
        {
            if (gameManager != null)
            {
                Player player = gameManager.GetPlayer();
                player?.Reset();
            }
        }

        private void ClearEnemies()
        {
            foreach (var enemy in enemies.ToList())
            {
                enemy.DisableExplosion();
                enemy.Remove();
            }
            enemies.Clear();
        }

        private void ClearRocks()
        {
            obstacles?.RemoveAllRocks();
        }

        private void ClearEnemyRockets()
        {
            var enemyRocketTags = new HashSet<string> {
                "enemyRocket", "enemyRocketRight", "enemyRocketDown", "enemyRocketLeft", "enemyRocketUp",
                 "enemyRocketUpright", "enemyRocketDownright", "enemyRocketDownleft", "enemyRocketUpleft"
            };

            var elementsToRemove = gameCanvas.Children.OfType<Rectangle>()
                .Where(element => element.Tag != null && enemyRocketTags.Contains(element.Tag.ToString()!))
                .ToList();

            foreach (var element in elementsToRemove)
                gameCanvas.Children.Remove(element);
        }

        private void ClearPlayerRockets()
        {
            var elementsToRemove = gameCanvas.Children.OfType<Rectangle>()
                .Where(element => element.Tag?.ToString() == "rocket")
                .ToList();

            foreach (var element in elementsToRemove)
                gameCanvas.Children.Remove(element);
        }

        private void ClearPowerUps()
        {
            powerUps?.Stop();

            if (powerUps != null)
            {
                DispatcherTimer restartTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };

                restartTimer.Tick += (s, e) =>
                {
                    powerUps.Start();
                    restartTimer.Stop();
                };

                restartTimer.Start();
            }
        }

        private void ResetPlayerHealth()
        {
            healthSystem?.Reset();
        }

        public void TriggerVictory()
        {
            isInLevelBreak = true; //Just to help in clearing each element on the screen except which
                                   //are related the victory state
            DisablePlayer();

            ClearRocks();

            powerUps?.Stop();

            healthSystem?.Remove();

            DispatcherTimer victoryDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            victoryDelayTimer.Tick += (s, e) =>
            {
                victoryDelayTimer.Stop();

                backgroundManager.SetStarSpeed(3.0);

                SoundEffectsManager.PlayVictorySound();

                EnlargeVictoryMessage();

                OnVictory?.Invoke();
            };

            victoryDelayTimer.Start();
        }

        private void EnlargeVictoryMessage()
        {
            messageTimer.Stop();

            messageText.Text = "Victory!";
            messageText.FontSize = 120;
            messageText.Width = gameCanvas.ActualWidth * 0.9;

            double leftPosition = (gameCanvas.ActualWidth - messageText.Width) / 2;
            double topPosition = (gameCanvas.ActualHeight - 150) / 2;

            Canvas.SetLeft(messageText, leftPosition);
            Canvas.SetTop(messageText, topPosition);

            Canvas.SetZIndex(messageText, 100);
            messageText.Visibility = Visibility.Visible;

            messageTimer.Interval = TimeSpan.FromSeconds(1000);
            messageTimer.Start();
        }

        public bool ShouldSpawnSpinnerEnemy()
        {
            return enableSpinnerEnemies && random.Next(100) < 30;
        }

        public void Reset()
        {
            levelCompleteMessageShown = false;
            isInLevelBreak = false;

            ResetPlayerHealth();

            messageTimer.Stop();
            levelTransitionTimer.Stop();

            ClearPowerUps();
        }

        public void Cleanup()
        {
            if (messageText != null && gameCanvas.Children.Contains(messageText))
            {
                gameCanvas.Children.Remove(messageText);
            }

            messageTimer.Stop();
            levelTransitionTimer.Stop();
        }
    }
}