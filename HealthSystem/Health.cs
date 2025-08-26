using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LuminWarrior
{
    public class Health
    {
        private Canvas gameCanvas;
        private Rectangle? healthBar;
        private Rectangle? healthBarBackground;

        public double MaxHealth { get; private set; }
        public double CurrentHealth { get; private set; }
        public double HealthBarWidth { get; private set; }

        public delegate void HealthDepletedHandler();
        public event HealthDepletedHandler? OnHealthDepleted;

        public Health(Canvas canvas, double maxHealth = 25, double healthBarWidth = 400)
        {
            gameCanvas = canvas;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            HealthBarWidth = healthBarWidth;

            gameCanvas.Loaded += (s, e) =>
            {
                InitializeHealthBar();
            };
        }

        public void InitializeHealthBar()
        {
            healthBarBackground = new Rectangle
            {
                Width = HealthBarWidth,
                Height = 20,
                Fill = Brushes.LightGray,
                RadiusX = 9,
                RadiusY = 9,
                Tag = "HealthBar"
            };
            healthBar = new Rectangle
            {
                Width = HealthBarWidth,
                Height = 20,
                Fill = Brushes.Lime,
                RadiusX = 9,
                RadiusY = 9,
                Tag = "HealthBar"
            };
            double barPosition = (gameCanvas.ActualWidth / 2) - (HealthBarWidth / 2);

            Canvas.SetTop(healthBarBackground, 45);
            Canvas.SetLeft(healthBarBackground, barPosition);
            Canvas.SetTop(healthBar, 45);
            Canvas.SetLeft(healthBar, barPosition);

            Canvas.SetZIndex(healthBarBackground, 60);
            Canvas.SetZIndex(healthBar, 61);

            gameCanvas.Children.Add(healthBarBackground);
            gameCanvas.Children.Add(healthBar);
        }

        public void TakeDamage(double amount)
        {
            if (amount <= 0) return;

            CurrentHealth -= amount;

            if (CurrentHealth < 0)
                CurrentHealth = 0;

            UpdateHealthBar();

            if (CurrentHealth <= 0)
                OnHealthDepleted?.Invoke();
        }

        public void Heal(double amount)
        {
            if (amount <= 0) return;

            CurrentHealth += amount;

            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;

            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.Width = (CurrentHealth / MaxHealth) * HealthBarWidth;
                UpdateHealthBarColor();
            }
        }

        private void UpdateHealthBarColor()
        {
            if (healthBar != null)
            {
                double healthPercentage = CurrentHealth / MaxHealth;

                healthBar.Fill = healthPercentage <= 0.2 ? Brushes.Red :
                                healthPercentage <= 0.5 ? Brushes.OrangeRed :
                                Brushes.Lime;
            }
        }

        public void Reset()
        {
            CurrentHealth = MaxHealth;
            UpdateHealthBar();
        }

        public void Remove()
        {
            if (healthBarBackground != null) gameCanvas.Children.Remove(healthBarBackground);
            if (healthBar != null) gameCanvas.Children.Remove(healthBar);
        }
    }
}