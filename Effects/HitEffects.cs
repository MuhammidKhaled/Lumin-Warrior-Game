using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LuminWarrior
{
    public class HitEffects
    {
        private Canvas gameCanvas;
        private readonly Random random = new Random();

        public HitEffects(Canvas canvas)
        {
            gameCanvas = canvas;
        }

        public void CreateSparkEffect(double x, double y, Color color, int dotsCount = 4)
        {
            for (int i = 0; i < dotsCount; i++)
            {
                Rectangle spark = new Rectangle
                {
                    Width = random.Next(2, 4),
                    Height = random.Next(2, 4),
                    Fill = new SolidColorBrush(color),
                    Tag = "sparkEffect"
                };

                Canvas.SetLeft(spark, x - (spark.Width / 2));
                Canvas.SetTop(spark, y - (spark.Height / 2));

                gameCanvas.Children.Add(spark);

                AnimateSpark(spark);
            }
        }

        private void AnimateSpark(Rectangle spark)
        {
            double angle = random.NextDouble() * 2 * Math.PI;
            double speed = random.Next(1, 4);
            double vx = Math.Cos(angle) * speed;
            double vy = Math.Sin(angle) * speed;
            double opacity = 1.0;
            double currentLeft = Canvas.GetLeft(spark);
            double currentTop = Canvas.GetTop(spark);
            SolidColorBrush brush = (SolidColorBrush)spark.Fill;

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };

            timer.Tick += (sender, e) =>
            {
                opacity -= 0.1;
                if (opacity <= 0)
                {
                    gameCanvas.Children.Remove(spark);
                    timer.Stop();
                    return;
                }

                currentLeft += vx;
                currentTop += vy;
                Canvas.SetLeft(spark, currentLeft);
                Canvas.SetTop(spark, currentTop);

                brush.Opacity = opacity;

                spark.Width = Math.Max(1, spark.Width - 0.3);
                spark.Height = Math.Max(1, spark.Height - 0.3);
            };

            timer.Start();
        }
    }
}
