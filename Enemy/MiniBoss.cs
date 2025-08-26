using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;

namespace LuminWarrior
{
    public class MiniBoss : Enemy
    {
        private double patrolDirection = 1; // 1 for right, -1 for left
        private const double PATROL_SPEED = 3;
        private const double PATROL_Y_POSITION = 100; 
        private const double PATROL_MARGIN = 100;

        private bool isEntering = true;
        private const double ENTRY_SPEED = 2;
        private const double FINAL_Y_POSITION = 100;

        private BossHealthBar? healthBar;
        private HitEffects hitEffects;

        public int Health { get; private set; } = 250;

        private List<ImageBrush>? explosionSprites;
        private SoundPlayer? explosionSound;
        private bool allowExplosions = true;

        public MiniBoss(Canvas canvas, Random rand): base(canvas, rand, EnemyType.Spinner)
        {
            Speed = 0;

            FireCooldown = 60;

            CreateMiniBoss(canvas);

            Canvas.SetTop(enemyRect, PATROL_Y_POSITION);
            Canvas.SetLeft(enemyRect, canvas.ActualWidth / 2 - enemyRect.Width / 2);

            SetInitialPosition();

            healthBar = new BossHealthBar(canvas, enemyRect, Health, enemyRect.Width);

            hitEffects = new HitEffects(canvas);
        }

        private void CreateMiniBoss(Canvas canvas)
        {
            Remove();

            ImageBrush miniBossImage = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/circularenemy.png"))
            };

            enemyRect = new Rectangle
            {
                Tag = "miniBoss",
                Width = 180,
                Height = 180,
                Fill = miniBossImage,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(0)
            };

            Canvas.SetTop(enemyRect, PATROL_Y_POSITION);
            Canvas.SetLeft(enemyRect, canvas.ActualWidth / 2 - enemyRect.Width / 2);

            gameCanvas.Children.Add(enemyRect);
        }

        private void SetInitialPosition()
        {
            Canvas.SetTop(enemyRect, -enemyRect.Height);
            Canvas.SetLeft(enemyRect, gameCanvas.ActualWidth / 2 - enemyRect.Width / 2);
            isEntering = true;
        }

        private void UpdateSpinnerRotation()
        {
            rotation += 10;
            if (rotation >= 360) rotation = 0;

            var transform = enemyRect.RenderTransform as RotateTransform;
            if (transform == null)
            {
                transform = new RotateTransform(rotation);
                enemyRect.RenderTransform = transform;
            }
            else
            {
                transform.Angle = rotation;
            }
        }

        public bool TakeDamage(int amount, Point? hitPosition = null)
        {
            Health -= amount;
            healthBar?.Update(Health);

            if (hitPosition.HasValue)
            {
                hitEffects.CreateSparkEffect(hitPosition.Value.X, hitPosition.Value.Y, Colors.Aqua);
            }

            return Health <= 0;
        }

        public new void Update()
        {
            if (!IsActive) return;

            if (isEntering)
            {
                double currentY = Canvas.GetTop(enemyRect);
                if (currentY < FINAL_Y_POSITION)
                {
                    Canvas.SetTop(enemyRect, currentY + ENTRY_SPEED);
                    if (healthBar != null)
                    {
                        healthBar.Update(Health);
                    }
                }
                else
                {
                    isEntering = false;
                    Canvas.SetTop(enemyRect, FINAL_Y_POSITION); // Ensure exact positioning
                    if (healthBar != null)
                    {
                        healthBar.Update(Health);
                    }
                }
                UpdateSpinnerRotation();
            }
            else
            {
                UpdateSpinnerRotation();
                Patrol();
                UpdateFireTimer();
            }

            if (healthBar != null)
            {
                healthBar.Update(Health);
            }
        }

        private void Patrol()
        {
            double currentX = Canvas.GetLeft(enemyRect);
            double canvasWidth = ((Canvas)enemyRect.Parent).ActualWidth;

            if (currentX <= PATROL_MARGIN)
            {
                patrolDirection = 1; // To the right
            }
            else if (currentX >= canvasWidth - enemyRect.Width - PATROL_MARGIN)
            {
                patrolDirection = -1; // To the left
            }

            Canvas.SetLeft(enemyRect, currentX + (PATROL_SPEED * patrolDirection));
        }

