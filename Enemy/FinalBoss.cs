using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;

namespace LuminWarrior
{
    public class FinalBoss : Enemy
    {
        private double patrolDirection = 1; // 1 for right, -1 for left
        private const double PATROL_SPEED = 2; 
        private const double PATROL_Y_POSITION = 100; 
        private const double PATROL_MARGIN = 100;

        private bool isEntering = true;
        private const double ENTRY_SPEED = 2;
        private const double FINAL_Y_POSITION = 100;

        private Rectangle wingLeftHitBox = new Rectangle();
        private Rectangle wingRightHitBox = new Rectangle();
        private Rectangle centerHitBox = new Rectangle();

        public int Health { get; private set; } = 400;
        private BossHealthBar? healthBar;
        private HitEffects? hitEffects;

        private List<ImageBrush> explosionSprites = null!;
        private SoundPlayer centerExplosionSound = new SoundPlayer();
        private SoundPlayer leftWingExplosionSound = new SoundPlayer();
        private SoundPlayer rightWingExplosionSound = new SoundPlayer();
        private bool allowExplosions = true;

        private int headFireTimer = 0;
        private int wingFireTimer = 0;
        private const int HEAD_FIRE_COOLDOWN = 90;
        private const int WING_FIRE_COOLDOWN = 120;
        

        public FinalBoss(Canvas canvas, Random rand): base(canvas, rand, EnemyType.Standard)
        {
            Speed = 0;
            FireCooldown = 60;

            CreateBoss(canvas);

            Canvas.SetTop(enemyRect, PATROL_Y_POSITION);
            Canvas.SetLeft(enemyRect, canvas.ActualWidth / 2 - enemyRect.Width / 2);

            SetInitialPosition();

            healthBar = new BossHealthBar(canvas, enemyRect, Health, enemyRect.Width);
            hitEffects = new HitEffects(canvas);
        }

        private void CreateBoss(Canvas canvas)
        {
            CleanupBaseElements();

            ImageBrush bossImage = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/boss.png"))
            };

            enemyRect = new Rectangle
            {
                Tag = "finalBoss",
                Width = 400,
                Height = 300,
                Fill = bossImage,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            wingLeftHitBox = new Rectangle
            {
                Width = enemyRect.Width / 3,
                Height = enemyRect.Height / 5,
                Fill = Brushes.Transparent,
                Tag = "bossHitBox"
            };

            wingRightHitBox = new Rectangle
            {
                Width = enemyRect.Width / 3,
                Height = enemyRect.Height / 5,
                Fill = Brushes.Transparent,
                Tag = "bossHitBox"
            };

            centerHitBox = new Rectangle
            {
                Width = enemyRect.Width / 3,
                Height = enemyRect.Height,
                Fill = Brushes.Transparent,
                Tag = "bossHitBox"
            };

            Canvas.SetTop(enemyRect, PATROL_Y_POSITION);
            Canvas.SetLeft(enemyRect, canvas.ActualWidth / 2 - enemyRect.Width / 2);

            gameCanvas.Children.Add(enemyRect);
            canvas.Children.Add(wingLeftHitBox);
            canvas.Children.Add(wingRightHitBox);
            canvas.Children.Add(centerHitBox);

            UpdateHitBoxPositions();
        }

        private void UpdateHitBoxPositions()
        {
            double bossX = Canvas.GetLeft(enemyRect);
            double bossY = Canvas.GetTop(enemyRect);

            Canvas.SetLeft(wingLeftHitBox, bossX);
            Canvas.SetTop(wingLeftHitBox, bossY + ((enemyRect.Height / 2) - (wingLeftHitBox.Height / 2)));

            Canvas.SetLeft(wingRightHitBox, bossX + (enemyRect.Width * 0.66));
            Canvas.SetTop(wingRightHitBox, bossY + ((enemyRect.Height / 2) - (wingRightHitBox.Height / 2)));

            Canvas.SetLeft(centerHitBox, bossX + (enemyRect.Width * 0.33));
            Canvas.SetTop(centerHitBox, bossY * 0.8);
        }

        private void SetInitialPosition()
        {
            Canvas.SetTop(enemyRect, -enemyRect.Height);
            Canvas.SetLeft(enemyRect, gameCanvas.ActualWidth / 2 - enemyRect.Width / 2);
            isEntering = true;
        }

