using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using LuminWarrior.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LuminWarrior
{
    public class PowerUps
    {
        private Canvas gameCanvas;
        private Random random;
        private GameManager gameManager;
        private Player player;
        private Health healthSystem;

        private List<PowerUpItem> activePowerUps;
        private DispatcherTimer spawnTimer;
        private int spawnInterval = 15;

        private List<Image> activePowerUpIndicators; 
        private const int INDICATOR_SIZE = 90;
        private const int INDICATOR_MARGIN = 10;

        private Path shieldPath = null!;

        private double originalRocketSpeed;
        private int originalCooldown;
        private bool multiDirectionalRocketsActive = false;

        public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.RegisterAttached("Direction", typeof(Vector), typeof(PowerUps));

        private enum PowerUpType
        {
            RocketBoost,
            Shield,
            HealthBoost
        }

        private class PowerUpItem
        {
            public PowerUpType Type { get; set; }
            public required Image Visual { get; set; }
            public bool Active { get; set; }
        }

        public PowerUps(Canvas canvas, Random rnd, GameManager manager, Player plyr, Health health)
        {
            gameCanvas = canvas;
            random = rnd;
            gameManager = manager;
            player = plyr;
            healthSystem = health;

            activePowerUps = new List<PowerUpItem>();
            activePowerUpIndicators = new List<Image>();

            InitializeShieldVisual();

            spawnTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(spawnInterval)
            };
            spawnTimer.Tick += SpawnPowerUp;
        }

        public void Start()
        {
            spawnTimer.Start();
        }

        public void Stop()
        {
            spawnTimer.Stop();

            foreach (var powerUp in activePowerUps.ToList())
            {
                gameCanvas.Children.Remove(powerUp.Visual);
            }
            activePowerUps.Clear();

            ClearAllIndicators();
            HideShield();
        }

        private void ClearAllIndicators()
        {
            foreach (var indicator in activePowerUpIndicators)
            {
                gameCanvas.Children.Remove(indicator);
            }
            activePowerUpIndicators.Clear();
        }

        private void AddPowerUpIndicator(PowerUpType type)
        {
            var imagePaths = new Dictionary<PowerUpType, string>
            {
                { PowerUpType.RocketBoost, "pack://application:,,,/assets/images/rocketspackage.png" },
                { PowerUpType.Shield, "pack://application:,,,/assets/images/shieldpackage.png" },
                { PowerUpType.HealthBoost, "pack://application:,,,/assets/images/healthpowerup.png" }
            };

            var indicator = new Image
            {
                Width = INDICATOR_SIZE,
                Height = INDICATOR_SIZE,
                Source = new BitmapImage(new Uri(imagePaths[type])),
                Opacity = 0.8,
                Tag = type.ToString()
            };

            gameCanvas.Children.Add(indicator);
            activePowerUpIndicators.Add(indicator);
            UpdateIndicatorPositions();
        }

        private void UpdateIndicatorPositions()
        {
            for (int i = 0; i < activePowerUpIndicators.Count; i++)
            {
                Canvas.SetLeft(activePowerUpIndicators[i], INDICATOR_MARGIN + i * (INDICATOR_SIZE + INDICATOR_MARGIN));
                Canvas.SetBottom(activePowerUpIndicators[i], INDICATOR_MARGIN);
            }
        }

        private void RemovePowerUpIndicator(PowerUpType type)
        {
            var indicator = activePowerUpIndicators.FirstOrDefault(img => img.Tag.ToString() == type.ToString());
            if (indicator != null)
            {
                gameCanvas.Children.Remove(indicator);
                activePowerUpIndicators.Remove(indicator);
                UpdateIndicatorPositions();
            }
        }

        private void InitializeShieldVisual()
        {
            shieldPath = new Path
            {
                Fill = null,
                Stroke = new SolidColorBrush(Color.FromArgb(220, 0, 255, 255)),
                StrokeThickness = 2,
                Visibility = Visibility.Hidden
            };

            gameCanvas.Children.Insert(0, shieldPath);
        }

        private void SpawnPowerUp(object? sender, EventArgs e)
        {
            if (activePowerUps.Count >= 2)
                return;

            PowerUpType type = (PowerUpType)random.Next(0, 3);

            PowerUpItem powerUp = new PowerUpItem
            {
                Type = type,
                Visual = CreatePowerUpVisual(type),
                Active = false
            };

            Canvas.SetLeft(powerUp.Visual, random.Next(50, (int)gameCanvas.ActualWidth - 50));
            Canvas.SetTop(powerUp.Visual, -50);

            gameCanvas.Children.Add(powerUp.Visual);
            activePowerUps.Add(powerUp);
        }

        private Image CreatePowerUpVisual(PowerUpType type)
        {
            var imagePaths = new Dictionary<PowerUpType, string>
            {
                { PowerUpType.RocketBoost, "pack://application:,,,/assets/images/rocketspackage.png" },
                { PowerUpType.Shield, "pack://application:,,,/assets/images/shieldpackage.png" },
                { PowerUpType.HealthBoost, "pack://application:,,,/assets/images/healthpowerup.png" }
            };

            return new Image
            {
                Width = 90,
                Height = 90,
                Source = new BitmapImage(new Uri(imagePaths[type])),
                Tag = "powerup"
            };
        }

        public void Update()
        {
            foreach (var powerUp in activePowerUps.ToList())
            {
                if (powerUp.Visual != null)
                {
                    double currentTop = Canvas.GetTop(powerUp.Visual);
                    Canvas.SetTop(powerUp.Visual, currentTop + 3);

                    if (currentTop > gameCanvas.ActualHeight)
                    {
                        gameCanvas.Children.Remove(powerUp.Visual);
                        activePowerUps.Remove(powerUp);
                    }
                }
            }

            if (IsShieldActive())
            {
                UpdateShieldPosition();
            }

            CheckCollisions();
        }

        private void PowerUpSound()
        {
            SoundEffectsManager.PlayPowerUpSound();
        }

        private void CheckCollisions()
        {
            Rect playerHitBox = player.HitBox;

            foreach (var powerUp in activePowerUps.ToList())
            {
                if (powerUp.Visual != null)
                {
                    Rect powerUpHitBox = new Rect(
                        Canvas.GetLeft(powerUp.Visual),
                        Canvas.GetTop(powerUp.Visual),
                        powerUp.Visual.Width,
                        powerUp.Visual.Height
                    );

                    if (playerHitBox.IntersectsWith(powerUpHitBox))
                    {
                        PowerUpSound();
                        ActivatePowerUp(powerUp.Type);
                        gameCanvas.Children.Remove(powerUp.Visual);
                        activePowerUps.Remove(powerUp);
                    }
                }
            }
        }

        private void ActivatePowerUp(PowerUpType type)
        {
            PowerUpSound();
            switch (type)
            {
                case PowerUpType.RocketBoost:
                    ActivateRocketBoost();
                    break;
                case PowerUpType.Shield:
                    ActivateShield();
                    break;
                case PowerUpType.HealthBoost:
                    ActivateHealthBoost();
                    break;
            }
        }

        private void ActivateRocketBoost()
        {
            AddPowerUpIndicator(PowerUpType.RocketBoost);

            originalRocketSpeed = player.RocketSpeedFactor;
            originalCooldown = player.FiringCooldown;

            player.RocketSpeedFactor = 2.0;
            player.FiringCooldown = 5;
            multiDirectionalRocketsActive = true;

            DispatcherTimer boostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10) // 10 seconds boost
            };

            boostTimer.Tick += (sender, e) =>
            {
                player.RocketSpeedFactor = originalRocketSpeed;
                player.FiringCooldown = originalCooldown;
                multiDirectionalRocketsActive = false;

                RemovePowerUpIndicator(PowerUpType.RocketBoost);

                boostTimer.Stop();
            };

            boostTimer.Start();
        }

        public List<Rectangle>? FireMultiDirectionalRockets(Rectangle leftRocket, Rectangle rightRocket)
        {
            if (!multiDirectionalRocketsActive)
                return null;

            List<Rectangle> additionalRockets = new List<Rectangle>();

            double playerLeft = Canvas.GetLeft(leftRocket) - 36.6;
            double playerTop = Canvas.GetTop(leftRocket) - 39;
            double playerWidth = 100;

            // Top-left diagonal rocket (up-left)
            Rectangle topLeftRocket = CreateRocket();
            Canvas.SetLeft(topLeftRocket, playerLeft + 10);
            Canvas.SetTop(topLeftRocket, playerTop + 15);
            topLeftRocket.Tag = "rocket_diagonal_left";
            // Direction vector: (-0.7071, -0.7071) for diagonal up-left
            topLeftRocket.SetValue(DirectionProperty, new Vector(-0.7071, -0.7071));
            AlignRocketWithDirection(topLeftRocket, 315);
            additionalRockets.Add(topLeftRocket);

            // Top-right diagonal rocket (up-right)
            Rectangle topRightRocket = CreateRocket();
            Canvas.SetLeft(topRightRocket, playerLeft + playerWidth - topRightRocket.Width - 50);
            Canvas.SetTop(topRightRocket, playerTop + 55);
            topRightRocket.Tag = "rocket_diagonal_right";
            // Direction vector: (0.7071, -0.7071) for diagonal up-right
            topRightRocket.SetValue(DirectionProperty, new Vector(0.7071, -0.7071));
            AlignRocketWithDirection(topRightRocket, 225);
            additionalRockets.Add(topRightRocket);

            // Bottom-left diagonal rocket (down-left)
            Rectangle bottomLeftRocket = CreateRocket();
            Canvas.SetLeft(bottomLeftRocket, playerLeft + 10);
            Canvas.SetTop(bottomLeftRocket, playerTop + 85);
            bottomLeftRocket.Tag = "rocket_diagonal_down_left";
            bottomLeftRocket.SetValue(DirectionProperty, new Vector(-0.7071, 0.7071));
            AlignRocketWithDirection(bottomLeftRocket, 45);
            additionalRockets.Add(bottomLeftRocket);

            // Bottom-right diagonal rocket (down-right)
            Rectangle bottomRightRocket = CreateRocket();
            Canvas.SetLeft(bottomRightRocket, playerLeft + playerWidth - bottomRightRocket.Width - 10);
            Canvas.SetTop(bottomRightRocket, playerTop + 85);
            bottomRightRocket.Tag = "rocket_diagonal_down_right";
            bottomRightRocket.SetValue(DirectionProperty, new Vector(0.7071, 0.7071));
            AlignRocketWithDirection(bottomRightRocket, 135);
            additionalRockets.Add(bottomRightRocket);

            return additionalRockets;
        }

        private void AlignRocketWithDirection(Rectangle rocket, double angleDegrees)
        {
            // Creating a rotation transform to orient the rocket in its direction
            RotateTransform rotateTransform = new RotateTransform(angleDegrees);
            rocket.RenderTransform = rotateTransform;
        }

        private static readonly DropShadowEffect RocketGlowEffect = new DropShadowEffect
        {
            Color = Colors.Aqua,
            BlurRadius = 36,
            ShadowDepth = 0,
            Opacity = 1
        };

        private Rectangle CreateRocket()
        {
            return new Rectangle
            {
                Tag = "rocket",
                Height = 39,
                Width = 3.3,
                RadiusX = 15,
                RadiusY = 15,
                Fill = Brushes.White,
                Stroke = Brushes.Aqua,
                StrokeThickness = 3,
                Effect = RocketGlowEffect
            };
        }

        public bool IsMultiDirectionalRocketsActive()
        {
            return multiDirectionalRocketsActive;
        }

        private void ActivateShield()
        {
            AddPowerUpIndicator(PowerUpType.Shield);

            shieldPath.Visibility = Visibility.Visible;
            UpdateShieldPosition();

            DispatcherTimer shieldTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15) // 15 seconds shield
            };

            shieldTimer.Tick += (sender, e) =>
            {
                HideShield();
                RemovePowerUpIndicator(PowerUpType.Shield);
                shieldTimer.Stop();
            };

            shieldTimer.Start();
        }

        private void UpdateShieldPosition()
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            Rect playerHitBox = player.HitBox;

            double playerX = playerHitBox.X;
            double playerY = playerHitBox.Y;
            double playerWidth = playerHitBox.Width;
            double playerHeight = playerHitBox.Height;

            double centerX = playerX + playerWidth / 2;
            double topY = playerY + 50;
            double radius = playerWidth * 0.7;

            // Creating semi-circle above player
            figure.StartPoint = new Point(centerX - radius, topY);

            ArcSegment arc = new ArcSegment
            {
                Point = new Point(centerX + radius, topY),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = true
            };

            figure.Segments.Add(arc);
            geometry.Figures.Add(figure);

            shieldPath.Data = geometry;
        }

        public bool IsShieldActive()
        {
            return shieldPath.Visibility == Visibility.Visible;
        }

        private void HideShield()
        {
            shieldPath.Visibility = Visibility.Hidden;
        }

        private void ActivateHealthBoost()
        {
            double healthBoost = healthSystem.MaxHealth * 0.25;

            healthSystem.Heal(healthBoost);
        }

        public void ClearAllActivePowerUps()
        {
            HideShield();

            ClearAllIndicators();

            if (multiDirectionalRocketsActive)
            {
                player.RocketSpeedFactor = originalRocketSpeed;
                player.FiringCooldown = originalCooldown;
                multiDirectionalRocketsActive = false;
            }
        }
    }
}