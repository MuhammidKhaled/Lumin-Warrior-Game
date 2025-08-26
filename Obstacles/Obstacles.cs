using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LuminWarrior
{
    public class Obstacles
    {
        private Canvas gameCanvas;
        private Random random;
        private List<Rectangle> rocks;
        private readonly double rockSpeed = 2;
        private readonly int maxRocks = 3;
        private bool isEnabled = false;

        public Obstacles(Canvas canvas)
        {
            gameCanvas = canvas;
            random = new Random();
            rocks = new List<Rectangle>();
        }

        public void Update()
        {
            if (isEnabled)
            {
                CreateRocks();
                MoveRocks();
            }
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                RemoveAllRocks();
            }
        }

        private void CreateRocks()
        {
            int currentRockCount = rocks.Count;
            if (currentRockCount >= maxRocks) return;

            int rocksToGenerate = Math.Min(2, maxRocks - currentRockCount);
            for (int i = 0; i < rocksToGenerate; i++)
            {
                Rectangle rock = new Rectangle
                {
                    Width = random.Next(50, 80),
                    Height = random.Next(50, 80),
                    Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/assets/images/rock.png"))),
                    Tag = "rock"
                };

                Canvas.SetLeft(rock, random.Next(0, (int)gameCanvas.ActualWidth - 50));
                Canvas.SetTop(rock, -random.Next(50, 200));

                gameCanvas.Children.Add(rock);
                rocks.Add(rock);
            }
        }

        private void MoveRocks()
        {
            foreach (var rock in rocks.ToList())
            {
                double top = Canvas.GetTop(rock);
                Canvas.SetTop(rock, top + rockSpeed);

                if (top > gameCanvas.ActualHeight)
                {
                    gameCanvas.Children.Remove(rock);
                    rocks.Remove(rock);
                }
            }
        }

        public void RemoveAllRocks()
        {
            foreach (var rock in rocks.ToList())
            {
                gameCanvas.Children.Remove(rock);
            }
            rocks.Clear();
        }

        public List<Rectangle> GetRocks()
        {
            return rocks;
        }
    }
}