        public bool TakeDamage(int amount, Point? hitPosition = null)
        {
            Health -= amount;

            if (healthBar != null)
            {
                healthBar.Update(Health);
            }

            if (hitPosition.HasValue)
            {
                hitEffects?.CreateSparkEffect(hitPosition.Value.X, hitPosition.Value.Y, Colors.Aqua);
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
                    UpdateHitBoxPositions();
                    healthBar?.Update(Health);
                }
                else
                {
                    isEntering = false;
                    Canvas.SetTop(enemyRect, FINAL_Y_POSITION);
                    UpdateHitBoxPositions();
                    healthBar?.Update(Health);
                }
                return;
            }

            Patrol();
            UpdateHitBoxPositions();

            if (headFireTimer > 0) headFireTimer--;
            if (wingFireTimer > 0) wingFireTimer--;

            UpdateFireTimer();
            healthBar?.Update(Health);
        }

        private void Patrol()
        {
            double currentX = Canvas.GetLeft(enemyRect);
            double canvasWidth = gameCanvas.ActualWidth;

            if (currentX <= PATROL_MARGIN)
            {
                patrolDirection = 1; // To right
            }
            else if (currentX >= canvasWidth - enemyRect.Width - PATROL_MARGIN)
            {
                patrolDirection = -1; // To left
            }

            Canvas.SetLeft(enemyRect, currentX + (PATROL_SPEED * patrolDirection));
        }

        public new Rectangle[] FireRocket()
        {
            List<Rectangle> allRockets = new List<Rectangle>();

            if (headFireTimer <= 0)
            {
                Rectangle? headRocket = FireHeadRocket();
                if (headRocket != null)
                {
                    allRockets.Add(headRocket);
                    headFireTimer = HEAD_FIRE_COOLDOWN;
                }
            }

            if (wingFireTimer <= 0)
            {
                Rectangle[]? wingRockets = FireWingRockets();
                if (wingRockets != null)
                {
                    allRockets.AddRange(wingRockets);
                    wingFireTimer = WING_FIRE_COOLDOWN;
                }
            }

            if (allRockets.Count > 0)
                return allRockets.ToArray();

            return allRockets.ToArray();
        }

        private Rectangle? FireHeadRocket()
        {
            if (!IsActive) return null;

            double centerX = Canvas.GetLeft(enemyRect) + (enemyRect.Width / 2);
            double bottomY = Canvas.GetTop(enemyRect) + enemyRect.Height;

            Rectangle headRocket = CreateRocket(centerX - 3, bottomY, "down");
            headRocket.Width = 5; 
            headRocket.Height = 60;

            return headRocket;
        }

        private Rectangle[]? FireWingRockets()
        {
            if (!IsActive) return null;

            double leftWingX = Canvas.GetLeft(enemyRect) + 30;
            double rightWingX = Canvas.GetLeft(enemyRect) + enemyRect.Width - 30;
            double wingY = Canvas.GetTop(enemyRect) + enemyRect.Height - (enemyRect.Height * 0.3);

            return new Rectangle[]
            {
                CreateDirectionalRocket(leftWingX + 50, wingY + 10, "bossDown"),
                CreateDirectionalRocket(leftWingX + 100, wingY + 10, "bossDown"),
                CreateDirectionalRocket(rightWingX - 100, wingY, "bossDown"),
                CreateDirectionalRocket(rightWingX - 50, wingY, "bossDown")
            };
        }

        private Rectangle CreateDirectionalRocket(double x, double y, string direction)
        {
            Rectangle rocket = CreateRocket(x, y, direction);
            rocket.Tag = "enemyRocket";

            return rocket;
        }

        public bool CheckCollision(Rect objectHitBox)
        {
            bool isComingFromAbove = objectHitBox.Top < Canvas.GetTop(enemyRect);

            if (isComingFromAbove)
            {
                // For rocks above the boss, only count collisions with the actual wing areas
                Rect leftWingArea = GetHitBox(wingLeftHitBox);
                Rect rightWingArea = GetHitBox(wingRightHitBox);

                leftWingArea.Height *= 0.5;  // Only top half of wing hitbox
                rightWingArea.Height *= 0.5; 

                return leftWingArea.IntersectsWith(objectHitBox) ||
                       rightWingArea.IntersectsWith(objectHitBox) ||
                       GetHitBox(centerHitBox).IntersectsWith(objectHitBox);
            }
            else
            {
                // For other collisions (like player rockets)
                return GetHitBox(wingLeftHitBox).IntersectsWith(objectHitBox) ||
                       GetHitBox(wingRightHitBox).IntersectsWith(objectHitBox) ||
                       GetHitBox(centerHitBox).IntersectsWith(objectHitBox);
            }
        }

