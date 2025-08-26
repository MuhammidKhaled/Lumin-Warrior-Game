using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.Windows.Input;
using LuminWarrior.Effects;


namespace LuminWarrior
{
    public class GameManager
    {
        private Canvas gameCanvas;
        private DispatcherTimer gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        private Random random;

        private BackgroundManager backgroundManager;
        private Player player;
        private List<Rectangle> playerRockets = new List<Rectangle>();
        private List<Enemy> enemies = new List<Enemy>();
        private List<Rectangle> enemyRockets = new List<Rectangle>();
        private Health healthSystem;
        private Obstacles obstacles;
        private MiniBoss miniBoss = null!;
        private bool miniBossActive = false;
        private bool miniBossDefeated = false;
        private FinalBoss finalBoss = null!;
        private bool finalBossActive = false;
        private bool finalBossDefeated = false;
        private SpaceStation spaceStation = null!;
        private bool spaceStationActive = false;
        private bool spaceStationDefeated = false;
        private HitEffects hitEffects;
        private PowerUps powerUps = null!;
        private Levels levelSystem = null!;
        private UIManager uiManager;
        private PausePopup pausePopup = null!;

        private bool isMouseControlEnabled = true;
        private bool mouseLeft = false, mouseRight = false, mouseUp = false, mouseDown = false;
        private bool keyLeft = false, keyRight = false, keyUp = false, keyDown = false;
        private int inputValidationCounter = 0;

        public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.RegisterAttached("Direction", typeof(Vector), typeof(GameManager));

        public bool IsGameRunning { get; private set; }
        public bool IsGamePaused { get; private set; }
        public int Score { get; private set; }
        public int Level { get; private set; }
        public int Lives { get; private set; } = 3;
        public bool IsVictorious { get; private set; } = false;
        public bool FreeFly { get; private set; } = false;


        private int enemySpawnCounter = 30;
        private int enemySpawnLimit = 30;
        private int baseEnemySpeed = 2;

        public double RocketSpeed { get; set; } = 8;

        public event Action<int>? OnScoreChanged;
        public event Action<int>? OnLevelChanged;
        public event Action<int>? OnLivesChanged;
        public event Action? OnGameOver;
        public event Action? OnVictory;

        public BackgroundManager GetBackgroundManager() => backgroundManager;
        public List<Enemy> GetEnemies() => enemies;
        public Obstacles GetObstacles() => obstacles;
        public PowerUps GetPowerUps() => powerUps;
        public Health GetHealth() => healthSystem;
        public HitEffects GetHitEffects() => hitEffects;
        public Player GetPlayer() => player;

        public void UpdateEnemySpawnLimit(int limit) => enemySpawnLimit = limit;
        public void UpdateBaseEnemySpeed(int speed) => baseEnemySpeed = speed;

        public GameManager(Canvas canvas, UIManager ui)
        {
            gameCanvas = canvas;
            uiManager = ui;
            random = new Random();
            backgroundManager = new BackgroundManager(gameCanvas);
            player = new Player(gameCanvas);
            obstacles = new Obstacles(gameCanvas);
            healthSystem = new Health(gameCanvas);
            hitEffects = new HitEffects(gameCanvas);
            InitializeGame();
        }

        private void InitializeGame()
        {
            healthSystem.OnHealthDepleted += HandleGameOver;
            powerUps = new PowerUps(gameCanvas, random, this, player, healthSystem);
            player.SetPowerUps(powerUps); 
            
            Score = 0;
            Level = 1;
            Lives = 3;

            OnLivesChanged?.Invoke(Lives);
            IsGameRunning = false;
            isMouseControlEnabled = false;

            gameTimer.Tick += GameLoop;

            levelSystem = new Levels(gameCanvas, this, random, backgroundManager, enemies, obstacles, player, powerUps, healthSystem,uiManager);
            levelSystem.OnLevelChanged += (level) => OnLevelChanged?.Invoke(level);
            levelSystem.OnVictory += () => OnVictory?.Invoke();

            pausePopup = new PausePopup(gameCanvas);
            pausePopup.OnResume += ResumeGame;
            pausePopup.OnMainMenu += ReturnToMainMenu;
        }