        public new Rectangle[]? FireRocket()
        {
            if (!IsActive || FireTimer > 0)
            {
                return null;
            }

            var rockets = CreateMinniBossRockets();
            FireTimer = FireCooldown;

            return rockets;
        }

        private Rectangle[] CreateMinniBossRockets()
        {
            const int ROCKET_COUNT = 8;
            const double ANGLE_STEP = 45; // 360 / 8

            double centerX = Canvas.GetLeft(enemyRect) + (enemyRect.Width / 2);
            double centerY = Canvas.GetTop(enemyRect) + (enemyRect.Height / 2);
            double radius = enemyRect.Width / 2;

            Rectangle[] rockets = new Rectangle[ROCKET_COUNT];

            // Creating rockets from 8 evenly spaced points around the perimeter
            for (int i = 0; i < ROCKET_COUNT; i++)
            {
                // Calculating portal position (0°, 45°, 90°, etc. + current rotation)
                double portalAngle = (rotation + (i * ANGLE_STEP)) * Math.PI / 180;
                double portalX = centerX + radius * Math.Cos(portalAngle);
                double portalY = centerY + radius * Math.Sin(portalAngle);

                Rectangle rocket = CreateRocket(portalX, portalY);
                rocket.Height = 51;
                rocket.Tag = GetRocketTag(i); // Setting tag based on angle
                rocket.SetValue(Canvas.ZIndexProperty, 1);
                rocket.SetValue(Enemy.DirectionProperty, new Vector(Math.Cos(portalAngle), Math.Sin(portalAngle)));

                AlignRocketWithDirection(rocket, portalAngle * 180 / Math.PI);
                rockets[i] = rocket;
            }

            return rockets;
        }

        private string GetRocketTag(int index)
        {
            // Setting tag based on angle (CreateMinniBossRockets() method)
            return index switch
            {
                0 => "enemyRocketRight",
                1 => "enemyRocketDownright",
                2 => "enemyRocketDown",
                3 => "enemyRocketDownleft",
                4 => "enemyRocketLeft",
                5 => "enemyRocketUpleft",
                6 => "enemyRocketUp",
                _ => "enemyRocketUpright"
            };
        }

        private void InitializeExplosionSprites()
        {
            explosionSprites ??= new List<ImageBrush>
            {
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion1.png"))),
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion2.png"))),
                new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion3.png")))
            };
        }

        private void InitializeExplosionSound()
        {
            explosionSound ??= new SoundPlayer("assets/sounds/explosionsound.wav");
            explosionSound.LoadAsync();
        }

        private void CreateMiniBossExplosion()
        {
            InitializeExplosionSprites();
            InitializeExplosionSound();
            if (enemyRect == null || gameCanvas == null || explosionSprites == null || explosionSprites.Count == 0)
                return;

            try
            {
                double centerX = Canvas.GetLeft(enemyRect) + (enemyRect.Width / 2);
                double centerY = Canvas.GetTop(enemyRect) + (enemyRect.Height / 2);

                Rectangle explosionRect = new Rectangle
                {
                    Width = enemyRect.Width * 1.8, 
                    Height = enemyRect.Height * 1.8,
                    Tag = "explosion"
                };

                explosionRect.Fill = explosionSprites[0]; 

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
                    Interval = TimeSpan.FromMilliseconds(200)
                };

                int currentFrame = 0;
                explosionTimer.Tick += (sender, e) =>
                {
                    currentFrame++;

                    if (currentFrame < explosionSprites.Count)
                    {
                        explosionRect.Fill = explosionSprites[currentFrame];
                    }
                    else
                    {
                        gameCanvas.Children.Remove(explosionRect);
                        explosionTimer.Stop();
                    }
                };

                explosionTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating explosion: {ex.Message}");
            }
        }

        public void DisableExplosions()
        {
            allowExplosions = false;
        }

        public new void Remove()
        {
            if (allowExplosions)
            {
                CreateMiniBossExplosion();
            }
            
            if (gameCanvas.Children.Contains(enemyRect))
                gameCanvas.Children.Remove(enemyRect);

            if (gameCanvas.Children.Contains(flameRect))
                gameCanvas.Children.Remove(flameRect);

            if (healthBar != null)
            {
                healthBar.Remove();
                healthBar = null;
            }
        }
    }
}
