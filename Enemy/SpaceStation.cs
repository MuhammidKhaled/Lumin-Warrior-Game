using System.Media;
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
    public class SpaceStation
    {
        private Canvas gameCanvas;
        private Rectangle stationRect = null!;
        private Random random;
        private HitEffects hitEffects;
        private List<Rectangle> activeProjectiles = new List<Rectangle>();

        private Rectangle mainHitBox = null!;
        private Rectangle horizontalBodyHitBox = null!;
        private Rectangle leftWeaponHitBox = null!;
        private Rectangle rightWeaponHitBox = null!;

        private int centralFireCooldown = 120;
        private int centralFireTimer = 60;
        private int sideFireTimer = 40;

        private bool isEntering = true;
        private double entrySpeed = 1;
        private double targetY = 0;

        private List<ImageBrush> explosionSprites = null!;

        private double shakeAmount = 0;
        private bool isShaking = false;
        private double originalX;
        private DispatcherTimer shakeTimer;

        public bool IsActive { get; private set; } = true;
        public int Health { get; private set; } = 600;

        public SpaceStation(Canvas canvas, Random rand, HitEffects effects)
        {
            gameCanvas = canvas;
            random = rand;
            hitEffects = effects;

            InitializeExplosionSprites();
            CreateStation();

            shakeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            shakeTimer.Tick += ShakeAnimation;
        }

        private void CreateStation()
        {
            ImageBrush stationImage = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/spacestation.png"))
            };

            stationRect = new Rectangle
            {
                Tag = "spaceStation",
                Width = gameCanvas.ActualWidth,
                Height = 400,
                Fill = stationImage
            };

            Canvas.SetTop(stationRect, -stationRect.Height);
            Canvas.SetLeft(stationRect, 0);
            originalX = 0;

            mainHitBox = new Rectangle
            {
                Tag = "stationHitBox",
                Width = stationRect.Width * 0.15,
                Height = stationRect.Height * 0.7,
                Fill = Brushes.Transparent
            };

            horizontalBodyHitBox = new Rectangle
            {
                Tag = "stationHitBox",
                Width = stationRect.Width * 0.95,
                Height = stationRect.Height * 0.27,
                Fill = Brushes.Transparent
            };

            leftWeaponHitBox = new Rectangle
            {
                Tag = "stationHitBox",
                Width = stationRect.Width * 0.15,
                Height = stationRect.Height * 0.7,
                Fill = Brushes.Transparent
            };

            rightWeaponHitBox = new Rectangle
            {
                Tag = "stationHitBox",
                Width = stationRect.Width * 0.15,
                Height = stationRect.Height * 0.7,
                Fill = Brushes.Transparent
            };

            gameCanvas.Children.Insert(0, stationRect);
            gameCanvas.Children.Add(mainHitBox);
            gameCanvas.Children.Add(horizontalBodyHitBox);
            gameCanvas.Children.Add(leftWeaponHitBox);
            gameCanvas.Children.Add(rightWeaponHitBox);

            UpdateHitBoxPositions();
        }

        private void UpdateHitBoxPositions()
        {
            double stationX = Canvas.GetLeft(stationRect);
            double stationY = Canvas.GetTop(stationRect);
            double stationWidth = stationRect.Width;
            double stationHeight = stationRect.Height;

            Canvas.SetLeft(horizontalBodyHitBox, stationX + ((stationWidth / 2) - (horizontalBodyHitBox.Width / 2)));
            Canvas.SetTop(horizontalBodyHitBox, stationY);  

            Canvas.SetLeft(mainHitBox, stationX + ((stationWidth / 2) - (mainHitBox.Width / 2)));
            Canvas.SetTop(mainHitBox, stationY + (stationHeight * -0.10));

            Canvas.SetLeft(leftWeaponHitBox, stationX + (stationWidth * 0.12)); 
            Canvas.SetTop(leftWeaponHitBox, stationY + (stationHeight * -0.1));

            Canvas.SetLeft(rightWeaponHitBox, stationX + (stationWidth * 0.72) ); 
            Canvas.SetTop(rightWeaponHitBox, stationY + (stationHeight * -0.1));
        }

        public void Update()
        {
            if (!IsActive) return;

            if (isEntering)
            {
                double currentY = Canvas.GetTop(stationRect);
                if (currentY < targetY)
                {
                    Canvas.SetTop(stationRect, currentY + entrySpeed);
                    UpdateHitBoxPositions();
                }
                else
                {
                    isEntering = false;
                }
            }

            centralFireTimer = Math.Max(0, centralFireTimer - 1);
            sideFireTimer = Math.Max(0, sideFireTimer - 1);

            CleanupProjectiles();
        }

        public List<Rectangle>? FireWeapons()
        {
            if (!IsActive) return null;
            List<Rectangle> projectiles = new List<Rectangle>();

            if (centralFireTimer <= 0)
            {
                double stationX = Canvas.GetLeft(stationRect);
                double stationY = Canvas.GetTop(stationRect);
                double stationWidth = stationRect.Width;

                double rocketX = stationX + (stationWidth / 2) + 5; // +5 because of the stroke thickness around the rocket
                double rocketY = stationY + horizontalBodyHitBox.Height + 20;

                var rocket = CreateRocket(rocketX, rocketY);
                projectiles.Add(rocket);
                activeProjectiles.Add(rocket);
                centralFireTimer = centralFireCooldown;
            }

            if (sideFireTimer <= 0)
            {
                double leftBaseX = Canvas.GetLeft(stationRect) + stationRect.ActualWidth * 0.21; 
                double leftBaseY = Canvas.GetTop(stationRect) + (stationRect.Height * -0.1);

                double rightBaseX = Canvas.GetLeft(stationRect) + stationRect.ActualWidth * 0.8; 
                double rightBaseY = Canvas.GetTop(stationRect) + (stationRect.Height * -0.1);

                int[] yOffsets = { 0, 70, 140 };

                for (int i = 0; i < 3; i++)
                {
                    double circleX = leftBaseX;
                    double circleY = leftBaseY + yOffsets[i]; 

                    var leftCircle = CreateEnergyCircle(circleX, circleY);
                    projectiles.Add(leftCircle);
                    activeProjectiles.Add(leftCircle);
                }

                for (int i = 0; i < 3; i++)
                {
                    double circleX = rightBaseX;
                    double circleY = rightBaseY + yOffsets[i];

                    var rightCircle = CreateEnergyCircle(circleX, circleY);
                    projectiles.Add(rightCircle);
                    activeProjectiles.Add(rightCircle);
                }

                sideFireTimer = 160;
            }

            return projectiles.Count > 0 ? projectiles : null;
        }

        private Rectangle CreateRocket(double x, double y)
        {
            DropShadowEffect rocketGlow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 42,
                ShadowDepth = 0,
                Opacity = 1
            };

            Rectangle rocket = new Rectangle
            {
                Tag = "stationRocket",
                Width = 9,
                Height = 81,
                RadiusX = 15,
                RadiusY = 15,
                Fill = Brushes.White,
                Stroke = Brushes.Red,
                StrokeThickness = 5,
                Effect = rocketGlow
            };

            Canvas.SetLeft(rocket, x);
            Canvas.SetTop(rocket, y);

            rocket.SetValue(Enemy.DirectionProperty, new Vector(0, 1));

            return rocket;
        }

        private Rectangle CreateEnergyCircle(double x, double y)
        {
            DropShadowEffect circleGlow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 30,
                ShadowDepth = 0,
                Opacity = 1
            };

            Rectangle circle = new Rectangle
            {
                Tag = "energyCircle",
                Width = 60,
                Height = 60,
                RadiusX = 30,
                RadiusY = 30,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Red, 
                StrokeThickness = 2,
                Effect = circleGlow
            };

            Canvas.SetLeft(circle, x - (circle.Width / 2));
            Canvas.SetTop(circle, y);

            circle.SetValue(Enemy.DirectionProperty, new Vector(0, 1));

            return circle;
        }

        public bool CheckCollision(Rect hitBox, out string hitSection, out Point hitPosition)
        {
            var hitBoxes = new[]
            {
                (leftWeaponHitBox, "leftWeapon"),
                (rightWeaponHitBox, "rightWeapon"),
                (horizontalBodyHitBox, "horizontalBody"),
                (mainHitBox, "main")
            };

            foreach (var (rectBox, section) in hitBoxes)
            {
                Rect rect = GetHitBox(rectBox);
                if (rect.IntersectsWith(hitBox))
                {
                    hitSection = section;
                    hitPosition = new Point(
                        (rect.Left + rect.Right) * 0.5,
                        (rect.Top + rect.Bottom) * 0.5
                    );
                    return true;
                }
            }

            hitSection = "";
            hitPosition = new Point();
            return false;
        }

        private Rect GetHitBox(Rectangle rect)
        {
            return new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect),rect.Width, rect.Height);
        }

        public bool CheckCollision(Rect hitBox)
        {
            string hitSection;
            Point hitPosition;
            return CheckCollision(hitBox, out hitSection, out hitPosition);
        }

        public bool TakeDamage(int damage, Point hitPosition)
        {
            if (!IsActive) return false;

            Health -= damage;

            hitEffects.CreateSparkEffect(hitPosition.X, hitPosition.Y, Colors.Aqua, 8);

            if (Health <= 0 && !isShaking)
            {
                StartDestructionSequence();
                return true;
            }

            return false;
        }

        private void StartDestructionSequence()
        {
            isShaking = true;
            shakeAmount = 10;
            shakeTimer.Start();

            SoundEffectsManager.PlayStationShakingSound();

            DispatcherTimer destructionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };

            destructionTimer.Tick += (sender, e) =>
            {
                destructionTimer.Stop();
                DestroyStation();
            };

            destructionTimer.Start();
        }

        private void ShakeAnimation(object? sender, EventArgs e)
        {
            if (!isShaking || !IsActive)
            {
                shakeTimer.Stop();
                return;
            }

            double offsetX = (random.NextDouble() * 2 - 1) * shakeAmount;

            Canvas.SetLeft(stationRect, originalX + offsetX);

            UpdateHitBoxPositions();

            if (Health <= 10)
            {
                shakeAmount = Math.Min(15, shakeAmount + 0.1);
            }
        }

        private void InitializeExplosionSprites()
        {
            explosionSprites = new List<ImageBrush>
            {
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion1.png"))),
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion2.png"))),
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion3.png")))
            };
        }

        private void CreateHitboxExplosions()
        {
            var hitboxes = new[] { mainHitBox, horizontalBodyHitBox, leftWeaponHitBox, rightWeaponHitBox };

            for (int i = 0; i < hitboxes.Length; i++)
            {
                int delay = i * 600;
                var hitbox = hitboxes[i];

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay) };
                timer.Tick += (sender, e) =>
                {
                    timer.Stop();
                    CreateHitboxExplosion(hitbox, new SoundPlayer("assets/sounds/explosionsound.wav"));
                };
                timer.Start();
            }
        }

        private void CreateHitboxExplosion(Rectangle hitbox, SoundPlayer explosionSound)
        {
            double centerX = Canvas.GetLeft(hitbox) + (hitbox.Width / 2);
            double centerY = Canvas.GetTop(hitbox) + (hitbox.Height / 2);

            Rectangle explosionRect = new Rectangle
            {
                Width = hitbox.Width * 0.8,
                Height = hitbox.Width * 0.8,
                Tag = "explosion"
            };

            if (explosionSprites != null && explosionSprites.Count > 0)
            {
                explosionRect.Fill = explosionSprites[random.Next(explosionSprites.Count)];
            }

            Canvas.SetLeft(explosionRect, centerX - (explosionRect.Width / 2));
            Canvas.SetTop(explosionRect, centerY - (explosionRect.Height / 2));

            gameCanvas.Children.Add(explosionRect);

            try
            {
                if (explosionSound != null)
                {
                    explosionSound.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing explosion sound: {ex.Message}");
            }

        
            DispatcherTimer explosionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000) 
            };

            int currentSprite = 0;
            explosionTimer.Tick += (sender, e) =>
            {
                currentSprite++;

                if (currentSprite < explosionSprites?.Count)
                {
                    explosionRect.Fill = explosionSprites[currentSprite];
                }
                else
                {
                    gameCanvas.Children.Remove(explosionRect);
                    explosionTimer.Stop();
                }
            };

            explosionTimer.Start();
        }

        private void DestroyStation()
        {
            IsActive = false;
            CreateHitboxExplosions();

            var finalTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            finalTimer.Tick += (sender, e) =>
            {
                finalTimer.Stop();
                Remove();
            };
            finalTimer.Start();
        }

        private void CleanupProjectiles()
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = activeProjectiles[i];
                double y = Canvas.GetTop(projectile);

                if (y < -50 || y > gameCanvas.ActualHeight + 50)
                {
                    gameCanvas.Children.Remove(projectile);
                    activeProjectiles.RemoveAt(i);
                }
            }
        }

        public void Remove()
        {
            if (shakeTimer.IsEnabled)
                shakeTimer.Stop();

            if (gameCanvas.Children.Contains(stationRect))
                gameCanvas.Children.Remove(stationRect);
            if (gameCanvas.Children.Contains(mainHitBox))
                gameCanvas.Children.Remove(mainHitBox);
            if (gameCanvas.Children.Contains(horizontalBodyHitBox))
                gameCanvas.Children.Remove(horizontalBodyHitBox);
            if (gameCanvas.Children.Contains(leftWeaponHitBox))
                gameCanvas.Children.Remove(leftWeaponHitBox);
            if (gameCanvas.Children.Contains(rightWeaponHitBox))
                gameCanvas.Children.Remove(rightWeaponHitBox);

            IsActive = false;
        }
    }
}