        public void StartGame()
        {
            IsGameRunning = true;

            if(!FreeFly)
            {
                uiManager?.ShowOriginalUI();
                healthSystem?.InitializeHealthBar();
                healthSystem?.Reset();
                powerUps.Start();
            }

            gameTimer.Start();
        }

        public void PauseGame()
        {
            IsGameRunning = false;
            IsGamePaused = true;
            gameTimer.Stop();
            powerUps.Stop();
        }

        public void ResumeGame()
        {
            IsGameRunning = true;
            IsGamePaused = false;
            gameTimer.Start();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!IsGameRunning) return;

            inputValidationCounter++;
            if (inputValidationCounter >= 10)
            {
                ValidateInputState();
                inputValidationCounter = 0;
            }

            backgroundManager.Update();
            UpdatePlayer();
            UpdateEnemies();
            obstacles.Update();
            powerUps.Update();
            UpdateRockets();
            CheckCollisions();
            UpdateDifficulty();
        }

        private void UpdatePlayer()
        {
            player.Update();
            if (player.IsFiring && player.FiringTimer <= 0)
            {
                var newRockets = player.FireRocket();
                foreach (var rocket in newRockets)
                {
                    playerRockets.Add(rocket);
                    gameCanvas.Children.Insert(0, rocket);
                }
            }
        }

        private void UpdateEnemies()
        {
            if (FreeFly) return;

            //Final boss
            if (finalBossActive)
            {
                finalBoss.Update();
                var rockets = finalBoss.FireRocket();
                if (rockets != null)
                {
                    foreach (var rocket in rockets)
                    {
                        enemyRockets.Add(rocket);
                        gameCanvas.Children.Add(rocket);
                    }
                }

                if (!finalBoss.IsActive)
                {
                    finalBossActive = false;
                    finalBossDefeated = true;
                    Score += 50;
                    OnScoreChanged?.Invoke(Score);
                    CheckVictoryCondition();
                }
            }

            //Space Station
            if (spaceStationActive)
            {
                spaceStation.Update();

                var stationRockets = spaceStation.FireWeapons();
                if (stationRockets != null)
                {
                    foreach (var rocket in stationRockets)
                    {
                        enemyRockets.Add(rocket);
                        gameCanvas.Children.Insert(0, rocket);
                    }
                }

                if (!spaceStation.IsActive)
                {
                    spaceStationActive = false;
                    spaceStationDefeated = true;
                    Score += 100;
                    OnScoreChanged?.Invoke(Score);
                    CheckVictoryCondition();
                }
            }

            if (finalBossActive || finalBossDefeated)
            {
                return;
            }

            //Mini Boss
            if (miniBossActive)
            {
                miniBoss.Update();

                var rockets = miniBoss.FireRocket();
                if (rockets != null)
                {
                    foreach (var rocket in rockets)
                    {
                        enemyRockets.Add(rocket);
                        gameCanvas.Children.Add(rocket);
                    }
                }

                if (!miniBoss.IsActive)
                {
                    miniBossActive = false;
                    miniBossDefeated = true;
                    Score += 25;
                    OnScoreChanged?.Invoke(Score);
                }

                return;
            }

            //Regular enemies
            enemySpawnCounter--;

            if (enemySpawnCounter < 0)
            {
                if (enemies.Count <= 3)
                {
                    SpawnEnemy();
                }
                enemySpawnCounter = enemySpawnLimit;
            }

            foreach (var enemy in enemies.ToList())
            {
                enemy.Update();

                var rockets = enemy.FireRocket();
                if (rockets != null)
                {
                    foreach (var rocket in rockets)
                    {
                        enemyRockets.Add(rocket);
                        gameCanvas.Children.Add(rocket);
                    }
                }
            }

            enemies.RemoveAll(enemy => !enemy.IsActive);
        }

        private void SpawnEnemy()
        {
            if (levelSystem?.IsInLevelBreak == true)
            {
                return;
            }

            bool isSpinner = levelSystem?.ShouldSpawnSpinnerEnemy() ?? false;
            var enemyType = isSpinner ? Enemy.EnemyType.Spinner : Enemy.EnemyType.Standard;

            var enemy = new Enemy(gameCanvas, random, enemyType)
            {
                Speed = baseEnemySpeed + (Level - 1),
                FireCooldown = isSpinner ? 80 : 100
            };
            enemies.Add(enemy);
        }

