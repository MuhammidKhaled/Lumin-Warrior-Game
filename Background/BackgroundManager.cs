using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LuminWarrior
{
    public class BackgroundManager
    {
        private Canvas gameCanvas;
        private Random random;
        private readonly int maxStars = 50;
        private double starSpeed = 0.5;

        public BackgroundManager(Canvas canvas)
        {
            gameCanvas = canvas;
            random = new Random();
        }

        public void Update()
        {
            CreateStars();
            MoveStars();
        }

        private void CreateStars()
        {
            int currentStarCount = gameCanvas.Children.OfType<Ellipse>()
                                           .Count(r => (string)r.Tag == "star");

            if (currentStarCount >= maxStars) return;

            int starsToGenerate = Math.Min(5, maxStars - currentStarCount);

            for (int i = 0; i < starsToGenerate; i++)
            {
                Ellipse star = new Ellipse
                {
                    Width = random.Next(1, 4),
                    Height = random.Next(1, 4),
                    Fill = Brushes.White,
                    Opacity = random.NextDouble() * 0.7 + 0.3,
                    Tag = "star"
                };

                double x = random.Next(0, (int)gameCanvas.ActualWidth);
                double y = random.Next(0, (int)gameCanvas.ActualHeight);

                Canvas.SetLeft(star, x);
                Canvas.SetTop(star, y);

                Canvas.SetZIndex(star, -100);

                gameCanvas.Children.Add(star);
            }
        }

        private void MoveStars()
        {
            foreach (var star in gameCanvas.Children.OfType<Ellipse>()
                                         .Where(s => (string)s.Tag == "star")
                                         .ToList())
            {
                double top = Canvas.GetTop(star);
                Canvas.SetTop(star, top + starSpeed);

                if (top > gameCanvas.ActualHeight)
                {
                    Canvas.SetLeft(star, random.Next(0, (int)gameCanvas.ActualWidth));
                    Canvas.SetTop(star, -5);
                }
            }
        }

        public void SetStarSpeed(double speed)
        {
            starSpeed = speed;
        }

        public void ClearStars()
        {
            var stars = gameCanvas.Children.OfType<Ellipse>()
                                         .Where(s => (string)s.Tag == "star")
                                         .ToList();

            foreach (var star in stars)
            {
                gameCanvas.Children.Remove(star);
            }
        }
    }
}