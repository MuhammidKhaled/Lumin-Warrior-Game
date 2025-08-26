using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using LuminWarrior.Effects;


namespace LuminWarrior
{
    public class Player
    {
        public Rectangle playerRect = null!;
        private Image leftFlame = null!;
        private Image rightFlame = null!;
        private double flameOffsetY = 0;
        private bool movingUp = true;

        private Canvas gameCanvas = null!;
        private PowerUps? powerUps;

        public bool MoveLeft { get; set; }
        public bool MoveRight { get; set; }
        public bool MoveUp { get; set; }
        public bool MoveDown { get; set; }
        public int Speed { get; set; } = 10;

        public bool IsFiring { get; set; }
        public int FiringCooldown { get; set; } = 10;
        public int FiringTimer { get; set; } = 0;
        public double RocketSpeedFactor { get; set; } = 1.0;

        public bool IsDisabled { get; private set; } = false;

        private List<ImageBrush>? explosionSprites;

        public Rectangle PlayerRectangle => playerRect;

        public Rect HitBox => new(Canvas.GetLeft(playerRect), Canvas.GetTop(playerRect),
                                 playerRect.Width, playerRect.Height);

        public Player(Canvas canvas)
        {
            gameCanvas = canvas;
            InitializePlayer();
            CreatePlayerFlames();
        }

        private void InitializePlayer()
        {
            playerRect = new Rectangle
            {
                Width = 100,
                Height = 100
            };

            ImageBrush playerImg = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/player.png"))
            };

            playerRect.Fill = playerImg;

            DropShadowEffect neonGlow = new DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 30,
                ShadowDepth = 0,
                Opacity = 0.3
            };

            playerRect.Effect = neonGlow;

            gameCanvas.Loaded += (s, e) =>
            {
                Canvas.SetLeft(playerRect, (gameCanvas.ActualWidth - playerRect.Width) / 2);
                Canvas.SetTop(playerRect, gameCanvas.ActualHeight - 150);
            };