        private void UpdateRockets()
        {
            for (int i = playerRockets.Count - 1; i >= 0; i--)
            {
                Rectangle rocket = playerRockets[i];
                double speed = 8 * player.RocketSpeedFactor;
                Vector direction = (Vector)rocket.GetValue(PowerUps.DirectionProperty);

                Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) + (direction.X * speed));
                Canvas.SetTop(rocket, Canvas.GetTop(rocket) - (-(direction.Y) * speed));

                if (Canvas.GetTop(rocket) < -50 ||
                    Canvas.GetTop(rocket) > gameCanvas.ActualHeight + 50 ||
                    Canvas.GetLeft(rocket) < -50 ||
                    Canvas.GetLeft(rocket) > gameCanvas.ActualWidth + 50)
                {
                    gameCanvas.Children.Remove(rocket);
                    playerRockets.RemoveAt(i);
                }
            }

            enemyRockets.RemoveAll(rocket =>
            {
                Enemy.MoveRocket(rocket, RocketSpeed);
                if (Enemy.IsRocketOffscreen(rocket, gameCanvas.ActualWidth, gameCanvas.ActualHeight))
                {
                    gameCanvas.Children.Remove(rocket);
                    return true;
                }
                return false;
            });
        }

        private void CheckCollisions()
        {
            foreach (var rocket in playerRockets.ToList())
            {
                Rect rocketHitBox = new Rect(Canvas.GetLeft(rocket), Canvas.GetTop(rocket),
                                          rocket.Width, rocket.Height);

                bool rocketHitRock = false;
                foreach (var rock in obstacles.GetRocks().ToList())
                {
                    Rect rockHitBox = new Rect(
                        Canvas.GetLeft(rock),
                        Canvas.GetTop(rock),
                        rock.Width,
                        rock.Height
                    );

                    if (rocketHitBox.IntersectsWith(rockHitBox))
                    {
                        gameCanvas.Children.Remove(rocket);
                        playerRockets.Remove(rocket);
                        gameCanvas.Children.Remove(rock);
                        obstacles.GetRocks().Remove(rock);
                        Effects.SoundEffectsManager.PlayRockHitSound();
                        rocketHitRock = true;
                        break;
                    }
                }

                if (rocketHitRock) continue;

                // Final boss collisions
                if (finalBossActive && finalBoss.CheckCollision(rocketHitBox))
                {
                    Point hitPosition = new Point(
                        Canvas.GetLeft(rocket) + (rocket.Width / 2),
                        Canvas.GetTop(rocket) + (rocket.Height / 2)
                    );

                    gameCanvas.Children.Remove(rocket);
                    playerRockets.Remove(rocket);
                    bool defeated = finalBoss.TakeDamage(1, hitPosition);

                    if (defeated)
                    {
                        finalBoss.Remove();
                        finalBossActive = false;
                        finalBossDefeated = true;
                        Score += 50;
                        OnScoreChanged?.Invoke(Score);
                        CheckVictoryCondition();
                    }
                    continue;
                }

                //Space station collisions
                if (spaceStationActive && spaceStation.CheckCollision(rocketHitBox))
                {
                    Point hitPosition = new Point(
                        Canvas.GetLeft(rocket) + (rocket.Width / 2),
                        Canvas.GetTop(rocket) + (rocket.Height / 2)
                    );

                    gameCanvas.Children.Remove(rocket);
                    playerRockets.Remove(rocket);
                    bool defeated = spaceStation.TakeDamage(1, hitPosition);

                    if (defeated)
                    {
                        spaceStationActive = false;
                        spaceStationDefeated = true;
                        Score += 100;
                        OnScoreChanged?.Invoke(Score);
                        CheckVictoryCondition();
                    }
                    continue;
                }

                // Mini-boss collisions
                if (miniBossActive && rocketHitBox.IntersectsWith(miniBoss.HitBox))
                {
                    Point hitPosition = new Point(
                        Canvas.GetLeft(rocket) + (rocket.Width / 2),
                        Canvas.GetTop(rocket) + (rocket.Height / 2)
                    );
                    gameCanvas.Children.Remove(rocket);
                    playerRockets.Remove(rocket);
                    bool defeated = miniBoss.TakeDamage(1, hitPosition);

                    if (defeated)
                    {
                        miniBoss.Remove();
                        miniBossActive = false;
                        miniBossDefeated = true;
                        Score += 25;
                        OnScoreChanged?.Invoke(Score);

                        levelSystem.HandleMiniBossDefeated();
                    }
                    continue;
                }

                // Regular enemy collisions
                foreach (var enemy in enemies.ToList())
                {
                    if (rocketHitBox.IntersectsWith(enemy.HitBox))
                    {
                        enemy.Remove();
                        gameCanvas.Children.Remove(rocket);
                        playerRockets.Remove(rocket);

                        Score += (enemy.GetEnemyType() == Enemy.EnemyType.Spinner) ? 3 : 1;
                        OnScoreChanged?.Invoke(Score);
                        break;
                    }
                }
            }

            // Player collisions
            foreach (var rocket in enemyRockets.ToList())
            {
                Rect rocketHitBox = new Rect(Canvas.GetLeft(rocket), Canvas.GetTop(rocket),
                                          rocket.Width, rocket.Height);

                if (powerUps.IsShieldActive() && rocketHitBox.IntersectsWith(player.HitBox))
                {
                    gameCanvas.Children.Remove(rocket);
                    enemyRockets.Remove(rocket);
                    continue;
                }

                if (rocketHitBox.IntersectsWith(player.HitBox))
                {
                    gameCanvas.Children.Remove(rocket);
                    enemyRockets.Remove(rocket);
                    healthSystem.TakeDamage(3);
                }
            }

            if (finalBossActive && finalBoss.HitBox.IntersectsWith(player.HitBox))
            {
                healthSystem.TakeDamage(20);
            }

            if (miniBossActive && miniBoss.HitBox.IntersectsWith(player.HitBox))
            {
                healthSystem.TakeDamage(15);
            }

            // Check player collision with regular enemies
            foreach (var enemy in enemies.ToList())
            {
                if (enemy.HitBox.IntersectsWith(player.HitBox))
                {
                    enemy.Remove();
                    healthSystem.TakeDamage(5);
                }
            }

            // Rocks collisions
            foreach (var rock in obstacles.GetRocks().ToList())
            {
                Rect rockHitBox = new Rect(
                    Canvas.GetLeft(rock),
                    Canvas.GetTop(rock),
                    rock.Width,
                    rock.Height
                );

                if (rockHitBox.IntersectsWith(player.HitBox))
                {
                    if (powerUps.IsShieldActive())
                    {
                        gameCanvas.Children.Remove(rock);
                        obstacles.GetRocks().Remove(rock);
                        Effects.SoundEffectsManager.PlayRockHitSound();
                        continue;
                    }
                    healthSystem.TakeDamage(1);
                    gameCanvas.Children.Remove(rock);
                    obstacles.GetRocks().Remove(rock);
                    Effects.SoundEffectsManager.PlayRockHitSound();
                    continue;
                }

                foreach (var enemy in enemies.ToList())
                {
                    if (rockHitBox.IntersectsWith(enemy.HitBox))
                    {
                        enemy.Remove();
                        gameCanvas.Children.Remove(rock);
                        obstacles.GetRocks().Remove(rock);
                        break;
                    }
                }

                if (finalBossActive)
                {
                    if (finalBoss.CheckCollision(rockHitBox))
                    {
                        gameCanvas.Children.Remove(rock);
                        obstacles.GetRocks().Remove(rock);
                        continue;
                    }
                }

                if (miniBossActive && rockHitBox.IntersectsWith(miniBoss.HitBox))
                {
                    gameCanvas.Children.Remove(rock);
                    obstacles.GetRocks().Remove(rock);
                }
            }
        }

        private void CheckVictoryCondition()
        {
            if (finalBossDefeated && spaceStationDefeated && !IsVictorious)
            {
                IsVictorious = true;
                levelSystem.TriggerVictory();
            }
        }

        private void UpdateDifficulty()
        {
            levelSystem.Update(Score);
            Level = levelSystem.CurrentLevel;

            if (Level == 5 && Score >= levelSystem.ScoreThreshold && !miniBossActive && !miniBossDefeated)
            {
                foreach (var enemy in enemies.ToList())
                {
                    enemy.Remove();
                }
                enemies.Clear();

                miniBoss = new MiniBoss(gameCanvas, random);
                miniBossActive = true;
                return;
            }

            if (Level == 6 && Score >= levelSystem.ScoreThreshold && !finalBossActive && !finalBossDefeated && miniBossDefeated)
            {
                foreach (var enemy in enemies.ToList())
                {
                    enemy.Remove();
                }
                enemies.Clear();

                finalBoss = new FinalBoss(gameCanvas, random);
                finalBossActive = true;

                DispatcherTimer stationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                stationTimer.Tick += (sender, e) =>
                {
                    stationTimer.Stop();
                    if (!spaceStationActive && !spaceStationDefeated)
                    {
                        spaceStation = new SpaceStation(gameCanvas, random, hitEffects);
                        spaceStationActive = true;
                    }
                };
                stationTimer.Start();
                return;
            }
        }

        public void SetFreeFlyMode(bool isFreeFly)
        {
            FreeFly = isFreeFly;
        }

        public void HandleKeyDown(Key key)
        {
            switch (key)
            {
                case Key.Left:
                    keyLeft = true;
                    UpdatePlayerMovement();
                    break;
                case Key.Right:
                    keyRight = true;
                    UpdatePlayerMovement();
                    break;
                case Key.Up:
                    keyUp = true;
                    UpdatePlayerMovement();
                    break;
                case Key.Down:
                    keyDown = true;
                    UpdatePlayerMovement();
                    break;
                case Key.Space:
                    player.IsFiring = true;
                    break;
                case Key.M:
                    ToggleMouseControl();
                    break;
                case Key.Escape:
                    SoundEffectsManager.PlayButtonClickSound();
                    if (IsGameRunning)
                    {
                        PauseGame();
                        pausePopup.Show();
                    }else if (IsGamePaused)
                    {
                        ResumeGame();
                        pausePopup.Hide();
                    }
                    break;
            }
        }

        public void HandleKeyUp(Key key)
        {
            switch (key)
            {
                case Key.Left:
                    keyLeft = false;
                    UpdatePlayerMovement();
                    break;
                case Key.Right:
                    keyRight = false;
                    UpdatePlayerMovement();
                    break;
                case Key.Up:
                    keyUp = false;
                    UpdatePlayerMovement();
                    break;
                case Key.Down:
                    keyDown = false;
                    UpdatePlayerMovement();
                    break;
                case Key.Space:
                    player.IsFiring = false;
                    break;
            }
        }

        public void HandleMouseMove(Point mousePosition)
        {
            if (!IsGameRunning || !isMouseControlEnabled || player == null ||
                keyLeft || keyRight || keyUp || keyDown) return;

            double playerCenterX = Canvas.GetLeft(player.PlayerRectangle) + player.PlayerRectangle.Width * 0.5;
            double playerCenterY = Canvas.GetTop(player.PlayerRectangle) + player.PlayerRectangle.Height * 0.5;

            double deltaX = mousePosition.X - playerCenterX;
            double deltaY = mousePosition.Y - playerCenterY;
            double distanceSquared = deltaX * deltaX + deltaY * deltaY;

            const double deadZoneSquared = 81;

            if (distanceSquared > deadZoneSquared)
            {
                double distance = Math.Sqrt(distanceSquared);
                double directionX = deltaX / distance;
                double directionY = deltaY / distance;

                mouseLeft = directionX < -0.05;
                mouseRight = directionX > 0.05;
                mouseUp = directionY < -0.05;
                mouseDown = directionY > 0.05;
            }
            else
            {
                mouseLeft = mouseRight = mouseUp = mouseDown = false;
            }
            UpdatePlayerMovement();
        }

        public void HandleMouseDown()
        {
            if (!IsGameRunning || !isMouseControlEnabled || player == null) return;

            player.IsFiring = true;
        }

        public void HandleMouseUp()
        {
            if (!IsGameRunning || !isMouseControlEnabled || player == null) return;

            player.IsFiring = false;
        }

        public void HandleMouseLeave()
        {
            if (!IsGameRunning || !isMouseControlEnabled || player == null) return;

            mouseLeft = false;
            mouseRight = false;
            mouseUp = false;
            mouseDown = false;

            UpdatePlayerMovement();
        }

        private void UpdatePlayerMovement()
        {
            if (player == null) return;

            player.MoveLeft = keyLeft || mouseLeft;
            player.MoveRight = keyRight || mouseRight;
            player.MoveUp = keyUp || mouseUp;
            player.MoveDown = keyDown || mouseDown;
        }

        private void ValidateInputState()
        {
            // Cache keyboard state checks
            bool leftPressed = Keyboard.IsKeyDown(Key.Left);
            bool rightPressed = Keyboard.IsKeyDown(Key.Right);
            bool upPressed = Keyboard.IsKeyDown(Key.Up);
            bool downPressed = Keyboard.IsKeyDown(Key.Down);

            bool stateChanged = false;

            if (keyLeft && !leftPressed) { keyLeft = false; stateChanged = true; }
            if (keyRight && !rightPressed) { keyRight = false; stateChanged = true; }
            if (keyUp && !upPressed) { keyUp = false; stateChanged = true; }
            if (keyDown && !downPressed) { keyDown = false; stateChanged = true; }

            if (stateChanged) UpdatePlayerMovement();
        }

        public void ToggleMouseControl()
        {
            isMouseControlEnabled = !isMouseControlEnabled;
            if (!isMouseControlEnabled)
            {
                mouseLeft = mouseRight = mouseUp = mouseDown = false;
                UpdatePlayerMovement();
            }
        }

        private void HandleGameOver()
        {
            Lives--;
            OnLivesChanged?.Invoke(Lives);

            levelSystem.ShowPlayerDestroyedMessage(Lives);

            if (Lives <= 0)
            {
                player.Destroy();
                powerUps.ClearAllActivePowerUps();
                IsGameRunning = false;
                gameTimer.Stop();
                OnGameOver?.Invoke();
            }
            else
            {
                player.Reset();
                healthSystem.Reset();
            }
        }

        public void ReturnToMainMenu()
        {
            if (finalBoss != null)
            {
                finalBoss.DisableExplosions();
            }
            if(miniBoss != null)
            {
                miniBoss.DisableExplosions();
            }
            Cleanup();
            uiManager?.ShowMenu();
        }

        public void Reset()
        {
            foreach (var enemy in enemies)
                enemy.Remove();

            if (miniBossActive)
            {
                miniBoss.Remove();
                miniBossActive = false;
            }
            miniBossDefeated = false;

            if (finalBossActive)
            {
                finalBoss.Remove();
                finalBossActive = false;
            }
            finalBossDefeated = false;

            if (spaceStationActive)
            {
                spaceStation.Remove();
                spaceStationActive = false;
            }
            spaceStationDefeated = false;

            foreach (var rocket in playerRockets.Concat(enemyRockets))
                gameCanvas.Children.Remove(rocket);

            obstacles.RemoveAllRocks();

            enemies.Clear();
            powerUps.Stop();
            playerRockets.Clear();
            enemyRockets.Clear();

            Score = 0;

            levelSystem.Reset();

            healthSystem.Reset();

            OnScoreChanged?.Invoke(Score);
        }

        public void Cleanup()
        {
            gameTimer.Stop();
            Effects.SoundEffectsManager.SetSilentMode(true);
            Effects.SoundEffectsManager.StopAllSounds();
            healthSystem.Remove();
            backgroundManager.ClearStars();
            obstacles.RemoveAllRocks();
            powerUps.Stop();
            pausePopup?.Remove();

            foreach (var enemy in enemies)
                enemy.Remove();

            foreach (var rocket in playerRockets.Concat(enemyRockets))
                gameCanvas.Children.Remove(rocket);

            if (finalBossActive)
            {
                finalBoss.Remove();
                finalBossActive = false;
            }

            if (spaceStationActive)
            {
                spaceStation.Remove();
                spaceStationActive = false;
            }

            if (miniBossActive)
            {
                miniBoss.Remove();
                miniBossActive = false;
            }

            Effects.SoundEffectsManager.SetSilentMode(false);
            gameCanvas.Children.Clear();
        }
    }
}