        private Rect GetHitBox(Rectangle rect)
        {
            return new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect),rect.Width, rect.Height);
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

        private void CreateHitboxExplosions()
        {
            InitializeExplosionSprites();
            InitializeExplosionSounds();

            if (wingLeftHitBox != null)
            {
                CreateHitboxExplosion(wingLeftHitBox, leftWingExplosionSound);
            }

            DispatcherTimer centerDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            centerDelayTimer.Tick += (sender, e) =>
            {
                centerDelayTimer.Stop();
                if (centerHitBox != null)
                {
                    CreateHitboxExplosion(centerHitBox, centerExplosionSound);
                }

                DispatcherTimer rightWingDelayTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(250) 
                };

                rightWingDelayTimer.Tick += (s, ev) =>
                {
                    rightWingDelayTimer.Stop();
                    if (wingRightHitBox != null)
                    {
                        CreateHitboxExplosion(wingRightHitBox, rightWingExplosionSound);
                    }
                };

                rightWingDelayTimer.Start();
            };

            centerDelayTimer.Start();
        }

        private void CreateHitboxExplosion(Rectangle hitbox, SoundPlayer explosionSound)
        {
            if (hitbox == null || gameCanvas == null) return;

            double centerX = Canvas.GetLeft(hitbox) + (hitbox.Width / 2);
            double centerY = Canvas.GetTop(hitbox) + (hitbox.Height / 2);

            Rectangle explosionRect = new Rectangle
            {
                Width = hitbox.Width * 3,
                Height = hitbox.Width * 3,
                Tag = "explosion",
                Fill = explosionSprites?[random.Next(explosionSprites.Count)]
            };

            Canvas.SetLeft(explosionRect, centerX - (explosionRect.Width / 2));
            Canvas.SetTop(explosionRect, centerY - (explosionRect.Height / 2));
            gameCanvas.Children.Add(explosionRect);

            try
            {
                explosionSound?.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing explosion sound: {ex.Message}");
            }

            DispatcherTimer explosionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };

            int currentSprite = 0;
            explosionTimer.Tick += (sender, e) =>
            {
                currentSprite++;
                if (explosionSprites != null && currentSprite < explosionSprites.Count)
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

        private void InitializeExplosionSounds()
        {
            centerExplosionSound = new SoundPlayer("assets/sounds/explosionsound.wav");
            leftWingExplosionSound = new SoundPlayer("assets/sounds/explosionsound.wav");
            rightWingExplosionSound = new SoundPlayer("assets/sounds/explosionsound.wav");

            centerExplosionSound.LoadAsync();
            leftWingExplosionSound.LoadAsync();
            rightWingExplosionSound.LoadAsync();
        }

        public void DisableExplosions()
        {
            allowExplosions = false;
        }

        public void CleanupBaseElements()
        {
            //Without explosions sounds
            if (flameRect != null && gameCanvas.Children.Contains(flameRect))
                gameCanvas.Children.Remove(flameRect);

            if (enemyRect != null && gameCanvas.Children.Contains(enemyRect))
                gameCanvas.Children.Remove(enemyRect);

            if (centerHitBox != null && gameCanvas.Children.Contains(centerHitBox))
                gameCanvas.Children.Remove(centerHitBox);
            if (wingLeftHitBox != null && gameCanvas.Children.Contains(wingLeftHitBox))
                gameCanvas.Children.Remove(wingLeftHitBox);
            if (wingRightHitBox != null && gameCanvas.Children.Contains(wingRightHitBox))
                gameCanvas.Children.Remove(wingRightHitBox);

            healthBar?.Remove();
            healthBar = null;
        }

        public new void Remove()
        {
            if (allowExplosions)
            {
                CreateHitboxExplosions();
            }

            if (gameCanvas.Children.Contains(flameRect))
                gameCanvas.Children.Remove(flameRect);

            if (gameCanvas.Children.Contains(enemyRect))
                gameCanvas.Children.Remove(enemyRect);

            if (centerHitBox != null && gameCanvas.Children.Contains(centerHitBox))
                gameCanvas.Children.Remove(centerHitBox);
            if (wingLeftHitBox != null && gameCanvas.Children.Contains(wingLeftHitBox))
                gameCanvas.Children.Remove(wingLeftHitBox);
            if (wingRightHitBox != null && gameCanvas.Children.Contains(wingRightHitBox))
                gameCanvas.Children.Remove(wingRightHitBox);

            healthBar?.Remove();
            healthBar = null;
        }
    }
}