            gameCanvas.Children.Add(playerRect);
        }

        private void CreatePlayerFlames()
        {
            leftFlame = new Image
            {
                Tag = "flame",
                Width = 18,
                Height = 80,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/aquaflame.png")),
                Opacity = 0.9
            };

            rightFlame = new Image
            {
                Tag = "flame",
                Width = 18,
                Height = 80,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/aquaflame.png")),
                Opacity = 0.9
            };

            gameCanvas.Children.Insert(0, leftFlame);
            gameCanvas.Children.Insert(0, rightFlame);
        }

        public void Update()
        {
            if (IsDisabled) return;

            UpdatePosition();
            UpdateFlames();
            UpdateFiringTimer();
        }

        private void UpdatePosition()
        {
            if (MoveUp && Canvas.GetTop(playerRect) > 0)
            {
                Canvas.SetTop(playerRect, Canvas.GetTop(playerRect) - Speed);
            }
            if (MoveDown && Canvas.GetTop(playerRect) + playerRect.Height + Speed < gameCanvas.ActualHeight)
            {
                Canvas.SetTop(playerRect, Canvas.GetTop(playerRect) + Speed);
            }
            if (MoveLeft && Canvas.GetLeft(playerRect) > 0)
            {
                Canvas.SetLeft(playerRect, Canvas.GetLeft(playerRect) - Speed);
            }
            if (MoveRight && Canvas.GetLeft(playerRect) + playerRect.Width < gameCanvas.ActualWidth)
            {
                Canvas.SetLeft(playerRect, Canvas.GetLeft(playerRect) + Speed);
            }
        }

        private void UpdateFlames()
        {
            if (leftFlame == null || rightFlame == null || playerRect == null)
                return;

            double leftFlameX = Canvas.GetLeft(playerRect) + 32;
            double rightFlameX = Canvas.GetLeft(playerRect) + playerRect.Width - 50;
            double flameY = Canvas.GetTop(playerRect) + (playerRect.Height - 60) + flameOffsetY;

            Canvas.SetLeft(leftFlame, leftFlameX);
            Canvas.SetTop(leftFlame, flameY);

            Canvas.SetLeft(rightFlame, rightFlameX);
            Canvas.SetTop(rightFlame, flameY);

            AnimateFlames();
        }

        private void AnimateFlames()
        {
            if (movingUp)
            {
                flameOffsetY -= 2;
                if (flameOffsetY <= -3) movingUp = false;
            }
            else
            {
                flameOffsetY += 2;
                if (flameOffsetY >= 3) movingUp = true;
            }
        }

        private void UpdateFiringTimer()
        {
            if (FiringTimer > 0)
                FiringTimer--;
        }

        private void PlayFiringSound()
        {
            SoundEffectsManager.PlayFiringSound();
        }

        public List<Rectangle> FireRocket()
        {
            if (IsDisabled) return new List<Rectangle>();

            List<Rectangle> rockets = new List<Rectangle>();

            if (FiringTimer <= 0)
            {
                PlayFiringSound();

                double leftWingOffset = 36.6;
                double rightWingOffset = playerRect.Width - 39;
                double verticalOffset = 6;

                DropShadowEffect glowEffect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 255, 255),
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8,
                };

                Rectangle leftRocket = CreateRocket();
                Rectangle rightRocket = CreateRocket();

                leftRocket.SetValue(PowerUps.DirectionProperty, new Vector(0, -1 + (-6)));
                rightRocket.SetValue(PowerUps.DirectionProperty, new Vector(0, -1 + (-6)));
                

                Canvas.SetLeft(leftRocket, Canvas.GetLeft(playerRect) + leftWingOffset);
                Canvas.SetTop(leftRocket, Canvas.GetTop(playerRect) + (leftRocket.Height + verticalOffset));

                Canvas.SetLeft(rightRocket, Canvas.GetLeft(playerRect) + rightWingOffset);
                Canvas.SetTop(rightRocket, Canvas.GetTop(playerRect) + (rightRocket.Height + verticalOffset));

                rockets.Add(leftRocket);
                rockets.Add(rightRocket);

                if (powerUps != null && powerUps.IsMultiDirectionalRocketsActive())
                {
                    var additionalRockets = powerUps.FireMultiDirectionalRockets(leftRocket, rightRocket);
                    if (additionalRockets != null && additionalRockets.Count > 0)
                    {
                        rockets.AddRange(additionalRockets);
                    }
                }

                FiringTimer = FiringCooldown;
            }

            return rockets;
        }

        private Rectangle CreateRocket()
        {
            DropShadowEffect glowEffect = new DropShadowEffect
            {
                Color = Colors.Aqua,
                BlurRadius = 36,
                ShadowDepth = 0,
                Opacity = 1
            };

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
                Effect = glowEffect,
            };
        }

        public void SetPowerUps(PowerUps powerUp)
        {
            powerUps = powerUp;
        }

        private void PlayDestructionSound()
        {
            SoundEffectsManager.PlayDestructionSound();
        }
       
        public void Destroy()
        {
            InitializeExplosionSprites();
            PlayDestructionSound();

            if (playerRect == null || gameCanvas == null || explosionSprites == null || explosionSprites.Count == 0)
                return;

            double centerX = Canvas.GetLeft(playerRect) + (playerRect.Width / 2);
            double centerY = Canvas.GetTop(playerRect) + (playerRect.Height / 2);

            Rectangle explosionRect = new Rectangle
            {
                Width = playerRect.Width * 1.5,
                Height = playerRect.Width * 1.5,
                Tag = "explosion",
                Fill = explosionSprites[0]
            };

            Canvas.SetLeft(explosionRect, centerX - (explosionRect.Width / 2));
            Canvas.SetTop(explosionRect, centerY - (explosionRect.Height / 2));

            gameCanvas.Children.Add(explosionRect);

            DispatcherTimer explosionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            int currentSprite = 0;
            explosionTimer.Tick += (sender, e) =>
            {
                currentSprite++;

                if (currentSprite < explosionSprites.Count)
                {
                    explosionRect.Fill = explosionSprites[currentSprite];
                }
                else
                {
                    gameCanvas.Children.Remove(explosionRect);
                    explosionTimer.Stop();

                    if (playerRect != null && gameCanvas.Children.Contains(playerRect))
                        gameCanvas.Children.Remove(playerRect);
                    if (leftFlame != null && gameCanvas.Children.Contains(leftFlame))
                        gameCanvas.Children.Remove(leftFlame);
                    if (rightFlame != null && gameCanvas.Children.Contains(rightFlame))
                        gameCanvas.Children.Remove(rightFlame);

                    playerRect = null!;
                    leftFlame = null!;
                    rightFlame = null!;
                }
            };

            explosionTimer.Start();
        }

        private void InitializeExplosionSprites()
        {
            if (explosionSprites == null)
            {
                explosionSprites = new List<ImageBrush>
                {
                    new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion1.png"))),
                    new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion2.png"))),
                    new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion3.png")))
                };
            }
        }

        public void Disable()
        {
            IsDisabled = true;
        }

        public void Enable()
        {
            IsDisabled = false;
        }

        public void Reset()
        {
            Canvas.SetLeft(playerRect, (gameCanvas.ActualWidth - playerRect.Width) / 2);
            Canvas.SetTop(playerRect, gameCanvas.ActualHeight - 150);

            MoveLeft = false;
            MoveRight = false;
            MoveUp = false;
            MoveDown = false;

            IsFiring = false;
            FiringTimer = 0;
            RocketSpeedFactor = 1.0;

            flameOffsetY = 0;
            movingUp = true;

            UpdateFlames();

            if (playerRect != null)
                playerRect.Visibility = Visibility.Visible;

            if (leftFlame != null)
                leftFlame.Visibility = Visibility.Visible;
            if (rightFlame != null)
                rightFlame.Visibility = Visibility.Visible;
        }
    }
}