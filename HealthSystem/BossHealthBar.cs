using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LuminWarrior
{
    public class BossHealthBar
    {
        private Rectangle backgroundBar = null!;
        private Rectangle healthBar = null!;
        private Canvas gameCanvas;
        private UIElement bossElement;
        private int maxHealth;
        private double offsetY = -24;
        private double width = 200;
        private double height = 15;

        public BossHealthBar(Canvas canvas, UIElement boss, int initialHealth, double barWidth = 200)
        {
            gameCanvas = canvas;
            bossElement = boss;
            maxHealth = initialHealth;
            width = barWidth;

            CreateHealthBar();
            UpdatePosition();
        }

        private void CreateHealthBar()
        {
            backgroundBar = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
                RadiusX = 5,
                RadiusY = 5
            };

            healthBar = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Red,
                RadiusX = 5,
                RadiusY = 5
            };

            gameCanvas.Children.Add(backgroundBar);
            gameCanvas.Children.Add(healthBar);
        }

        public void Update(int currentHealth)
        {
            double healthPercentage = (double)currentHealth / maxHealth;

            healthBar.Width = width * healthPercentage;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (bossElement == null || !gameCanvas.Children.Contains(bossElement))
                return;

            double bossX = Canvas.GetLeft(bossElement);
            double bossY = Canvas.GetTop(bossElement);
            double bossWidth = ((FrameworkElement)bossElement).Width;

            double barX = bossX + (bossWidth / 2) - (width / 2);
            double barY = bossY + offsetY;

            Canvas.SetLeft(backgroundBar, barX);
            Canvas.SetTop(backgroundBar, barY);

            Canvas.SetLeft(healthBar, barX);
            Canvas.SetTop(healthBar, barY);
        }

        public void Remove()
        {
            if (gameCanvas.Children.Contains(backgroundBar))
                gameCanvas.Children.Remove(backgroundBar);

            if (gameCanvas.Children.Contains(healthBar))
                gameCanvas.Children.Remove(healthBar);
        }
    }
}
