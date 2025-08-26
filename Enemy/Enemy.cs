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
    public class Enemy
    {
        public enum EnemyType
        {
            Standard,
            Spinner
        }

        protected Rectangle enemyRect = null!;
        protected Rectangle? flameRect;
        protected Canvas gameCanvas;
        protected Random random;
        protected EnemyType type;
        protected double rotation = 0;
        protected ImageBrush[] explosionFrames = null!;
        private bool allowExplosions = true;

        public int Speed { get; set; } = 2;

        public double FlameOffsetY { get; set; } = 0;
        private bool movingFlameUp = true;

        public int FireCooldown { get; set; } = 100;
        public int FireTimer { get; set; } = 0;

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.RegisterAttached(
                "Direction",
                typeof(Vector),
                typeof(Enemy),
                new PropertyMetadata(new Vector(0, 1)));

        public bool IsActive => gameCanvas.Children.Contains(enemyRect);
        public Rect HitBox => new(Canvas.GetLeft(enemyRect), Canvas.GetTop(enemyRect),
                                 enemyRect.Width, enemyRect.Height);

        public Enemy(Canvas canvas, Random rand, EnemyType enemyType = EnemyType.Standard)
        {
            gameCanvas = canvas;
            random = rand;
            type = enemyType;
            CreateEnemy();
            if (type == EnemyType.Standard)
            {
                CreateFlame();
            }
        }

        public EnemyType GetEnemyType()
        {
            return type;
        }

        private void CreateEnemy()
        {
            string imagePath = type == EnemyType.Standard
                ? "pack://application:,,,/assets/images/redenemy.png"
                : "pack://application:,,,/assets/images/circularenemy.png";

            enemyRect = new Rectangle
            {
                Tag = "enemy",
                Width = type == EnemyType.Standard ? 80 : 60,
                Height = type == EnemyType.Standard ? 100 : 60,
                Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(imagePath)) },
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(0)
            };

            Canvas.SetTop(enemyRect, -enemyRect.Height);
            Canvas.SetLeft(enemyRect, random.Next(20, (int)gameCanvas.ActualWidth - 100));
            gameCanvas.Children.Add(enemyRect);
        }

        private void CreateFlame()
        {
            flameRect = new Rectangle
            {
                Tag = "enemyFlame",
                Width = 120,
                Height = 60,
                Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/redflame.png")) }
            };

            UpdateFlamePosition();
            gameCanvas.Children.Insert(0, flameRect);
        }

        public void Update()
        {
            if (!IsActive) return;

            MoveEnemy();

            if (type == EnemyType.Standard)
            {
                UpdateFlamePosition();
                UpdateFlameAnimation();
            }
            else if (type == EnemyType.Spinner)
            {
                UpdateSpinnerRotation();
            }

            UpdateFireTimer();
        }

        private void MoveEnemy()
        {
            Canvas.SetTop(enemyRect, Canvas.GetTop(enemyRect) + Speed);

            if (Canvas.GetTop(enemyRect) > gameCanvas.ActualHeight)
            {
                Remove();
            }
        }

        private void UpdateFlamePosition()
        {
            if (!IsActive || flameRect == null) return;

            double flameX = Canvas.GetLeft(enemyRect) + (enemyRect.Width - 100);
            double flameY = Canvas.GetTop(enemyRect) + (enemyRect.Height - 120) + FlameOffsetY;

            Canvas.SetLeft(flameRect, flameX);
            Canvas.SetTop(flameRect, flameY);
        }

        private void UpdateFlameAnimation()
        {
            if (movingFlameUp)
            {
                FlameOffsetY -= 2;
                if (FlameOffsetY <= -3) movingFlameUp = false;
            }
            else
            {
                FlameOffsetY += 2;
                if (FlameOffsetY >= 3) movingFlameUp = true;
            }
        }

        private void UpdateSpinnerRotation()
        {
            rotation += 10;
            if (rotation >= 360) rotation = 0;

            var transform = (RotateTransform)enemyRect.RenderTransform;
            transform.Angle = rotation;
        }

        protected void UpdateFireTimer()
        {
            if (FireTimer > 0)
                FireTimer--;
        }

        public Rectangle[]? FireRocket()
        {
            if (!IsActive || FireTimer > 0 ||
                Canvas.GetTop(enemyRect) <= 0 ||
                Canvas.GetTop(enemyRect) >= gameCanvas.ActualHeight)
            {
                return null;
            }

            if (type == EnemyType.Standard)
            {
                var rocket = CreateStandardRocket();
                FireTimer = FireCooldown;
                return new[] { rocket };
            }
            else
            {
                var rockets = CreateSpinnerRockets();
                FireTimer = FireCooldown;
                return rockets;
            }
        }

        private Rectangle CreateStandardRocket()
        {
            double rocketX = Canvas.GetLeft(enemyRect) + (enemyRect.Width / 2) - 2;
            double rocketY = Canvas.GetTop(enemyRect) + enemyRect.Height;

            return CreateRocket(rocketX, rocketY);
        }

        private Rectangle[] CreateSpinnerRockets()
        {
            double centerX = Canvas.GetLeft(enemyRect) + (enemyRect.Width / 2);
            double centerY = Canvas.GetTop(enemyRect) + (enemyRect.Height / 2);
            double radius = enemyRect.Width / 2; 

            List<Rectangle> rockets = new List<Rectangle>();

            // Calculating the current position of each portal based on rotation
            // And adding a direction property to each rocket

            // Right portal (0° + rotation)
            double rightPortalAngle = rotation * Math.PI / 180;
            double rightPortalX = centerX + radius * Math.Cos(rightPortalAngle);
            double rightPortalY = centerY + radius * Math.Sin(rightPortalAngle);
            Rectangle rightRocket = CreateRocket(rightPortalX, rightPortalY);
            rightRocket.Tag = "enemyRocketRight";
            rightRocket.SetValue(Canvas.ZIndexProperty, 1);
            rightRocket.SetValue(DirectionProperty, new Vector(Math.Cos(rightPortalAngle), Math.Sin(rightPortalAngle)));
            AlignRocketWithDirection(rightRocket, rightPortalAngle * 180 / Math.PI);
            rockets.Add(rightRocket);

            // Down portal (90° + rotation)
            double downPortalAngle = (rotation + 90) * Math.PI / 180;
            double downPortalX = centerX + radius * Math.Cos(downPortalAngle);
            double downPortalY = centerY + radius * Math.Sin(downPortalAngle);
            Rectangle downRocket = CreateRocket(downPortalX, downPortalY);
            downRocket.Tag = "enemyRocketDown";
            downRocket.SetValue(Canvas.ZIndexProperty, 1);
            downRocket.SetValue(DirectionProperty, new Vector(Math.Cos(downPortalAngle), Math.Sin(downPortalAngle)));
            AlignRocketWithDirection(downRocket, downPortalAngle * 180 / Math.PI);
            rockets.Add(downRocket);

            // Left portal (180° + rotation)
            double leftPortalAngle = (rotation + 180) * Math.PI / 180;
            double leftPortalX = centerX + radius * Math.Cos(leftPortalAngle);
            double leftPortalY = centerY + radius * Math.Sin(leftPortalAngle);
            Rectangle leftRocket = CreateRocket(leftPortalX, leftPortalY);
            leftRocket.Tag = "enemyRocketLeft";
            leftRocket.SetValue(Canvas.ZIndexProperty, 1);
            leftRocket.SetValue(DirectionProperty, new Vector(Math.Cos(leftPortalAngle), Math.Sin(leftPortalAngle)));
            AlignRocketWithDirection(leftRocket, leftPortalAngle * 180 / Math.PI);
            rockets.Add(leftRocket);

            // Up portal (270° + rotation)
            double upPortalAngle = (rotation + 270) * Math.PI / 180;
            double upPortalX = centerX + radius * Math.Cos(upPortalAngle);
            double upPortalY = centerY + radius * Math.Sin(upPortalAngle);
            Rectangle upRocket = CreateRocket(upPortalX, upPortalY);
            upRocket.Tag = "enemyRocketUp";
            upRocket.SetValue(Canvas.ZIndexProperty, 1);
            upRocket.SetValue(DirectionProperty, new Vector(Math.Cos(upPortalAngle), Math.Sin(upPortalAngle)));
            AlignRocketWithDirection(upRocket, upPortalAngle * 180 / Math.PI);
            rockets.Add(upRocket);

            return rockets.ToArray();
        }

        protected void AlignRocketWithDirection(Rectangle rocket, double angleDegrees)
        {
            // Adjusting the rotation angle based on the original orientation of the rocket 
            // if the rocket points upward by default, add 90 degrees 
            // if the rocket points rightward by default, no adjustment needed
            // if it points downward by default (most common), add 270 degrees (or subtract 90)

            // Assuming rocket naturally points upward:
            double adjustedAngle = angleDegrees + 90;

            rocket.RenderTransform = new RotateTransform(adjustedAngle);
        }

        protected Rectangle CreateRocket(double x, double y, string direction = "down")
        {
            DropShadowEffect enemyRocketGlow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 27,
                ShadowDepth = 0,
                Opacity = 1
            };

            Rectangle rocket = new Rectangle
            {
                Width = 3.3,
                Height = (direction == "bossDown") ? 60 : 39,
                RadiusX = 15,
                RadiusY = 15,
                Fill = Brushes.White,
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                Effect = enemyRocketGlow,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Tag = "enemyRocket"
            };

            if (direction == "left" || direction == "right")
            {
                RotateTransform rotation = new RotateTransform(90);
                rocket.RenderTransform = rotation;
            }

            Canvas.SetLeft(rocket, x);
            Canvas.SetTop(rocket, y);

            return rocket;
        }

        public static void MoveRocket(Rectangle rocket, double speed = 8)
        {
            Vector direction = (Vector)rocket.GetValue(DirectionProperty);
            string? tag = rocket.Tag as string;

            if (direction.X != 0 || direction.Y != 0)
            {
                Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) + direction.X * speed);
                Canvas.SetTop(rocket, Canvas.GetTop(rocket) + direction.Y * speed);
            }
            else if (tag != null)
            {
                // Handling diagonal movement
                if (tag.Contains("Upright"))
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) + speed * 0.7071); // cos(45°)
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) - speed * 0.7071);   // -sin(45°)
                }
                else if (tag.Contains("Downright"))
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) + speed * 0.7071);
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) + speed * 0.7071);
                }
                else if (tag.Contains("Downleft"))
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) - speed * 0.7071);
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) + speed * 0.7071);
                }
                else if (tag.Contains("Upleft"))
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) - speed * 0.7071);
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) - speed * 0.7071);
                }
                // Handling cardinal directions
                else if (tag == "enemyRocketRight")
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) + speed);
                }
                else if (tag == "enemyRocketLeft")
                {
                    Canvas.SetLeft(rocket, Canvas.GetLeft(rocket) - speed);
                }
                else if (tag == "enemyRocketUp")
                {
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) - speed);
                }
                else if (tag == "enemyRocketDown")
                {
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) + speed);
                }
                else
                {
                    Canvas.SetTop(rocket, Canvas.GetTop(rocket) + speed);
                }
            }
            else
            {
                Canvas.SetTop(rocket, Canvas.GetTop(rocket) + speed);
            }
        }

        public static bool IsRocketOffscreen(Rectangle rocket, double canvasWidth, double canvasHeight)
        {
            string? tag = rocket.Tag as string;
            double x = Canvas.GetLeft(rocket);
            double y = Canvas.GetTop(rocket);

            if (tag != null)
            {
                if (tag.Contains("Right"))
                    return x > canvasWidth;
                else if (tag.Contains("Left"))
                    return x < -rocket.Width;
                else if (tag.Contains("Up") && !tag.Contains("Down"))
                    return y < -rocket.Height;
                else if (tag.Contains("Down"))
                    return y > canvasHeight;
                else
                    return y > canvasHeight;
            }
            else
            {
                return y > canvasHeight;
            }
        }

        private void InitializeExplosionFrames()
        {
            explosionFrames = new ImageBrush[]
            {
                new ImageBrush { ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion1.png")) },
                new ImageBrush { ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion2.png")) },
                new ImageBrush { ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/explosion3.png")) }
            };
        }

        protected void CreateExplosion()
        {
            if (explosionFrames == null) InitializeExplosionFrames();
            if (enemyRect == null) return;

            var explosionRect = new Rectangle
            {
                Width = enemyRect.Width,
                Height = enemyRect.Height,
                Tag = "explosion"
            };

            int currentFrame = 0;
            var animationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.1) };

            animationTimer.Tick += (sender, e) =>
            {
                explosionRect.Fill = explosionFrames![currentFrame];
                if (++currentFrame >= explosionFrames.Length)
                {
                    animationTimer.Stop();
                    gameCanvas.Children.Remove(explosionRect);
                }
            };

            Canvas.SetLeft(explosionRect, Canvas.GetLeft(enemyRect));
            Canvas.SetTop(explosionRect, Canvas.GetTop(enemyRect));
            gameCanvas.Children.Add(explosionRect);
            animationTimer.Start();
        }

        private void PlayDestructionSound()
        {
            SoundEffectsManager.PlayDestructionSound();
        }

        public void DisableExplosion()
        {
            allowExplosions = false;
        }

        public virtual void Remove()
        {
            if (allowExplosions)
            {
                CreateExplosion();
                PlayDestructionSound();
            }

            if (flameRect != null && gameCanvas.Children.Contains(flameRect))
                gameCanvas.Children.Remove(flameRect);

            if (gameCanvas.Children.Contains(enemyRect))
                gameCanvas.Children.Remove(enemyRect);
        }
    }
}